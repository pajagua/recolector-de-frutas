import sys
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(RAIZ / "src"))

import cv2
import numpy as np

from citribot_vision.camaras import abrir_par, cargar_config, leer_par

cfg = cargar_config()
t = cfg["tablero"]
patron = (t["columnas"], t["filas"])
dir_i = RAIZ / "data" / "calibracion_imagenes" / "izq"
dir_d = RAIZ / "data" / "calibracion_imagenes" / "der"

izq, der = abrir_par(cfg)
n = len(list(dir_i.glob("*.png")))
print(f"ESPACIO guarda el par (solo si el tablero se ve en ambas), q sale. Ya hay {n} pares.")

while True:
    fi, fd = leer_par(izq, der)
    if fi is None:
        continue
    gi = cv2.cvtColor(fi, cv2.COLOR_BGR2GRAY)
    gd = cv2.cvtColor(fd, cv2.COLOR_BGR2GRAY)
    ok_i, ci = cv2.findChessboardCorners(gi, patron, cv2.CALIB_CB_FAST_CHECK)
    ok_d, cd = cv2.findChessboardCorners(gd, patron, cv2.CALIB_CB_FAST_CHECK)

    vi, vd = fi.copy(), fd.copy()
    if ok_i:
        cv2.drawChessboardCorners(vi, patron, ci, ok_i)
    if ok_d:
        cv2.drawChessboardCorners(vd, patron, cd, ok_d)
    vista = cv2.resize(np.hstack([vi, vd]), None, fx=0.5, fy=0.5)
    cv2.putText(vista, f"pares: {n}  tablero: {'OK' if ok_i and ok_d else '--'}", (10, 25),
                cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 0) if ok_i and ok_d else (0, 0, 255), 2)
    cv2.imshow("calibracion", vista)

    k = cv2.waitKey(1) & 0xFF
    if k == ord("q"):
        break
    if k == ord(" ") and ok_i and ok_d:
        cv2.imwrite(str(dir_i / f"{n:03d}.png"), fi)
        cv2.imwrite(str(dir_d / f"{n:03d}.png"), fd)
        n += 1

izq.release()
der.release()
cv2.destroyAllWindows()
