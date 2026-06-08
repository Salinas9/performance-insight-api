from fastapi import FastAPI

from app.routes.performance import router as performance_router


app = FastAPI(
    title="Performance Insight API",
    description="API para analizar resultados de rendimiento y visualizar dashboards a partir de muestras manuales o resultados de JMeter.",
    version="2.0.0",
)

app.include_router(performance_router)
