# Control (PC)

Por ahora solo está `espacio_trabajo.py`: verifica que la esfera del árbol entra en el alcance del brazo ([docs/espacio_de_trabajo.md](../docs/espacio_de_trabajo.md)). Se corre así: `python control/espacio_trabajo.py [--carro MM] [--figura]`.

Plan:

| Módulo | Qué hace |
|---|---|
| cinemática directa | tabla de Denavit-Hartenberg de los 6 GDL y matrices de transformación |
| cinemática inversa | posición de la naranja → valores de J0…J5 |
| calibración mano-ojo | marco de las cámaras → marco del robot |
| trayectorias | aproximación, agarre con torsión, retiro y depósito en el cajón según tamaño |
| enlace serial | comandos al MEGA ([docs/protocolo.md](../docs/protocolo.md)) |

Geometría medida en el CAD: [docs/arquitectura.md](../docs/arquitectura.md#5-geometría-útil-para-la-cinemática).
