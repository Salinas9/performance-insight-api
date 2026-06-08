# Performance Insight API

API desarrollada en **Python + FastAPI** para analizar muestras de rendimiento, procesar resultados de **JMeter** y mostrar un **dashboard web** con métricas, problemas detectados y recomendaciones.

## Qué hace

La API permite:

- recibir muestras manuales en JSON;
- subir un archivo `.jtl/.csv` generado por JMeter;
- calcular media, mínimo, máximo, P95, throughput y tasa de errores;
- detectar endpoints problemáticos;
- mostrar un dashboard HTML con gráficos SVG sin depender de un frontend separado.

## Estructura principal

```text
api/
  index.py
app/
  index.py
  main.py
  models/
  routes/
  services/
scripts/
  run-fastapi.ps1
  run-jmeter-smoke.ps1
TestPlans/
  HttpBin-Smoke.jmx
requirements.txt
vercel.json
```

## Requisitos

- Python 3.12
- JMeter 5.6.3

## Instalación local

```powershell
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
```

## Ejecutar la API

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-fastapi.ps1
```

La API quedará disponible en:

- `http://127.0.0.1:8000/`
- `http://127.0.0.1:8000/docs`

## Endpoints

- `GET /`
- `GET /health`
- `GET /api/performance/health`
- `GET /api/performance/example`
- `GET /api/performance/dashboard-data`
- `GET /api/performance/current-report`
- `POST /api/performance/analyze`
- `POST /api/performance/dashboard`
- `POST /api/performance/jmeter/upload`

## Ejemplo de análisis manual

```json
{
  "samples": [
    {
      "endpoint": "/api/users",
      "timeMs": 120,
      "success": true,
      "timestamp": "2026-06-08T18:00:00Z",
      "responseCode": "200",
      "responseMessage": "OK",
      "threadName": "manual-1"
    },
    {
      "endpoint": "/api/users",
      "timeMs": 980,
      "success": false,
      "timestamp": "2026-06-08T18:00:05Z",
      "responseCode": "500",
      "responseMessage": "Server Error",
      "threadName": "manual-1"
    }
  ]
}
```

## Pruebas con JMeter

El proyecto incluye:

- un plan de prueba de ejemplo en `TestPlans/HttpBin-Smoke.jmx`;
- un script para lanzar JMeter y subir automáticamente el `.jtl` a la API;
- un dashboard que interpreta el último informe cargado.

Ejecutar prueba:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-jmeter-smoke.ps1 -Domain dummyjson.com -Threads 1 -Loops 1
```

Esto hace:

1. ejecuta JMeter en modo no gráfico;
2. genera `results.jtl`;
3. sube el fichero a `POST /api/performance/jmeter/upload`;
4. actualiza el dashboard.

## Dashboard

El dashboard se genera en el servidor con:

- HTML
- CSS
- SVG inline

No necesita React, Angular ni librerías de charting externas.

## Despliegue en Vercel

La estructura está preparada para **Vercel** con `FastAPI`.

Archivos clave:

- `index.py`
- `requirements.txt`
- `vercel.json`
- `.python-version`

Pasos:

1. subir el repositorio a GitHub;
2. importarlo en Vercel;
3. dejar que Vercel detecte Python;
4. desplegar.

## Limitación importante en Vercel

El almacenamiento del “último informe” es **en memoria**. En local funciona bien. En Vercel, al ser un entorno más efímero, ese estado puede perderse entre invocaciones.

Para una versión académica es válido, pero para producción sería mejor guardar el informe en una base de datos o almacenamiento externo.

## Documentación adicional

- [JMETER-WORKFLOW.md](C:\Users\Cynthia\Desktop\PerformanceInsight.Api\JMETER-WORKFLOW.md)
