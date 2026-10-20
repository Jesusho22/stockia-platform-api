"""Synthetic sales history of Peruvian restaurants (training data).

Why synthetic data: StockIA has no real restaurant history yet and public
restaurant datasets are either from other countries or need an account to
download. The generator reproduces the patterns the business documents
describe and that are well known in Lima's restaurant market, so the model
learns them and can be retrained later with the real sales stored by the API
(see data/api_export.py).

Patterns included (each one is a multiplier over the dish base demand):
- Day of week, different per restaurant type (cevichería peaks on weekends,
  "menú" restaurants on weekdays).
- Quincena: payday on the 15th and the last day of the month.
- National holidays and holiday eves (calendar_pe.py).
- Seasonality: December (Christmas) and July (Fiestas Patrias, gratificación).
- Weather: hot days raise ceviche and salads, cold days raise soups; drizzle
  lowers dine-in traffic a little.
- Slow trend per dish, short promotions and Poisson-Gamma noise.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from datetime import date, timedelta

import numpy as np
import pandas as pd

from ..config import RANDOM_SEED
from . import calendar_pe
from .weather import load_weather


@dataclass(frozen=True)
class Dish:
    name: str
    base_units: float
    temp_effect: float = 0.0  # change per °C above 22 °C (0.04 = +4% per degree)
    rain_effect: float = -0.05  # change on drizzle days


@dataclass(frozen=True)
class RestaurantProfile:
    kind: str
    dishes: tuple[Dish, ...]
    weekday_factors: tuple[float, ...]  # Monday..Sunday
    holiday_factor: float = 1.2
    payday_factor: float = 1.12
    closed_days: tuple[tuple[int, int], ...] = field(default_factory=tuple)  # (month, day)


PROFILES: tuple[RestaurantProfile, ...] = (
    RestaurantProfile(
        kind="cevicheria",
        dishes=(
            Dish("Ceviche clásico", 45, temp_effect=0.045),
            Dish("Leche de tigre", 28, temp_effect=0.05),
            Dish("Arroz con mariscos", 24, temp_effect=0.01),
            Dish("Jalea mixta", 15, temp_effect=0.015),
            Dish("Chicharrón de pescado", 12),
        ),
        weekday_factors=(0.70, 0.75, 0.80, 0.85, 1.00, 1.45, 1.60),
        holiday_factor=1.35,
    ),
    RestaurantProfile(
        kind="polleria",
        dishes=(
            Dish("Pollo a la brasa 1/4", 60, temp_effect=-0.01),
            Dish("Pollo a la brasa 1/2", 26, temp_effect=-0.01),
            Dish("Pollo a la brasa entero", 15, temp_effect=-0.015),
            Dish("Anticuchos", 11),
        ),
        weekday_factors=(0.80, 0.80, 0.85, 0.90, 1.20, 1.35, 1.40),
        holiday_factor=1.30,
        payday_factor=1.18,
    ),
    RestaurantProfile(
        kind="criollo-menu",
        dishes=(
            Dish("Menú ejecutivo", 80),
            Dish("Lomo Saltado", 20),
            Dish("Ají de gallina", 18),
            Dish("Seco de res", 14, temp_effect=-0.01),
            Dish("Caldo de gallina", 12, temp_effect=-0.06, rain_effect=0.10),
        ),
        weekday_factors=(1.05, 1.05, 1.05, 1.05, 1.00, 0.60, 0.35),
        holiday_factor=0.50,
        payday_factor=1.06,
        closed_days=((1, 1), (12, 25)),
    ),
    RestaurantProfile(
        kind="pizzeria",
        dishes=(
            Dish("Pizza Margarita", 30),
            Dish("Pizza Americana", 25),
            Dish("Lasaña", 12, temp_effect=-0.02),
            Dish("Ensalada César con Pollo", 10, temp_effect=0.04),
        ),
        weekday_factors=(0.70, 0.85, 0.80, 0.90, 1.30, 1.45, 1.15),
        holiday_factor=1.20,
    ),
    RestaurantProfile(
        kind="chifa",
        dishes=(
            Dish("Arroz chaufa", 50),
            Dish("Tallarín saltado", 30),
            Dish("Pollo chi jau kay", 18),
            Dish("Wantán frito", 15),
            Dish("Sopa wantán", 12, temp_effect=-0.06, rain_effect=0.10),
        ),
        weekday_factors=(0.85, 0.85, 0.90, 0.95, 1.10, 1.30, 1.35),
        holiday_factor=1.25,
    ),
    RestaurantProfile(
        # The StockIA demo restaurant: same menu as the API seed data.
        kind="stockia-demo",
        dishes=(
            Dish("Pizza Margarita", 28),
            Dish("Lomo Saltado", 32),
            Dish("Ensalada César con Pollo", 22, temp_effect=0.04),
        ),
        weekday_factors=(0.80, 0.85, 0.90, 0.95, 1.15, 1.35, 1.20),
        holiday_factor=1.20,
    ),
)

MONTH_FACTORS = {1: 1.0, 2: 0.97, 3: 0.97, 4: 0.98, 5: 1.0, 6: 0.99, 7: 1.08, 8: 1.0, 9: 0.98, 10: 1.0, 11: 1.02, 12: 1.12}
NEGATIVE_BINOMIAL_SHAPE = 15.0  # higher = less noise


def _promotion_days(rng: np.random.Generator, start: date, days: int) -> set[date]:
    promos: set[date] = set()
    for _ in range(max(1, days // 120)):
        first = start + timedelta(days=int(rng.integers(0, max(1, days - 6))))
        promos.update(first + timedelta(days=offset) for offset in range(int(rng.integers(3, 7))))
    return promos


def generate(start: date = date(2023, 1, 1), end: date = date(2025, 12, 31), restaurants_per_profile: int = 2,
             weather_source: str = "auto", seed: int = RANDOM_SEED) -> pd.DataFrame:
    """Daily units sold per restaurant and dish.

    Columns: restaurant_id, restaurant_kind, date, dish, units, temp_max_c,
    precipitation_mm, weather_source.
    """
    rng = np.random.default_rng(seed)
    weather = load_weather(start, end, source=weather_source, seed=seed).set_index("date")
    days = pd.date_range(start, end, freq="D").date
    rows = []
    restaurant_id = 0
    for profile in PROFILES:
        for _ in range(restaurants_per_profile):
            restaurant_id += 1
            scale = float(rng.uniform(0.6, 1.5))
            for dish in profile.dishes:
                yearly_trend = float(rng.normal(0.0, 0.12))
                promos = _promotion_days(rng, start, len(days))
                walk = 0.0
                for index, day in enumerate(days):
                    if (day.month, day.day) in profile.closed_days:
                        units = 0
                    else:
                        temp, rain = weather.loc[day, ["temp_max_c", "precipitation_mm"]]
                        walk = 0.98 * walk + rng.normal(0, 0.015)
                        level = dish.base_units * scale * np.exp(yearly_trend * index / 365 + walk)
                        factor = profile.weekday_factors[day.weekday()] * MONTH_FACTORS[day.month]
                        if calendar_pe.is_holiday(day):
                            factor *= profile.holiday_factor
                        elif calendar_pe.is_holiday_eve(day):
                            factor *= 1.0 + (profile.holiday_factor - 1.0) * 0.35
                        if calendar_pe.is_payday_window(day):
                            factor *= profile.payday_factor
                        factor *= 1.0 + dish.temp_effect * (float(temp) - 22.0)
                        if float(rain) >= 0.5:
                            factor *= 1.0 + dish.rain_effect
                        if day in promos:
                            factor *= 1.3
                        expected = max(level * factor, 0.1)
                        gamma = rng.gamma(NEGATIVE_BINOMIAL_SHAPE, expected / NEGATIVE_BINOMIAL_SHAPE)
                        units = int(rng.poisson(gamma))
                    rows.append((restaurant_id, profile.kind, day, dish.name, units))
    sales = pd.DataFrame(rows, columns=["restaurant_id", "restaurant_kind", "date", "dish", "units"])
    weather_columns = weather.reset_index()[["date", "temp_max_c", "precipitation_mm", "source"]]
    sales = sales.merge(weather_columns.rename(columns={"source": "weather_source"}), on="date", how="left")
    return sales
