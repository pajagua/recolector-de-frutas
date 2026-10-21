# Guía — Entrega 6: Control de Lazo Cerrado
## CitriBot RRP — Robótica I

> La Entrega 6 es la evolución directa de la Entrega 5 (lazo abierto). 
> El hilo conductor es: "ahora cerramos el lazo". 
> Cada indicador debe demostrar que pasaste de mandar comandos a ciegas 
> a un sistema con retroalimentación real.

---

## 1. Selección de Motores

### ¿Qué quiere ver el profesor?

Una **justificación ingenieril** de por qué elegiste cada motor para cada articulación.
No es repetir el BOM, es demostrar que el motor seleccionado cumple los 
requerimientos de torque, velocidad y resolución de esa articulación.

### Contenido esperado

- Tabla comparativa: Para cada DoF → requerimiento de torque (calculado), 
  velocidad máxima necesaria, y cómo el motor elegido cumple o supera eso
- Especificaciones del datasheet: Voltaje nominal, RPM sin carga, torque stall, 
  corriente stall, relación de reducción
- Justificación técnica: ¿Por qué el JGA25-370 para la cintura y no un NEMA 17? 
  ¿Por qué DC y no stepper en esas articulaciones?
- Diagrama de asignación: Qué motor va en qué articulación (mapeo DoF ↔ actuador)

> TIP: El profesor ya sabe que en la Entrega 5 usaste el JGA25-370 con L298N. 
> Acá quiere ver el análisis de ingeniería detrás de esa selección.

---

## 2. Drivers

### ¿Qué quiere ver el profesor?

La selección técnica y el dimensionamiento de los drivers para cada motor, 
demostrando que el driver soporta la corriente y voltaje del motor.

### Contenido esperado

- Tabla de drivers: Driver ↔ Motor asignado, con especificaciones clave
- Verificación de compatibilidad: Corriente stall del motor vs. corriente máxima del driver
- Esquema de conexión: Arduino → Driver → Motor (pines 8, 9, 10 → L298N)
- Caída de voltaje: L298N ~2V y cómo afecta el voltaje efectivo al motor
- Modos de operación: Para el Sabertooth → modo Simplified Serial, etc.

> IMPORTANTE: Los dos primeros DoF deben usar motores DC en closed-loop. 
> Verificar que los drivers soporten control bidireccional con frenado activo.

---

## 3. Control de Lazo Abierto

### ¿Qué quiere ver el profesor?

Un resumen breve de la Entrega 5, usado como línea base para comparar 
contra el lazo cerrado.

### Contenido esperado

- Código del lazo abierto (referencia a Entrega 5)
- Demostración de limitaciones: qué pasa con perturbaciones (carga, fricción)
- Datos experimentales: gráficas mostrando el error acumulado sin feedback
- Transición: "Por estas limitaciones, es necesario cerrar el lazo"

> NOTA: Este indicador es el puente narrativo entre la Entrega 5 y los 
> indicadores siguientes. No debería ser extenso.

---

## 4. Sensores

### ¿Qué quiere ver el profesor?

La selección, justificación y caracterización de los sensores de 
retroalimentación que van a cerrar el lazo.

### Contenido esperado

- Tipo de sensor: encoder incremental, absoluto, potenciómetro, IMU, etc.
- Especificaciones del datasheet: PPR, tipo de salida (cuadratura A/B), voltaje
- Cálculo de resolución angular: 360° / (PPR × factor_cuadratura × reductora)
- Esquema de conexión: pines del encoder → pines de interrupción del micro
- Código de lectura: rutina usando interrupts
- Calibración: pulsos → grados o radianes

### Ejemplo de lo que espera ver:

```
Encoder del JGA25-370:
  - PPR: 11 pulsos/rev (antes de la reductora)
  - Reductora: 1:100
  - Resolución efectiva: 11 × 100 × 4 (cuadratura) = 4400 pulsos/rev
  - Resolución angular: 360° / 4400 = 0.0818° por pulso
```

---

## 5. Control de Lazo Cerrado: Modelo del Motor

### ¿Qué quiere ver el profesor?

El desarrollo matemático COMPLETO del modelo del motor DC, desde las 
ecuaciones fundamentales hasta la función de transferencia, con todo 
el álgebra desplegado paso a paso.

### Contenido esperado (PASO A PASO — OBLIGATORIO)

a) Ecuaciones fundamentales:

```
Circuito eléctrico:
  V(t) = R·i(t) + L·(di/dt) + e(t)

Back-EMF:
  e(t) = Ke·ω(t)

Ecuación mecánica (2da ley Newton rotacional):
  J·(dω/dt) = Km·i(t) - b·ω(t)
```

b) Transformada de Laplace:

```
V(s) = (R + sL)·I(s) + Ke·Ω(s)
J·s·Ω(s) = Km·I(s) - b·Ω(s)
```

c) Función de transferencia G(s) = Ω(s)/V(s):

Deducir paso a paso → despejar I(s) → sustituir → obtener:

```
G(s) = Km / [(R + sL)(Js + b) + Ke·Km]

Si L ≈ 0:
G(s) = K / (τs + 1)

donde:
  K = Km / (R·b + Ke·Km)    [RPM/V]
  τ = J·R / (R·b + Ke·Km)   [s]
```

d) Identificación de parámetros:

| Parámetro | Cómo obtenerlo | Valor del proyecto |
|---|---|---|
| K (ganancia) | Experimental: vel. steady-state / voltaje | ~23.3 RPM/V |
| τ (cte. tiempo) | Experimental: tiempo al 63.2% | ~0.12 s |
| R (resistencia) | Multímetro en terminales | Medir |
| Ke, Km | Datasheet o cálculo | Calcular |

> IMPORTANTE: El profesor quiere TODO el álgebra, no solo G(s) = K/(τs+1).

---

## 6. Simulación

### ¿Qué quiere ver el profesor?

La simulación completa del sistema de control en lazo cerrado 
(no solo el motor en lazo abierto como en la Entrega 5).

### Contenido esperado

- Modelo en MATLAB/Simulink: motor como tf() o ss(), PID, lazo cerrado
- Diseño del PID: método de sintonización (Ziegler-Nichols, pidtune(), etc.)
- Gráficas de respuesta: step response → settling time, overshoot, error SS
- Comparación lazo abierto vs. cerrado: gráfica superpuesta
- Análisis de robustez: variación de carga, caída de voltaje de batería
- Parámetros del PID obtenidos: Kp, Ki, Kd con unidades y justificación

### Diagrama de bloques del sistema:

```
Ref(θ_des) → [e(t)] → [PID] → [u(t)/PWM] → [L298N] → [V(t)] → [Motor G(s)] → [ω(t)]
                ↑                                                        |
                |                                                        ↓
                └────────────── [Encoder] ← [θ(t)] ← [∫ vel→pos] ──────┘
```

---

## 7. Implementación

### ¿Qué quiere ver el profesor?

El código REAL del controlador PID corriendo en el microcontrolador, 
leyendo el encoder y actuando sobre el motor.

### Contenido esperado

- Código del firmware: PID discreto, lectura encoder por interrupts, PWM
- Algoritmo PID discreto: 
  u[k] = u[k-1] + Kp·(e[k]-e[k-1]) + Ki·Ts·e[k] + Kd·(e[k]-2e[k-1]+e[k-2])/Ts
- Anti-windup: mecanismo para evitar saturación del integrador
- Período de muestreo (Ts): justificar (típ. 1-10 ms para motores DC)
- Transferencia de parámetros: cómo pasaste Kp, Ki, Kd de MATLAB al firmware
- Esquema de conexión real: foto o diagrama del montaje físico

> NOTA: El código tiene que funcionar en el hardware real. No basta con MATLAB.

---

## 8. Validación

### ¿Qué quiere ver el profesor?

Evidencia experimental CUANTITATIVA de que el lazo cerrado funciona 
mejor que el lazo abierto.

### Contenido esperado

| Prueba | Qué medir | Qué mostrar |
|---|---|---|
| Seguimiento de referencia | Pos. real vs. deseada (step, rampa) | Gráfica + error |
| Rechazo a perturbaciones | Torque externo, ver corrección | Gráfica temporal |
| Open-loop vs. closed-loop | Error en ambos modos | Tabla: error RMS, overshoot, settling time |
| Repetibilidad | Mismo movimiento ×5-10 | Media ± desviación estándar |
| Régimen permanente | Steady-state error → 0 | Gráfica zoom |

> IMPORTANTE: Validación ≠ Simulación. Datos REALES del hardware, capturados 
> vía Serial y graficados (MATLAB, Python, o Serial Plotter).

---

## 9. Video YouTube

### Estructura recomendada

| Sección | Duración | Contenido |
|---|---|---|
| Intro | 15-30 seg | Nombre del equipo, proyecto, entrega |
| Simulación MATLAB | 1-2 min | Script/Simulink en pantalla, gráficas PID |
| Demo hardware | 2-3 min | Motor con PID: seguimiento, perturbación, recuperación |
| Datos en vivo | 1 min | Serial Plotter: pos. real vs. referencia |
| Cierre | 15 seg | Conclusiones |

---

## 10. Referencias

### Fuentes esperadas

- Datasheets: JGA25-370, L298N, encoder, ESP32-S3
- Libros: Ogata, Nise, Craig
- Notas de clase del profesor
- Software: versión MATLAB, Arduino IDE, librerías
- Recursos online consultados

---

## Checklist Final

| # | Indicador | Estado | Evidencia |
|---|---|---|---|
| 1 | Selección de motores | Pendiente | Tabla + justificación técnica |
| 2 | Drivers | Pendiente | Esquema + verificación corriente |
| 3 | Control lazo abierto | Listo (E5) | Código + gráficas (resumen) |
| 4 | Sensores | Pendiente | Datasheet + código lectura |
| 5 | Modelo del motor | Pendiente | Desarrollo matemático completo |
| 6 | Simulación | Pendiente | MATLAB/Simulink PID + gráficas |
| 7 | Implementación | Pendiente | Código firmware + foto montaje |
| 8 | Validación | Pendiente | Datos experimentales reales |
| 9 | Video YouTube | Pendiente | Enlace al video |
| 10 | Referencias | Pendiente | Lista formateada |

---

## Progresión Lógica (hilo conductor)

```
Motor elegido → Driver dimensionado → Probado en lazo abierto (limitaciones) 
→ Sensor seleccionado → Modelo matemático deducido → PID diseñado en simulación 
→ PID implementado en firmware → Validado con datos reales → Video demostrativo
```

Cada indicador es un eslabón de esta cadena. Si falta uno, la narrativa se rompe.
