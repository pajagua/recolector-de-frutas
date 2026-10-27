# Arquitectura de CitriBot

Resumen de las decisiones de diseño vigentes. Si cambia algo, actualizá este archivo y la BOM.

## 1. Requisitos del TPF

- Configuración asignada: **RRP**, fruta **naranja**, herramienta de **succión + torsión**.
- Los **dos primeros grados de libertad** deben ser **motores DC con control de posición en lazo cerrado**.
- Visión **estéreo** (al menos 2 cámaras): localizar naranjas, resolver oclusiones y medir el tamaño.
- Espacio de trabajo: esfera de 30 cm de diámetro elevada del suelo. Según el boceto del grupo, a 5 cm del borde de la base y con el centro a 35 cm del suelo. Verificación: [espacio_de_trabajo.md](espacio_de_trabajo.md).
- Depositar las naranjas en al menos **dos cajones según su tamaño**.

## 2. Cadena cinemática (6 GDL)

```
J0 riel (P) – J1 cintura (R) – J2 hombro (R) – J3 brazo (P) – J4 muñeca (R) – J5 torsión (R)
```

| Art. | Tipo | Actuador | Transmisión | Driver | Realimentación |
|---|---|---|---|---|---|
| J0 | P | NEMA 17 17HS2408 | por definir (riel 1,2 m) | TB6600 | final de carrera |
| J1 | R | 37D 90:1, 12 V | correa T5, polea 10 dientes → plato 1M1B 100 dientes (≈ 10:1) | Sabertooth canal 1 | encoder 64 CPR en el motor |
| J2 | R | 5840-36ZY sinfín, 12 V, 12 rpm | directa (eje D8) | Sabertooth canal 2 | AS5600 en el eje del hombro |
| J3 | P | NEMA 17 (≈ 4,5 kg·cm), arriba del cubo de E2 | correa GT2 (polea de 20 dientes, 40 mm por vuelta) que arrastra un tubo de aluminio 30 × 30 de 440 mm; carrera 340 mm | A4988 en la RAMPS | final de carrera |
| J4 | R | MG996R | directa | PWM | interna del servo |
| J5 | R | MG996R | correa GT2 66:66, eje hueco sobre 2 × 6806 | PWM | interna del servo |

## 3. Herramienta

- Horquilla U atornillada a la brida de E3 (CB-402, 4 × M5 en rombo de radio 28,5).
- La manguera de vacío va por fuera del brazo y baja por el eje hueco de la torsión.
- Pose natural (J4 = 0): ventosa perpendicular al prismático, hacia abajo.
- Ventosa JXD SAB40 (fuelle) o SAF-40 (plana), rosca hembra G1/4, con racor push-in macho 1/4 encastrado en el hexágono del eje hueco.
- El sensor de presión MPS20N0040D confirma que la ventosa agarró la naranja.

## 4. Electrónica

- **Arduino MEGA 2560 + RAMPS 1.4** (préstamo del laboratorio).
- **Sabertooth dual 25A** para los dos motores DC (J1, J2).
- **TB6600** para el riel; **A4988** en la RAMPS para el prismático.
- Servos con fuente propia de 6 V (UBEC), no desde el 5 V del MEGA.
- 2 webcams **Logitech C270** en un mástil fijo a la base, a 120 mm entre lentes, mirando la esfera de trabajo (ver [vision/README.md](../vision/README.md#montaje-de-las-cámaras)).
- Visión, cinemática y planificación corren en la **notebook**; el MEGA recibe comandos por serial (ver [protocolo.md](protocolo.md)).

## 5. Geometría útil para la cinemática

Medida en el ensamblaje SolidWorks (`cad/solidworks/CB-000`), origen sobre el eje J1 en la cara superior del tablero, Z hacia arriba:

- Suelo (pie de las patas, sin riel): z = −80 mm. Eje J1 a 194,8 / 275,0 / 275,0 / 355,2 mm de los bordes del tablero.
- Cara superior del plato 1M1B: z = 91,5 mm.
- Eje J2 horizontal a z = 355,5 mm; corta al eje J1.
- Plano medio de E2 (eje de J3) desplazado 83,0 mm del eje J1, a lo largo del eje J2.
- El eje del prismático corta al eje J2.
- Cara exterior de la brida de E3: 105 mm + q3 del eje J2, con q3 = 0 … 340 mm (q3 = 0 retraído).
- Herramienta (`.scad` v4.2), con la cara de la brida como referencia: eje de cabeceo de la muñeca a 67,6 mm sobre el eje del prismático (65,4 mm en el STEP del CAD). Labio de la ventosa SAB40 a 55,2 mm del eje de cabeceo. Con la ventosa alineada con el brazo, el labio queda a 105 + q3 + 122,8 mm del eje J2 (227,8 … 567,8 mm).
- Rangos: J1 de −40° a +120° respecto del árbol por el mástil de las cámaras (límite por software −35° … +115°; los cajones de descarga tienen que quedar en ese rango); J2 de −45° a +45° con el brazo apuntando al árbol (ver [cad/solidworks/README.md](../cad/solidworks/README.md#rangos-de-movimiento-verificados-por-interferencias)); J3 340 mm.

## 6. Fabricación

- Impresora Ender 3 V2 (220 × 220 × 250 mm), PETG.
- La torre CB-210 (318 mm) y E2 (470 mm) no entran en la impresora: hay que partirlas. E2 va en 2 tramos (cubo del hombro + cola).
- E3 es un tubo de aluminio comprado (30 × 30 × 2, 440 mm), con brida y tapón con pinza de correa impresos, fijados con pasadores al ras.
- E2: imprimir el **cubo del hombro macizo** (≥ 6 perímetros, 50 % de relleno), porque transmite el torque de J2. La cola puede ir liviana (3 perímetros, 15 %); en la v7 una cola más pesada **baja** el pico de J2, porque el brazo tira siempre hacia adelante (§8).

## 7. Puntos abiertos

- ¿El profesor cuenta el riel como GDL 1? La guía pide DC en lazo cerrado en los dos primeros GDL, y el riel usa un paso a paso.
- Torque del 5840: un vendedor da ≈ 70 kg·cm (12 V, 12 rpm) sin aclarar si es nominal o de bloqueo. En la v7 el hombro pide 39 kg·cm de pico (§8). Si los 70 kg·cm fueran de bloqueo, el margen se achica: probar colgando 1 kg a 40 cm del eje y medir la corriente (bloqueo: 6,5 A).
- Medir el 5840 comprado con calibre antes de imprimir CB-210. El CAD sigue el plano del vendedor: M4 prof. 6, eje a 20 mm del borde, buje de 5 mm.
- Riel J0: el alto del carro sube todo el robot. El brazo cubre la esfera mientras el carro mida hasta 51 mm (ver [espacio_de_trabajo.md](espacio_de_trabajo.md)).
- J3: la correa no es autobloqueante. Con el motor sin corriente el tubo puede deslizar si el brazo está inclinado: mantener el driver habilitado.
- J3: medir el juego del tubo en el canal impreso de E2 y anotar qué trae el kit GT2 (BOM T04): puede cubrir la correa y las poleas de J3. El husillo T8 ya pedido (T05) queda de repuesto.
- Pesar las piezas impresas y la herramienta, y volver a correr `python control/torques.py` con esos valores.
- Sabertooth: el CAD modela un 2x60, el del laboratorio es 2x25.
- Bomba de vacío, electroválvula y fuente de 12 V: sin elegir.
- Riel J0: sin diseñar.

## 8. Verificación de motores

Calculado con `python control/torques.py`. Usa los volúmenes y centros de masa del CAD v7.

Masas supuestas:
- cubo de E2 0,55 kg y cola 0,34 kg (PETG, impresión del §6);
- NEMA 17 0,28 kg arriba del cubo (22 mm delante de J2); tubo 0,27 kg; brida 0,08 kg; tapón con pinza 0,03 kg;
- herramienta 0,405 kg y naranja 0,18 kg (boceto del grupo).

El peor caso es con el brazo horizontal, acelerando en 0,5 s.

| Motor | Caso más exigente | Pide | Tiene | Margen |
|---|---|---|---|---|
| J2: 5840, 12 V, 9 rpm con carga | J3 extendido con naranja: 35,2 kg·cm de gravedad + 4,2 de inercia | **39 kg·cm** de pico (59 con factor 1,5) | ≈ 70 kg·cm (vendedor) | × 1,8 |
| J2, mismo motor | J3 retraído: el brazo casi se equilibra | 3–4 kg·cm | | |
| J1: 37D 90:1 + correa 10:1 | brazo extendido, I = 0,24 kg·m², roce como en el boceto (μ = 0,15) | 9,7 kg·cm en la salida = **1,1 kg·cm** en el motor | ≈ 8 kg·cm de trabajo (32 de bloqueo) | × 7 |
| J3: NEMA 17 + correa GT2 (polea 20T) | extendido, J2 a 45°, roce 0,25 del tubo en la guía de 98 mm | 18 N = **1,3 kg·cm** | 4,5 kg·cm (≈ 2,5 a 300 rpm) | × 2 |
| J4/J5: MG996R | sin cambios: la herramienta es la misma que en la entrega 6 | | | |

- **Balance de J2.** En la v6 el NEMA 17 en la punta de la cola hacía de contrapeso (pico de 25 kg·cm). En la v7 el motor está sobre el cubo y la cola es más corta, así que el brazo tira siempre hacia adelante: de +3 kg·cm con J3 retraído a +35 kg·cm extendido con naranja.
  - Si la cola se imprime al 60 %, el pico baja a 35 kg·cm. Un contrapeso de 0,3 kg en la pared trasera de E2 (≈ 38 cm detrás de J2) lo bajaría unos 11 kg·cm más.
  - El cálculo del boceto daba 80,5 kg·cm, sin contrapeso. El contrapeso de 5,36 kg sigue sin hacer falta.
- **Sinfín autobloqueante.** El 5840 sostiene el brazo quieto sin corriente: el motor solo hace fuerza al moverse.
- **Fijación del motor.** 39 kg·cm pasan por el eje D8 al buje CB-305 (mejor de aluminio) y se reaccionan en los 2 tornillos M4 contra la torre (≈ 140 N de corte en total).
