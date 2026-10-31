# StockIA Platform API

Backend de **StockIA** (DataBite Corp): API REST en **ASP.NET Core (.NET 10)** para la Web Application en Vue
([Jesusho22/stockia-webapp](https://github.com/Jesusho22/stockia-webapp)), con un **modelo de Machine Learning**
de pronóstico de demanda entrenado en Python y ejecutado dentro de la API en formato ONNX.

El código está organizado en los **8 Bounded Contexts del Capítulo IV del informe** y sigue la estructura del
ejemplo del curso (`catch-up-platform`) y del proyecto guía **QULLQA**: monolito modular con un `AppDbContext`,
DDD por bounded context, EF Core + MySQL con **migraciones**, rutas kebab-case (`api/v1/[controller]`),
OpenAPI + **Scalar**, `Result` + ProblemDetails, recursos `.resx` en inglés/español, Cortex.Mediator para
eventos de dominio y JWT propio con BCrypt.

| Verificación (GitHub Actions) | Resultado |
|---|---|
| Compilación + pruebas unitarias xUnit (reglas de dominio y paridad Python ↔ C# del modelo) | ✅ 42 pruebas |
| Migraciones EF Core al día con el modelo | ✅ |
| Smoke test de la API contra MySQL 8.4 real | ✅ 69/69 |
| Web App (Vue) + API + MySQL de punta a punta con Playwright | ✅ 34/34 |
| Pipeline ML: pruebas + reentrenamiento con clima real de Open-Meteo | ✅ WAPE 0.287 vs 0.305 del baseline |

---

## 1. Arquitectura

### 1.1 Bounded Contexts (Capítulo IV)

| Carpeta | Bounded Context | Qué hace | Componentes del C4 que implementa |
|---|---|---|---|
| `Iam/` | Identity & Access Management | Inicio de sesión con JWT, roles, equipo, cambio y **recuperación de contraseña**, **bloqueo de cuenta** | Login Controller, Account Lock Service, Session Manager, Password Reset Service, Password Change Service |
| `RestaurantRegistration/` | Restaurant Registration | Alta del restaurante con su dueño (primer Administrador) y datos del restaurante | Restaurant Onboarding Controller, Plan Selection, Profile Management |
| `SubscriptionPayments/` | Subscription & Payments | Planes y suscripción (pago simulado) | Subscription Controller, Stripe Payment Adapter (simulado), Plan Validation |
| `StockManagement/` | Stock Management | Insumos (lotes) con vencimiento, estado y costo; descuento de stock; registro de consumos | Add / Modify / Remove Supplies, Inventory Aggregator |
| `RecipesManagement/` | Recipes Management | Recetas (platos) y **ventas**: valida stock y descuenta insumos al vender ("Dish Sold") | Recipe CRUD Controller, Ingredient Deduction Engine |
| `MlRecommendations/` | ML & Recommendations | Pronóstico de demanda con el modelo ONNX, clima y calendario peruano; recomendaciones de compra y menú | Sales Data Collector, Demand Forecasting Engine, Weather API Adapter, Menu Recommendation Engine |
| `NotificationsMessaging/` | Notifications & Messaging | Alertas de stock y vencimiento, entrega por WhatsApp/correo (simulada), correos transaccionales | Alert Event Listener, SendGrid Adapter (simulado) |
| `AnalyticsDashboard/` | Analytics & Dashboard | KPIs del panel y **ahorro semanal** (solo Administrador) | Metrics View Updater, Alerts Panel, Demand Prediction View, Weekly Savings Calculator |
| `Shared/` | — | `Result`, `Quantity` (32 decimales), UnitOfWork, `AppDbContext`, migraciones, convención kebab-case, seed demo | — |

Descartados por el equipo (StockIA trabaja con **un solo restaurante**): Gamification Engine, Multi-Branch
Consolidator y Account Deactivation Component.

### 1.2 Capas de cada bounded context

```
<BoundedContext>/
├── Domain/
│   ├── Model/Aggregates · Entities · ValueObjects · Commands · Queries · Events
│   ├── Repositories/          interfaces
│   └── Services/              reglas puras (p. ej. StockAllocationService, AccountLockService)
├── Application/
│   ├── Errors/                enum de errores del contexto (títulos de los ProblemDetails)
│   ├── Services/              interfaces I*CommandService / I*QueryService
│   ├── Internal/CommandServices · QueryServices · EventHandlers · OutboundServices
│   └── ACL/                   implementación de la facade para otros contextos
├── Infrastructure/
│   └── Persistence/EFC/Configuration/Extensions · Repositories   (+ servicios externos)
├── Interfaces/
│   ├── ACL/                   I<Contexto>ContextFacade + DTOs que ven los otros contextos
│   └── REST/                  Controllers · Resources · Transform (assemblers)
└── Resources/                 <Contexto>Messages.resx / .es.resx
```

Igual que en `catch-up-platform`: `Shared/Application/Patterns/Result.cs`,
`Shared/Infrastructure/Interfaces/ASP/Configuration/KebabCaseRouteNamingConvention.cs`,
`Shared/Infrastructure/Persistence/EFC/{Configuration,Interceptors,Migrations,Repositories}`,
`Resources/SharedResource*.resx`, `AddOpenApi` + `MapScalarApiReference`, `AddProblemDetails` +
`UseExceptionHandler` y `Database.Migrate()` al arrancar.

### 1.3 Comunicación entre contextos

Un contexto nunca usa los repositorios ni las tablas de otro: llama a su **facade ACL**
(`Interfaces/ACL`), normalmente a través de un *outbound service* propio (`ExternalStockService`,
`ExternalIamService`…). Ejemplos:

- Recipes Management → Stock Management: `CheckAvailability`, `DeductStock` y `PricesOf` al vender o listar platos.
- Restaurant Registration → IAM (`CreateOwnerAccount`) y → Subscription & Payments (`CreateDefaultSubscription`) al registrar un restaurante, en una sola transacción.
- IAM → Notifications & Messaging (`SendEmail`) para el enlace de recuperación de contraseña.
- Analytics & Dashboard → Stock, Recipes, ML y Notifications para el resumen y el ahorro semanal.

Eventos de dominio: `StockLevelChangedEvent` (Stock → Notifications genera alertas) y
`DemandForecastGeneratedEvent` (ML → recomendaciones).

Detalles técnicos:
- **Base de datos:** MySQL 8 con `MySql.EntityFrameworkCore`, tablas en `snake_case`, esquema creado por las
  migraciones de `Shared/Infrastructure/Persistence/EFC/Migrations` (ver 6.1).
- **Cantidades con 32 decimales:** `Quantity` es un entero `BigInteger` escalado ×10^32, guardado como
  `varchar(80)` y enviado en JSON como texto (`"1.12345678901234567890123456789012"`).
- **Zona horaria:** las fechas de negocio se calculan con la hora de Lima (`IRestaurantClock`); las ventas se guardan en UTC.
- **Errores:** `application/problem+json`; `title` es el código (`CannotSell`, `AccountLocked`…) y
  `details` trae datos extra (insumos faltantes, intentos restantes, minutos de bloqueo).

---

## 2. Endpoints

Todas las rutas empiezan por `/api/v1` y siguen la convención kebab-case. Salvo las públicas, requieren
`Authorization: Bearer <token>`. **ADMIN** = solo Administrador (el Empleado recibe 403).
Documentación interactiva: **`http://localhost:5000/scalar`** (OpenAPI en `/openapi/v1.json`; `/swagger` redirige ahí).

| Contexto | Método y ruta | Acceso | Descripción |
|---|---|---|---|
| — | `GET /health` | Pública | Estado de la API y del modelo ML |
| IAM | `POST /authentication/sign-in` | Pública | Usuario + JWT. 401 con `details.remainingAttempts`; tras 5 errores, 423 `AccountLocked` (15 min) |
| IAM | `POST /authentication/password-reset-requests` | Pública | Envía el enlace de recuperación por correo (misma respuesta exista o no el correo) |
| IAM | `POST /authentication/password-resets` | Pública | Nueva contraseña con el código del enlace; también desbloquea la cuenta |
| IAM | `GET /users` | ADMIN | Equipo del restaurante (incluye `locked`) |
| IAM | `GET /users/me`, `GET /users/{id}` | Usuario | Perfil |
| IAM | `POST /users` | ADMIN | Invita un miembro (contraseña temporal `stockia123`) |
| IAM | `PATCH /users/{id}`, `POST /users/{id}/change-password` | El propio usuario | Perfil y cambio de contraseña (verifica la actual) |
| IAM | `PATCH /users/{id}/role`, `POST /users/{id}/unlock`, `DELETE /users/{id}` | ADMIN | Rol, desbloqueo y retiro (siempre queda un Administrador) |
| Restaurant Registration | `POST /restaurant-registrations` | Pública | Registra restaurante + dueño (Administrador) + plan gratuito y devuelve el JWT |
| Restaurant Registration | `GET /restaurants/current` / `PUT` | Usuario / ADMIN | Datos del restaurante |
| Subscription & Payments | `GET /plans` | Pública | Planes (Esencial, Profesional, IoT Completo) |
| Subscription & Payments | `GET /subscriptions/current`, `POST /subscriptions` | ADMIN | Plan actual / cambiar plan |
| Stock Management | `GET/POST /inventory-items`, `GET/PUT/DELETE /inventory-items/{id}` | Usuario | Insumos (lotes) |
| Recipes Management | `GET/POST /recipes`, `GET/PUT/DELETE /recipes/{id}` | Usuario | Platos; `price` = suma del costo de sus ingredientes |
| Recipes Management | `GET /sales?from&to`, `GET /sales/{id}` | Usuario | Historial (más reciente primero) |
| Recipes Management | `POST /sales` | Usuario | `{recipeId, quantity, channel}`; 422 `CannotSell` si falta stock o está vencido |
| Recipes Management | `POST /sales/{id}/void` | Usuario | Anula la venta (los insumos usados no regresan); 409 si ya estaba anulada |
| ML & Recommendations | `GET /demand-forecasts`, `GET /demand-forecasts/latest`, `GET /demand-forecasts/model` | Usuario | Pronósticos y estado del modelo |
| ML & Recommendations | `POST /demand-forecasts` | Usuario | `{horizonDays}` (1–7) genera uno nuevo |
| ML & Recommendations | `GET /recommendations`, `POST /recommendations/{id}/apply` | ADMIN | Recomendaciones de compra y menú |
| Notifications & Messaging | `GET/POST /alerts`, `PUT/DELETE /alerts/{id}` | Usuario | Alertas |
| Notifications & Messaging | `POST /alerts/{id}/deliveries`, `POST /alerts/{id}/acknowledge` | Usuario | Envío por canal / atender |
| Analytics & Dashboard | `GET /dashboard/summary` | Usuario | KPIs del día |
| Analytics & Dashboard | `GET /dashboard/weekly-savings` | ADMIN | Ahorro semanal (stock rescatado y merma vs. semana anterior) |

`DataBit.StockIA.Platform/DataBit.StockIA.Platform.http` trae ejemplos listos para usar.

---

## 3. Reglas de negocio implementadas

- **Ventas sin stock no se registran.** Antes de vender se revisan todos los ingredientes; si falta alguno,
  422 con el motivo de cada insumo (`NOT_FOUND`, `EXPIRED`, `INSUFFICIENT`). No hay descuentos parciales.
- **Lotes vencidos no se usan** y, entre los lotes válidos, se consume primero el **más fresco**.
- **Precio del plato** = Σ (cantidad del ingrediente × costo unitario del lote que se usaría).
- **Cantidades exactas con 32 decimales** en insumos, recetas y descuentos.
- **Roles:** Administrador y Empleado. El Empleado no ve equipo, recomendaciones, suscripción, datos
  financieros del ahorro semanal ni edita el restaurante (la API responde 403 aunque se llame directamente).
- **Bloqueo de cuenta:** 5 contraseñas incorrectas seguidas bloquean la cuenta 15 minutos (incluso con la
  contraseña correcta). Un inicio de sesión correcto reinicia el conteo; el Administrador puede desbloquear
  y la recuperación de contraseña también desbloquea.
- **Recuperación de contraseña:** código de 6 dígitos dentro de un enlace enviado por correo; vence en
  30 minutos, admite 5 intentos y sirve una sola vez. Solo se guarda el hash del código. La respuesta no
  revela si el correo existe.
- **Ahorro semanal (últimos 7 días):** *stock rescatado* = valor de lo que las ventas usaron de lotes a
  ≤ 3 días de vencer; *merma* = lotes que vencieron en la semana con stock. Ahorro estimado = rescatado +
  la reducción de la merma frente a la semana anterior (si bajó).
- **Alertas automáticas:** sin stock, bajo el mínimo, por vencer (≤ 3 días) y vencido; al cambiar el stock
  y cada hora. Una alerta **CRITICAL** solo se atiende después de entregarse por WhatsApp y correo.
- **Recomendaciones:** con cada pronóstico se propone comprar lo que falta ("Comprar X kg de … antes del
  dd/MM") y cambios de menú si un plato sube ≥ 15 % o baja ≥ 20 %.
- **Registro del restaurante:** restaurante, Administrador y plan gratuito en una sola transacción.

---

## 4. Cómo ejecutarla

### Opción A — Docker (recomendada)

```bash
docker compose up --build
```

Levanta MySQL 8.4 y la API en `http://localhost:5000` (Scalar en `/scalar`). Al arrancar se aplican las
migraciones y se cargan los datos demo. En este modo la API devuelve la copia del correo de recuperación
(demo), porque el envío de correos es simulado.

### Opción B — .NET local (Rider, Visual Studio o terminal)

Requisitos: .NET SDK 10 y un MySQL 8 local (`root`/`root`, puerto 3306; ver `appsettings.Development.json`).

```bash
dotnet run --project DataBit.StockIA.Platform
# Si tu MySQL tiene otra contraseña:
ConnectionStrings__DefaultConnection="server=127.0.0.1;port=3306;user=root;password=TU_CLAVE;database=stockia;SslMode=Preferred" \
  dotnet run --project DataBit.StockIA.Platform
```

### Variables de entorno (producción)

| Variable | Ejemplo | Uso |
|---|---|---|
| `STOCKIA_DB_HOST`, `STOCKIA_DB_PORT` | `mysql.example.com`, `3306` | Servidor MySQL |
| `STOCKIA_DB_USER`, `STOCKIA_DB_PASSWORD`, `STOCKIA_DB_NAME` | `stockia`, `***`, `stockia` | Credenciales |
| `STOCKIA_DB_SSLMODE` | `Required` | `Preferred` en local, `Required` en la nube |
| `STOCKIA_JWT_SECRET` | 32+ caracteres aleatorios | Firma de los tokens |
| `STOCKIA_CORS_ORIGINS` | `https://stockia.vercel.app` | Orígenes extra (separados por coma) |
| `Iam__PasswordReset__ResetLinkBaseUrl` | `https://stockia.vercel.app` | Dominio de la Web App para el enlace del correo |
| `Iam__PasswordReset__ExposeSimulatedEmail` | `false` | `true` solo en demos: la API devuelve la copia del correo simulado |
| `PORT` | `8080` | Puerto (Azure, Render y similares) |

Otras opciones en `appsettings.json`: `Seed:DemoData`, `OpenApi:Enabled`, `Weather:Enabled` (si es `false`
usa la climatología de Lima), `Restaurant:TimeZone`, `Iam:InvitationTemporaryPassword`.

### Cuentas demo

| Rol | Email | Contraseña |
|---|---|---|
| Administrador | `admin@databitecorp.com` | `stockia123` |
| Empleado | `empleado@databitecorp.com` | `stockia123` |

El seed crea el restaurante "DataBite Corp — Restaurante Demo", 10 insumos (con un lote vencido, uno por
vencer, uno sin stock y dos lotes de pollo para la regla del lote más fresco), 4 recetas, 120 días de
ventas, el registro de consumos de las dos últimas semanas (para el ahorro semanal) y un primer pronóstico ML.

---

## 5. Conexión con la Web Application

En el repo `stockia-webapp` (rama `main`):

```bash
npm install
npm run dev          # usa VITE_STOCKIA_API_URL=http://localhost:5000/api/v1 (.env.development)
```

La Web App tiene las mismas 8 carpetas de bounded context (`iam`, `restaurant-registration`,
`stock-management`, `recipes-management`, `ml-recommendations`, `notifications-messaging`,
`analytics-dashboard`, `subscription-payments`) más `shared`. Para producción se cambia
`VITE_STOCKIA_API_URL` y se agrega el dominio a `STOCKIA_CORS_ORIGINS` (los `*.vercel.app` ya están permitidos).

---

## 6. Pruebas y CI

| Qué | Dónde | Cómo correrlo |
|---|---|---|
| Pruebas unitarias: Quantity, asignación de stock, calendario peruano, alertas, bloqueo y recuperación de cuenta, ahorro semanal y **paridad del modelo ONNX con Python** | `DataBit.StockIA.Platform.Tests` (una carpeta por bounded context) | `dotnet test` |
| Smoke test de la API (69 verificaciones) | `tests/smoke/smoke_test.py` | `python tests/smoke/smoke_test.py http://localhost:5000/api/v1` |
| E2E Web App + API (Playwright, 34 verificaciones, capturas en `tests/e2e/screenshots`) | `tests/e2e/webapp_e2e.py` | `python tests/e2e/webapp_e2e.py http://localhost:4173` |
| Pruebas del pipeline ML (16) | `ml/tests` | `cd ml && python -m pytest` |

Workflows de GitHub Actions:
- **`ci.yml`** (cada push/PR): compila, pruebas unitarias, verifica que las migraciones estén al día, levanta
  MySQL + API, corre el smoke test y prueba la Web App con Playwright contra la API real (usa la rama de la
  Web App con el mismo nombre si existe; si no, `main`).
- **`ef-migrations.yml`**: crea una migración y la sube a la misma rama (ver 6.1).
- **`ml.yml`** (cambios en `ml/` o a mano): pruebas del pipeline y reentrenamiento con clima real.
- **`deploy-azure.yml`** (push a `main`): publica la API en Azure App Service.

### 6.1 Migraciones

El esquema se crea con migraciones de EF Core (`Shared/Infrastructure/Persistence/EFC/Migrations`) y la API
las aplica al arrancar (`Database.Migrate()`). Después de cambiar el modelo:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add NombreDelCambio --project DataBit.StockIA.Platform \
  --output-dir Shared/Infrastructure/Persistence/EFC/Migrations
```

Sin .NET local: escribe el nombre en `.github/ef-migration-request`, haz push y el workflow
`ef-migrations.yml` agrega la migración a la rama. El CI falla si el modelo tiene cambios sin migración.

---

## 7. Modelo de Machine Learning (pronóstico de demanda)

### 7.1 Qué predice

Las **unidades vendidas por plato y por día** para los próximos **7 días**. Con eso la API calcula el
consumo de insumos de las recetas y genera las recomendaciones de compra y menú.

### 7.2 De dónde sale la data

| Fuente | Qué aporta | Acceso |
|---|---|---|
| **Generador sintético** (`ml/stockia_ml/data/synthetic.py`) | Historial 2023–2025 de 12 restaurantes peruanos (cevichería, pollería, menú criollo, pizzería, chifa y el perfil del demo, 2 de cada uno) | Se **crea** localmente; semilla fija 2026, reproducible |
| **Open-Meteo** (`weather.py`) | Temperatura máxima y lluvia diarias de Lima: API *archive* para entrenar y API *forecast* para predecir | Gratis, **sin API key**. Si no responde se usa la climatología mensual de Lima |
| **Calendario peruano** (`calendar_pe.py`) | Feriados nacionales (incluidos los nuevos: 7 jun desde 2024, 23 jul desde 2023, 6 ago, 9 dic), Jueves/Viernes Santo, vísperas y quincena | Código propio, verificado contra el paquete `holidays` |
| **Ventas reales de StockIA** (`api_export.py`) | Las ventas guardadas por la API, agregadas por plato y día | Se **descargan** de la propia API con un usuario Administrador |

**¿Por qué datos sintéticos?** StockIA aún no tiene historial real, y los datasets públicos de
restaurantes son de otros países (otros feriados, clima y hábitos) o piden cuenta para descargarse
(Kaggle). El generador reproduce patrones conocidos del mercado limeño:
día de la semana distinto por tipo de restaurante, quincena (días 1, 15, 16 y fin de mes), feriados y
vísperas, diciembre y julio (gratificación), calor → más ceviche y ensaladas, frío → más sopas,
garúa → menos clientes, tendencia lenta por plato, promociones cortas y ruido Poisson-Gamma.
Cuando haya ventas reales suficientes, el modelo se reentrena con ellas (sección 7.6).

### 7.3 Variables (14)

| Grupo | Variables |
|---|---|
| Historial del plato (≥ 7 días de antigüedad) | `lag_7`, `lag_14`, `mean_7_13`, `mean_7_34`, `same_weekday_mean_4w` |
| Calendario | `day_of_week`, `is_weekend`, `day_of_month`, `month`, `is_holiday`, `is_holiday_eve`, `is_payday_window` |
| Clima | `temp_max_c`, `precipitation_mm` |

Todos los rezagos tienen al menos 7 días, así que los 7 días del horizonte se predicen
directamente, sin encadenar predicciones. Se necesitan 34 días de historial; con menos de 14 días con
ventas, la API usa un **baseline** (promedio del mismo día de la semana) y lo indica en la respuesta
(`method: BASELINE`).

### 7.4 Algoritmo y resultados

`HistGradientBoostingRegressor` de scikit-learn con pérdida **Poisson** (adecuada para conteos de
unidades). Validación temporal: las últimas 8 semanas no se usan para entrenar.

| Métrica (últimas 8 semanas, 2 912 filas) | Modelo | Mismo día semana (4 sem.) | Mismo día semana pasada |
|---|---|---|---|
| WAPE (error absoluto / unidades) | **0.287** | 0.299 | 0.380 |
| MAE (unidades) | **9.45** | 9.82 | 12.48 |
| Sesgo | -0.007 | -0.029 | -0.001 |
| WAPE en feriados, vísperas y quincena | **0.318** | 0.339 | — |
| Sesgo en esos días | **+0.002** | -0.078 | — |

La mayor ganancia está en los días especiales: el baseline se queda corto (‑7.8 %) en feriados y
quincenas, y el modelo no tiene ese sesgo. Con clima real de Open-Meteo (CI) el resultado es
WAPE 0.287 vs 0.305.

La **confianza** que muestra la Web App es `1 − WAPE` de validación (0.71), multiplicada por la
cobertura del historial del restaurante.

### 7.5 Cómo llega a la API

1. `train.py` exporta el modelo a **ONNX** (`ml/models/demand_model.onnx`, entrada `features`, salida
   `units`) junto con `model_metadata.json` (versión, orden de variables, métricas) y
   `parity_fixture.json` (casos de prueba).
2. El `.csproj` copia esos archivos a la salida y `OnnxDemandModel` (`MlRecommendations/Infrastructure/MachineLearning`) los carga con
   `Microsoft.ML.OnnxRuntime`; si el orden de variables no coincide, el modelo se desactiva y se usa el
   baseline.
3. `DemandFeatureBuilder` (C#) arma las mismas 14 variables que `features.py`. La prueba
   `ModelParityTests` verifica que las variables y las predicciones de C# coinciden con las de Python
   (diferencia ≈ 5·10⁻⁵).

### 7.6 Reentrenar

```bash
cd ml
python -m venv .venv && source .venv/bin/activate
pip install -r requirements.txt          # versiones fijadas (skl2onnx necesita scikit-learn 1.6.1)
python -m pytest                          # 16 pruebas del pipeline

# Solo datos sintéticos + clima real (o --weather climatology sin internet)
python -m stockia_ml.train --weather open-meteo

# Con ventas reales de la API
python -m stockia_ml.data.api_export --api http://localhost:5000/api/v1 \
       --email admin@databitecorp.com --password stockia123
python -m stockia_ml.train --data data/processed/stockia_sales.csv --with-synthetic
```

Se reescriben los archivos de `ml/models/`; basta con recompilar la API. Antes de publicar un modelo
nuevo hay que subir `model_version` y revisar que `metrics.json` siga mejor que el baseline.

### 7.7 Límites conocidos

- Hoy aprende de datos simulados: sus números son orientativos hasta reentrenar con ventas reales
  (idealmente 3 meses o más por restaurante).
- No conoce eventos locales (partidos, ferias) ni promociones del propio restaurante.
- El clima es el de Lima; para otras ciudades se usan las coordenadas del restaurante en Open-Meteo,
  pero el entrenamiento solo vio clima limeño.

---

## 8. Despliegue en Azure (igual que QULLQA)

| Pieza | Servicio |
|---|---|
| API | Azure App Service (Linux, .NET 10), publicado por `.github/workflows/deploy-azure.yml` en cada push a `main` |
| MySQL | Azure Database for MySQL – Flexible Server (Burstable B1ms), `STOCKIA_DB_SSLMODE=Required` |
| Web App | Vercel, con `VITE_STOCKIA_API_URL=https://<app>.azurewebsites.net/api/v1` |

Configuración del App Service (*Settings → Environment variables*):

| Variable | Valor |
|---|---|
| `STOCKIA_DB_HOST` | `<servidor>.mysql.database.azure.com` |
| `STOCKIA_DB_PORT` / `STOCKIA_DB_NAME` | `3306` / `stockia` (las migraciones crean las tablas al arrancar) |
| `STOCKIA_DB_USER` / `STOCKIA_DB_PASSWORD` | Administrador del servidor MySQL |
| `STOCKIA_DB_SSLMODE` | `Required` |
| `STOCKIA_JWT_SECRET` | 32+ caracteres aleatorios (`openssl rand -base64 48`) |
| `STOCKIA_CORS_ORIGINS` | URL de la Web App en Vercel |
| `Iam__PasswordReset__ResetLinkBaseUrl` | URL de la Web App en Vercel |

*Startup command:* `dotnet DataBit.StockIA.Platform.dll`. En GitHub: secreto `AZURE_WEBAPP_PUBLISH_PROFILE`
(perfil de publicación del App Service) y variable `AZURE_WEBAPP_NAME`. El servidor MySQL debe permitir el
acceso desde servicios de Azure (*Networking → Allow public access from any Azure service*).

Alternativas sin Azure: Render o Railway con el `Dockerfile` (puerto `PORT`) y MySQL en Aiven o Railway.

---

## 9. Pendientes / simulaciones

- Pagos (Stripe/PayPal), WhatsApp y correo están **simulados** detrás de interfaces (`IPaymentGateway`,
  `INotificationSender`); se reemplazan sin tocar el dominio. El correo de recuperación se registra en el log
  (y en modo demo se devuelve en la respuesta).
- Sensores IoT del plan "IoT Completo": fuera del alcance de esta versión.
- Descartados por el equipo (un solo restaurante): Gamification Engine, Multi-Branch Consolidator y Account
  Deactivation Component del C4.
