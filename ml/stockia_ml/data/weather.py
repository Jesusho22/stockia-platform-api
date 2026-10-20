"""Daily weather for the model: Open-Meteo, with a climatology fallback.

Source: Open-Meteo Historical Weather API (https://open-meteo.com/en/docs/historical-weather-api),
free and without API key for non-commercial use. Data: ERA5 reanalysis.
Variables used: daily maximum temperature (°C) and daily precipitation (mm).

When the API cannot be reached (offline machine, firewall), the pipeline uses
the monthly climatology of Lima plus a little noise, so it always runs. The
`source` column tells which one was used.
"""

from __future__ import annotations

import json
import urllib.error
import urllib.parse
import urllib.request
from datetime import date

import numpy as np
import pandas as pd

from ..config import DEFAULT_LATITUDE, DEFAULT_LONGITUDE, LIMA_CLIMATOLOGY, RAW_DIR, TIMEZONE

ARCHIVE_URL = "https://archive-api.open-meteo.com/v1/archive"


def fetch_open_meteo(start: date, end: date, latitude: float = DEFAULT_LATITUDE,
                     longitude: float = DEFAULT_LONGITUDE, timeout: float = 30) -> pd.DataFrame:
    """Downloads daily weather between two dates (inclusive)."""
    query = urllib.parse.urlencode({
        "latitude": latitude,
        "longitude": longitude,
        "start_date": start.isoformat(),
        "end_date": end.isoformat(),
        "daily": "temperature_2m_max,precipitation_sum",
        "timezone": TIMEZONE,
    })
    with urllib.request.urlopen(f"{ARCHIVE_URL}?{query}", timeout=timeout) as response:
        payload = json.load(response)
    daily = payload["daily"]
    frame = pd.DataFrame({
        "date": pd.to_datetime(daily["time"]).date,
        "temp_max_c": daily["temperature_2m_max"],
        "precipitation_mm": daily["precipitation_sum"],
    })
    frame["temp_max_c"] = frame["temp_max_c"].astype(float)
    frame["precipitation_mm"] = frame["precipitation_mm"].fillna(0.0).astype(float)
    frame["source"] = "open-meteo"
    return frame


def climatology(start: date, end: date, seed: int = 0, noise: bool = True) -> pd.DataFrame:
    """Lima climatology for each day, optionally with realistic day-to-day noise."""
    rng = np.random.default_rng(seed)
    days = pd.date_range(start, end, freq="D")
    temps, rains = [], []
    anomaly = 0.0
    for day in days:
        mean_temp, mean_rain = LIMA_CLIMATOLOGY[day.month]
        if noise:
            # AR(1) anomaly: warm/cold spells last several days.
            anomaly = 0.7 * anomaly + rng.normal(0, 0.8)
            temps.append(round(mean_temp + anomaly, 1))
            # Garúa: light drizzle on some winter days, almost never in summer.
            rains.append(round(float(rng.exponential(mean_rain * 4)) if rng.random() < 0.25 else 0.0, 1))
        else:
            temps.append(mean_temp)
            rains.append(mean_rain)
    return pd.DataFrame({
        "date": days.date,
        "temp_max_c": temps,
        "precipitation_mm": rains,
        "source": "climatology",
    })


def load_weather(start: date, end: date, source: str = "auto", seed: int = 0,
                 latitude: float = DEFAULT_LATITUDE, longitude: float = DEFAULT_LONGITUDE) -> pd.DataFrame:
    """Weather for the period.

    source: "open-meteo" (fail if unreachable), "climatology" or "auto"
    (Open-Meteo first, climatology when it fails). Downloads are cached in
    data/raw/ so the API is called once per period.
    """
    cache = RAW_DIR / f"weather_{latitude:.2f}_{longitude:.2f}_{start}_{end}.csv"
    if source in ("auto", "open-meteo"):
        if cache.exists():
            frame = pd.read_csv(cache, parse_dates=["date"])
            frame["date"] = frame["date"].dt.date
            return frame
        try:
            frame = fetch_open_meteo(start, end, latitude, longitude)
            RAW_DIR.mkdir(parents=True, exist_ok=True)
            frame.to_csv(cache, index=False)
            return frame
        except (urllib.error.URLError, TimeoutError, OSError, KeyError, ValueError) as error:
            if source == "open-meteo":
                raise
            print(f"[weather] Open-Meteo not reachable ({error}); using Lima climatology.")
    return climatology(start, end, seed=seed)
