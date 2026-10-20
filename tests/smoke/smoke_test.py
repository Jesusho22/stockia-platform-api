"""Black-box test of the StockIA Platform API (run against a fresh database with demo data).

Usage: python tests/smoke/smoke_test.py http://localhost:5000/api/v1
Prints PASS/FAIL per check; failures are also GitHub Actions annotations.
"""

import json
import sys
import urllib.error
import urllib.request
from decimal import Decimal

API = (sys.argv[1] if len(sys.argv) > 1 else "http://localhost:5000/api/v1").rstrip("/")
results = []


def call(method, path, token=None, body=None, headers=None):
    request = urllib.request.Request(f"{API}{path}", method=method, data=json.dumps(body).encode() if body is not None else None)
    request.add_header("Accept", "application/json")
    if body is not None:
        request.add_header("Content-Type", "application/json")
    if token:
        request.add_header("Authorization", f"Bearer {token}")
    for key, value in (headers or {}).items():
        request.add_header(key, value)
    try:
        with urllib.request.urlopen(request, timeout=60) as response:
            raw = response.read()
            return response.status, json.loads(raw) if raw else None, dict(response.headers)
    except urllib.error.HTTPError as error:
        raw = error.read()
        try:
            payload = json.loads(raw) if raw else None
        except json.JSONDecodeError:
            payload = raw.decode(errors="replace")
        return error.code, payload, dict(error.headers)


def check(name, condition, detail=""):
    results.append(bool(condition))
    print(("PASS " if condition else "FAIL ") + name + ("" if condition else f" -> {detail}"), flush=True)
    if not condition:
        print(f"::error title=Smoke test::{name} -> {str(detail)[:400]}")


status, health, _ = call("GET", "/health")
check("health", status == 200 and health["status"] == "ok", health)
check("demand model loaded", health and health["demandModel"] != "baseline", health)

status, plans, _ = call("GET", "/plans")
check("plans are public", status == 200 and [plan["name"] for plan in plans] == ["Esencial", "Profesional", "IoT Completo"], plans)

status, body, _ = call("POST", "/authentication/sign-in", body={"email": "admin@databitecorp.com", "password": "wrong-pass1"})
check("wrong password -> 401 InvalidCredentials", status == 401 and body["title"] == "InvalidCredentials", body)

status, admin, _ = call("POST", "/authentication/sign-in", body={"email": "ADMIN@databitecorp.com", "password": "stockia123"})
check("admin sign-in", status == 200 and admin["role"] == "ADMIN" and admin["token"] and admin["restaurantName"].startswith("DataBite"), admin)
token = admin["token"]

status, _, _ = call("GET", "/inventory-items")
check("no token -> 401", status == 401, status)

status, items, _ = call("GET", "/inventory-items", token)
check("inventory listed newest first", status == 200 and len(items) == 10 and items[0]["id"] > items[-1]["id"], items)
chicken = [item for item in items if item["name"] == "Pechuga de pollo"]
expired_lot = next(item for item in chicken if item["status"] == "EXPIRED")
fresh_lot = next(item for item in chicken if item["status"] != "EXPIRED")
check("quantities are strings", isinstance(fresh_lot["quantity"], str) and fresh_lot["quantity"] == "12.5", fresh_lot)

q32 = "1.12345678901234567890123456789012"
status, created, _ = call("POST", "/inventory-items", token, {
    "name": "Limón", "unit": "kg", "quantity": q32, "minThreshold": 0.5, "storageType": "AMBIENT", "shelfLifeDays": 10, "unitCost": 6.5})
check("supply with 32 decimals", status == 201 and created["quantity"] == q32 and created["minThreshold"] == "0.5", created)
status, items, _ = call("GET", "/inventory-items", token)
check("new supply appears first", items[0]["name"] == "Limón", items[0])
status, body, _ = call("POST", "/inventory-items", token, {
    "name": "X", "unit": "kg", "quantity": "0.123456789012345678901234567890123", "minThreshold": 1, "storageType": "AMBIENT",
    "shelfLifeDays": 1, "unitCost": 1})
check("33 decimals rejected", status == 400, body)

status, recipes, _ = call("GET", "/recipes", token)
price = {recipe["dishName"]: Decimal(str(recipe["price"])) for recipe in recipes}
check("dish price = sum of ingredients", price.get("Ensalada César con Pollo") == Decimal("3.12") and price.get("Pizza Margarita") == Decimal("5.98"), price)
recipe_id = {recipe["dishName"]: recipe["id"] for recipe in recipes}

status, sales_before, _ = call("GET", "/sales", token)
status, blocked, _ = call("POST", "/sales", token, {"recipeId": recipe_id["Lomo Saltado"], "quantity": 1})
shortages = (blocked or {}).get("details", {}).get("shortages", []) if isinstance(blocked, dict) else []
check("no stock -> 422 CannotSell with shortages", status == 422 and blocked["title"] == "CannotSell" and shortages
      and shortages[0]["reason"] == "INSUFFICIENT" and shortages[0]["itemName"] == "Lomo fino", blocked)
status, sales_after, _ = call("GET", "/sales", token)
check("blocked sale is not recorded", len(sales_after) == len(sales_before), (len(sales_before), len(sales_after)))

status, blocked, _ = call("POST", "/sales", token, {"recipeId": recipe_id["Fettuccine Alfredo"], "quantity": 1})
shortages = blocked.get("details", {}).get("shortages", []) if isinstance(blocked, dict) else []
check("expired product -> 422 with reason EXPIRED", status == 422 and any(
    item["reason"] == "EXPIRED" and item["itemName"] == "Crema de leche" for item in shortages), blocked)

status, sale, _ = call("POST", "/sales", token, {"recipeId": recipe_id["Ensalada César con Pollo"], "quantity": 1})
check("sale with fresh lot", status == 201 and Decimal(str(sale["total"])) == Decimal("3.12") and sale["status"] == "CONFIRMED", sale)
status, items, _ = call("GET", "/inventory-items", token)
by_id = {item["id"]: item for item in items}
check("fresh lot deducted, expired untouched", by_id[fresh_lot["id"]]["quantity"] == "12.32" and by_id[expired_lot["id"]]["quantity"] == "18",
      (by_id[fresh_lot["id"]], by_id[expired_lot["id"]]))
status, sales_now, _ = call("GET", "/sales", token)
check("sales newest first", sales_now[0]["id"] == sale["id"], sales_now[:2])
status, voided, _ = call("POST", f"/sales/{sale['id']}/void", token)
check("void sale", status == 200 and voided["status"] == "VOIDED", voided)
status, body, _ = call("POST", f"/sales/{sale['id']}/void", token)
check("void twice -> 409", status == 409, body)

status, latest, _ = call("GET", "/demand-forecasts/latest", token)
check("seeded forecast uses the ML model", status == 200 and latest["method"] == "MACHINE_LEARNING" and len(latest["dataPoints"]) == 7, latest)
status, forecast, _ = call("POST", "/demand-forecasts", token, {"horizonDays": 7})
check("generate forecast", status == 201 and len(forecast["dataPoints"]) == 7 and 0 < forecast["confidenceScore"] <= 1
      and all(point["projectedUnits"] > 0 for point in forecast["dataPoints"]), forecast)
print(f"   forecast: {[(p['date'], p['dishName'], p['projectedUnits']) for p in forecast['dataPoints']]} weather={forecast['weatherCondition']}/{forecast['weatherSource']} confidence={forecast['confidenceScore']}")
status, model, _ = call("GET", "/demand-forecasts/model", token)
check("model info", status == 200 and model["ready"] and model["modelVersion"] == "1.0.0", model)

status, recommendations, _ = call("GET", "/recommendations", token)
check("purchase recommendation for Lomo fino", status == 200 and any(
    item["type"] == "PURCHASE_SUGGESTION" and "Lomo fino" in item["message"] for item in recommendations), recommendations)
status, applied, _ = call("POST", f"/recommendations/{recommendations[0]['id']}/apply", token)
check("apply recommendation", status == 200 and applied["applied"], applied)

status, alerts, _ = call("GET", "/alerts", token)
check("automatic alerts", status == 200 and any(alert["type"] == "CRITICAL_STOCK" and "Lomo fino" in alert["message"] for alert in alerts)
      and any(alert["type"] == "EXPIRING_SOON" and "Tomate" in alert["message"] for alert in alerts), alerts)
status, alert, _ = call("POST", "/alerts", token, {"type": "IOT_FAULT", "severity": "CRITICAL", "message": "Refrigerador #2 abierto 6 minutos", "channel": "WHATSAPP"})
check("manual critical alert pending email", status == 201 and alert["deliveredChannels"] == ["WHATSAPP"] and not alert["delivered"], alert)
status, body, _ = call("POST", f"/alerts/{alert['id']}/acknowledge", token)
check("cannot attend undelivered alert", status == 409 and body["title"] == "NotDelivered", body)
status, alert, _ = call("POST", f"/alerts/{alert['id']}/deliveries", token)
check("retry delivery", status == 200 and alert["delivered"], alert)
status, alert, _ = call("POST", f"/alerts/{alert['id']}/acknowledge", token)
check("attend alert", status == 200 and alert["acknowledged"], alert)

status, employee, _ = call("POST", "/authentication/sign-in", body={"email": "empleado@databitecorp.com", "password": "stockia123"})
etoken = employee["token"]
for path in ("/users", "/recommendations", "/subscriptions/current"):
    status, body, _ = call("GET", path, etoken)
    check(f"employee forbidden {path}", status == 403, (status, body))
status, _, _ = call("GET", "/inventory-items", etoken)
check("employee can see inventory", status == 200, status)
status, body, _ = call("PUT", "/restaurants/current", etoken, {"name": "Hack"})
check("employee cannot edit restaurant", status == 403, body)

status, restaurant, _ = call("PUT", "/restaurants/current", token, {"name": "DataBite Corp — Restaurante Demo", "address": "Av. Larco 456", "phone": "+51 999 888 777"})
check("admin edits restaurant", status == 200 and restaurant["address"] == "Av. Larco 456", restaurant)
status, body, _ = call("POST", f"/users/{admin['id']}/change-password", token, {"currentPassword": "bad", "newPassword": "Nueva2026"})
check("change password verifies current", status == 422 and body["title"] == "CurrentPasswordInvalid", body)
status, _, _ = call("POST", f"/users/{admin['id']}/change-password", token, {"currentPassword": "stockia123", "newPassword": "Nueva2026"})
check("change password", status == 204, status)
status, _, _ = call("POST", f"/users/{admin['id']}/change-password", token, {"currentPassword": "Nueva2026", "newPassword": "stockia123"})
check("restore password", status == 204, status)

status, member, _ = call("POST", "/users", token, {"fullName": "Ana Torres", "email": "ana@databitecorp.com", "role": "EMPLOYEE"})
check("invite member", status == 201 and member["role"] == "EMPLOYEE", member)
status, member, _ = call("PATCH", f"/users/{member['id']}/role", token, {"role": "ADMIN"})
check("change role", status == 200 and member["role"] == "ADMIN", member)
status, team, _ = call("GET", "/users", token)
check("team newest first", team[0]["email"] == "ana@databitecorp.com", team)
status, _, _ = call("DELETE", f"/users/{member['id']}", token)
check("remove member", status == 204, status)
status, body, _ = call("DELETE", f"/users/{admin['id']}", token)
check("cannot remove yourself", status == 409 and body["title"] == "CannotRemoveYourself", body)

status, body, _ = call("POST", "/authentication/sign-up", body={"fullName": "Luis Paz", "restaurantName": "La Esquina", "email": "admin@databitecorp.com", "password": "Esquina2026"})
check("sign-up email taken -> 409", status == 409 and body["title"] == "EmailAlreadyTaken", body)
status, body, _ = call("POST", "/authentication/sign-up", body={"fullName": "Luis Paz", "restaurantName": "La Esquina", "email": "luis@laesquina.pe", "password": "abc"})
check("sign-up weak password -> 400", status == 400, body)
status, owner, _ = call("POST", "/authentication/sign-up", body={"fullName": "Luis Paz", "restaurantName": "La Esquina", "email": "luis@laesquina.pe", "password": "Esquina2026"})
check("sign-up creates restaurant and admin", status == 201 and owner["role"] == "ADMIN" and owner["restaurantName"] == "La Esquina", owner)
status, subscription, _ = call("GET", "/subscriptions/current", owner["token"])
check("new restaurant gets the free plan", status == 200 and subscription["planId"] == plans[0]["id"], subscription)
status, subscription, _ = call("POST", "/subscriptions", owner["token"], {"planId": plans[1]["id"], "paymentMethod": "STRIPE"})
check("upgrade plan (simulated Stripe)", status == 200 and subscription["planId"] == plans[1]["id"] and subscription["paymentMethod"] == "STRIPE", subscription)
status, other_items, _ = call("GET", "/inventory-items", owner["token"])
check("tenant isolation", status == 200 and other_items == [], other_items)
status, body, _ = call("POST", "/demand-forecasts", owner["token"], {})
check("forecast without recipes -> 422", status == 422 and body["title"] == "NoDishes", body)

status, summary, _ = call("GET", "/dashboard/summary", token)
check("dashboard summary", status == 200 and summary["registeredSupplies"] == 11 and summary["pendingAlerts"] > 0, summary)

status, _, headers = call("OPTIONS", "/inventory-items", headers={"Origin": "http://localhost:4173", "Access-Control-Request-Method": "GET",
                                                                  "Access-Control-Request-Headers": "authorization"})
check("CORS preflight for the Web App", headers.get("Access-Control-Allow-Origin") == "http://localhost:4173", (status, headers))

print(f"\n{sum(results)}/{len(results)} passed")
print(f"::notice title=API smoke tests::{sum(results)}/{len(results)} passed | forecast {forecast.get('method')} {forecast.get('modelVersion')} "
      f"confidence {forecast.get('confidenceScore')} weather {forecast.get('weatherCondition')}/{forecast.get('weatherSource')} | "
      f"{[(p['date'][5:], p['projectedUnits']) for p in forecast.get('dataPoints', [])]}")
sys.exit(0 if all(results) else 1)
