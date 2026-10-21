# Electrónica

Todavía sin diagrama. Acá van el diagrama de conexiones, la asignación de pines y, si se hace, la PCB.

## Conexiones previstas

| Componente | Se conecta a | Notas |
|---|---|---|
| Motor 37D (J1) + encoder | Sabertooth canal 1; encoder a pines de interrupción del MEGA | |
| Motor 5840 (J2) | Sabertooth canal 2 | |
| AS5600 (J2) | I2C del MEGA (SDA 20, SCL 21) | |
| NEMA 17 (J0) | TB6600 (STEP/DIR desde el MEGA) | |
| NEMA 14 (J3) | A4988 en un zócalo de la RAMPS | |
| 2 × MG996R (J4, J5) | PWM del MEGA; alimentación desde UBEC 6 V | no usar el 5 V del MEGA |
| Bomba y electroválvula | MOSFET (D8/D9/D10 de la RAMPS o módulo aparte) | |
| MPS20N0040D | HX710B al MEGA (2 pines digitales) | |
| Finales de carrera | conectores de endstop de la RAMPS | |
| Sabertooth | serial del MEGA (modo Packetized Serial) | definir puerto en `firmware/include/config.h` |

## Qué falta

- Asignación definitiva de pines (en `firmware/include/config.h` y acá).
- Diagrama de conexiones (draw.io, Fritzing o KiCad).
- Dimensionar la fuente de 12 V y el UBEC de 6 V.
