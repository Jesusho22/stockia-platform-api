"""Peruvian calendar used as model features.

The same rules are implemented in C# (DemandForecasting/Domain/Services/PeruCalendar.cs)
so the API builds exactly the same features the model was trained with. The
`holidays` package is only used in the tests to check these rules.
"""

from __future__ import annotations

import calendar
from datetime import date, timedelta
from functools import lru_cache

# National holidays with a fixed date: (month, day, first year it applies).
FIXED_HOLIDAYS = (
    (1, 1, 1900),    # Año Nuevo
    (5, 1, 1900),    # Día del Trabajo
    (6, 7, 2024),    # Batalla de Arica y Día de la Bandera (Ley 31788)
    (6, 29, 1900),   # San Pedro y San Pablo
    (7, 23, 2023),   # Día de la Fuerza Aérea del Perú (Ley 31822)
    (7, 28, 1900),   # Fiestas Patrias
    (7, 29, 1900),   # Fiestas Patrias
    (8, 6, 2022),    # Batalla de Junín
    (8, 30, 1900),   # Santa Rosa de Lima
    (10, 8, 1900),   # Combate de Angamos
    (11, 1, 1900),   # Todos los Santos
    (12, 8, 1900),   # Inmaculada Concepción
    (12, 9, 2022),   # Batalla de Ayacucho
    (12, 25, 1900),  # Navidad
)


def easter_sunday(year: int) -> date:
    """Anonymous Gregorian algorithm (Meeus/Jones/Butcher)."""
    a = year % 19
    b, c = divmod(year, 100)
    d, e = divmod(b, 4)
    f = (b + 8) // 25
    g = (b - f + 1) // 3
    h = (19 * a + b - d - g + 15) % 30
    i, k = divmod(c, 4)
    l = (32 + 2 * e + 2 * i - h - k) % 7
    m = (a + 11 * h + 22 * l) // 451
    month, day = divmod(h + l - 7 * m + 114, 31)
    return date(year, month, day + 1)


@lru_cache(maxsize=64)
def holidays_of(year: int) -> frozenset[date]:
    easter = easter_sunday(year)
    days = {date(year, month, day) for month, day, since in FIXED_HOLIDAYS if year >= since}
    days.add(easter - timedelta(days=3))  # Jueves Santo
    days.add(easter - timedelta(days=2))  # Viernes Santo
    return frozenset(days)


def is_holiday(day: date) -> bool:
    return day in holidays_of(day.year)


def is_holiday_eve(day: date) -> bool:
    return is_holiday(day + timedelta(days=1))


def is_payday_window(day: date) -> bool:
    """Quincena: salaries are paid on the 15th and the last day of the month.

    Restaurants usually sell more on payday and the day after.
    """
    last_day = calendar.monthrange(day.year, day.month)[1]
    return day.day in (1, 15, 16, last_day)
