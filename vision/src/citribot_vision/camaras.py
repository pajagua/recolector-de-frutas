from pathlib import Path

import cv2
import yaml

RAIZ = Path(__file__).resolve().parents[2]


def cargar_config():
    with open(RAIZ / "config" / "camaras.yaml", encoding="utf-8") as f:
        return yaml.safe_load(f)


def abrir(indice, ancho, alto, fps):
    cap = cv2.VideoCapture(indice, cv2.CAP_DSHOW)
    cap.set(cv2.CAP_PROP_FOURCC, cv2.VideoWriter_fourcc(*"MJPG"))
    cap.set(cv2.CAP_PROP_FRAME_WIDTH, ancho)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, alto)
    cap.set(cv2.CAP_PROP_FPS, fps)
    # Balance de blancos automático apagado. La exposición se fija aparte, una vez probadas las cámaras.
    cap.set(cv2.CAP_PROP_AUTO_WB, 0)
    return cap


def abrir_par(cfg):
    c = cfg["camaras"]
    izq = abrir(c["izq"], c["ancho"], c["alto"], c["fps"])
    der = abrir(c["der"], c["ancho"], c["alto"], c["fps"])
    if not izq.isOpened() or not der.isOpened():
        raise RuntimeError("No se pudo abrir una de las cámaras (revisar índices en camaras.yaml)")
    return izq, der


def leer_par(izq, der):
    # grab() en ambas primero para minimizar el desfase entre las dos imágenes.
    izq.grab()
    der.grab()
    ok_i, fi = izq.retrieve()
    ok_d, fd = der.retrieve()
    return (fi, fd) if ok_i and ok_d else (None, None)
