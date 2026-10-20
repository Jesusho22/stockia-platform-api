"""Exports the real sales of a restaurant from the StockIA API as training data.

Usage (from the ml/ folder, with the API running):
    python -m stockia_ml.data.api_export --api http://localhost:5000/api/v1 \\
        --email admin@databitecorp.com --password stockia123

It signs in, reads /sales (voided sales are ignored), adds up the units of
each dish per day and joins the weather of those days. The CSV can be used to
retrain:
    python -m stockia_ml.train --data data/processed/stockia_sales.csv --with-synthetic
"""

from __future__ import annotations

import argparse
import json
import urllib.request
from collections import defaultdict
from datetime import date

import pandas as pd

from ..config import PROCESSED_DIR
from .weather import load_weather


def _request(url: str, token: str | None = None, body: dict | None = None):
    headers = {"Content-Type": "application/json", "Accept": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    data = json.dumps(body).encode() if body is not None else None
    with urllib.request.urlopen(urllib.request.Request(url, data=data, headers=headers), timeout=60) as response:
        return json.load(response)


def export(api: str, email: str, password: str, weather: str = "auto") -> pd.DataFrame:
    session = _request(f"{api}/authentication/sign-in", body={"email": email, "password": password})
    sales = _request(f"{api}/sales", token=session["token"])
    units: dict[tuple[date, str], float] = defaultdict(float)
    for sale in sales:
        if sale.get("status") != "CONFIRMED":
            continue
        day = date.fromisoformat(str(sale["saleDate"])[:10])
        for line in sale.get("lineItems", []):
            units[(day, line["dishName"])] += float(line["quantity"])
    if not units:
        raise SystemExit("The restaurant has no confirmed sales yet.")
    frame = pd.DataFrame([(day, dish, value) for (day, dish), value in units.items()], columns=["date", "dish", "units"])
    frame["restaurant_id"] = session.get("restaurantId", 0)
    climate = load_weather(frame["date"].min(), frame["date"].max(), source=weather)
    frame = frame.merge(climate[["date", "temp_max_c", "precipitation_mm"]], on="date", how="left")
    return frame.sort_values(["dish", "date"]).reset_index(drop=True)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--api", default="http://localhost:5000/api/v1")
    parser.add_argument("--email", required=True)
    parser.add_argument("--password", required=True)
    parser.add_argument("--weather", choices=["auto", "open-meteo", "climatology"], default="auto")
    parser.add_argument("--out", default=str(PROCESSED_DIR / "stockia_sales.csv"))
    args = parser.parse_args()
    frame = export(args.api.rstrip("/"), args.email, args.password, args.weather)
    PROCESSED_DIR.mkdir(parents=True, exist_ok=True)
    frame.to_csv(args.out, index=False)
    print(f"[export] {len(frame)} rows ({frame['dish'].nunique()} dishes) -> {args.out}")


if __name__ == "__main__":
    main()
