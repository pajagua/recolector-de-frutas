"""Espacio de trabajo de CitriBot: ¿la esfera del árbol entra en el alcance del brazo?

Uso (desde la raíz del repo):
    python control/espacio_trabajo.py                    # informe en consola
    python control/espacio_trabajo.py --carro 50         # con un carro de riel de 50 mm de alto
    python control/espacio_trabajo.py --figura           # además guarda docs/imagenes/espacio_trabajo.png

Requiere numpy (y matplotlib para la figura). El método está explicado en docs/espacio_de_trabajo.md.

Resumen del método:
1. El riel (J0) pone el robot frente a cualquier punto del árbol, así que alcanza con mirar el corte
   vertical perpendicular al riel: y = distancia horizontal desde el eje J1 hacia el árbol, z = altura.
2. En ese corte, con J1 apuntando el brazo al árbol, la ventosa recorre un sector de corona circular
   con centro en el eje J2: radios entre RHO_MIN y RHO_MAX (J3) y ángulos entre J2_MIN y J2_MAX (J2).
3. La esfera entra si todos sus puntos caen dentro de ese sector.
"""
import argparse
import math
from pathlib import Path

import numpy as np

# ---- requisito (TPF + boceto del grupo), mm ----
D_ESFERA = 300.0      # esfera de 30 cm de diámetro
SEPARACION = 50.0     # entre el borde de la base y la esfera
Z_CENTRO = 350.0      # suelo -> centro de la esfera

# ---- robot (medido en cad/solidworks, CB-000 v7), mm ----
BORDE = 194.8         # eje J1 -> borde del tablero del lado del árbol (el más cercano, lado de la bandeja de electrónica)
BORDE_OPUESTO = 355.2 # eje J1 -> borde opuesto (solo para la figura)
Z_TABLERO = 80.0      # suelo -> cara superior del tablero (patas + tablero, sin riel)
Z_J2 = 355.5          # cara superior del tablero -> eje J2
U_BRIDA = 105.0       # eje J2 -> cara exterior de la brida de E3 con J3 retraído
CARRERA = 340.0       # J3 (v7: tubo de 440 mm movido por correa GT2)
HERRAMIENTA = 122.8   # brida -> labio de la ventosa SAB40, alineada con el brazo (cabeceo -90°), según el .scad v4.2
J2_MIN, J2_MAX = -45.0, 45.0   # elevación del brazo sin choques en toda la carrera de J3, apuntando al árbol (Sweep7.cs, cada 15°)

RHO_MIN = U_BRIDA + HERRAMIENTA
RHO_MAX = U_BRIDA + CARRERA + HERRAMIENTA


def calcular(carro=0.0):
    """Devuelve los puntos de la esfera (corte central) con su distancia y ángulo respecto del eje J2."""
    R = D_ESFERA / 2
    yc = BORDE + SEPARACION + R
    zh = Z_TABLERO + carro + Z_J2
    # el corte central de la esfera contiene a todos los demás cortes (los que no pasan por el centro son discos más chicos)
    g = np.linspace(-R, R, 301)
    Y, Z = np.meshgrid(g, g)
    adentro = Y ** 2 + Z ** 2 <= R ** 2
    y, z = yc + Y[adentro], Z_CENTRO + Z[adentro]
    rho = np.hypot(y, z - zh)
    theta = np.degrees(np.arctan2(z - zh, y))
    ok = (rho >= RHO_MIN) & (rho <= RHO_MAX) & (theta >= J2_MIN) & (theta <= J2_MAX)
    return dict(R=R, yc=yc, zh=zh, y=y, z=z, rho=rho, theta=theta, ok=ok)


def informe(carro):
    r = calcular(carro)
    d = math.hypot(r["yc"], Z_CENTRO - r["zh"])
    print(f"Carro del riel: {carro:.0f} mm  ->  eje J2 a {r['zh']:.1f} mm del suelo")
    print(f"Esfera: centro a {r['yc']:.0f} mm del eje J1 y {Z_CENTRO:.0f} mm de altura (a {d:.1f} mm del eje J2)")
    print(f"  necesita  rho {r['rho'].min():6.1f} .. {r['rho'].max():6.1f} mm   J2 {r['theta'].min():6.1f} .. {r['theta'].max():6.1f} grados")
    print(f"  tiene     rho {RHO_MIN:6.1f} .. {RHO_MAX:6.1f} mm   J2 {J2_MIN:6.1f} .. {J2_MAX:6.1f} grados")
    print(f"  margen    cerca {r['rho'].min() - RHO_MIN:5.1f} mm, lejos {RHO_MAX - r['rho'].max():5.1f} mm")
    print(f"  puntos de la esfera alcanzables: {100 * r['ok'].mean():.1f} %")
    return r


def figura(carro, salida):
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    from matplotlib.patches import Circle, Polygon, Rectangle, Wedge

    r = calcular(carro)
    zh = r["zh"]
    fig, ax = plt.subplots(figsize=(10, 6.2), dpi=150)
    # sector alcanzable por la ventosa
    ax.add_patch(Wedge((0, zh), RHO_MAX, J2_MIN, J2_MAX, width=RHO_MAX - RHO_MIN, color="#9ecae1", alpha=0.45,
                       label=f"alcance de la ventosa: {RHO_MIN:.0f}–{RHO_MAX:.0f} mm, J2 {J2_MIN:.0f}° a +{J2_MAX:.0f}°"))
    # esfera del árbol
    ax.add_patch(Circle((r["yc"], Z_CENTRO), r["R"], fill=False, lw=2, ec="#e6550d", label="esfera del árbol (Ø300)"))
    # base: patas + tablero y torre simplificada
    zt = Z_TABLERO + carro
    ax.add_patch(Rectangle((-BORDE_OPUESTO, zt - 16), BORDE_OPUESTO + BORDE, 16, color="#c8a97e"))
    ax.add_patch(Rectangle((-BORDE_OPUESTO, 0), BORDE_OPUESTO + BORDE, zt - 16, fill=False, hatch="//", ec="#999999", lw=0))
    ax.add_patch(Polygon([(-80, zt), (80, zt), (80, zt + 80), (45, zt + 140), (45, zh), (-45, zh), (-45, zt + 140), (-80, zt + 80)],
                         color="#888888", alpha=0.8))

    def brazo(elev, q3, alpha):
        c, s = math.cos(math.radians(elev)), math.sin(math.radians(elev))

        def caja(u0, u1, h, color):
            pts = [(u0, -h), (u1, -h), (u1, h), (u0, h)]
            ax.add_patch(Polygon([(u * c - v * s, zh + u * s + v * c) for u, v in pts], color=color, alpha=alpha))
        caja(-377, 93, 27.5, "#f0f0f0")                      # E2 (cola corta; el NEMA 17 va arriba del cubo)
        caja(-345 + q3, 95 + q3, 15, "#b0b8c8")              # tubo de E3 (440 mm)
        caja(95 + q3, 105 + q3, 40, "#fd8d3c")               # brida
        caja(105 + q3, RHO_MIN + q3, 22, "#555555")          # herramienta con la ventosa alineada
        ax.plot([0, (RHO_MIN + q3) * c], [zh, zh + (RHO_MIN + q3) * s], color="k", lw=0.6, ls=":")

    brazo(-8, 0, 0.55)
    brazo(-8, CARRERA, 0.35)
    ax.plot(0, zh, "ko")
    ax.annotate("eje J2", (0, zh), (15, zh + 40))
    ax.annotate("", (BORDE, 40), (BORDE + SEPARACION, 40), arrowprops=dict(arrowstyle="<->"))
    ax.text(BORDE + SEPARACION / 2, 52, f"{SEPARACION:.0f}", ha="center")
    ax.axhline(0, color="k", lw=1)
    ax.axvline(0, color="k", lw=0.5, ls="--")
    ax.text(5, 8, "eje J1", fontsize=8)
    ax.set_aspect("equal")
    ax.set_xlim(-470, 720)
    ax.set_ylim(0, 900)
    ax.set_xlabel("distancia horizontal desde el eje J1, hacia el árbol [mm]")
    ax.set_ylabel("altura sobre el suelo [mm]")
    ax.set_title(f"CitriBot v7 – corte perpendicular al riel (carro de {carro:.0f} mm). Brazo a −8°, J3 retraído y extendido")
    ax.legend(loc="upper left", fontsize=8)
    ax.grid(alpha=0.3)
    fig.tight_layout()
    fig.savefig(salida)
    print(f"figura: {salida}")


if __name__ == "__main__":
    p = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    p.add_argument("--carro", type=float, default=0.0, help="alto del carro del riel en mm (sube todo el robot)")
    p.add_argument("--figura", action="store_true", help="guardar docs/imagenes/espacio_trabajo.png")
    a = p.parse_args()
    informe(a.carro)
    if a.figura:
        figura(a.carro, Path(__file__).resolve().parents[1] / "docs" / "imagenes" / "espacio_trabajo.png")
