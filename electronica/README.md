# Electrónica

Todavía sin diagrama. Acá van el diagrama de conexiones, la asignación de pines y, si se hace, la PCB.

## Conexiones previstas

| Componente | Se conecta a | Notas |
|---|---|---|
| Motor 37D (J1) + encoder | Sabertooth canal 1; encoder a pines de interrupción del MEGA | |
| Motor 5840 (J2) | Sabertooth canal 2 | |
| AS5600 (J2) | I2C del MEGA (SDA 20, SCL 21) | |
| NEMA 17 (J0) | TB6600 (STEP/DIR desde el MEGA) | |
| NEMA 17 (J3, telescópico 2:1 con correa GT2) | A4988 en un zócalo de la RAMPS | regular la corriente a ≈ 1,2 A, con disipador; 40 pasos por mm de q3 a 1/16 (80 mm por vuelta en la brida), ≤ 150 rpm con el 17HS4401 |
| 2 × MG996R (J4, J5) | PCA9685 (canales 0 y 1); alimentación desde el UBEC de 6 V por la bornera V+ del PCA9685 | no usar el 5 V del MEGA |
| PCA9685 | I2C del MEGA (SDA 20, SCL 21), junto con el AS5600 (direcciones 0x40 y 0x36); VCC a 5 V | en el soporte CB-142, al lado de la bandeja |
| Bomba y electroválvula | MOSFET (D8/D9/D10 de la RAMPS o módulo aparte) | |
| MPS20N0040D | HX710B al MEGA (2 pines digitales) | |
| Finales de carrera (J0, J1, J3) | conectores de endstop de la RAMPS | X = D3/D2; Y = D14/D15 y Z = D18/D19 son también Serial3 y Serial1: no usar el mismo puerto para el Sabertooth |
| Sabertooth | serial del MEGA (modo Packetized Serial) | definir puerto en `firmware/include/config.h` |

## Qué falta

- Asignación definitiva de pines (en `firmware/include/config.h` y acá).
- Diagrama de conexiones (draw.io, Fritzing o KiCad).
- Dimensionar la fuente de 12 V y el UBEC de 6 V.
