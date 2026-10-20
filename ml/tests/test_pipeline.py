import json
from datetime import date, timedelta

import holidays
import numpy as np
import onnxruntime as ort
import pandas as pd
import pytest

from stockia_ml.config import MODELS_DIR
from stockia_ml.data import calendar_pe, synthetic
from stockia_ml.data.weather import climatology
from stockia_ml.features import FEATURE_ORDER, build_training_frame, features_for_day


@pytest.mark.parametrize("year", range(2020, 2031))
def test_calendar_matches_holidays_package(year):
    # The package also lists Easter Sunday; it is always a Sunday, so it is not modeled.
    reference = set(holidays.Peru(years=year)) - {calendar_pe.easter_sunday(year)}
    assert set(calendar_pe.holidays_of(year)) == reference


def test_payday_window():
    assert calendar_pe.is_payday_window(date(2026, 2, 28))
    assert calendar_pe.is_payday_window(date(2026, 10, 15))
    assert calendar_pe.is_payday_window(date(2026, 10, 1))
    assert not calendar_pe.is_payday_window(date(2026, 10, 20))


def test_climatology_without_noise_is_the_monthly_mean():
    frame = climatology(date(2026, 1, 1), date(2026, 1, 3), noise=False)
    assert frame["temp_max_c"].tolist() == [26.5, 26.5, 26.5]


@pytest.fixture(scope="module")
def small_sales():
    return synthetic.generate(date(2025, 1, 1), date(2025, 4, 30), restaurants_per_profile=1, weather_source="climatology")


def test_synthetic_data_is_reproducible(small_sales):
    again = synthetic.generate(date(2025, 1, 1), date(2025, 4, 30), restaurants_per_profile=1, weather_source="climatology")
    pd.testing.assert_frame_equal(small_sales, again)
    assert (small_sales["units"] >= 0).all()


def test_vectorized_features_match_serving_features(small_sales):
    frame = build_training_frame(small_sales)
    series = {key: group.set_index("date")["units"].to_dict() for key, group in frame.groupby(["restaurant_id", "dish"])}
    full = {key: group.set_index("date")["units"].to_dict()
            for key, group in small_sales.groupby(["restaurant_id", "dish"])}
    for _, row in frame.sample(50, random_state=1).iterrows():
        history = full[(row["restaurant_id"], row["dish"])]
        vector = features_for_day(history, row["date"], row["temp_max_c"], row["precipitation_mm"])
        assert np.allclose(vector, [row[name] for name in FEATURE_ORDER])
    assert series  # frame not empty


def test_exported_model_reproduces_the_parity_fixture():
    model = MODELS_DIR / "demand_model.onnx"
    fixture = json.loads((MODELS_DIR / "parity_fixture.json").read_text())
    session = ort.InferenceSession(str(model), providers=["CPUExecutionProvider"])
    for case in fixture["cases"]:
        target = date.fromisoformat(case["date"])
        history = {date.fromisoformat(day): units for day, units in case["history"].items()}
        vector = features_for_day(history, target, case["temp_max_c"], case["precipitation_mm"])
        assert np.allclose(vector, case["features"])
        units = session.run(["units"], {"features": np.array([vector], dtype=np.float32)})[0][0][0]
        assert units == pytest.approx(case["prediction"], rel=1e-4)
        assert target - timedelta(days=34) <= min(history) if history else True
