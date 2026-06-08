# JMeter + Performance Insight API en Python

## Qué hace

Este proyecto ya puede:

- recibir un `.jtl` o `.csv` de JMeter en `POST /api/performance/jmeter/upload`;
- guardar el último informe cargado en memoria;
- mostrar ese informe real en el dashboard `GET /`;
- exponer el informe actual en `GET /api/performance/current-report`.

## 1. Descargar JMeter

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\download-jmeter.ps1
```

## 2. Crear entorno Python e instalar dependencias

```powershell
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
```

## 3. Arrancar la API FastAPI

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-fastapi.ps1
```

## 4. Ejecutar la prueba smoke incluida

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-jmeter-smoke.ps1 -Domain dummyjson.com -Threads 1 -Loops 1
```

## 5. Cambiar target sin tocar el plan

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-jmeter-smoke.ps1 `
  -ApiBaseUrl http://127.0.0.1:8000 `
  -Protocol https `
  -Domain dummyjson.com `
  -Threads 2 `
  -Loops 2
```

## 6. Artefactos

Cada ejecución deja:

- `artifacts/jmeter/<timestamp>/results.jtl`
- `artifacts/jmeter/<timestamp>/html-report/index.html`
- `artifacts/jmeter/<timestamp>/jmeter.log`

## 7. Endpoints útiles

- `GET /`
- `GET /api/performance/dashboard-data`
- `GET /api/performance/current-report`
- `POST /api/performance/jmeter/upload`
- `POST /api/performance/dashboard`

## 8. URLs locales

- Dashboard HTML: `http://127.0.0.1:8000/`
- Swagger: `http://127.0.0.1:8000/docs`
- JSON actual: `http://127.0.0.1:8000/api/performance/current-report`
