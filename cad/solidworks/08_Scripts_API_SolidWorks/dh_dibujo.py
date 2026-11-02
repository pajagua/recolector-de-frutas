import sys
import numpy as np
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt

OUT = sys.argv[1]
# marcos DH en la pose guardada de CB-000 (mm), calculados con Marcos.cs
M = [
    ('{0} base (eje J1)', (0.00, 0.00, 0.00), (1.00, 0.00, 0.00), (0.00, 0.00, 1.00)),
    ('{1} cintura (eje J2)', (0.00, 0.00, 355.46), (0.49, 0.87, 0.00), (0.87, -0.49, 0.00)),
    ('{2} E2 (eje J3)', (72.27, -40.82, 355.46), (-0.26, -0.47, 0.84), (0.42, 0.74, 0.54)),
    ('{3} E3 (eje J4)', (144.01, 86.18, 447.79), (-0.26, -0.47, 0.84), (-0.87, 0.49, 0.00)),
    ('{4} caja (eje J5)', (145.95, 85.15, 447.83), (0.42, 0.75, 0.51), (0.25, 0.44, -0.86)),
    ('{5} ventosa (TCP)', (156.20, 103.28, 412.33), (0.42, 0.75, 0.51), (0.25, 0.44, -0.86)),
]
fig = plt.figure(figsize=(15, 8.5), dpi=110)
ax = fig.add_axes([0.0, 0.0, 0.58, 1.0], projection='3d')
L = 40
o = [np.array(m[1]) for m in M]
# eslabones: base -> J1/J2 -> E2 -> E3 (prismático) -> muñeca -> ventosa
cad = [o[0], o[1], o[2], o[3], o[4], o[5]]
ax.plot(*zip(*cad), color='0.35', lw=4, solid_capstyle='round', zorder=1)
# tablero
b = np.array([[-60, -140, 0], [260, -140, 0], [260, 180, 0], [-60, 180, 0], [-60, -140, 0]])
ax.plot(b[:, 0], b[:, 1], b[:, 2], color='#c8a46e', lw=1.5)
off = [(-40, -60, -30), (-120, -40, 20), (-20, 20, -60), (-40, 40, 55), (50, -40, 25), (30, 30, -55)]
for (name, O, X, Z), dl in zip(M, off):
    O, X, Z = np.array(O), np.array(X), np.array(Z); Y = np.cross(Z, X)
    for v, c in ((X, '#d62728'), (Y, '#2ca02c'), (Z, '#1f4fd6')):
        ax.quiver(*O, *(v * L), color=c, lw=2.2, arrow_length_ratio=0.25)
    ax.text(*(O + np.array(dl)), name, fontsize=10, color='black')
ax.set_box_aspect((1, 1, 1.5))
ax.set_xlim(-60, 260); ax.set_ylim(-140, 180); ax.set_zlim(0, 500)
ax.view_init(elev=14, azim=-35)
ax.set_xlabel('X (mm)'); ax.set_ylabel('Y (mm)'); ax.set_zlabel('Z (mm)')
ax.set_title('CitriBot: sistemas de referencia DH (pose guardada de CB-000)', fontsize=12)

tx = fig.add_axes([0.58, 0.0, 0.42, 1.0]); tx.axis('off')
y = 0.96
tx.text(0, y, 'Ejes:  X rojo,  Y verde,  Z azul  (z$_i$ = eje de la articulación i+1)', fontsize=11); y -= 0.08
tx.text(0, y, 'Parámetros de Denavit-Hartenberg (estándar)', fontsize=12, weight='bold'); y -= 0.06
filas = [('i', 'art.', 'θ$_i$', 'd$_i$ (mm)', 'a$_i$', 'α$_i$'),
         ('1', 'J1 R', 'q$_1$', '355,5', '0', '90°'),
         ('2', 'J2 R', 'q$_2$ + 90°', '83,0', '0', '90°'),
         ('3', 'J3 P', '0', '172,6 + q$_3$', '0', '90°'),
         ('4', 'J4 R', 'q$_4$ + 90°', '−2,2', '0', '−90°'),
         ('5', 'J5 R', 'q$_5$', '41,2', '0', '0')]
xs = [0.0, 0.08, 0.22, 0.45, 0.7, 0.82]
for f in filas:
    for xx, t in zip(xs, f):
        tx.text(xx, y, t, fontsize=11, weight='bold' if f[0] == 'i' else 'normal')
    y -= 0.05
y -= 0.03
for t in ['q$_1$: giro de la cintura, desde X de la base (en la pose guardada, 60,5°).',
          'q$_2$: elevación de E2 sobre la horizontal (en la pose guardada, 32,4°).',
          'q$_3$: carrera del prismático, 0 … 340 mm (0 = retraído).',
          'q$_4$: cabeceo de la muñeca, 0 = ventosa perpendicular al brazo',
          '      (el STEP de la herramienta trae 1,9° de más).',
          'q$_5$: torsión de la ventosa.',
          'd$_4$ = −2,2 mm: el eje de torsión pasa 2,2 mm al costado del',
          '      eje del prismático (así viene en el STEP).',
          'd$_5$: del eje de cabeceo al labio de la ventosa.',
          '',
          'En SolidWorks: SR0 … SR5 están en las piezas CB-111, CB-210,',
          'CB-301, CB-401, CB-603 y CB-606 (se mueven con su eslabón).',
          'El riel (J0) no está modelado: iría antes de {0}.']:
    tx.text(0, y, t, fontsize=10); y -= 0.036
fig.savefig(OUT, facecolor='white'); print('ok')
