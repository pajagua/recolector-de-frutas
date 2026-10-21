# Arquitectura de CitriBot

Resumen de las decisiones de diseño vigentes. Si cambia algo, actualizá este archivo y la BOM.

## 1. Requisitos del TPF

- Configuración asignada: **RRP**, fruta **naranja**, herramienta de **succión + torsión**.
- Los **dos primeros grados de libertad** deben ser **motores DC con control de posición en lazo cerrado**.
- Visión **estéreo** (al menos 2 cámaras): localizar naranjas, resolver oclusiones y medir el tamaño.
- Espacio de trabajo: esfera de 30 cm de diámetro elevada del suelo.
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
| J3 | P | NEMA 14 | tornillo TSF-E3 impreso, carrera 53,5 mm | A4988 en la RAMPS | final de carrera |
| J4 | R | MG996R | directa | PWM | interna del servo |
| J5 | R | MG996R | correa GT2 66:66, eje hueco sobre 2 × 6806 | PWM | interna del servo |

## 3. Herramienta

- Horquilla U atornillada al extremo de E3 con placa maciza.
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

- Cara superior del plato 1M1B: z = 91,5 mm.
- Eje J2 horizontal a z = 355,5 mm; corta al eje J1.
- Plano medio de E2 (eje de J3) desplazado 83,0 mm del eje J1, a lo largo del eje J2.
- El eje del prismático corta al eje J2.
- Cara exterior de la placa de E3 en la pose inicial: 258,5 mm del eje J2.
- Rangos: J1 limitado por cables (±170° por software); J2 libre salvo más de ≈ 55° bajo la horizontal; J3 53,5 mm.

## 6. Fabricación

- Impresora Ender 3 V2 (220 × 220 × 250 mm), PETG.
- La torre CB-210 (318 mm) y E2 (260 mm) no entran en la impresora: hay que partirlas.

## 7. Puntos abiertos

- ¿El profesor cuenta el riel como GDL 1? La guía pide DC en lazo cerrado en los dos primeros GDL, y el riel usa un paso a paso.
- Torque del 5840: ¿los 90 kg·cm del vendedor son nominales o de bloqueo?
- Medidas reales del 5840 (agujeros y eje) con calibre antes de imprimir CB-210.
- NEMA 14 en J3: confirmar torque; un NEMA 17 no entra en E2 sin rediseño.
- J3: el tornillo TSF-E3 cubre solo 53,5 mm de carrera y E3 no tiene tuerca modelada.
- Sabertooth: el CAD modela un 2x60, el del laboratorio es 2x25.
- Bomba de vacío, electroválvula y fuente de 12 V: sin elegir.
- Riel J0: sin diseñar.
