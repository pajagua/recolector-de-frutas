# Lista de componentes: todo menos el prismático

Lo que hace falta para la **herramienta**, la **base y la cintura (J1)**, el **hombro (J2)**, la **electrónica** y la **visión**. Sale de la BOM ([bom/BOM.md](../bom/BOM.md), columna **ID**) y del CAD v7.7.

- **No incluye** el prismático J3 ni el eslabón E2, porque dependen de qué versión se use: telescópica (oficial desde la v7.12, [telescopica_lista_componentes.md](telescopica_lista_componentes.md) y BOM principal) o correa simple (v7A).
- **No incluye** el riel J0, que todavía no está diseñado.
- **Estado** es el de la BOM: *Prestado* (laboratorio), *Pedido* (compra grupal), *Por confirmar*, *En mano* o *Falta*.

---

## 1. Herramienta (muñeca J4 + torsión J5 + succión)

### Comprado

| ID | Componente | Especificación | Cant. | Para qué | Estado |
|:--|:--|:--|:--:|:--|:--|
| M05 | Servo MG996R | 180°, engranajes metálicos, 4,8–7,2 V. Con su horn redondo de plástico y tornillos | 2 | muñeca (cabeceo) y torsión | Pedido |
| T03 | Correa GT2 cerrada | 6 mm de ancho, **232 mm (116 dientes)** | 1 | torsión: polea del servo → eje hueco (66:66) | Por confirmar (puede venir en el kit T04) |
| R03 | Rodamiento 6806-ZZ | 30 × 42 × 7 mm | 2 | eje hueco | Pedido |
| R02 | Rodamiento 608ZZ | 8 × 22 × 7 mm | 1 (de 3) | pivote libre de la muñeca | Falta |
| H32 | Seguro DIN 471 para eje de 30 mm | 30 × 1,5 | 1 | frena el eje hueco | Falta |
| N01 | Ventosa JXD SAB40 (fuelle) | Ø40, rosca hembra G1/4 | 1 | agarre de la naranja | Pedido |
| N02 | Ventosa JXD SAF-40 (plana) | Ø40, rosca hembra G1/4 | 1 | alternativa para pruebas | Pedido |
| N03 | Racor recto push-in | manguera 6 mm × 1/4" BSP, hexágono de 12 | 1 | dentro del eje hueco | Pedido |
| N04 | Manguera PU | 6 × 4 mm | 2 m | ventosa → bomba | Falta |
| N05 | Bomba de vacío 12 V | mini bomba de diafragma | 1 | vacío | Falta (sin elegir) |
| N06 | Electroválvula 12 V | 2/2 o 3/2, normalmente cerrada | 1 | soltar la naranja | Falta (sin elegir) |
| S03 | Sensor de presión MPS20N0040D | 0–40 kPa, con HX710B | 1 | confirmar el agarre | Pedido |

### Tornillería

| ID | Tornillo | Cant. | Para qué |
|:--|:--|:--:|:--|
| H10 + H16 | M5 × 25 + tuerca M5 autoblocante | 4 + 4 | horquilla CB-601 → brida de E3 |
| H04 + H27 | M3 × 12 Allen + tuerca M3 | 4 + 4 | pestañas del servo de muñeca → horquilla (tuercas atrapadas) |
| H04 | M3 × 12 | 4 | tapa CB-604 → caja |
| H03 | M3 × 10 | 6 | 4 pestañas del servo de torsión → porta-servo · 2 porta-servo → piso de la caja |
| H12 + H18 | M8 × 25 + tuerca M8 | 1 + 1 | pivote libre de la muñeca |
| – | Tornillos del kit del servo (M2–M2,5) | 8 | 4 por horn: horn de muñeca → caja, horn de torsión → polea CB-605 |

### Impreso (PETG) — STL en [cad/imprimir/00_Herramienta/](../cad/imprimir/00_Herramienta/)

| ID | Pieza | Relleno | Notas |
|:--|:--|:--|:--|
| P13 | CB-601 Horquilla U | ≥ 4 perímetros | v7.7: pestañas 9 × 49 con tuercas atrapadas |
| P14 | CB-602 Porta-servo | ≥ 4 perímetros | ranura para el cable |
| P15 | CB-603 Caja de torsión | ≥ 4 perímetros | v7.7: asiento del horn de plástico |
| P16 | CB-604 Tapa de la caja | ≥ 4 perímetros | |
| P17 | CB-605 Polea del servo (66 dientes) | 100 % | v7.7: asiento del horn de plástico |
| P18 | CB-606 Eje hueco con polea (66 dientes) | 100 % | |
| P19 | CB-607 Separador del pivote | ≥ 4 perímetros | |
| P29 | CB-621 Separador del eje hueco | 100 % | |
| P30 | CB-622 Arandela del seguro | 100 % | |
| P28 | CB-620 Llave del racor | 50 % | herramienta de montaje, no va en el robot |

La brida donde se atornilla la horquilla es parte del prismático: CB-402 en la versión de correa simple, CB-432B en la telescópica.

---

## 2. Base y cintura (J1)

### Comprado

| ID | Componente | Especificación | Cant. | Para qué | Estado |
|:--|:--|:--|:--:|:--|:--|
| X01 | Tablero de contrachapado o MDF | **400 × 300 × 16 mm** | 1 | base | Falta |
| M01 | Motorreductor 37D con encoder | 12 V, 90:1, encoder 64 CPR | 1 | J1 (lazo cerrado) | Por confirmar (reducción real) |
| T01 | Polea T5 | 10 dientes, ancho 17 mm, agujero 6 mm, con prisionero | 1 | eje del 37D | Falta |
| T02 | Correa T5 cerrada | 16 mm de ancho, 600 mm (120 dientes) | 1 | 37D → plato 1M1B (≈ 10:1) | Falta |
| R01 | Rodamiento 625ZZ | 5 × 16 × 5 mm | 8 | rodillos de la lazy susan | Falta |
| R02 | Rodamiento 608ZZ | 8 × 22 × 7 mm | 2 (de 3) | eje central de J1 | Falta |
| S04 | Final de carrera con palanca | microswitch 19,8 × 6,4 × 9,5, palanca metálica plana | 1 (de 3) | home de J1 (poste CB-161; lo aprieta la leva de la torre) | En mano |

### Tornillería

| ID | Tornillo | Cant. | Para qué |
|:--|:--|:--:|:--|
| H09 + H20 + H16 | M5 × 30 + arandela M5 + tuerca M5 autoblocante | 4 + 4 + 4 | patas → tablero |
| H08 + H22 + H16 | M5 × 20 + separador M5 × 10 + tuerca M5 autoblocante | 8 + 8 + 8 | rodillos de la lazy susan |
| H06 + H19 + H14 | M4 × 40 + arandela M4 + tuerca M4 autoblocante | 4 + 4 + 4 | placa fija 1M2A → tablero |
| H05 + H19 + H14 | M4 × 30 + arandela M4 + tuerca M4 autoblocante | 4 + 4 + 4 | soporte del motor de J1 → tablero |
| H07 + H14 | M4 × 45 + tuerca M4 autoblocante | 6 + 6 | torre → plato 1M1B |
| H13 + H17 + H23 | M8 × 65 + tuerca M8 autoblocante + separador M8 × 20 (tubo de aluminio 10 × 8, cortado a 20 mm) | 1 + 1 + 2 | eje central de J1 |
| H34 | M2 × 12 + tuerca M2 | 2 + 2 | final de carrera de J1 → poste CB-161 |
| H36 | tirafondo Ø3,5 × 16 | 2 | poste CB-161 → tablero |
| H02 | M3 × 8 | 6 | motor 37D → soporte CB-131 |

### Impreso (PETG) — STL en [01_Base_J0](../cad/imprimir/01_Base_J0/) y [02_Cintura_J1](../cad/imprimir/02_Cintura_J1/)

| ID | Pieza | Cant. | Notas |
|:--|:--|:--:|:--|
| P01 | CB-112 Pata de la base | 4 | |
| P02 | CB-121 Placa fija J1 (1M2A) | 1 | |
| P03 | CB-131 Soporte del motor de J1 | 1 | |
| P05 | CB-201 Plato giratorio con dientes T5 (1M1B) | 1 | |
| P06 | CB-210 Torre de la cintura | 1 | **318 mm de alto: no entra en la Ender 3 V2**, hay que partirla. Lleva la leva del final de carrera de J1 en la base |
| P32 | CB-161 Poste del final de carrera de J1 | 1 | parado sobre la base |

---

## 3. Hombro (J2), sin el eslabón E2

| ID | Componente | Especificación | Cant. | Para qué | Estado |
|:--|:--|:--|:--:|:--|:--|
| M02 | Motorreductor sinfín 5840-36ZY | 12 V, 12 rpm, eje D8 | 1 | J2 (directo) | Pedido |
| S01 | Módulo AS5600 | encoder magnético 12 bit, I2C | 1 | ángulo real de E2 | Por confirmar |
| S02 | Imán de neodimio diametral | Ø6 × 2,5 mm | 1 | para el AS5600 | Por confirmar (suele venir con el módulo) |
| H11 | M4 × 10 Allen | – | 2 | motor 5840 → torre | Falta |
| H05 + H15 | M4 × 30 + tuerca M4 | – | 2 + 2 | soporte del imán → torre | Falta |
| H01 | M3 × 6 Allen | – | 4 | AS5600 → tapa CB-303 | Falta |
| H03 | M3 × 10 | – | 3 | tapa CB-303 → E2 | Falta |
| X04 | Cianoacrilato | – | 1 | imán en el soporte | Falta |

Impreso: P07 **CB-232** soporte del imán, P10 **CB-303** tapa de E2 / soporte del AS5600 y P11 **CB-305** buje D8. El buje conviene hacerlo en aluminio, porque transmite todo el torque del hombro. STL en [02_Cintura_J1](../cad/imprimir/02_Cintura_J1/) y [03_Hombro_E2_J2](../cad/imprimir/03_Hombro_E2_J2/).

---

## 4. Electrónica y alimentación

| ID | Componente | Especificación | Cant. | Para qué | Estado |
|:--|:--|:--|:--:|:--|:--|
| E01 | Arduino MEGA 2560 | – | 1 | controlador | Prestado |
| E02 | RAMPS 1.4 | shield, 5 zócalos de driver | 1 | drivers, finales de carrera, MOSFET | Prestado |
| E04 | Sabertooth dual 25A | 2 canales DC, 6–30 V | 1 | motores de J1 y J2 | Prestado |
| E03 | Driver A4988 | 2 A pico | 1 | NEMA 17 de J3 | Prestado |
| E05 | Driver TB6600 | 4 A | 1 | NEMA 17 del riel | Prestado |
| E06 | ProtoScrew Shield | borneras | 1 | cableado | Por confirmar si se usa |
| E07 | Módulo MOSFET 12 V | entrada lógica de 5 V, ≥ 3 A | 2 | bomba y electroválvula | Falta (alternativa: salidas D8/D9/D10 de la RAMPS) |
| E08 | Módulo PCA9685 | 16 canales PWM, I2C | 1 | los 2 servos (V+ desde el UBEC A02) | En mano |
| A01 | Fuente 12 V DC | ≥ 10 A (estimado) | 1 | motores y bomba | Falta (dimensionar) |
| A02 | Regulador UBEC 6 V | ≥ 5 A | 1 | los 2 servos (no del 5 V del MEGA) | Falta |
| A03 | Parada de emergencia + fusible | hongo NC + portafusible | 1 | seguridad | Falta |
| A04 | Cables, borneras y conectores | Dupont, JST, termocontraíble | 1 lote | cableado | Falta |
| S04 | Final de carrera con palanca | microswitch 19,8 × 6,4 × 9,5, palanca metálica plana | 3 | home de J0, J1 y J3 (el de J1 está en la sección 2) | En mano |

**Tornillería de la bandeja:**
- 4 × M4 × 30 + arandela M4 + tuerca M4 autoblocante: bandeja → tablero (H05, H19, H14).
- 4 × M3 × 10: Sabertooth → bandeja (H03).
- 5 × M3 × 8: Arduino → bandeja (H02).
- 2 × tirafondo Ø3,5 × 16: soporte CB-142 del PCA9685 → tablero (H36). 4 × M2,5 × 8: PCA9685 → CB-142 (H35). Impreso: P31 **CB-142**.
- Final de carrera de J0 (provisorio): placa CB-162 bajo el tablero con 2 × tirafondo Ø3,5 × 16 (H36) y el switch con 2 × M2 × 10 (H33). Impreso: P33 **CB-162**.

**Impreso:** P04 **CB-141** bandeja de electrónica, 210 × 100 mm.

> El CAD modela un Sabertooth 2x60 y el del laboratorio es 2x25: revisar que los agujeros de la bandeja coincidan antes de imprimirla.

---

## 5. Visión (cámaras y mástil)

| ID | Componente | Especificación | Cant. | Para qué | Estado |
|:--|:--|:--|:--:|:--|:--|
| V01 | Cámara web Logitech C270 | 720p, foco fijo | 2 | visión estéreo | Pedido |
| X08 | Tubo cuadrado de aluminio | 20 × 20 × 1,5 mm, **627 mm** | 1 | mástil | Falta |
| V03 | Tablero de calibración | damero 9 × 6 esquinas, cuadro de 25 mm, sobre base rígida | 1 | calibración estéreo | Falta |
| V02 | Notebook | Python + OpenCV | 1 | visión y control | En mano |
| H09 + H16 | M5 × 30 + tuerca M5 autoblocante | – | 1 + 1 | pivote de cabeceo del soporte | Falta |
| H04 + H27 | M3 × 12 + tuerca M3 | – | 4 + 4 | base del soporte → adaptador | Falta |
| H28 | Prisionero M4 × 6 | – | 2 | pie y cabezal → tubo | Falta |
| H29 | Tirafondo 4 × 12 | – | 4 | pie → tablero | Falta |
| H30 | Autorroscante Ø3,5 × 10 | – | 4 | tapas → soporte | Falta |

Impreso: P22 **CB-151** pie, P23 **CB-153** cabezal, P24 **CB-154** adaptador, P25 **CB-156** base del soporte y P26 **CB-157** tapas (2). STL en [01_Base_J0](../cad/imprimir/01_Base_J0/). P27 **CB-158** solo sirve para probar las cámaras en un trípode.

> Si se arma la telescópica, el tubo de 20 × 20 del interior (263 mm) sale de la misma barra que el mástil: comprar una barra de 1 m.

---

## 6. Consumibles y herramientas de taller

| ID | Material | Cant. | Notas |
|:--|:--|:--:|:--|
| X02 | Filamento PETG 1,75 mm | ≈ 3 kg | para todo lo de arriba; calcular con el slicer |
| X03 | Epoxi bicomponente | 1 | buje del hombro (y brida y tapones del prismático) |
| – | Pinza de seguros exterior | 1 | para el seguro DIN 471 del eje hueco |
| – | Teflón de rosca | 1 | rosca del racor en la ventosa |

---

## 7. Cosas a revisar en la BOM

- **R04 y R05 (bujes de bronce)** figuran en la BOM, pero el CAD no los usa: los rodillos llevan 625ZZ y el eje de J1, 608ZZ. No comprarlos salvo que se cambie el diseño.
- **P20 (anillo de retención)** no hace falta: lo reemplazan CB-621, CB-622 y el seguro.
- La tornillería de la herramienta ya está corregida en la BOM para el horn de plástico. No se usan los 8 M3 × 8 de horns metálicos. El servo de muñeca va con 4 M3 × 12 Allen y tuerca.
