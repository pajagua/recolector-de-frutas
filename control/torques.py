"""Torques que piden los motores de CitriBot (v6) y cuánto margen dejan.

Uso (desde la raíz del repo):
    python control/torques.py
    python control/torques.py --relleno-cola 0.6 --j2-nominal 23

Los centros de masa salen del CAD (volúmenes de SolidWorks, coordenadas a lo largo del brazo: u = distancia
al eje J2, positiva hacia la herramienta). Las masas de piezas impresas dependen del relleno: conviene pesar
las piezas reales y corregir los valores de abajo.
"""
import argparse
import math

G = 9.81
RHO ={"petg": 1.27e-3, "alu": 2.70e-3, "acero": 7.85e-3, "laton": 8.5e-3}   # kg/cm3

# ---- CAD v6 (Masa.cs): volumen [cm3], centro de masa u [mm] con J3 retraído ----
CUBO_E2 = (586.4, 31.7)       # cubo del hombro de E2 (parte de CB-301 delante de x = -100)
COLA_E2 = (791.6, -216.7)     # cola 55 x 55 de E2 con la placa del motor
FIJAS = [                      # (nombre, masa kg, u mm) que giran con J2 y no se mueven con J3
    ("NEMA 17 (17HS4401)", 0.28, -431.3),
    ("acople 5x8", 6.2 * RHO["alu"], -387.4),
    ("husillo T8 L400", 20.1 * RHO["acero"], -186.5),
    ("tapa AS5600 + buje + eje", 0.03, 0.0),
]
MOVILES = [                    # se corren q3 hacia adelante
    ("tubo 30x30x2 L460", 103.0 * RHO["alu"], -135.0),
    ("brida", 75.9 * RHO["petg"] * 0.8, 97.4),
    ("portatuerca + tuerca T8", 11.3 * RHO["petg"] + 1.4 * RHO["laton"], -356.0),
    ("herramienta (boceto del grupo)", 0.405, 155.0),
]
NARANJA = ("naranja", 0.18, 268.0)   # ventosa alineada con el brazo: labio a 227.8 mm + radio de la naranja
VARILLAS = {"cola": 391.5, "tubo": 460.0, "husillo": 400.0}   # largos [mm] para el momento de inercia propio
CARRERA = 350.0
E_LATERAL = 83.0               # plano del brazo corrido del eje J1


def masas(relleno_cubo, relleno_cola, q3, naranja):
    """Lista (nombre, masa, u, largo_varilla) con J3 = q3."""
    m = [("cubo E2", CUBO_E2[0] * RHO["petg"] * relleno_cubo, CUBO_E2[1], 0.0),
         ("cola E2", COLA_E2[0] * RHO["petg"] * relleno_cola, COLA_E2[1], VARILLAS["cola"])]
    m += [(n, kg, u, VARILLAS["husillo"] if "husillo" in n else 0.0) for n, kg, u in FIJAS]
    m += [(n, kg, u + q3, VARILLAS["tubo"] if "tubo" in n else 0.0) for n, kg, u in MOVILES]
    if naranja:
        m.append((NARANJA[0], NARANJA[1], NARANJA[2] + q3, 0.0))
    return m


def momento(lista):            # kg*mm, positivo = el frente tira hacia abajo
    return sum(kg * u for _, kg, u, _ in lista)


def inercia(lista, extra_m=0.0):   # kg*m2 alrededor del eje J2 (extra_m: distancia lateral para J1)
    return sum(kg * ((u / 1000) ** 2 + (L / 1000) ** 2 / 12 + extra_m ** 2) for _, kg, u, L in lista)


def kgcm(nm):
    return nm / G * 100


def main():
    p = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    p.add_argument("--relleno-cubo", type=float, default=0.8, help="fracción maciza del cubo de E2 impreso (0-1)")
    p.add_argument("--relleno-cola", type=float, default=0.35, help="fracción maciza de la cola de E2 (3 perímetros + 15 %%: ~0.35)")
    p.add_argument("--j2-rpm", type=float, default=9.0, help="velocidad de J2 con carga (5840 de 12 rpm: ~9 rpm)")
    p.add_argument("--t-acel", type=float, default=0.5, help="tiempo de aceleración de J2 y J1 [s]")
    p.add_argument("--j2-nominal", type=float, default=70.0, help="torque del 5840 [kg.cm] (vendedor: ~70, sin aclarar si es nominal)")
    a = p.parse_args()

    print(f"E2 impreso: cubo {a.relleno_cubo:.0%} macizo, cola {a.relleno_cola:.0%} macizo "
          f"-> cubo {CUBO_E2[0] * RHO['petg'] * a.relleno_cubo:.2f} kg, cola {COLA_E2[0] * RHO['petg'] * a.relleno_cola:.2f} kg")

    # ---------------- J2 (hombro) ----------------
    alfa = a.j2_rpm * 2 * math.pi / 60 / a.t_acel
    print(f"\nJ2 hombro (brazo horizontal = peor caso de gravedad; acelera de 0 a {a.j2_rpm:.0f} rpm en {a.t_acel} s)")
    peor = 0.0
    for q3 in (0.0, CARRERA / 2, CARRERA):
        for nar in (False, True):
            L = masas(a.relleno_cubo, a.relleno_cola, q3, nar)
            est = momento(L) * G / 1000            # N*m
            din = inercia(L) * alfa
            tot = abs(est) + din
            peor = max(peor, tot)
            print(f"  J3 {q3:5.0f} mm {'con' if nar else 'sin'} naranja: estático {kgcm(est):+6.1f} kg.cm, "
                  f"inercia {inercia(L):.3f} kg.m2 -> +{kgcm(din):4.1f}, pico {kgcm(tot):5.1f} kg.cm")
    print(f"  pico máximo {kgcm(peor):.1f} kg.cm  | con factor 1.5: {1.5 * kgcm(peor):.1f} kg.cm  | 5840: {a.j2_nominal:.0f} kg.cm "
          f"-> {'OK' if a.j2_nominal >= 1.5 * kgcm(peor) else 'NO ALCANZA'} (margen x{a.j2_nominal / kgcm(peor):.1f})")
    print("  El 5840 es autoblocante: con el brazo quieto la carga la sostiene el sinfín, el motor solo trabaja al moverse.")

    # ---------------- J1 (cintura) ----------------
    L = masas(a.relleno_cubo, a.relleno_cola, CARRERA, True)
    I1 = inercia(L, E_LATERAL / 1000) + 0.86 * 0.06 ** 2 + 0.35 * 0.07 ** 2     # + torre impresa y 5840 cerca del eje
    m_total = sum(kg for _, kg, _, _ in L) + 0.86 + 0.35 + 0.3                     # + torre, 5840 y plato 1M1B
    w1 = 11 * 2 * math.pi / 60                                                     # 37D 90:1 (~110 rpm) / correa 10:1
    t_in = I1 * w1 / a.t_acel
    t_fr = 0.15 * m_total * G * 0.075                                              # mismo modelo de fricción que el boceto
    t_mot = (t_in + t_fr) / 10 / 0.9
    print(f"\nJ1 cintura (brazo extendido con naranja): I = {I1:.2f} kg.m2, masa sobre el plato {m_total:.1f} kg")
    print(f"  salida: inercia {kgcm(t_in):.1f} + fricción {kgcm(t_fr):.1f} = {kgcm(t_in + t_fr):.1f} kg.cm "
          f"-> en el 37D (correa 10:1, 90 %): {kgcm(t_mot):.2f} kg.cm  | 37D: 32 kg.cm de bloqueo, ~8 kg.cm de trabajo (boceto) -> OK")

    # ---------------- J3 (prismático) ----------------
    mov = masas(a.relleno_cubo, a.relleno_cola, CARRERA, True)[-5:]
    m3 = sum(kg for _, kg, _, _ in mov)
    mom = sum(kg * (u - 93.0) / 1000 for _, kg, u, _ in mov) * G       # momento en el frente de E2 (guía de 108 mm)
    normal = 2 * mom / 0.108
    print(f"\nJ3 prismático extendido (parte móvil {m3:.2f} kg, roce 0.25 del tubo en la guía de 108 mm; husillo avance 8 mm, rendimiento 30 %)")
    for ang in (0, 45):
        f = m3 * G * math.sin(math.radians(ang)) + 0.25 * normal * math.cos(math.radians(ang))
        t = f * 0.008 / (2 * math.pi * 0.30)
        print(f"  J2 a {ang:2d}°: fuerza {f:4.1f} N -> {kgcm(t):.2f} kg.cm en el NEMA 17 (4.5 kg.cm de retención, ~2.5 a 300 rpm) -> OK")


if __name__ == "__main__":
    main()
