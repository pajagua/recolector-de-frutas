# Control en Lazo Abierto — CitriBot RRP

## Resumen de Motores y Drivers

Según tu lista de componentes, tienes **3 tipos de motores** diferentes, cada uno requiere un método de control distinto:

| Motor | Tipo | Driver | Método de Control |
|:---|:---|:---|:---|
| Metal DC Gearmotor 37Dx70.1L (×2) | DC con caja reductora | Sabertooth | Señal Serial o PWM analógico |
| NEMA 17 (×2) | Paso a paso (Stepper) | TB6600 + RAMPS 1.4 (A4988) | Pulsos STEP/DIR |
| MG995 (×2) | Servo | Directo al Arduino | Señal PWM (Servo library) |

---

## Arquitectura de Control

```
Arduino MEGA 2560
    │
    ├── RAMPS 1.4 Shield
    │      ├── A4988 Driver ──► NEMA 17 (Riel, eje prismático)
    │      └── A4988 Driver ──► NEMA 17 (Transmisión secundaria)
    │
    ├── TB6600 Driver ──────────► (Reservado para articulación principal)
    │
    ├── Sabertooth Driver ──────► DC Gearmotor #1 (1ra articulación)
    │                           ► DC Gearmotor #2 (muñeca) 
    │
    ├── Pin PWM ────────────────► MG995 Servo #1 (Torsión)
    └── Pin PWM ────────────────► MG995 Servo #2 (Muñeca)
```

> [!IMPORTANT]
> En **lazo abierto** no usamos ningún sensor de retroalimentación (ni encoders, ni finales de carrera, ni IMU). Solo enviamos comandos a ciegas y confiamos en que el motor los ejecute.

---

## Código Arduino MEGA 2560

### ¿Qué hace cada parte?

El código se divide en **4 bloques lógicos**:

1. **Definición de pines:** Asigna cada pin del Arduino MEGA a su función (STEP, DIR, ENABLE, PWM, Servo).
2. **Funciones por tipo de motor:** Cada tipo de motor tiene sus propias funciones (`moverStepper()`, `moverDC()`, `moverServo()`).
3. **Secuencia de demostración:** En el `loop()`, se ejecuta una rutina predefinida que mueve todos los motores en secuencia.
4. **Control de velocidad:** Para los DC se usa PWM (0-255), para los steppers se controla la velocidad variando el retardo entre pulsos, y para los servos se envía el ángulo deseado.

```cpp
// ============================================================
// CONTROL EN LAZO ABIERTO — CitriBot RRP
// Robótica I — Universidad Nacional de Asunción
// ============================================================
// Este código controla TODOS los motores del brazo robótico
// CitriBot en modo LAZO ABIERTO (sin retroalimentación).
//
// Hardware:
//   - Arduino MEGA 2560 + RAMPS 1.4
//   - 2× NEMA 17 (Steppers) vía A4988 en RAMPS
//   - 2× DC Gearmotor 37Dx70.1L vía Sabertooth
//   - 2× MG995 Servo
//   - 1× TB6600 (reservado)
// ============================================================

#include <Servo.h>

// ===================== PINES RAMPS 1.4 =====================
// La RAMPS 1.4 montada sobre el Arduino MEGA tiene pines 
// predefinidos para los drivers de stepper (X, Y, Z, E0, E1).
// Usaremos los ejes X e Y para nuestros 2 NEMA 17.

// --- Stepper 1: NEMA 17 — Riel (Eje X en RAMPS) ---
#define STEPPER1_STEP_PIN   54   // A0 en MEGA = Pin 54
#define STEPPER1_DIR_PIN    55   // A1 en MEGA = Pin 55
#define STEPPER1_ENABLE_PIN 38   // Enable compartido

// --- Stepper 2: NEMA 17 — Transmisión secundaria (Eje Y en RAMPS) ---
#define STEPPER2_STEP_PIN   60   // A6 en MEGA = Pin 60
#define STEPPER2_DIR_PIN    61   // A7 en MEGA = Pin 61
#define STEPPER2_ENABLE_PIN 56   // A2 en MEGA = Pin 56

// ================== PINES SABERTOOTH ==================
// El driver Sabertooth puede controlarse de varias formas.
// Aquí usamos el modo "Simplified Serial" por Serial1 del MEGA.
// Sabertooth S1 → Pin TX1 (Pin 18 del MEGA)
// Velocidad serial: 9600 baudios (por defecto del Sabertooth)
//
// En modo Simplified Serial:
//   Motor 1: Valores 1-127 (1=full reversa, 64=stop, 127=full adelante)
//   Motor 2: Valores 128-255 (128=full reversa, 192=stop, 255=full adelante)

#define SABERTOOTH_SERIAL   Serial1
#define SABERTOOTH_BAUDRATE 9600

// Valores de parada para cada motor del Sabertooth
#define DC_MOTOR1_STOP 64
#define DC_MOTOR2_STOP 192

// ===================== PINES SERVO =====================
// Los servos MG995 se conectan directamente a pines PWM del MEGA.
// Usamos pines que NO están ocupados por la RAMPS.
#define SERVO1_PIN 11   // Servo de Torsión
#define SERVO2_PIN 6    // Servo de Muñeca

// =================== PARÁMETROS ===================
// Estos valores definen el comportamiento del lazo abierto.
// Puedes modificarlos para ajustar velocidades y tiempos.

#define STEPPER_DELAY_US     800   // Microsegundos entre pulsos (menor = más rápido)
#define STEPPER_STEPS_REV    200   // Pasos por revolución (1.8° por paso, sin microstepping)
#define STEPPER_MICROSTEP    16    // A4988 en RAMPS usa 1/16 microstepping por defecto

// Velocidad de los motores DC (0-63 = magnitud desde el punto medio)
#define DC_SPEED_LOW   20
#define DC_SPEED_MED   40
#define DC_SPEED_HIGH  63

// ===================== OBJETOS =====================
Servo servo1;  // Servo de torsión
Servo servo2;  // Servo de muñeca

// ============================================================
//                    FUNCIONES AUXILIARES
// ============================================================

// -------------------- STEPPERS (NEMA 17) --------------------
// Función: moverStepper
// Descripción: Envía una cantidad determinada de pulsos al driver
//   A4988 para mover el motor paso a paso.
//
// Parámetros:
//   stepPin  - Pin STEP del driver
//   dirPin   - Pin DIR del driver  
//   pasos    - Número de pasos a dar
//   direccion - true = horario, false = antihorario
//   retardo  - Microsegundos entre pulsos (controla velocidad)
//
// En LAZO ABIERTO: No verificamos si el motor realmente se movió.
// Si el torque de carga supera al del motor, se "pierden pasos"
// y la posición real será diferente a la comandada.
// ============================================================
void moverStepper(int stepPin, int dirPin, long pasos, bool direccion, int retardo) {
  // 1) Establecer la dirección de giro
  digitalWrite(dirPin, direccion ? HIGH : LOW);
  
  // 2) Enviar los pulsos STEP uno por uno
  for (long i = 0; i < pasos; i++) {
    digitalWrite(stepPin, HIGH);   // Flanco de subida = 1 paso
    delayMicroseconds(retardo);    // Medio período
    digitalWrite(stepPin, LOW);    // Flanco de bajada
    delayMicroseconds(retardo);    // Medio período
  }
  // El período total de cada paso = 2 × retardo (en µs)
  // Frecuencia de pasos = 1,000,000 / (2 × retardo) Hz
  // Con retardo=800µs → Freq = 625 pasos/s
  // Con microstepping 1/16: 625/16 ≈ 39 RPM para un motor de 200 pasos/rev
}

// Función auxiliar: mover stepper una fracción de revolución
void moverStepperGrados(int stepPin, int dirPin, float grados, bool direccion, int retardo) {
  // Calcular pasos necesarios considerando microstepping
  long pasos = (long)((grados / 360.0) * STEPPER_STEPS_REV * STEPPER_MICROSTEP);
  moverStepper(stepPin, dirPin, pasos, direccion, retardo);
}

// -------------------- MOTORES DC (Sabertooth) --------------------
// Función: moverDC
// Descripción: Envía un comando serial al Sabertooth para mover
//   un motor DC a una velocidad determinada.
//
// Parámetros:
//   motor     - 1 o 2 (qué motor del Sabertooth)
//   velocidad - -63 a +63 (negativo=reversa, 0=stop, positivo=adelante)
//
// El Sabertooth en modo Simplified Serial interpreta:
//   Byte para Motor 1: 1 (full rev) ... 64 (stop) ... 127 (full fwd)
//   Byte para Motor 2: 128 (full rev) ... 192 (stop) ... 255 (full fwd)
// ============================================================
void moverDC(int motor, int velocidad) {
  // Limitar velocidad al rango válido
  velocidad = constrain(velocidad, -63, 63);
  
  byte comando;
  if (motor == 1) {
    // Motor 1: rango 1-127, centro en 64
    comando = (byte)(DC_MOTOR1_STOP + velocidad);
  } else {
    // Motor 2: rango 128-255, centro en 192
    comando = (byte)(DC_MOTOR2_STOP + velocidad);
  }
  
  SABERTOOTH_SERIAL.write(comando);
}

// Función: frenarDC
// Descripción: Detiene ambos motores DC
void frenarDC() {
  moverDC(1, 0);
  moverDC(2, 0);
}

// -------------------- SERVOS (MG995) --------------------
// Función: moverServo
// Descripción: Mueve un servo MG995 a un ángulo específico.
//
// Parámetros:
//   servo  - Referencia al objeto Servo
//   angulo - 0 a 180 grados
//
// El MG995 tiene un rango de ~120° útiles (varía por unidad).
// La señal PWM que genera la librería Servo es de 50Hz con
// pulso entre 544µs (0°) y 2400µs (180°).
//
// En LAZO ABIERTO: Técnicamente el servo tiene su propio lazo
// cerrado interno (un potenciómetro dentro), pero desde el punto
// de vista de NUESTRO sistema, es lazo abierto porque el Arduino
// no recibe ninguna señal de vuelta del servo.
// ============================================================
void moverServo(Servo &servo, int angulo) {
  angulo = constrain(angulo, 0, 180);
  servo.write(angulo);
}

// Función: barridoServo
// Descripción: Mueve un servo suavemente de un ángulo a otro
void barridoServo(Servo &servo, int desde, int hasta, int retardoMs) {
  if (desde < hasta) {
    for (int pos = desde; pos <= hasta; pos++) {
      servo.write(pos);
      delay(retardoMs);
    }
  } else {
    for (int pos = desde; pos >= hasta; pos--) {
      servo.write(pos);
      delay(retardoMs);
    }
  }
}

// ============================================================
//                         SETUP
// ============================================================
void setup() {
  // --- Comunicación Serial para debug ---
  Serial.begin(115200);
  Serial.println("=== CitriBot RRP — Control Lazo Abierto ===");
  Serial.println("Iniciando...");
  
  // --- Inicializar comunicación con Sabertooth ---
  SABERTOOTH_SERIAL.begin(SABERTOOTH_BAUDRATE);
  delay(100);  // Esperar a que el Sabertooth arranque
  frenarDC();  // Asegurar que los motores DC estén detenidos
  
  // --- Configurar pines de los Steppers ---
  pinMode(STEPPER1_STEP_PIN, OUTPUT);
  pinMode(STEPPER1_DIR_PIN, OUTPUT);
  pinMode(STEPPER1_ENABLE_PIN, OUTPUT);
  
  pinMode(STEPPER2_STEP_PIN, OUTPUT);
  pinMode(STEPPER2_DIR_PIN, OUTPUT);
  pinMode(STEPPER2_ENABLE_PIN, OUTPUT);
  
  // Habilitar los drivers A4988 (LOW = habilitado, HIGH = deshabilitado)
  // ¡OJO! En los A4988, el pin ENABLE es activo en BAJO.
  digitalWrite(STEPPER1_ENABLE_PIN, LOW);
  digitalWrite(STEPPER2_ENABLE_PIN, LOW);
  
  // --- Inicializar Servos ---
  servo1.attach(SERVO1_PIN);
  servo2.attach(SERVO2_PIN);
  
  // Llevar servos a posición inicial (90° = centro)
  servo1.write(90);
  servo2.write(90);
  
  Serial.println("Todos los motores inicializados.");
  Serial.println("Esperando 3 segundos antes de iniciar demo...");
  delay(3000);
}

// ============================================================
//                      LOOP PRINCIPAL
//               SECUENCIA DE DEMOSTRACIÓN
// ============================================================
// Esta secuencia muestra el control en lazo abierto de TODOS
// los motores del CitriBot. Cada movimiento es "a ciegas":
// el Arduino no sabe si el motor realmente se movió.
// ============================================================
void loop() {
  
  // ========== FASE 1: STEPPERS ==========
  Serial.println("\n--- FASE 1: Motores Paso a Paso (NEMA 17) ---");
  
  // Stepper 1 (Riel): Mover 90° en sentido horario
  Serial.println("Stepper 1 (Riel): Avanzando 90 grados...");
  moverStepperGrados(STEPPER1_STEP_PIN, STEPPER1_DIR_PIN, 90.0, true, STEPPER_DELAY_US);
  delay(1000);
  
  // Stepper 1 (Riel): Regresar 90° en sentido antihorario
  Serial.println("Stepper 1 (Riel): Retrocediendo 90 grados...");
  moverStepperGrados(STEPPER1_STEP_PIN, STEPPER1_DIR_PIN, 90.0, false, STEPPER_DELAY_US);
  delay(1000);
  
  // Stepper 2 (Transmisión): Girar 180°
  Serial.println("Stepper 2 (Transmision): Girando 180 grados...");
  moverStepperGrados(STEPPER2_STEP_PIN, STEPPER2_DIR_PIN, 180.0, true, STEPPER_DELAY_US);
  delay(1000);
  
  // Stepper 2: Regresar
  Serial.println("Stepper 2 (Transmision): Regresando...");
  moverStepperGrados(STEPPER2_STEP_PIN, STEPPER2_DIR_PIN, 180.0, false, STEPPER_DELAY_US);
  delay(1000);
  
  // ========== FASE 2: MOTORES DC ==========
  Serial.println("\n--- FASE 2: Motores DC (Sabertooth) ---");
  
  // Motor DC 1: Adelante a velocidad media por 2 segundos
  Serial.println("DC Motor 1: Adelante (velocidad media)...");
  moverDC(1, DC_SPEED_MED);
  delay(2000);
  
  // Motor DC 1: Frenar
  Serial.println("DC Motor 1: Frenando...");
  moverDC(1, 0);
  delay(1000);
  
  // Motor DC 1: Reversa a velocidad baja por 2 segundos
  Serial.println("DC Motor 1: Reversa (velocidad baja)...");
  moverDC(1, -DC_SPEED_LOW);
  delay(2000);
  
  // Motor DC 1: Frenar
  moverDC(1, 0);
  delay(1000);
  
  // Motor DC 2: Adelante a velocidad alta por 1.5 segundos
  Serial.println("DC Motor 2: Adelante (velocidad alta)...");
  moverDC(2, DC_SPEED_HIGH);
  delay(1500);
  
  // Motor DC 2: Frenar
  Serial.println("DC Motor 2: Frenando...");
  moverDC(2, 0);
  delay(1000);
  
  // ========== FASE 3: SERVOS ==========
  Serial.println("\n--- FASE 3: Servos MG995 ---");
  
  // Servo 1 (Torsión): Barrido de 90° a 0° y luego a 180°
  Serial.println("Servo 1 (Torsion): Barrido 90 -> 0...");
  barridoServo(servo1, 90, 0, 15);
  delay(500);
  
  Serial.println("Servo 1 (Torsion): Barrido 0 -> 180...");
  barridoServo(servo1, 0, 180, 15);
  delay(500);
  
  Serial.println("Servo 1 (Torsion): Regresando a centro...");
  barridoServo(servo1, 180, 90, 15);
  delay(1000);
  
  // Servo 2 (Muñeca): Movimiento rápido entre posiciones
  Serial.println("Servo 2 (Muneca): Posicion 45...");
  moverServo(servo2, 45);
  delay(700);
  
  Serial.println("Servo 2 (Muneca): Posicion 135...");
  moverServo(servo2, 135);
  delay(700);
  
  Serial.println("Servo 2 (Muneca): Regresando a centro...");
  moverServo(servo2, 90);
  delay(1000);
  
  // ========== FIN DE SECUENCIA ==========
  Serial.println("\n=== SECUENCIA COMPLETA ===");
  Serial.println("Esperando 5 segundos para repetir...\n");
  delay(5000);
}
```

---

## Explicación Línea por Línea

### Bloque 1 — Pines RAMPS 1.4
La RAMPS 1.4 tiene pines **fijos** ya asignados para cada eje de una impresora 3D. Nosotros "reciclamos" esos ejes:
- **Eje X** (pines 54, 55, 38) → Nuestro Stepper 1 (Riel)
- **Eje Y** (pines 60, 61, 56) → Nuestro Stepper 2 (Transmisión)

Cada driver A4988 necesita 3 señales:
- **STEP:** Cada pulso HIGH→LOW mueve el motor exactamente 1 micro-paso
- **DIR:** HIGH = giro horario, LOW = giro antihorario
- **ENABLE:** LOW = driver activo (¡es activo en bajo!)

### Bloque 2 — Sabertooth (Motores DC)
El Sabertooth es un driver bidireccional de alta corriente. En modo **Simplified Serial**:
- Se comunica por un solo cable serial (TX del Arduino → S1 del Sabertooth)
- Un solo byte controla cada motor:
  - Motor 1: byte `1`=full reversa, `64`=parado, `127`=full adelante
  - Motor 2: byte `128`=full reversa, `192`=parado, `255`=full adelante

### Bloque 3 — Servos MG995
Los servos son los más simples de controlar. La librería `Servo.h` de Arduino genera automáticamente la señal PWM correcta (50 Hz, pulso de 1-2ms). Solo necesitas llamar `servo.write(angulo)`.

> [!NOTE]
> Aunque el servo MG995 tiene internamente un potenciómetro que actúa como retroalimentación interna, desde el punto de vista de **nuestro sistema** (el Arduino), sigue siendo lazo abierto porque el Arduino **nunca recibe información** del servo sobre su posición real.

---

## Código MATLAB — Simulación y Visualización

Este script de MATLAB simula el comportamiento de los motores en lazo abierto y genera gráficas para visualizar las señales de control y las respuestas esperadas de cada motor.

```matlab
%% ============================================================
%  SIMULACIÓN DE CONTROL EN LAZO ABIERTO — CitriBot RRP
%  Robótica I — Universidad Nacional de Asunción
%  ============================================================
%  Este script simula el comportamiento de los 3 tipos de motores
%  del brazo CitriBot controlados en lazo abierto.
%  
%  Modelos utilizados:
%    Motor DC:     Función de transferencia de 1er orden
%    Stepper:      Respuesta ideal (escalón de posición)
%    Servo MG995:  Respuesta de 2do orden subamortiguada
%  ============================================================

clear; clc; close all;

%% =================== PARÁMETROS DEL SISTEMA ===================

% --- Motor DC 37Dx70.1L (con caja reductora) ---
% Modelamos el motor DC como un sistema de primer orden:
%   G(s) = K / (tau*s + 1)
% donde:
%   K   = ganancia estática (velocidad final / voltaje aplicado)
%   tau = constante de tiempo (tiempo para alcanzar 63.2% de la vel. final)

dc.K   = 100;       % [RPM/V] Ganancia del motor (aprox.)
dc.tau = 0.15;      % [s] Constante de tiempo mecánica + eléctrica
dc.Vmax = 12;       % [V] Voltaje máximo de alimentación

% --- Motor Paso a Paso NEMA 17 ---
% En lazo abierto, el stepper se modela como un integrador ideal:
% cada pulso incrementa la posición en un ángulo fijo (1.8°/paso).
% La velocidad depende de la frecuencia de pulsos.

stepper.step_angle = 1.8;          % [°/paso] Ángulo por paso completo
stepper.microstep  = 16;           % Factor de microstepping (A4988)
stepper.angle_per_ustep = stepper.step_angle / stepper.microstep;  % 0.1125°
stepper.pulse_delay_us = 800;      % [µs] Retardo entre pulsos
stepper.pulse_freq = 1e6 / (2 * stepper.pulse_delay_us); % [Hz] Frecuencia de pulsos

% --- Servo MG995 ---
% Modelamos el servo como un sistema de 2do orden:
%   G(s) = wn^2 / (s^2 + 2*zeta*wn*s + wn^2)
% El MG995 tiene una velocidad de ~0.2s/60° → wn ≈ 30 rad/s

servo.wn   = 30;     % [rad/s] Frecuencia natural
servo.zeta = 0.7;    % Factor de amortiguamiento (ligeramente subamortiguado)
servo.max_angle = 180; % [°] Rango máximo

%% =================== SIMULACIÓN TEMPORAL ===================
dt = 0.001;          % [s] Paso de simulación (1 ms)
t_total = 10;        % [s] Tiempo total de simulación
t = 0:dt:t_total;    % Vector de tiempo
N = length(t);

%% ---------- 1. SIMULACIÓN DEL MOTOR DC ----------
% Secuencia de comandos PWM (lazo abierto):
%   0-2s:   PWM = 0     (detenido)
%   2-4s:   PWM = 50%   (velocidad media adelante)
%   4-5s:   PWM = 0     (freno)
%   5-7s:   PWM = -30%  (velocidad baja reversa)
%   7-10s:  PWM = 0     (detenido)

% Señal de comando (porcentaje de PWM, -100 a +100)
pwm_cmd = zeros(1, N);
pwm_cmd(t >= 2 & t < 4) = 50;      % 50% adelante
pwm_cmd(t >= 5 & t < 7) = -30;     % 30% reversa

% Voltaje aplicado al motor
V_motor = (pwm_cmd / 100) * dc.Vmax;

% Simular respuesta del motor DC (Euler hacia adelante)
% dw/dt = (1/tau) * (K*V - w)
dc_vel = zeros(1, N);   % Velocidad angular [RPM]
dc_pos = zeros(1, N);   % Posición angular [°] (integral de velocidad)

for i = 2:N
    dw = (1/dc.tau) * (dc.K * V_motor(i-1) - dc_vel(i-1));
    dc_vel(i) = dc_vel(i-1) + dw * dt;
    dc_pos(i) = dc_pos(i-1) + dc_vel(i-1) * (360/60) * dt;  % RPM → °/s
end

%% ---------- 2. SIMULACIÓN DEL STEPPER NEMA 17 ----------
% Secuencia de comandos (lazo abierto):
%   0-1s:   Detenido
%   1-3s:   Avanzar 90° en sentido horario
%   3-4s:   Detenido
%   4-6s:   Retroceder 90° en sentido antihorario
%   6-10s:  Detenido

% Calcular cuántos pasos para 90°
steps_for_90 = round(90 / stepper.angle_per_ustep);
% Tiempo que tarda en dar esos pasos
time_for_90 = steps_for_90 * (2 * stepper.pulse_delay_us * 1e-6);

stepper_pos = zeros(1, N);         % Posición acumulada [°]
stepper_vel_cmd = zeros(1, N);     % Velocidad comandada [°/s]

% Velocidad angular del stepper cuando está en movimiento
stepper_speed = stepper.angle_per_ustep * stepper.pulse_freq;  % [°/s]

% Avance (1s hasta completar 90°)
t_start_fwd = 1.0;
t_end_fwd = t_start_fwd + time_for_90;
stepper_vel_cmd(t >= t_start_fwd & t < t_end_fwd) = stepper_speed;

% Retroceso (4s hasta completar 90°)
t_start_rev = 4.0;
t_end_rev = t_start_rev + time_for_90;
stepper_vel_cmd(t >= t_start_rev & t < t_end_rev) = -stepper_speed;

% Integrar para obtener posición
for i = 2:N
    stepper_pos(i) = stepper_pos(i-1) + stepper_vel_cmd(i-1) * dt;
end

%% ---------- 3. SIMULACIÓN DEL SERVO MG995 ----------
% Secuencia de comandos (lazo abierto):
%   0-1s:   Posición 90° (centro)
%   1-3s:   Ir a 0°
%   3-5s:   Ir a 180°
%   5-7s:   Ir a 90° (centro)
%   7-10s:  Mantener 90°

servo_cmd = 90 * ones(1, N);       % Comando de ángulo [°]
servo_cmd(t >= 1 & t < 3) = 0;
servo_cmd(t >= 3 & t < 5) = 180;
servo_cmd(t >= 5) = 90;

% Simular respuesta del servo (sistema de 2do orden)
% d²θ/dt² + 2*zeta*wn*dθ/dt + wn²*θ = wn²*θ_cmd
servo_pos = zeros(1, N);           % Posición real del servo [°]
servo_vel = zeros(1, N);           % Velocidad del servo [°/s]
servo_pos(1) = 90;                 % Posición inicial

for i = 2:N
    error = servo_cmd(i-1) - servo_pos(i-1);
    accel = servo.wn^2 * error - 2 * servo.zeta * servo.wn * servo_vel(i-1);
    servo_vel(i) = servo_vel(i-1) + accel * dt;
    servo_pos(i) = servo_pos(i-1) + servo_vel(i-1) * dt;
end

%% =================== GRÁFICAS ===================

figure('Name', 'Control en Lazo Abierto — CitriBot RRP', ...
       'Position', [100 50 900 750], 'Color', 'w');

% --- Motor DC ---
subplot(3,2,1);
plot(t, pwm_cmd, 'b-', 'LineWidth', 1.5);
xlabel('Tiempo [s]'); ylabel('PWM [%]');
title('Motor DC — Comando PWM (Entrada)');
grid on; ylim([-60 60]);

subplot(3,2,2);
yyaxis left;
plot(t, dc_vel, 'r-', 'LineWidth', 1.5);
ylabel('Velocidad [RPM]');
yyaxis right;
plot(t, dc_pos, 'g-', 'LineWidth', 1.2);
ylabel('Posición [°]');
xlabel('Tiempo [s]');
title('Motor DC — Respuesta (Velocidad y Posición)');
grid on; legend('Velocidad', 'Posición', 'Location', 'best');

% --- Stepper NEMA 17 ---
subplot(3,2,3);
plot(t, stepper_vel_cmd, 'b-', 'LineWidth', 1.5);
xlabel('Tiempo [s]'); ylabel('Velocidad [°/s]');
title('Stepper NEMA 17 — Velocidad Comandada');
grid on;

subplot(3,2,4);
plot(t, stepper_pos, 'r-', 'LineWidth', 1.5);
xlabel('Tiempo [s]'); ylabel('Posición [°]');
title('Stepper NEMA 17 — Posición Acumulada');
grid on;

% --- Servo MG995 ---
subplot(3,2,5);
stairs(t, servo_cmd, 'b-', 'LineWidth', 1.5);
xlabel('Tiempo [s]'); ylabel('Ángulo [°]');
title('Servo MG995 — Comando de Ángulo (Entrada)');
grid on; ylim([-10 200]);

subplot(3,2,6);
plot(t, servo_pos, 'r-', 'LineWidth', 1.5);
hold on;
stairs(t, servo_cmd, 'b--', 'LineWidth', 1);
xlabel('Tiempo [s]'); ylabel('Ángulo [°]');
title('Servo MG995 — Respuesta Real vs Comando');
legend('Respuesta real', 'Comando', 'Location', 'best');
grid on; ylim([-10 200]);

sgtitle('CONTROL EN LAZO ABIERTO — CitriBot RRP', 'FontSize', 14, 'FontWeight', 'bold');

%% =================== ANÁLISIS ADICIONAL ===================
% Imprimir resumen de la simulación

fprintf('\n========================================\n');
fprintf('  RESUMEN DE SIMULACIÓN — LAZO ABIERTO\n');
fprintf('========================================\n\n');

fprintf('MOTOR DC (37Dx70.1L):\n');
fprintf('  Ganancia K = %.0f RPM/V\n', dc.K);
fprintf('  Constante de tiempo tau = %.3f s\n', dc.tau);
fprintf('  Velocidad máxima teórica = %.0f RPM (a %dV)\n', dc.K*dc.Vmax, dc.Vmax);
fprintf('  Tiempo de establecimiento (5%%) ≈ %.3f s\n\n', 3*dc.tau);

fprintf('STEPPER NEMA 17:\n');
fprintf('  Ángulo por micro-paso = %.4f°\n', stepper.angle_per_ustep);
fprintf('  Frecuencia de pulsos = %.0f Hz\n', stepper.pulse_freq);
fprintf('  Velocidad angular = %.1f °/s (%.1f RPM)\n', stepper_speed, stepper_speed/6);
fprintf('  Tiempo para 90° = %.3f s (%d micro-pasos)\n\n', time_for_90, steps_for_90);

fprintf('SERVO MG995:\n');
fprintf('  Frecuencia natural wn = %.0f rad/s\n', servo.wn);
fprintf('  Amortiguamiento zeta = %.2f\n', servo.zeta);
fprintf('  Tiempo de establecimiento (2%%) ≈ %.3f s\n', 4/(servo.zeta*servo.wn));
fprintf('  Sobrepaso máximo ≈ %.1f%%\n', 100*exp(-pi*servo.zeta/sqrt(1-servo.zeta^2)));

%% =================== DIAGRAMA DE BLOQUES ===================
% Visualizar la diferencia entre lazo abierto y lazo cerrado

figure('Name', 'Lazo Abierto vs Lazo Cerrado', ...
       'Position', [150 100 800 400], 'Color', 'w');

% Texto explicativo con anotaciones
annotation('textbox', [0.05 0.55 0.9 0.4], ...
    'String', {'\bf LAZO ABIERTO (Esta entrega):', '', ...
    '  Comando → [Controlador/Arduino] → [Driver] → [Motor] → Movimiento', '', ...
    '  ❌ No hay sensor midiendo la salida', ...
    '  ❌ No hay corrección de errores', ...
    '  ✅ Simple de implementar', ...
    '  ✅ No requiere sensores adicionales'}, ...
    'FontSize', 11, 'EdgeColor', [0.2 0.6 0.2], 'LineWidth', 2, ...
    'BackgroundColor', [0.9 1 0.9]);

annotation('textbox', [0.05 0.05 0.9 0.4], ...
    'String', {'\bf LAZO CERRADO (Siguiente entrega):', '', ...
    '  Comando → [Controlador] → [Driver] → [Motor] → Movimiento', ...
    '                    ↑                                      │', ...
    '                    └──────── [Sensor/Encoder] ←────────┘', '', ...
    '  ✅ Corrige errores automáticamente', ...
    '  ✅ Mayor precisión', ...
    '  ❌ Requiere sensores (encoders, IMU, etc.)', ...
    '  ❌ Más complejo de diseñar y sintonizar'}, ...
    'FontSize', 11, 'EdgeColor', [0.8 0.2 0.2], 'LineWidth', 2, ...
    'BackgroundColor', [1 0.9 0.9]);

sgtitle('Comparación: Lazo Abierto vs Lazo Cerrado', 'FontSize', 14, 'FontWeight', 'bold');
```

---

## Cómo Interpretar las Gráficas de MATLAB

### Motor DC
- **Gráfica izquierda (Entrada):** Muestra la señal PWM que envías al motor. Es un escalón: 0% → 50% → 0% → -30% → 0%.
- **Gráfica derecha (Salida):** Muestra cómo la velocidad del motor NO cambia instantáneamente (tiene una constante de tiempo \(\tau\)). La curva roja sube exponencialmente hasta estabilizarse. La curva verde muestra la posición acumulada (integral de la velocidad).

### Stepper NEMA 17
- **Gráfica izquierda:** La velocidad es un escalón perfecto (o se mueve a X°/s o está parado).
- **Gráfica derecha:** La posición es una rampa lineal. El stepper es "ideal" en lazo abierto: si no pierde pasos, la posición final es exactamente la comandada.

### Servo MG995
- **Gráfica izquierda:** Los comandos de ángulo son escalones (saltos instantáneos de posición deseada).
- **Gráfica derecha:** La respuesta real (roja) sigue al comando (azul punteado) pero con un retardo y un pequeño sobrepaso (overshoot), característico de un sistema de 2do orden subamortiguado.

---

## Parámetros que Puedes Modificar en MATLAB

| Variable | Qué controla | Efecto al aumentar |
|:---|:---|:---|
| `dc.K` | Ganancia del motor DC | Motor gira más rápido para el mismo voltaje |
| `dc.tau` | Constante de tiempo DC | Motor responde más lento |
| `stepper.pulse_delay_us` | Retardo entre pulsos | Stepper gira más lento (pero con más torque) |
| `servo.wn` | Frecuencia natural del servo | Servo responde más rápido |
| `servo.zeta` | Amortiguamiento del servo | Menos overshoot pero más lento |

> [!TIP]
> **Para mejorar el controlador desde MATLAB:** Juega con `dc.tau` y `dc.K` para que la simulación coincida con el comportamiento real que observes en tu video. Luego, en la siguiente entrega (lazo cerrado), podrás diseñar un PID en MATLAB usando `pidtune()` o Simulink y transferir esos parámetros al Arduino.
