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
| J3 | P | NEMA 17 (≈ 4,5 kg·cm) | husillo T8 de 400 mm (avance 8 mm), tubo de aluminio 30 × 30 que se retrae por detrás del hombro; carrera 350 mm | A4988 en la RAMPS | final de carrera |
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
- Visión, cinemática y planificación corren en la **notebook**; el MEGA recibe comandos por serial (ver [protocolo.md](protocolo.md)).

## 5. Geometría útil para la cinemática

Medida en el ensamblaje SolidWorks (`cad/solidworks/CB-000`), origen sobre el eje J1 en la cara superior del tablero, Z hacia arriba:

- Suelo (pie de las patas, sin riel): z = −80 mm. Eje J1 a 194,8 / 275,0 / 275,0 / 355,2 mm de los bordes del tablero.
- Cara superior del plato 1M1B: z = 91,5 mm.
- Eje J2 horizontal a z = 355,5 mm; corta al eje J1.
- Plano medio de E2 (eje de J3) desplazado 83,0 mm del eje J1, a lo largo del eje J2.
- El eje del prismático corta al eje J2.
- Cara exterior de la brida de E3: 105 mm + q3 del eje J2, con q3 = 0 … 350 mm (q3 = 0 retraído).
- Herramienta (`.scad` v4.2), con la cara de la brida como referencia: eje de cabeceo de la muñeca a 67,6 mm sobre el eje del prismático (65,4 mm en el STEP del CAD). Labio de la ventosa SAB40 a 55,2 mm del eje de cabeceo. Con la ventosa alineada con el brazo, el labio queda a 105 + q3 + 122,8 mm del eje J2 (227,8 … 577,8 mm).
- Rangos: J1 limitado por cables (±170° por software); J2 de −45° a +45° con el brazo apuntando al árbol (ver [cad/solidworks/README.md](../cad/solidworks/README.md#rangos-de-movimiento-verificados-por-interferencias)); J3 350 mm.

## 6. Fabricación

- Impresora Ender 3 V2 (220 × 220 × 250 mm), PETG.
- La torre CB-210 (318 mm) y E2 (504,5 mm) no entran en la impresora: hay que partirlas. E2 va en 3 tramos (cubo del hombro + 2 tramos de cola).
- E3 es un tubo de aluminio comprado (30 × 30 × 2, 460 mm), con brida y portatuerca impresos.
- E2: imprimir el **cubo del hombro macizo** (≥ 6 perímetros, 50 % de relleno), porque transmite el torque de J2, y la **cola liviana** (3 perímetros, 15 %), porque hace de contrapeso y su masa entra en el torque de J2 (§8).

## 7. Puntos abiertos

- ¿El profesor cuenta el riel como GDL 1? La guía pide DC en lazo cerrado en los dos primeros GDL, y el riel usa un paso a paso.
- Torque del 5840: un vendedor da ≈ 70 kg·cm (12 V, 12 rpm) sin aclarar si es nominal o de bloqueo. Con la cola liviana el hombro pide 25 kg·cm de pico (§8). Si los 70 kg·cm fueran de bloqueo, el margen se achica: medir la corriente al levantar el brazo extendido (bloqueo: 6,5 A).
- Medir el 5840 comprado con calibre antes de imprimir CB-210. El CAD sigue el plano del vendedor: M4 prof. 6, eje a 20 mm del borde, buje de 5 mm.
- Riel J0: el alto del carro sube todo el robot. El brazo cubre la esfera mientras el carro mida hasta 79 mm (ver [espacio_de_trabajo.md](espacio_de_trabajo.md)).
- J3: el husillo T8 de avance 8 mm no es autobloqueante. Con el motor sin corriente el tubo puede deslizar si el brazo está inclinado: mantener el driver habilitado o usar un T8 de avance 2 mm (más lento).
- J3: confirmar que el pedido "Rosca p/ Varilla" (BOM T05) es un T8 de ≥ 400 mm, y medir el juego del tubo en el canal impreso de E2.
- Pesar las piezas impresas y la herramienta, y volver a correr `python control/torques.py` con esos valores.
- Sabertooth: el CAD modela un 2x60, el del laboratorio es 2x25.
- Bomba de vacío, electroválvula y fuente de 12 V: sin elegir.
- Riel J0: sin diseñar.

## 8. Verificación de motores

Calculado con `python control/torques.py`. Usa los volúmenes y centros de masa del CAD v6.

Masas supuestas:
- cubo de E2 0,60 kg y cola 0,35 kg (PETG, impresión del §6);
- NEMA 17 0,28 kg; husillo 0,16 kg; tubo 0,28 kg; brida 0,08 kg;
- herramienta 0,405 kg y naranja 0,18 kg (boceto del grupo).

El peor caso es con el brazo horizontal, acelerando en 0,5 s.

| Motor | Caso más exigente | Pide | Tiene | Margen |
|---|---|---|---|---|
| J2: 5840, 12 V, 9 rpm con carga | J3 extendido con naranja: 19,6 kg·cm de gravedad + 5,6 de inercia | **25 kg·cm** de pico (38 con factor 1,5) | ≈ 70 kg·cm (vendedor) | × 2,8 |
| J2, mismo motor | J3 retraído sin naranja: la cola pesa más | 21 kg·cm | | |
| J1: 37D 90:1 + correa 10:1 | brazo extendido, I = 0,31 kg·m², roce como en el boceto (μ = 0,15) | 11,7 kg·cm en la salida = **1,3 kg·cm** en el motor | ≈ 8 kg·cm de trabajo (32 de bloqueo) | × 6 |
| J3: NEMA 17 + T8 avance 8 | extendido, J2 a 45°, roce 0,25 del tubo en la guía | 17 N = **0,7 kg·cm** | 4,5 kg·cm (≈ 2,5 a 300 rpm) | × 3,5 |
| J4/J5: MG996R | sin cambios: la herramienta es la misma que en la entrega 6 | | | |

- **Balance de J2.** La cola de E2 con el NEMA 17 hace de contrapeso. Con J3 retraído el brazo tira hacia atrás (−19 kg·cm); con J3 extendido y naranja, hacia adelante (+20 kg·cm). Ninguna de las dos cosas es mucho peor que la otra.
  - Si la cola se imprime maciza al 60 %, el peor caso pasa a ser J3 retraído: 27 kg·cm de pico.
  - El cálculo del boceto daba 80,5 kg·cm, sin contrapeso. El contrapeso de 5,36 kg ya no hace falta.
- **Sinfín autobloqueante.** El 5840 sostiene el brazo quieto sin corriente: el motor solo hace fuerza al moverse.
- **Fijación del motor.** 25 kg·cm pasan por el eje D8 al buje CB-305 (mejor de aluminio) y se reaccionan en los 2 tornillos M4 contra la torre (≈ 90 N de corte en total).
