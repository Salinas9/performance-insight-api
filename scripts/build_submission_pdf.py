from __future__ import annotations

from datetime import datetime
from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.enums import TA_LEFT
from reportlab.lib.pagesizes import LETTER
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import inch
from reportlab.platypus import (
    Paragraph,
    SimpleDocTemplate,
    Spacer,
    Table,
    TableStyle,
)


ROOT = Path(__file__).resolve().parents[1]
OUTPUT_DIR = ROOT / "docs"
PDF_PATH = OUTPUT_DIR / "Performance_Insight_API_Memoria.pdf"


def build_pdf() -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    doc = SimpleDocTemplate(
        str(PDF_PATH),
        pagesize=LETTER,
        leftMargin=inch,
        rightMargin=inch,
        topMargin=inch,
        bottomMargin=inch,
    )

    styles = getSampleStyleSheet()
    styles.add(
        ParagraphStyle(
            name="BodySmall",
            parent=styles["BodyText"],
            fontName="Helvetica",
            fontSize=10.5,
            leading=14,
            spaceAfter=8,
            alignment=TA_LEFT,
        )
    )
    styles.add(
        ParagraphStyle(
            name="TitleLarge",
            parent=styles["Title"],
            fontName="Helvetica-Bold",
            fontSize=24,
            leading=28,
            textColor=colors.black,
            alignment=TA_LEFT,
        )
    )
    styles.add(
        ParagraphStyle(
            name="SubTitleGray",
            parent=styles["BodyText"],
            fontName="Helvetica",
            fontSize=13,
            leading=16,
            textColor=colors.HexColor("#555555"),
            spaceAfter=14,
        )
    )
    styles["Heading1"].fontName = "Helvetica-Bold"
    styles["Heading1"].fontSize = 16
    styles["Heading1"].textColor = colors.HexColor("#2E74B5")
    styles["Heading1"].spaceAfter = 8
    styles["Heading2"].fontName = "Helvetica-Bold"
    styles["Heading2"].fontSize = 13
    styles["Heading2"].textColor = colors.HexColor("#2E74B5")
    styles["Heading2"].spaceAfter = 6

    story = []
    story.append(Paragraph("MEMORIA TÉCNICA", styles["BodySmall"]))
    story.append(Paragraph("Performance Insight API en Python", styles["TitleLarge"]))
    story.append(
        Paragraph(
            "Migración desde ASP.NET, análisis de resultados de JMeter y estructura preparada para Vercel.",
            styles["SubTitleGray"],
        )
    )

    metadata = [
        ("Autora", "Cynthia"),
        ("Fecha", datetime.now().strftime("%d/%m/%Y")),
        ("Tecnologías", "Python 3.12, FastAPI, JMeter 5.6.3, Vercel"),
        ("Estado", "Migración completada y validada en local"),
    ]
    for label, value in metadata:
        story.append(Paragraph(f"<b>{label}:</b> {value}", styles["BodySmall"]))

    story.append(Spacer(1, 10))
    story.append(Paragraph("Objetivo del proyecto", styles["Heading1"]))
    story.append(
        Paragraph(
            "El objetivo del proyecto es ofrecer una API que analice resultados de rendimiento, acepte muestras manuales o ficheros de JMeter y muestre un dashboard entendible con métricas técnicas y recomendaciones.",
            styles["BodySmall"],
        )
    )

    story.append(Paragraph("Migración realizada", styles["Heading1"]))
    story.append(
        Paragraph(
            "La versión original estaba construida en ASP.NET. En esta migración se ha trasladado la API a Python usando FastAPI para facilitar un despliegue posterior en Vercel, manteniendo la idea funcional del proyecto.",
            styles["BodySmall"],
        )
    )
    for bullet in [
        "Se ha creado una entrada Vercel en api/index.py.",
        "La lógica se ha separado en modelos, rutas y servicios.",
        "Se ha mantenido el parser de JMeter y el dashboard HTML generado en servidor.",
        "Se ha preparado un requirements.txt y un vercel.json para despliegue.",
    ]:
        story.append(Paragraph(f"• {bullet}", styles["BodySmall"]))

    story.append(Paragraph("Arquitectura de la solución", styles["Heading1"]))
    story.append(
        Paragraph(
            "La aplicación queda organizada de forma modular. El fichero api/index.py expone la app a Vercel. app/main.py crea la instancia FastAPI, app/routes/performance.py publica los endpoints y app/services concentra el análisis, el parser de JMeter, la generación del dashboard y el almacenamiento del último informe.",
            styles["BodySmall"],
        )
    )

    story.append(Paragraph("Endpoints principales", styles["Heading2"]))
    endpoint_rows = [
        ["Método", "Ruta", "Descripción"],
        ["GET", "/", "Muestra el dashboard HTML generado por el servidor."],
        ["GET", "/api/performance/health", "Comprueba que la API está viva."],
        ["GET", "/api/performance/current-report", "Devuelve el último informe procesado."],
        ["POST", "/api/performance/analyze", "Analiza una carga manual enviada en JSON."],
        ["POST", "/api/performance/dashboard", "Calcula y guarda el dashboard desde muestras JSON."],
        ["POST", "/api/performance/jmeter/upload", "Sube y procesa un .jtl/.csv de JMeter."],
    ]
    endpoint_table = Table(endpoint_rows, colWidths=[0.9 * inch, 2.1 * inch, 3.5 * inch])
    endpoint_table.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#F2F4F7")),
                ("TEXTCOLOR", (0, 0), (-1, 0), colors.black),
                ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
                ("FONTNAME", (0, 1), (-1, -1), "Helvetica"),
                ("FONTSIZE", (0, 0), (-1, -1), 9.5),
                ("GRID", (0, 0), (-1, -1), 0.5, colors.HexColor("#D3DAE6")),
                ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
                ("LEFTPADDING", (0, 0), (-1, -1), 6),
                ("RIGHTPADDING", (0, 0), (-1, -1), 6),
                ("TOPPADDING", (0, 0), (-1, -1), 6),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 6),
            ]
        )
    )
    story.append(endpoint_table)
    story.append(Spacer(1, 12))

    story.append(Paragraph("Flujo de uso", styles["Heading1"]))
    for line in [
        "python -m venv .venv",
        ".\\.venv\\Scripts\\python.exe -m pip install -r requirements.txt",
        "powershell -ExecutionPolicy Bypass -File .\\scripts\\run-fastapi.ps1",
        "powershell -ExecutionPolicy Bypass -File .\\scripts\\run-jmeter-smoke.ps1 -Domain dummyjson.com -Threads 1 -Loops 1",
    ]:
        story.append(Paragraph(f"<font name='Courier'>{line}</font>", styles["BodySmall"]))

    story.append(Paragraph("Resultados de las pruebas JMeter", styles["Heading1"]))
    result_rows = [
        ["Escenario", "Target", "Muestras", "Media", "P95", "Máx", "Errores", "Lectura"],
        ["Smoke local", "GET /api/performance/health", "6", "1.33 ms", "2 ms", "2 ms", "0%", "Estado OK; la subida y el parser funcionan."],
        ["Smoke público", "dummyjson.com", "4", "555.5 ms", "1640 ms", "1640 ms", "0%", "Se detecta latencia alta en /products?delay=1500."],
    ]
    result_table = Table(
        result_rows,
        colWidths=[1.05 * inch, 1.55 * inch, 0.65 * inch, 0.75 * inch, 0.7 * inch, 0.7 * inch, 0.75 * inch, 2.15 * inch],
    )
    result_table.setStyle(
        TableStyle(
            [
                ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#F2F4F7")),
                ("TEXTCOLOR", (0, 0), (-1, 0), colors.black),
                ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
                ("FONTNAME", (0, 1), (-1, -1), "Helvetica"),
                ("FONTSIZE", (0, 0), (-1, -1), 9),
                ("GRID", (0, 0), (-1, -1), 0.5, colors.HexColor("#D3DAE6")),
                ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
                ("LEFTPADDING", (0, 0), (-1, -1), 5),
                ("RIGHTPADDING", (0, 0), (-1, -1), 5),
                ("TOPPADDING", (0, 0), (-1, -1), 6),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 6),
            ]
        )
    )
    story.append(result_table)
    story.append(Spacer(1, 12))

    story.append(Paragraph("Despliegue en Vercel", styles["Heading1"]))
    story.append(
        Paragraph(
            "La estructura del proyecto queda preparada para desplegarse en Vercel gracias a FastAPI. El repositorio incorpora requirements.txt, .python-version, vercel.json y el entrypoint api/index.py. El siguiente paso operativo consiste en subir el repositorio a GitHub y conectarlo a una cuenta de Vercel.",
            styles["BodySmall"],
        )
    )
    for bullet in [
        "Vercel detecta Python y usa api/index.py como función.",
        "La API puede servirse sin necesidad de mantener ASP.NET.",
        "El estado del último informe está en memoria; para producción convendría persistencia externa.",
    ]:
        story.append(Paragraph(f"• {bullet}", styles["BodySmall"]))

    story.append(Paragraph("Conclusiones", styles["Heading1"]))
    story.append(
        Paragraph(
            "La migración a Python cumple el objetivo de acercar el proyecto a un despliegue compatible con Vercel. Además, se mantiene el valor académico principal de la práctica: construir una API propia, someterla a pruebas con JMeter y analizar los resultados mediante métricas y visualización.",
            styles["BodySmall"],
        )
    )
    story.append(
        Paragraph(
            "Como mejora futura, sería recomendable desplegarla públicamente, añadir almacenamiento persistente para los informes y guardar histórico de ejecuciones para comparar campañas de rendimiento.",
            styles["BodySmall"],
        )
    )

    doc.build(story)


if __name__ == "__main__":
    build_pdf()
