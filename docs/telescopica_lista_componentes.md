# Lista de componentes: prismático telescópico (v7B)

Todo lo que hace falta para armar el mecanismo telescópico 2:1 del ensamblaje `CB-450B_Prismatico_Telescopico_Solo`: E2, el motor, las dos correas, los dos tramos de tubo y la brida. Cómo funciona y animación: [prismatico_opciones.md](prismatico_opciones.md#3--telescópica-21). Montaje paso a paso: [v7_prismatico.md §5.2](v7_prismatico.md#52-variante-b).

El hombro (motor 5840, buje, AS5600 e imán), la cintura, la base y la herramienta no cambian.

> La BOM oficial ([bom/BOM.md](../bom/BOM.md)) sigue con la versión de correa simple (v7A). La columna **BOM** dice qué ítem de esa lista se usa, se cambia o falta. Si se adopta la telescópica, hay que pasar estos cambios a la BOM.

---

## 1. Para comprar

### Motor y transmisión

| Componente | Especificación | Cant. | Para qué | BOM |
|:--|:--|:--:|:--|:--|
| Motor paso a paso NEMA 17 | 17HS4401 (40 mm, 0,42 N·m) sirve hasta ≈ 150 rpm. **Mejor:** 17HS19 (48 mm, 0,55 N·m), mismo montaje | 1 | mueve el tramo intermedio | M03 (por confirmar) |
| Polea GT2 | 20 dientes, agujero 5 mm, correa de 6 mm, 2 prisioneros | 1 | eje del motor | T07 (puede venir en el kit T04) |
| Polea loca GT2 dentada | 20 dientes, agujero 5 mm, con rodamientos | 1 | punta trasera de E2 | T08 (puede venir en el kit T04) |
| **Polea loca GT2 dentada** | **16 dientes, agujero 3 mm**, con rodamientos | **2** | puntas del tramo intermedio (multiplicador) | **nueva** |
| **Correa GT2 abierta** | 6 mm de ancho, **2 m** | 1 | etapa 1 (≈ 470 mm) + multiplicador (≈ 580 mm) + extremos | **T09 pasa de 1 m a 2 m** |

### Perfiles de aluminio

| Componente | Especificación | Cant. | Para qué | BOM |
|:--|:--|:--:|:--|:--|
| Tubo cuadrado de aluminio | 30 × 30 × 2 mm, cortado a **275 mm** | 1 | tramo intermedio (CB-411B) | **X05 pasa de 440 a 275 mm** |
| **Tubo cuadrado de aluminio** | **20 × 20 × 1,5 mm**, cortado a **263 mm** | 1 | tramo interior con la brida (CB-421B) | **nuevo** |

> **Ahorro:** el mástil de las cámaras (X08) usa el mismo tubo de 20 × 20 × 1,5 (627 mm). Con **una barra de 1 m** salen las dos piezas (627 + 263 = 890 mm, más los cortes).

### Tornillería

| Componente | Cant. | Para qué | BOM |
|:--|:--:|:--|:--|
| Inserto térmico M3 (Ø ext. ≈ 4,6 mm, largo 5–6 mm) | 4 | base del motor en E2 | H26 |
| Tornillo M3 × 6 Allen | 4 | motor → placa CB-306 | H01 |
| Tornillo M3 × 10 | 5 | 4 placa CB-306 → insertos de E2 · 1 anclaje de la correa 2:1 en E2 | H03 (+1) |
| Tornillo M3 × 25 + tuerca M3 autoblocante | 1 | eje de la polea 16T trasera | **nuevo** |
| Tornillo M3 × 20 + tuerca M3 autoblocante | 1 | eje de la polea 16T delantera | **nuevo** |
| Tornillo M3 × 4 | 2 | soporte de la polea delantera → intermedio (rosca en el aluminio) | **nuevo** |
| Tornillo M3 × 5 | 2 | pinza de la etapa 1 → intermedio (rosca en el aluminio) | **nuevo** |
| Tornillo M2 × 6 | 2 | correa 2:1 → tubo interior (con la placa dentada) | **nuevo** |
| Pasador de acero Ø5 × 20 + clip E de 5 mm + arandela M5 | 1 | eje de la polea loca 20T | **nuevo** (reemplaza el M5 × 70, H25) |
| Pasador de acero Ø3 × 30 | 1 | tapón trasero del intermedio | H24 (1 de 2) |
| Pasador de acero Ø3 × 25 | 1 | buje trasero del interior | **nuevo** |
| Pasador de acero Ø3 × 20 | 1 | brida del interior | **nuevo** |
| Precinto (≈ 3 mm) | 2 | extremos de la correa de la etapa 1 en la pinza | X07 |

> Los M3 × 4 y M3 × 5 no pueden asomar dentro del intermedio: por ahí corre la correa 2:1. Si no los conseguís, limá M3 × 6 a medida.

### Consumibles y sensor

| Componente | Cant. | Para qué | BOM |
|:--|:--:|:--|:--|
| Epoxi bicomponente | 1 | brida, tapón y bujes en los tubos (junto con los pasadores) | X03 |
| Cinta de PTFE adhesiva (≥ 20 mm) | 0,5 m | canal de E2 y buje delantero | X06 |
| Filamento PETG 1,75 mm | ≈ 0,4 kg | piezas impresas de abajo | X02 |
| Final de carrera con palanca | 1 | home de J3 (telescopio retraído) | S04 (1 de 3) |

### Herramientas

| Herramienta | Para qué |
|:--|:--|
| Sierra para aluminio | cortar los tubos a 275 y 263 mm |
| Macho M3 + broca de 2,5 mm | roscar los 4 agujeros de la cara superior del intermedio |
| Broca de 3 mm | agujeros de los pasadores |
| Lima o amoladora | ventana de 14,5 × 9,4 mm y muesca de 3 × 9,4 mm en el intermedio (para las poleas 16T) |
| Soldador (≈ 220 °C) | insertos térmicos |

---

## 2. Para imprimir (PETG)

| Pieza | STL | Medida (mm) | Relleno | Notas |
|:--|:--|:--:|:--|:--|
| CB-301B Eslabón E2 (manga corta) | [11_v7B…/CB-301B](../cad/imprimir/11_v7B_Prismatico_Telescopico/CB-301B_Eslabon_E2_v7B_Telescopico.stl) | 297 × 95 × 87 | cubo ≥ 6 perímetros y 50 %; manga 4 perímetros y 25 % | **No entra en la Ender 3 V2** (220 × 220): partir en 2 tramos, o imprimir en diagonal si entra |
| CB-306 Placa del NEMA 17 | [03_Hombro…/CB-306](../cad/imprimir/03_Hombro_E2_J2/CB-306_Placa_Soporte_NEMA17_Prismatico.stl) | 62 × 62 × 5 | 100 % | la misma que en la versión oficial |
| CB-412B Tapón trasero del intermedio | [CB-412B](../cad/imprimir/11_v7B_Prismatico_Telescopico/CB-412B_Tapon_Trasero_Intermedio_Polea.stl) | 26 × 37 × 30 | 100 % | lleva la polea 16T trasera |
| CB-413B Soporte de la polea delantera | [CB-413B](../cad/imprimir/11_v7B_Prismatico_Telescopico/CB-413B_Soporte_Polea_Delantera.stl) | 26 × 9 × 16 | 100 % | |
| CB-414B Buje delantero del intermedio | [CB-414B](../cad/imprimir/11_v7B_Prismatico_Telescopico/CB-414B_Buje_Delantero_Intermedio.stl) | 13 × 26 × 26 | 100 % | guía el tubo interior; mejor en PTFE o UHMW si se puede mecanizar |
| CB-415B Pinza de la correa de la etapa 1 | [CB-415B](../cad/imprimir/11_v7B_Prismatico_Telescopico/CB-415B_Pinza_Correa_Etapa1.stl) | 23 × 20 × 7 | 100 % | |
| CB-422B Buje trasero del interior | [CB-422B](../cad/imprimir/11_v7B_Prismatico_Telescopico/CB-422B_Buje_Trasero_Interior_Pinza_2a1.stl) | 18 × 26 × 26 | 100 % | patines + canal de la correa 2:1 |
| CB-432B Brida de la herramienta (tubo 20 × 20) | [CB-432B](../cad/imprimir/11_v7B_Prismatico_Telescopico/CB-432B_Brida_E3_Herramienta_20x20.stl) | 30 × 80 × 80 | 100 % en el tapón | mismos 4 agujeros M5 que la brida oficial: la herramienta no cambia |

**Faltan diseñar dos piezas chicas** (no están en el CAD):

- **Placa de apriete del anclaje:** sujeta los dos extremos de la correa 2:1 en E2, con 1 M3 × 10.
- **Placa dentada del tubo interior:** traba el tramo inferior de la correa 2:1 contra el tubo, con 2 M2 × 6.

---

## 3. Qué deja de hacer falta (respecto de la versión oficial)

| Sale | BOM |
|:--|:--|
| Tubo 30 × 30 × 2 de 440 mm (queda uno de 275 mm) | X05 |
| CB-301 Eslabón E2 de 470 mm (lo reemplaza CB-301B de 297 mm) | P08 |
| CB-403 Tapón trasero con pinza | P12 |
| CB-402 Brida de E3 (la reemplaza CB-432B) | P09 |
| Tornillo M5 × 70 + 2 arandelas M5 + 1 tuerca M5 autoblocante del eje de la polea loca | H25, H20, H16 |
| 1 pasador Ø3 × 30 | H24 |

---

## 4. Ajustes de firmware

- **40 pasos/mm de q3** a 1/16 de micropaso (la versión oficial usa 80).
- Con el 17HS4401, limitar el motor a ≈ 150 rpm (≈ 200 mm/s en la brida). Con el 17HS19 de 48 mm hay más margen.
- La correa no es autobloqueante: dejar el driver del NEMA habilitado para que el brazo inclinado no deslice.
