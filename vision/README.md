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

## Montaje de las cámaras

Las dos **Logitech C270** van en un mástil fijo a la base (CAD: `cad/solidworks/01_Base_J0/CB-150_Modulo_Camaras.SLDASM`, detalle en
[cad/solidworks/README.md §6.11](../cad/solidworks/README.md#611-cámaras-estéreo-cb-150--v71)). Valores nominales en `config/camaras.yaml` (`montaje`).

- 120 mm entre lentes, paralelas, a 670 mm sobre el tablero y a 774 mm del centro de la esfera de trabajo.
- **Pose de observación del brazo**: J3 retraído y J2 = −45°. En otras poses la herramienta tapa parte de la esfera.
- Sacar las fotos con el robot quieto: las dos webcams no están sincronizadas.
- Fijar exposición y balance de blancos, y usar MJPEG o puertos USB distintos para no saturar el ancho de banda.
- Medir el campo de visión real con el damero (en 720p la C270 puede recortar el sensor; en 640 × 480 suele ver más en vertical).
- Calibración cámara → robot: damero en la brida de E3, en varias poses conocidas del brazo.
