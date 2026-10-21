# Entrega 7 – Prototipo de brazo con motores GA25-370

Adaptación del brazo **"3 Axis Robot Arm Mrk1"** (MakerWorld) a motores DC **JGA25-370** en J1 (base) y J2 (hombro). Se quitó J3 y se alargó el primer eslabón. La base lleva una plataforma para el Arduino UNO y el L298N.

Cada articulación usa la reductora planetaria 4:1 del diseño original (sol de 15 dientes, 3 planetas de 15, corona de 45, módulo 0.8). Un adaptador con la cara de un NEMA 17 aloja la reductora del GA25.

## Piezas modificadas por el grupo

| Archivo | Pieza |
|---|---|
| `Base.stl`, `para_imprimir/Base.3mf` | Base alta (+28.8 mm) con plataforma para Arduino UNO y L298N |
| `para_imprimir/BaseSimple.3mf` | Base alta sin plataforma |
| `Brazo3Ejes_GA25.stl` | Adaptador GA25 ("falso NEMA 17") |
| `EngranajeSol.stl`, `para_imprimir/EngranajeSol.3mf` | Sol GA25: mismos dientes que el original, agujero D de 4 mm y prisionero |
| `Eslabon.stl`, `para_imprimir/Eslabon.3mf` | Arm_1 largo: 150 mm entre centros, brida para la herramienta (4 × M4 a Ø24) |
| `Axe_Sortie_Reforzado.stl`, `para_imprimir/AxeSortie.3mf` | Eje de salida con hexágono macizo (el original es hueco y se parte) |
| `Prueba.stl` | Probeta de encaje del adaptador |

## Piezas que no están en el repo

Las piezas originales sin modificar (`Couronne A`, `Pivot B`, `Satellite A` ×3 por articulación, `Support_Satellite A`, `Rondelle A`, `Support_Bras D`) son del autor del diseño Mrk1 y se descargan del diseño original. Por eso tampoco está la placa completa `AcopleNemaGA.3mf`, que las incluye.
