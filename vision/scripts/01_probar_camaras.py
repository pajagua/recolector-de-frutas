import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "src"))

import cv2
import numpy as np

from citribot_vision.camaras import abrir_par, cargar_config, leer_par

cfg = cargar_config()
izq, der = abrir_par(cfg)
print("Resolución izq:", izq.get(cv2.CAP_PROP_FRAME_WIDTH), "x", izq.get(cv2.CAP_PROP_FRAME_HEIGHT))
print("Resolución der:", der.get(cv2.CAP_PROP_FRAME_WIDTH), "x", der.get(cv2.CAP_PROP_FRAME_HEIGHT))
print("q para salir")

while True:
    fi, fd = leer_par(izq, der)
    if fi is None:
        continue
    vista = np.hstack([fi, fd])
    vista = cv2.resize(vista, None, fx=0.5, fy=0.5)
    cv2.imshow("izq | der", vista)
    if cv2.waitKey(1) & 0xFF == ord("q"):
        break

izq.release()
der.release()
cv2.destroyAllWindows()
