# StockIA Platform API

Backend de **StockIA** (DataBite Corp): API REST en **ASP.NET Core (.NET 10)** que da servicio a la
Web Application en Vue ([Jesusho22/stockia-webapp](https://github.com/Jesusho22/stockia-webapp), rama
`feature/platform-api`), con un **modelo de Machine Learning** de pronóstico de demanda entrenado en Python
y ejecutado dentro de la API en formato ONNX.

La estructura sigue el proyecto guía **QULLQA**: monolito modular, un `AppDbContext`, DDD por
bounded context, EF Core + MySQL, Cortex.Mediator para eventos de dominio, JWT propio con BCrypt,
`Result<T>` + ProblemDetails, recursos `.resx` en inglés/español y Swagger.

| Verificación (GitHub Actions) | Resultado |
|---|---|
| Compilación + pruebas unitarias xUnit (incluye paridad Python ↔ C# del modelo) | ✅ |
| Smoke test de la API contra MySQL 8.4 real | ✅ 53/53 |
| Web App (Vue) + API + MySQL de punta a punta con Playwright | ✅ 25/25 |
| Pipeline ML: pruebas + reentrenamiento con clima real de Open-Meteo | ✅ WAPE 0.287 vs 0.305 del baseline |

---

## 1. Arquitectura

```
DataBit.StockIA.Platform/
├── Shared/               Result<T>, Quantity (32 decimales), UnitOfWork, AppDbContext, JWT/usuario actual,
│                         reloj de Lima, ProblemDetails, middleware de errores, seed de datos demo
├── Iam/                  Usuarios, restaurante, sign-in/sign-up, roles, invitaciones, contraseña
├── Subscription/         Planes y suscripción del restaurante (pasarela de pago simulada)
├── ProductInventory/     Insumos (lotes con vencimiento y costo) y recetas (platos)
├── SalesOrder/           Ventas: valida y descuenta stock por receta, anulación
├── DemandForecasting/    Pronóstico de demanda (modelo ONNX + clima Open-Meteo + calendario peruano)
├── Alerts/               Alertas de inventario y recomendaciones de compra / menú
├── Dashboard/            Resumen que compone los demás contextos (solo lectura)
└── Resources/            Mensajes en/es (.resx)
```

Cada bounded context tiene las mismas capas que QULLQA:

| Capa | Contenido |
|---|---|
| `Domain` | Aggregates, Entities, ValueObjects, Commands, Queries, Errors, Events, interfaces de Repositories y servicios de dominio (reglas puras, sin EF) |
| `Application` | CommandServices, QueryServices, EventHandlers, facades ACL (`I<Contexto>ContextFacade`) y OutboundServices |
| `Infrastructure` | Repositorios EF Core, configuración del modelo, servicios externos (ONNX, Open-Meteo, pago, notificaciones) |
| `Interfaces` | Controllers REST, Resources (DTO) y Assemblers |

Los contextos no se referencian entre sí: se comunican mediante **facades ACL** (por ejemplo, SalesOrder
llama a `IProductInventoryContextFacade.CheckRecipeSale` antes de registrar la venta) y **eventos de
dominio** (`StockLevelChangedEvent` → Alerts genera alertas; `DemandForecastGeneratedEvent` → Alerts
genera recomendaciones).

Detalles técnicos:
- **Base de datos:** MySQL 8 con `MySql.EntityFrameworkCore`. Las tablas se crean con `EnsureCreated` al
  arrancar (igual que QULLQA, sin migraciones), con nombres en `snake_case`.
- **Cantidades con 32 decimales:** `Quantity` es un entero `BigInteger` escalado ×10^32. Se guarda como
  `varchar(80)` y viaja en JSON como texto (`"1.12345678901234567890123456789012"`); también acepta números
  JSON. Ni `decimal` ni `DECIMAL` de MySQL llegan a 32 decimales, por eso no se usan.
- **Zona horaria:** las fechas de negocio (vencimientos, "hoy") se calculan con la hora de Lima
  (`IRestaurantClock`); las ventas se guardan en UTC.
- **Errores:** respuestas `application/problem+json`; `title` es el código del error
  (`CannotSell`, `InvalidCredentials`…) y `extensions.details` trae los datos extra (por ejemplo, los
  insumos faltantes). La Web App traduce esos códigos con i18n.

---

## 2. Endpoints

Todas las rutas empiezan por `/api/v1`. Salvo las marcadas como públicas, requieren
`Authorization: Bearer <token>`. **ADMIN** = solo Administrador (los empleados reciben 403).
Swagger: `http://localhost:5000/swagger`.

| Contexto | Método y ruta | Acceso | Descripción |
|---|---|---|---|
| — | `GET /health` | Pública | Estado de la API y del modelo ML |
| IAM | `POST /authentication/sign-in` | Pública | Devuelve el usuario + JWT |
| IAM | `POST /authentication/sign-up` | Pública | Crea restaurante + primer Administrador + plan gratuito |
| IAM | `GET /users` | ADMIN | Equipo del restaurante |
| IAM | `GET /users/me`, `GET /users/{id}` | Usuario | Perfil |
| IAM | `POST /users` | ADMIN | Invita un miembro (contraseña temporal `stockia123`) |
| IAM | `PATCH /users/{id}` | El propio usuario | Nombre y email |
| IAM | `POST /users/{id}/change-password` | El propio usuario | Verifica la contraseña actual |
| IAM | `PATCH /users/{id}/role`, `DELETE /users/{id}` | ADMIN | Siempre queda al menos un Administrador |
| IAM | `GET /restaurants/current` / `PUT` | Usuario / ADMIN | Datos del restaurante |
| Subscription | `GET /plans` | Pública | Planes (Esencial, Profesional, IoT Completo) |
| Subscription | `GET /subscriptions/current`, `POST /subscriptions` | ADMIN | Plan actual / cambiar plan |
| Inventory | `GET/POST /inventory-items`, `GET/PUT/DELETE /inventory-items/{id}` | Usuario | Insumos (lotes) |
| Inventory | `GET/POST /recipes`, `PUT/DELETE /recipes/{id}` | Usuario | Platos; `price` = suma del costo de sus ingredientes |
| Sales | `GET /sales?from&to`, `GET /sales/{id}` | Usuario | Historial (más reciente primero) |
| Sales | `POST /sales` | Usuario | `{recipeId, quantity, channel}`; 422 `CannotSell` si falta stock |
| Sales | `POST /sales/{id}/void` | Usuario | Anula y devuelve el stock; 409 si ya estaba anulada |
| Forecast | `GET /demand-forecasts`, `GET /demand-forecasts/latest` | Usuario | Pronósticos |
| Forecast | `POST /demand-forecasts` | Usuario | `{horizonDays}` (1–7) genera uno nuevo |
| Forecast | `GET /demand-forecasts/model` | Usuario | Versión, métricas y estado del modelo |
| Alerts | `GET/POST /alerts`, `PUT/DELETE /alerts/{id}` | Usuario | Alertas |
| Alerts | `POST /alerts/{id}/deliveries`, `POST /alerts/{id}/acknowledge` | Usuario | Envío por canal / confirmar lectura |
| Alerts | `GET /recommendations`, `POST /recommendations/{id}/apply` | ADMIN | Recomendaciones de compra y menú |
| Dashboard | `GET /dashboard/summary` | Usuario | KPIs del día |

El archivo `DataBit.StockIA.Platform/DataBit.StockIA.Platform.http` trae ejemplos listos para usar.

---

## 3. Reglas de negocio implementadas

- **Ventas sin stock no se registran.** Antes de vender se revisan todos los ingredientes de la receta;
  si falta alguno, la respuesta es 422 con el motivo de cada insumo: `NOT_FOUND`, `EXPIRED` o
  `INSUFFICIENT`. No se hacen descuentos parciales.
- **Lotes vencidos no se usan** y, entre los lotes válidos de un mismo producto, se consume primero
  el **más fresco** (el que vence más tarde), tal como pidió el equipo.
- **Precio del plato** = Σ (cantidad del ingrediente × costo unitario del lote que se usaría).
- **Cantidades exactas con 32 decimales** en insumos, recetas y descuentos.
- **Roles:** Administrador y Empleado. El empleado no ve ni llama a equipo, recomendaciones,
  suscripción ni datos del restaurante (la API responde 403 aunque se llame directamente).
- **Alertas automáticas:** sin stock, bajo el mínimo, por vencer (≤ 3 días) y vencido. Se evalúan al
  cambiar el stock y cada hora (`Alerts:SweepIntervalMinutes`). No se duplican alertas abiertas.
  Una alerta **CRITICAL** solo se puede confirmar después de entregarse por WhatsApp y email
  (envío simulado; 409 `NotDelivered` si falta).
- **Recomendaciones:** cada pronóstico calcula cuánto se necesitará de cada insumo en el horizonte; si
  supera el stock usable, propone "Comprar X kg de … antes del dd/MM". Si un plato sube ≥ 15 % o
  baja ≥ 20 % frente a su promedio, propone cambios de menú.
- **Registro (sign-up):** crea restaurante, Administrador y suscripción gratuita en una sola transacción.

---

## 4. Cómo ejecutarla

### Opción A — Docker (recomendada)

```bash
docker compose up --build
```

Levanta MySQL 8.4 y la API en `http://localhost:5000` (Swagger en `/swagger`). La base se crea y se
llena con los datos demo en el primer arranque.

### Opción B — .NET local

Requisitos: .NET SDK 10 y un MySQL 8 local (`root`/`root`, puerto 3306; ver
`appsettings.Development.json`).

```bash
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
| `PORT` | `8080` | Puerto (Render/Railway lo definen solos) |

Otras opciones en `appsettings.json`: `Seed:DemoData`, `Swagger:Enabled`, `Weather:Enabled`
(si es `false` usa la climatología de Lima), `Restaurant:TimeZone`, `Iam:InvitationTemporaryPassword`.

### Cuentas demo

| Rol | Email | Contraseña |
|---|---|---|
| Administrador | `admin@databitecorp.com` | `stockia123` |
| Empleado | `empleado@databitecorp.com` | `stockia123` |

El seed crea el restaurante "DataBite Corp — Restaurante Demo", 10 insumos (con un lote vencido, uno
por vencer, uno sin stock y dos lotes de pollo para mostrar la regla del lote más fresco), 4 recetas,
120 días de ventas y un primer pronóstico ML.

---

## 5. Conexión con la Web Application

En el repo `stockia-webapp`, rama `feature/platform-api`:

```bash
npm install
npm run dev          # usa VITE_STOCKIA_API_URL=http://localhost:5000/api/v1 (.env.development)
```

Para producción se cambia `VITE_STOCKIA_API_URL` en `.env.production` (o en las variables del hosting,
por ejemplo Vercel) y se agrega el dominio de la Web App a `STOCKIA_CORS_ORIGINS` (los `*.vercel.app`
ya están permitidos).

Lo que cambió en la Web App: guarda el JWT en `localStorage` (`stockia.session`), lo envía en cada
llamada, cierra la sesión con aviso cuando la API responde 401, y las reglas de venta, alertas, pronóstico
y suscripción ahora se hacen en la API (la Web App solo muestra los mensajes de error traducidos).

---

## 6. Pruebas y CI

| Qué | Dónde | Cómo correrlo |
|---|---|---|
| Pruebas unitarias (Quantity, asignación de stock, calendario peruano, alertas, **paridad del modelo ONNX con Python**) | `DataBit.StockIA.Platform.Tests` | `dotnet test` |
| Smoke test de la API (53 verificaciones) | `tests/smoke/smoke_test.py` | `python tests/smoke/smoke_test.py http://localhost:5000` |
| E2E Web App + API (Playwright, 25 verificaciones, capturas en `tests/e2e/screenshots`) | `tests/e2e/webapp_e2e.py` | `python tests/e2e/webapp_e2e.py http://localhost:4173` |
| Pruebas del pipeline ML (16) | `ml/tests` | `cd ml && python -m pytest` |

Workflows de GitHub Actions:
- **`ci.yml`** (cada push/PR): compila y prueba; luego levanta MySQL + API, corre el smoke test,
  descarga la Web App, la compila y la prueba con Playwright contra la API real. Se puede lanzar a mano
  indicando otra rama de la Web App (`webapp_ref`).
- **`ml.yml`** (cambios en `ml/` o a mano): pruebas del pipeline y reentrenamiento con clima real.

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
2. El `.csproj` copia esos archivos a la salida y `OnnxDemandModel` los carga con
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

## 8. Despliegue (propuesta)

| Pieza | Opción gratuita sugerida |
|---|---|
| API | Render o Railway con el `Dockerfile` (puerto `PORT`) |
| MySQL | Aiven for MySQL (plan gratuito) o Railway MySQL, con `STOCKIA_DB_SSLMODE=Required` |
| Web App | Vercel, con `VITE_STOCKIA_API_URL=https://<api>/api/v1` |

---

## 9. Pendientes / simulaciones

- Pagos (Stripe/PayPal) y envío de alertas por WhatsApp/email están **simulados** detrás de
  interfaces (`IPaymentGateway`, `INotificationSender`); se reemplazan sin tocar el dominio.
- Sensores IoT del plan "IoT Completo": fuera del alcance de esta versión.
