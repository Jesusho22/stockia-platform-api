"""Feature engineering shared by training and serving.

Every feature of day `t` uses sales that are at least 7 days old, so the same
model forecasts any of the next 7 days directly. The API rebuilds these exact
features in C# (DemandForecasting/Domain/Services/DemandFeatureBuilder.cs);
FEATURE_ORDER is written into models/model_metadata.json and checked by the
C# tests with models/parity_fixture.json.
"""

from __future__ import annotations

from datetime import date, timedelta
from typing import Mapping

import numpy as np
import pandas as pd

from .data import calendar_pe

FEATURE_ORDER: tuple[str, ...] = (
    "lag_7",
    "lag_14",
    "mean_7_13",
    "mean_7_34",
    "same_weekday_mean_4w",
    "day_of_week",
    "is_weekend",
    "day_of_month",
    "month",
    "is_holiday",
    "is_holiday_eve",
    "is_payday_window",
    "temp_max_c",
    "precipitation_mm",
)

# Oldest sale a feature looks at: t-34.
HISTORY_DAYS_REQUIRED = 34


def calendar_features(day: date) -> dict[str, float]:
    return {
        "day_of_week": float(day.weekday()),
        "is_weekend": float(day.weekday() >= 5),
        "day_of_month": float(day.day),
        "month": float(day.month),
        "is_holiday": float(calendar_pe.is_holiday(day)),
        "is_holiday_eve": float(calendar_pe.is_holiday_eve(day)),
        "is_payday_window": float(calendar_pe.is_payday_window(day)),
    }


def features_for_day(history: Mapping[date, float], target: date, temp_max_c: float,
                     precipitation_mm: float) -> list[float]:
    """Feature vector of one dish for one day (serving-style, readable version).

    history: units sold per day; missing days count as 0.
    """
    def units(days_back: int) -> float:
        return float(history.get(target - timedelta(days=days_back), 0.0))

    values = {
        "lag_7": units(7),
        "lag_14": units(14),
        "mean_7_13": float(np.mean([units(back) for back in range(7, 14)])),
        "mean_7_34": float(np.mean([units(back) for back in range(7, 35)])),
        "same_weekday_mean_4w": float(np.mean([units(back) for back in (7, 14, 21, 28)])),
        **calendar_features(target),
        "temp_max_c": float(temp_max_c),
        "precipitation_mm": float(precipitation_mm),
    }
    return [values[name] for name in FEATURE_ORDER]


def complete_daily_grid(sales: pd.DataFrame) -> pd.DataFrame:
    """One row per restaurant, dish and calendar day (days without sales = 0 units)."""
    frames = []
    for (restaurant_id, dish), group in sales.groupby(["restaurant_id", "dish"], sort=False):
        group = group.set_index(pd.to_datetime(group["date"])).sort_index()
        full_index = pd.date_range(group.index.min(), group.index.max(), freq="D")
        group = group.reindex(full_index)
        group["units"] = group["units"].fillna(0.0)
        group["restaurant_id"] = restaurant_id
        group["dish"] = dish
        group["date"] = full_index.date
        frames.append(group.reset_index(drop=True))
    return pd.concat(frames, ignore_index=True)


def build_training_frame(sales: pd.DataFrame) -> pd.DataFrame:
    """Adds FEATURE_ORDER columns (vectorized) and drops rows without enough history.

    sales: restaurant_id, dish, date, units, temp_max_c, precipitation_mm.
    """
    grid = complete_daily_grid(sales)
    grid = grid.sort_values(["restaurant_id", "dish", "date"]).reset_index(drop=True)
    by_series = grid.groupby(["restaurant_id", "dish"], sort=False)["units"]

    shifted = {back: by_series.shift(back) for back in range(7, 35)}
    grid["lag_7"] = shifted[7]
    grid["lag_14"] = shifted[14]
    grid["mean_7_13"] = pd.concat([shifted[back] for back in range(7, 14)], axis=1).mean(axis=1, skipna=False)
    grid["mean_7_34"] = pd.concat([shifted[back] for back in range(7, 35)], axis=1).mean(axis=1, skipna=False)
    grid["same_weekday_mean_4w"] = pd.concat([shifted[back] for back in (7, 14, 21, 28)], axis=1).mean(axis=1, skipna=False)

    dates = pd.to_datetime(grid["date"])
    grid["day_of_week"] = dates.dt.weekday.astype(float)
    grid["is_weekend"] = (dates.dt.weekday >= 5).astype(float)
    grid["day_of_month"] = dates.dt.day.astype(float)
    grid["month"] = dates.dt.month.astype(float)
    grid["is_holiday"] = [float(calendar_pe.is_holiday(day)) for day in grid["date"]]
    grid["is_holiday_eve"] = [float(calendar_pe.is_holiday_eve(day)) for day in grid["date"]]
    grid["is_payday_window"] = [float(calendar_pe.is_payday_window(day)) for day in grid["date"]]

    # Weather of days without sales rows comes from the same date of other series.
    weather = sales.groupby("date")[["temp_max_c", "precipitation_mm"]].first()
    grid = grid.drop(columns=["temp_max_c", "precipitation_mm"], errors="ignore").merge(
        weather, left_on="date", right_index=True, how="left")
    grid["temp_max_c"] = grid["temp_max_c"].astype(float)
    grid["precipitation_mm"] = grid["precipitation_mm"].fillna(0.0).astype(float)

    return grid.dropna(subset=list(FEATURE_ORDER)).reset_index(drop=True)
