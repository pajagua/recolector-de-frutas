# Firmware – Arduino MEGA 2560 + RAMPS 1.4

Proyecto PlatformIO. Todavía sin código: solo `setup()` con el puerto serial.

```
cd firmware
pio run              # compilar
pio run -t upload    # subir al MEGA
pio device monitor   # monitor serial (115200)
```

| Ruta | Qué va |
|---|---|
| `include/config.h` | pines, relaciones de transmisión, límites y velocidades de cada eje |
| `src/main.cpp` | lazo principal: lectura de comandos serial y control de ejes |
| `lib/` | módulos propios (por ejemplo, PID de J1/J2, lectura del AS5600) |
| `test/` | pruebas |

Librerías ya declaradas en `platformio.ini`: AccelStepper, PID.
Protocolo con la PC: [docs/protocolo.md](../docs/protocolo.md).
