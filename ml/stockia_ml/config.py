"""Shared constants of the StockIA demand forecasting pipeline."""

from pathlib import Path

ML_ROOT = Path(__file__).resolve().parents[1]
DATA_DIR = ML_ROOT / "data"
RAW_DIR = DATA_DIR / "raw"
PROCESSED_DIR = DATA_DIR / "processed"
MODELS_DIR = ML_ROOT / "models"

# Lima, Peru (Cercado). Used for weather when a restaurant has no coordinates.
DEFAULT_LATITUDE = -12.0464
DEFAULT_LONGITUDE = -77.0428
TIMEZONE = "America/Lima"

# Forecasting horizon: the features only use data that is at least 7 days old,
# so one model predicts any of the next 7 days directly (no recursion).
HORIZON_DAYS = 7
MIN_LAG_DAYS = 7

# Monthly climatology of Lima (mean daily max temperature in °C and mean daily
# precipitation in mm). Used when Open-Meteo is not reachable, both when
# building the training data and when the API forecasts (same numbers in
# DemandForecasting/Infrastructure/Weather/LimaClimatology.cs).
LIMA_CLIMATOLOGY = {
    1: (26.5, 0.05),
    2: (27.5, 0.10),
    3: (27.0, 0.05),
    4: (25.0, 0.02),
    5: (22.5, 0.05),
    6: (20.0, 0.15),
    7: (19.0, 0.25),
    8: (18.5, 0.30),
    9: (19.0, 0.20),
    10: (20.5, 0.10),
    11: (22.5, 0.05),
    12: (24.5, 0.05),
}

RANDOM_SEED = 2026
