/*
 * CONTROLADOR PI DE VELOCIDAD EN LAZO CERRADO (Closed-Loop) - v2
 * Plataforma: Arduino Uno (ATmega328P, lógica 5V) + L298N
 *
 * Cambios respecto a v1 (diagnóstico de la respuesta "ruidosa"):
 *  1. Decodificación de cuadratura x4 (interrupciones en A y B)  -> x2 resolución.
 *  2. Periodo de control 20 ms medido con micros() y dt REAL      -> menos quantization y sin jitter de millis().
 *  3. Se eliminó la deadband que apagaba el motor                 -> sin limit cycle.
 *     En su lugar: compensación de fricción (feedforward u0).
 *  4. El sentido de giro lo define el SIGNO DEL SETPOINT, no la salida del PI
 *     -> el PI nunca invierte el puente H bruscamente.
 *  5. Anti-windup por integración condicional (conditional integration).
 *
 * Protocolo serial (115200 baud):
 *   Entrada : velocidad deseada en rad/s (ej. "15", "-10"). "0" detiene el motor.
 *   Salida  : CSV "setpoint,velocidad_filtrada,pwm\n" cada periodo de control.
 */

#include <Arduino.h>

// -------------------------------------------------------------
// DEFINICIÓN DE PINES (Arduino Uno)
// -------------------------------------------------------------
const uint8_t ENC_A = 2;   // INT0 -> PD2
const uint8_t ENC_B = 3;   // INT1 -> PD3
const uint8_t ENA   = 9;   // PWM (Timer1, ~490 Hz)
const uint8_t IN1   = 8;   // Dirección 1 del L298N
const uint8_t IN2   = 7;   // Dirección 2 del L298N

// -------------------------------------------------------------
// PARÁMETROS DEL ENCODER  (!! VERIFICAR CON DATASHEET !!)
// -------------------------------------------------------------
const float ENCODER_PPR  = 11.0;  // Pulsos por vuelta en el eje del MOTOR (por canal)
const float GEAR_RATIO   = 12.0;  // Relación de la reductora
const float DECODE_MULT  = 4.0;   // x4: se cuentan los 4 flancos (A y B, subida y bajada)
const float TOTAL_CPR    = ENCODER_PPR * GEAR_RATIO * DECODE_MULT; // = 528 conteos/vuelta de salida

// -------------------------------------------------------------
// TEMPORIZACIÓN
// -------------------------------------------------------------
const unsigned long CONTROL_PERIOD_US = 20000UL; // 20 ms (50 Hz). Ventana de medición de velocidad.

// -------------------------------------------------------------
// GANANCIAS DEL CONTROLADOR (PROVISIONALES - sin identificación de planta)
// Unidades: PWM / (rad/s) y PWM / rad
// -------------------------------------------------------------
float kp = 3.0;
float ki = 15.0;

// Compensación de fricción estática: PWM mínimo con el que el motor empieza a girar.
// Se reutiliza el 30 que se usaba como deadband. Ajustar experimentalmente.
const float PWM_FRICTION = 30.0;
const int   PWM_MAX      = 255;

// Filtro EWMA de la velocidad medida (1.0 = sin filtro)
const float ALPHA = 0.5;

// -------------------------------------------------------------
// VARIABLES DE ESTADO
// -------------------------------------------------------------
volatile long    pulse_count = 0;   // Conteo acumulado (modificado en la ISR)
volatile uint8_t enc_state   = 0;   // Último estado (A<<1 | B)

long          previous_pulses   = 0;
unsigned long previous_time_us  = 0;
float         filtered_vel      = 0.0;  // rad/s
float         error_integral    = 0.0;  // rad
float         setpoint_rad_s    = 0.0;  // rad/s
bool          motor_enabled     = false;

// -------------------------------------------------------------
// TABLA DE DECODIFICACIÓN x4
// Índice = (estado_anterior << 2) | estado_nuevo, estado = (A<<1)|B
// Secuencia "forward" 00 -> 01 -> 11 -> 10 -> 00 (+1). Coincide con el
// sentido de conteo del código de posición (A==B en flanco de A -> ++).
// Transiciones inválidas (salto de 2 estados) devuelven 0.
// -------------------------------------------------------------
const int8_t QEM[16] = {
   0, +1, -1,  0,
  -1,  0,  0, +1,
  +1,  0,  0, -1,
   0, -1, +1,  0
};

// -------------------------------------------------------------
// ISR COMÚN PARA A Y B
// Lectura directa del registro PIND (más rápido que digitalRead, ~0.1 us vs ~4 us).
// ¡Específico del ATmega328P! Si cambias de MCU hay que reescribir esta línea.
// -------------------------------------------------------------
void encoderISR() {
  uint8_t pins = PIND;
  uint8_t a = (pins >> 2) & 0x01;   // PD2 = pin 2
  uint8_t b = (pins >> 3) & 0x01;   // PD3 = pin 3
  uint8_t new_state = (a << 1) | b;
  pulse_count += QEM[(enc_state << 2) | new_state];
  enc_state = new_state;
}

// -------------------------------------------------------------
// ACTUACIÓN: dir = +1 / -1 / 0, pwm = 0..255
// -------------------------------------------------------------
void setMotor(int dir, int pwm_val) {
  if (dir == 1) {
    digitalWrite(IN1, HIGH);
    digitalWrite(IN2, LOW);
  } else if (dir == -1) {
    digitalWrite(IN1, LOW);
    digitalWrite(IN2, HIGH);
  } else {
    digitalWrite(IN1, LOW);
    digitalWrite(IN2, LOW);
    pwm_val = 0;
  }
  analogWrite(ENA, pwm_val);
}

// -------------------------------------------------------------
// RESET DEL CONTROLADOR (al parar o cambiar de sentido)
// -------------------------------------------------------------
void resetController() {
  error_integral = 0.0;
  filtered_vel   = 0.0;
}

// -------------------------------------------------------------
// SETUP
// -------------------------------------------------------------
void setup() {
  Serial.begin(115200);

  pinMode(ENC_A, INPUT_PULLUP);
  pinMode(ENC_B, INPUT_PULLUP);
  enc_state = (digitalRead(ENC_A) << 1) | digitalRead(ENC_B); // Estado inicial válido
  attachInterrupt(digitalPinToInterrupt(ENC_A), encoderISR, CHANGE);
  attachInterrupt(digitalPinToInterrupt(ENC_B), encoderISR, CHANGE);

  pinMode(IN1, OUTPUT);
  pinMode(IN2, OUTPUT);
  pinMode(ENA, OUTPUT);
  setMotor(0, 0);

  previous_time_us = micros();
}

// -------------------------------------------------------------
// LOOP
// -------------------------------------------------------------
void loop() {
  // ---- 1. Comandos por serial ----
  if (Serial.available() > 0) {
    float new_sp = Serial.parseFloat();
    while (Serial.available() > 0) Serial.read();   // Descartar \r \n

    // Si cambia el sentido de giro (o se detiene), reiniciar el estado del PI
    if ((new_sp > 0) != (setpoint_rad_s > 0) || new_sp == 0.0) {
      resetController();
    }
    setpoint_rad_s = new_sp;
    motor_enabled  = (new_sp != 0.0);
  }

  // ---- 2. Ejecución periódica con dt real ----
  unsigned long now_us = micros();
  unsigned long dt_us  = now_us - previous_time_us;   // Correcto aun con overflow de micros()
  if (dt_us < CONTROL_PERIOD_US) return;

  float dt = dt_us * 1.0e-6;                          // s
  previous_time_us = now_us;

  // ---- 3. Medición de velocidad ----
  noInterrupts();
  long current_pulses = pulse_count;
  interrupts();

  long  delta_pulses = current_pulses - previous_pulses;
  previous_pulses    = current_pulses;

  float raw_vel = ((float)delta_pulses / TOTAL_CPR) * TWO_PI / dt;   // rad/s
  filtered_vel  = ALPHA * raw_vel + (1.0 - ALPHA) * filtered_vel;

  // ---- 4. Motor deshabilitado ----
  if (!motor_enabled) {
    setMotor(0, 0);
    return;
  }

  // ---- 5. PI trabajando sobre MAGNITUDES (sentido fijado por el setpoint) ----
  int   dir     = (setpoint_rad_s > 0) ? 1 : -1;
  float sp_mag  = fabs(setpoint_rad_s);
  float vel_mag = filtered_vel * dir;                 // Velocidad en el sentido deseado
  float error   = sp_mag - vel_mag;

  // Salida tentativa con la integral actual
  float u = PWM_FRICTION + kp * error + ki * error_integral;

  // ---- 6. Anti-windup: integración condicional ----
  // Solo se integra si no estamos saturados, o si el error ayuda a salir de la saturación.
  bool sat_high = (u >= PWM_MAX) && (error > 0);
  bool sat_low  = (u <= 0)       && (error < 0);
  if (!sat_high && !sat_low) {
    error_integral += error * dt;
    u = PWM_FRICTION + kp * error + ki * error_integral;
  }

  // ---- 7. Saturación al rango del PWM (nunca se invierte el puente H) ----
  if (u > PWM_MAX) u = PWM_MAX;
  if (u < 0)       u = 0;
  int pwm_val = (int)(u + 0.5);

  setMotor(dir, pwm_val);

  // ---- 8. Telemetría CSV (con signo, para comparar con el setpoint) ----
  Serial.print(setpoint_rad_s);
  Serial.print(',');
  Serial.print(filtered_vel);
  Serial.print(',');
  Serial.println(pwm_val);
}
