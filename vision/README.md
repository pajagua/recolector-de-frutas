# Visión – cámaras estéreo (PC)

```
cd vision
python -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
python scripts/01_probar_camaras.py        # ver las dos cámaras
python scripts/02_capturar_calibracion.py  # fotos del damero para la stereo calibration
```

Índices de cámara, resolución y tamaño del damero: `config/camaras.yaml`.

| Ruta | Qué va |
|---|---|
| `src/citribot_vision/` | código reutilizable (cámaras, calibración, detección, triangulación) |
| `scripts/` | scripts numerados por etapa |
| `data/calibracion/` | resultados de la calibración (matrices) |
| `data/calibracion_imagenes/`, `data/capturas/` | fotos (Git las ignora, solo se guarda la carpeta) |
