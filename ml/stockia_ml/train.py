"""Trains, evaluates and exports the StockIA demand model.

Usage (from the ml/ folder):
    python -m stockia_ml.train                         # synthetic data, weather "auto"
    python -m stockia_ml.train --weather open-meteo    # fail if Open-Meteo is not reachable
    python -m stockia_ml.train --data data/processed/stockia_sales.csv --with-synthetic

Outputs in models/:
    demand_model.onnx     model loaded by the API (DemandForecasting bounded context)
    model_metadata.json   feature order, metrics, data sources, confidence
    parity_fixture.json   cases the C# tests use to check features and predictions
    metrics.json          evaluation against the baselines
"""

from __future__ import annotations

import argparse
import json
from datetime import date, datetime, timedelta, timezone
from pathlib import Path

import numpy as np
import onnx
import onnxruntime as ort
from onnx import helper
import pandas as pd
import sklearn
from skl2onnx import to_onnx
from sklearn.ensemble import HistGradientBoostingRegressor

from .config import MODELS_DIR, PROCESSED_DIR, RANDOM_SEED
from .data import synthetic
from .features import FEATURE_ORDER, HISTORY_DAYS_REQUIRED, build_training_frame, features_for_day

MODEL_VERSION = "1.0.0"
TEST_DAYS = 56
TARGET_OPSET = {"": 17, "ai.onnx.ml": 3}


def make_model() -> HistGradientBoostingRegressor:
    # Poisson loss: the target is a count of units and the error grows with the level.
    return HistGradientBoostingRegressor(
        loss="poisson",
        learning_rate=0.05,
        max_iter=300,
        max_leaf_nodes=15,
        min_samples_leaf=100,
        l2_regularization=1.0,
        random_state=RANDOM_SEED,
    )


def metrics(actual: np.ndarray, predicted: np.ndarray) -> dict[str, float]:
    error = predicted - actual
    total = float(np.sum(actual)) or 1.0
    return {
        "mae": round(float(np.mean(np.abs(error))), 3),
        "rmse": round(float(np.sqrt(np.mean(error ** 2))), 3),
        "wape": round(float(np.sum(np.abs(error)) / total), 4),
        "bias": round(float(np.sum(error) / total), 4),
    }


def load_sales(data: Path | None, with_synthetic: bool, weather: str) -> tuple[pd.DataFrame, list[str]]:
    sources = []
    frames = []
    if data is not None:
        real = pd.read_csv(data, parse_dates=["date"])
        real["date"] = real["date"].dt.date
        frames.append(real)
        sources.append(f"csv:{data.name}")
    if data is None or with_synthetic:
        generated = synthetic.generate(weather_source=weather)
        PROCESSED_DIR.mkdir(parents=True, exist_ok=True)
        generated.to_csv(PROCESSED_DIR / "synthetic_sales.csv", index=False)
        frames.append(generated)
        weather_used = ",".join(sorted(generated["weather_source"].dropna().unique()))
        sources.append(f"synthetic(weather={weather_used})")
    sales = pd.concat(frames, ignore_index=True)
    return sales, sources


def export_onnx(model: HistGradientBoostingRegressor, path: Path) -> None:
    """Exports the model; the input is "features" [n, 14] and the output "units" [n, 1].

    skl2onnx writes the raw tree score (log of the expected units for the
    Poisson loss), so an Exp node is appended: the API reads units directly.
    """
    sample = np.zeros((1, len(FEATURE_ORDER)), dtype=np.float32)
    onnx_model = to_onnx(model, sample, target_opset=TARGET_OPSET)
    graph = onnx_model.graph
    graph.input[0].name, old_input = "features", graph.input[0].name
    for node in graph.node:
        node.input[:] = ["features" if name == old_input else name for name in node.input]
    raw_output = graph.output[0]
    graph.node.append(helper.make_node("Exp", [raw_output.name], ["units"], name="PoissonLink"))
    units = helper.make_tensor_value_info("units", raw_output.type.tensor_type.elem_type, [None, 1])
    graph.output.remove(raw_output)
    graph.output.append(units)
    onnx.checker.check_model(onnx_model)
    path.write_bytes(onnx_model.SerializeToString())


def onnx_predict(path: Path, features: np.ndarray) -> np.ndarray:
    session = ort.InferenceSession(str(path), providers=["CPUExecutionProvider"])
    input_name = session.get_inputs()[0].name
    return session.run(None, {input_name: features.astype(np.float32)})[0].reshape(-1)


def parity_cases(frame: pd.DataFrame, model_path: Path, count: int = 24) -> list[dict]:
    """Cases with raw history, so C# rebuilds the features from scratch."""
    rng = np.random.default_rng(RANDOM_SEED)
    rows = frame.iloc[rng.choice(len(frame), size=count, replace=False)]
    series = {key: group.set_index("date")["units"] for key, group in frame.groupby(["restaurant_id", "dish"])}
    cases = []
    for _, row in rows.iterrows():
        target: date = row["date"]
        history_series = series[(row["restaurant_id"], row["dish"])]
        history = {}
        for back in range(1, HISTORY_DAYS_REQUIRED + 1):
            day = target - timedelta(days=back)
            if day in history_series.index:
                history[day] = float(history_series.loc[day])
        vector = features_for_day(history, target, row["temp_max_c"], row["precipitation_mm"])
        expected = [float(row[name]) for name in FEATURE_ORDER]
        assert np.allclose(vector, expected), (vector, expected)
        prediction = float(onnx_predict(model_path, np.array([vector]))[0])
        cases.append({
            "date": target.isoformat(),
            "temp_max_c": float(row["temp_max_c"]),
            "precipitation_mm": float(row["precipitation_mm"]),
            "history": {day.isoformat(): units for day, units in sorted(history.items()) if units},
            "features": vector,
            "prediction": round(prediction, 6),
        })
    return cases


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--data", type=Path, help="CSV with restaurant_id,dish,date,units,temp_max_c,precipitation_mm")
    parser.add_argument("--with-synthetic", action="store_true", help="add the synthetic data to --data")
    parser.add_argument("--weather", choices=["auto", "open-meteo", "climatology"], default="auto")
    parser.add_argument("--out", type=Path, default=MODELS_DIR)
    args = parser.parse_args()

    sales, sources = load_sales(args.data, args.with_synthetic, args.weather)
    frame = build_training_frame(sales)
    cutoff = frame["date"].max() - timedelta(days=TEST_DAYS)
    train, test = frame[frame["date"] <= cutoff], frame[frame["date"] > cutoff]
    print(f"[train] rows={len(frame)} train={len(train)} test={len(test)} cutoff={cutoff}")

    model = make_model().fit(train[list(FEATURE_ORDER)].to_numpy(np.float32), train["units"].to_numpy())
    predicted = model.predict(test[list(FEATURE_ORDER)].to_numpy(np.float32))
    actual = test["units"].to_numpy(float)
    evaluation = {
        "model": metrics(actual, predicted),
        "baseline_lag_7": metrics(actual, test["lag_7"].to_numpy(float)),
        "baseline_same_weekday_mean_4w": metrics(actual, test["same_weekday_mean_4w"].to_numpy(float)),
        "test_days": TEST_DAYS,
        "test_rows": int(len(test)),
    }
    # Holidays, holiday eves and paydays: where the calendar features matter most.
    special = (test["is_holiday"] + test["is_holiday_eve"] + test["is_payday_window"]).to_numpy() > 0
    evaluation["special_days"] = {
        "rows": int(special.sum()),
        "model": metrics(actual[special], predicted[special]),
        "baseline_same_weekday_mean_4w": metrics(actual[special], test["same_weekday_mean_4w"].to_numpy(float)[special]),
    }
    print(json.dumps(evaluation, indent=2))

    # Final model: refit with every row (the test period is the most recent data).
    final_model = make_model().fit(frame[list(FEATURE_ORDER)].to_numpy(np.float32), frame["units"].to_numpy())
    args.out.mkdir(parents=True, exist_ok=True)
    model_path = args.out / "demand_model.onnx"
    export_onnx(final_model, model_path)

    sample = frame[list(FEATURE_ORDER)].to_numpy(np.float32)[:2000]
    max_difference = float(np.max(np.abs(onnx_predict(model_path, sample) - final_model.predict(sample))))
    print(f"[onnx] max |onnx - sklearn| = {max_difference:.6f}")

    session = ort.InferenceSession(str(model_path), providers=["CPUExecutionProvider"])
    metadata = {
        "model_name": "stockia-demand",
        "model_version": MODEL_VERSION,
        "trained_at": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "algorithm": f"HistGradientBoostingRegressor(loss=poisson), scikit-learn {sklearn.__version__}",
        "target": "units sold per dish and day",
        "horizon_days": 7,
        "feature_order": list(FEATURE_ORDER),
        "history_days_required": HISTORY_DAYS_REQUIRED,
        "onnx_input": session.get_inputs()[0].name,
        "onnx_output": session.get_outputs()[0].name,
        "onnx_opset": TARGET_OPSET,
        "data_sources": sources,
        "training_rows": int(len(frame)),
        "date_range": [frame["date"].min().isoformat(), frame["date"].max().isoformat()],
        "metrics": evaluation,
        # Confidence reported by the API = (1 - WAPE) × history coverage.
        "confidence_base": round(max(0.0, 1.0 - evaluation["model"]["wape"]), 4),
        "onnx_max_abs_difference": round(max_difference, 6),
    }
    (args.out / "model_metadata.json").write_text(json.dumps(metadata, indent=2, ensure_ascii=False) + "\n")
    (args.out / "metrics.json").write_text(json.dumps(evaluation, indent=2) + "\n")
    fixture = {"feature_order": list(FEATURE_ORDER), "cases": parity_cases(frame, model_path)}
    (args.out / "parity_fixture.json").write_text(json.dumps(fixture, indent=2) + "\n")
    print(f"[train] saved {model_path} ({model_path.stat().st_size // 1024} KB)")


if __name__ == "__main__":
    main()
