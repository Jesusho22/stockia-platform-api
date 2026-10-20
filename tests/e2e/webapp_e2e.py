"""End-to-end test: the Vue Web Application (Jesusho22/stockia-webapp) against this API.

Usage: python tests/e2e/webapp_e2e.py http://localhost:4173
Needs the API on http://localhost:5000 with the demo data (run after smoke_test.py).
Screenshots are written to tests/e2e/screenshots/.
"""

import json
import os
import re
import sys
import urllib.request

from playwright.sync_api import expect, sync_playwright

BASE = (sys.argv[1] if len(sys.argv) > 1 else "http://localhost:4173").rstrip("/")
API = "http://localhost:5000/api/v1"
SHOTS = os.path.join(os.path.dirname(__file__), "screenshots")
os.makedirs(SHOTS, exist_ok=True)
results, console_errors = [], []


def check(name, condition, detail=""):
    results.append(bool(condition))
    print(("PASS " if condition else "FAIL ") + name + ("" if condition else f" -> {detail}"), flush=True)
    if not condition:
        print(f"::error title=Web App e2e::{name} -> {str(detail)[:300]}")


def api(path, token=None, body=None, method=None):
    request = urllib.request.Request(f"{API}{path}", data=json.dumps(body).encode() if body is not None else None, method=method)
    request.add_header("Content-Type", "application/json")
    if token:
        request.add_header("Authorization", f"Bearer {token}")
    with urllib.request.urlopen(request, timeout=60) as response:
        raw = response.read()
        return json.loads(raw) if raw else None


def wait_url(page, fragment, timeout=20000):
    """SPA routes change with pushState: poll the URL instead of waiting for a page load."""
    expect(page).to_have_url(re.compile(re.escape(fragment)), timeout=timeout)


def toast_text(page):
    page.wait_for_selector(".p-toast-message", timeout=8000)
    return " ".join(page.locator(".p-toast-message").all_inner_texts())


def clear_toasts(page):
    page.evaluate("document.querySelectorAll('.p-toast-message').forEach(e => e.remove())")


def sign_in_as(page, role_button):
    page.goto(f"{BASE}/auth/sign-in")
    page.get_by_role("button", name=role_button).click()
    page.get_by_role("button", name="Sign in", exact=True).click()
    wait_url(page, "/app/dashboard")


def sign_out(page):
    toggle = page.locator(".sidebar .nav-group-toggle")
    if toggle.get_attribute("aria-expanded") != "true":
        toggle.click()
    page.locator(".sidebar").get_by_role("button", name="Sign out").click()
    wait_url(page, "/auth/sign-in")


admin_token = api("/authentication/sign-in", body={"email": "admin@databitecorp.com", "password": "stockia123"})["token"]

current_page = None


def run(playwright):
    global current_page
    browser = playwright.chromium.launch()
    page = browser.new_page(viewport={"width": 1366, "height": 900})
    current_page = page
    page.on("pageerror", lambda error: console_errors.append(str(error)))

    # Sign-in rules
    page.goto(f"{BASE}/app/inventory")
    wait_url(page, "/auth/sign-in")
    check("starts in English and asks to sign in", page.locator("html").get_attribute("lang") == "en" and "Sign in to continue" in toast_text(page))
    clear_toasts(page)
    page.fill("#sign-in-email", "admin@databitecorp.com")
    page.fill("#sign-in-password", "wrong-pass1")
    page.get_by_role("button", name="Sign in", exact=True).click()
    check("wrong password from the API -> access denied", "Incorrect email or password" in toast_text(page))
    clear_toasts(page)

    sign_in_as(page, "Administrator")
    expect(page.get_by_role("heading", name=re.compile("Carla Gallardo"))).to_be_visible()
    session = page.evaluate("JSON.parse(localStorage.getItem('stockia.session'))")
    check("session keeps the JWT", bool(session and session.get("token")), session)
    page.screenshot(path=f"{SHOTS}/01-dashboard.png")

    # Inventory
    page.goto(f"{BASE}/app/inventory")
    expect(page.get_by_role("cell", name="Papa", exact=True)).to_be_visible()
    check("freshest chicken lot marked as used first", page.locator("tr", has_text="Pechuga de pollo").filter(has_text="Used first in sales").count() == 1)
    q32 = "1.12345678901234567890123456789012"
    page.get_by_role("button", name="New supply").click()
    page.fill("#item-name", "Limón")
    page.fill("#item-quantity", q32)
    page.fill("#item-threshold", "0.5")
    page.locator("#item-cost").fill("6.5")
    page.get_by_role("button", name="Save", exact=True).click()
    expect(page.locator(".p-datatable-tbody tr").first).to_contain_text("Limón")
    check("new supply first with 32 decimals", q32 in page.locator(".p-datatable-tbody tr").first.inner_text())
    stored = [item for item in api("/inventory-items", admin_token) if item["name"] == "Limón"]
    check("API stored the 32 decimals", stored and stored[0]["quantity"] == q32, stored)
    page.screenshot(path=f"{SHOTS}/02-inventory.png", full_page=True)

    # Recipes and sales rules
    page.goto(f"{BASE}/app/recipes")
    expect(page.locator("tr", has_text="Ensalada César con Pollo")).to_be_visible()
    check("dish price from ingredients (S/ 3.12)", "S/ 3.12" in page.locator("tr", has_text="Ensalada César con Pollo").inner_text())
    sales_before = len(api("/sales", admin_token))
    dialog = page.locator(".sale-blocked-dialog")
    page.get_by_role("button", name="Sell one Lomo Saltado").click()
    expect(dialog).to_be_visible()
    check("no stock -> centered dialog", "not enough stock" in dialog.inner_text().lower() and "Lomo fino" in dialog.inner_text())
    page.screenshot(path=f"{SHOTS}/03-sale-blocked.png")
    dialog.get_by_role("button", name="Understood").click()
    page.get_by_role("button", name="Sell one Fettuccine Alfredo").click()
    expect(dialog).to_be_visible()
    check("expired product -> dialog explains it", "expired" in dialog.inner_text().lower() and "Crema de leche" in dialog.inner_text())
    dialog.get_by_role("button", name="Understood").click()
    check("blocked sales were not recorded", len(api("/sales", admin_token)) == sales_before)
    page.get_by_role("button", name="Sell one Ensalada César con Pollo").click()
    check("sale registered by the API", "sold for S/ 3.12" in toast_text(page))
    clear_toasts(page)
    page.goto(f"{BASE}/app/sales")
    expect(page.locator(".p-datatable-tbody tr").first).to_contain_text("Ensalada")
    check("sales history newest first", "Ensalada" in page.locator(".p-datatable-tbody tr").first.inner_text())

    # Forecast (ML) and recommendations
    page.goto(f"{BASE}/app/forecast")
    expect(page.get_by_text(re.compile(r"ML model v1\.0\.0"))).to_be_visible()
    check("forecast made by the ML model", page.locator(".chart-col").count() == 7)
    page.get_by_role("button", name="Generate").click()
    check("generate a new forecast", "generated" in toast_text(page).lower())
    clear_toasts(page)
    page.screenshot(path=f"{SHOTS}/04-forecast.png", full_page=True)
    page.goto(f"{BASE}/app/recommendations")
    expect(page.get_by_text(re.compile("Lomo fino")).first).to_be_visible()
    check("purchase recommendation from the forecast", page.get_by_text(re.compile("Comprar .* Lomo fino")).count() >= 1)
    page.goto(f"{BASE}/app/alerts")
    expect(page.get_by_text("Lomo fino sin stock disponible.")).to_be_visible()
    check("automatic alerts from the API", True)

    # Settings
    page.goto(f"{BASE}/app/settings/account")
    expect(page.get_by_role("heading", name="Subscription type")).to_be_visible()
    page.fill("#settings-current-password", "badpass1")
    page.fill("#settings-new-password", "Nueva2026")
    page.fill("#settings-confirm-password", "Nueva2026")
    page.locator('section[aria-labelledby="settings-password-title"]').get_by_role("button", name="Change password").click()
    check("API verifies the current password", "current password is not correct" in toast_text(page))
    clear_toasts(page)
    page.fill("#settings-restaurant-address", "Av. Larco 789, Miraflores")
    page.locator('section[aria-labelledby="settings-restaurant-title"]').get_by_role("button", name="Save changes").click()
    check("restaurant data saved in the API", "Restaurant data updated" in toast_text(page)
          and api("/restaurants/current", admin_token)["address"] == "Av. Larco 789, Miraflores")
    clear_toasts(page)
    sign_out(page)

    # Employee
    sign_in_as(page, "Employee")
    side = page.locator(".sidebar")
    check("employee menu hides admin sections", side.get_by_role("link", name="Roles & permissions").count() == 0
          and side.get_by_role("link", name="Recommendations").count() == 0)
    page.goto(f"{BASE}/app/roles")
    wait_url(page, "/app/dashboard")
    check("employee blocked from roles", "Access denied" in toast_text(page))
    clear_toasts(page)
    sign_out(page)

    # Sign-up creates a restaurant in the API
    page.goto(f"{BASE}/auth/sign-up")
    page.fill("#sign-up-full-name", "Rosa Quispe")
    page.fill("#sign-up-restaurant", "Picantería Rosa")
    page.fill("#sign-up-email", "rosa@picanteria.pe")
    page.fill("#sign-up-password", "Picante2026")
    page.fill("#sign-up-confirm-password", "Picante2026")
    page.get_by_role("button", name="Create account").click()
    wait_url(page, "/app/dashboard")
    page.goto(f"{BASE}/app/settings/plans")
    expect(page.get_by_text("Your current plan")).to_be_visible()
    check("new restaurant on the free plan", "Esencial" in page.locator("li", has_text="Your current plan").inner_text())

    # Expired token -> back to sign-in
    page.evaluate("""() => {
        const session = JSON.parse(localStorage.getItem('stockia.session'));
        session.token = 'invalid.token.value';
        localStorage.setItem('stockia.session', JSON.stringify(session));
    }""")
    page.goto(f"{BASE}/app/inventory")
    wait_url(page, "/auth/sign-in", 15000)
    check("invalid token -> signed out with notice", "Your session expired" in toast_text(page))

    # Mobile
    mobile = browser.new_page(viewport={"width": 390, "height": 800}, is_mobile=True, has_touch=True)
    mobile.goto(f"{BASE}/auth/sign-in")
    mobile.get_by_role("button", name="Administrator").click()
    mobile.get_by_role("button", name="Sign in", exact=True).click()
    wait_url(mobile, "/app/dashboard")
    mobile.get_by_role("button", name="Open navigation menu").click()
    check("mobile dropdown menu", mobile.locator("#mobile-menu").is_visible())
    mobile.locator("#mobile-menu").get_by_role("link", name="Inventory").click()
    wait_url(mobile, "/app/inventory")
    expect(mobile.get_by_role("cell", name="Papa", exact=True)).to_be_visible()
    check("mobile without horizontal scroll", mobile.evaluate("document.documentElement.scrollWidth - window.innerWidth") <= 0)
    mobile.screenshot(path=f"{SHOTS}/05-mobile-inventory.png")
    browser.close()


with sync_playwright() as playwright:
    try:
        run(playwright)
    except Exception as error:  # noqa: BLE001 - report any failure as an annotation
        import traceback
        location = next((frame for frame in reversed(traceback.extract_tb(error.__traceback__)) if frame.filename.endswith("webapp_e2e.py")), None)
        message = str(error).replace("\n", " ")[:600]
        print(f"::error title=Web App e2e crashed (line {location.lineno if location else "?"})::{type(error).__name__}: {message}")
        if current_page is not None:
            try:
                current_page.screenshot(path=f"{SHOTS}/crash.png", full_page=True)
                print(f"::error title=Page at crash::{current_page.url} | {current_page.inner_text('body')[:500].replace(chr(10), ' ')}")
            except Exception:  # noqa: BLE001
                pass
        results.append(False)

check("no JavaScript errors", not console_errors, console_errors[:3])
print(f"\n{sum(results)}/{len(results)} passed")
print(f"::notice title=Web App e2e::{sum(results)}/{len(results)} passed")
sys.exit(0 if all(results) else 1)
