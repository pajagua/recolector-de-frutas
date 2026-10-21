/*
 * CONTROL HÍBRIDO: VELOCIDAD Y POSICIÓN INDEPENDIENTES
 * 
 * Comandos por Serial (115200 baud, No Line Ending o Newline):
 * 'P90'  -> Ir a 90 grados (Control de Posición)
 * 'V100' -> Girar a 100 rad/s continuamente (Control de Velocidad)
 * 'S'    -> Stop / Apagar Motor
 */

#include <Arduino.h>

// Pines del L298N (Configurados para Arduino Mega)
const int ENA = 9;  // PWM
const int IN1 = 8;  // Dirección 1
const int IN2 = 7;  // Dirección 2

// Pines del Encoder
const int ENC_A = 2; // Pin de interrupción
const int ENC_B = 3; // Pin de interrupción

// Constantes físicas
const float CPR_BASE = 132.0; 
const float SAMPLE_TIME_S = 0.01; // 10 ms

// --- GANANCIAS PID ---
// 1. Ganancias para Posición (Sintonizadas en MATLAB)
float kp_pos = 1.0843; 
float ki_pos = 2.5465;

// 2. Ganancias para Velocidad (Sintonizadas en MATLAB)
float kp_vel = 0.10008; 
float ki_vel = 10.1957;

// --- VARIABLES DE ESTADO ---
enum ControlMode { MODE_IDLE, MODE_POSITION, MODE_VELOCITY };
ControlMode current_mode = MODE_IDLE;

float setpoint = 0.0; // Puede significar rad (posición) o rad/s (velocidad)
float error_integral = 0.0;

volatile long pulse_count = 0; 
long previous_pulses = 0;
unsigned long previous_time = 0; 

// Interrupción del Encoder
void readEncoder() {
  int b = digitalRead(ENC_B);
  if (b > 0) pulse_count++;
  else       pulse_count--;
}

void setMotor(int dir, int pwmVal) {
  analogWrite(ENA, pwmVal);
  if (dir == 1) {
    digitalWrite(IN1, HIGH);
    digitalWrite(IN2, LOW);
  } else if (dir == -1) {
    digitalWrite(IN1, LOW);
    digitalWrite(IN2, HIGH);
  } else {
    digitalWrite(IN1, LOW);
    digitalWrite(IN2, LOW);
  }
}

void setup() {
  Serial.begin(115200);
  Serial.println("\n--- SISTEMA HIBRIDO INICIADO ---");
  Serial.println("Comandos:");
  Serial.println("  P<grados>  -> Posicion (Ej: P90 o P-180)");
  Serial.println("  V<rad/s>   -> Velocidad (Ej: V50 o V-100)");
  Serial.println("  S          -> Stop de emergencia");
  
  pinMode(ENC_A, INPUT_PULLUP);
  pinMode(ENC_B, INPUT_PULLUP);
  attachInterrupt(digitalPinToInterrupt(ENC_A), readEncoder, RISING);

  pinMode(ENA, OUTPUT);
  pinMode(IN1, OUTPUT);
  pinMode(IN2, OUTPUT);

  setMotor(0, 0);
}

void loop() {
  // 1. PARSER DE COMANDOS SERIALES
  if (Serial.available() > 0) {
    char cmd = Serial.read();
    
    if (cmd == 'P' || cmd == 'p') {
      float grados = Serial.parseFloat();
      setpoint = grados * (PI / 180.0); // Convertir a radianes
      current_mode = MODE_POSITION;
      error_integral = 0.0; // Resetear integrador al cambiar de modo
      Serial.print("\n>>> MODO POSICION: "); Serial.print(grados); Serial.println(" grados");
    } 
    else if (cmd == 'V' || cmd == 'v') {
      float vel = Serial.parseFloat();
      setpoint = vel; // Rad/s
      current_mode = MODE_VELOCITY;
      error_integral = 0.0;
      Serial.print("\n>>> MODO VELOCIDAD: "); Serial.print(vel); Serial.println(" rad/s");
    }
    else if (cmd == 'S' || cmd == 's') {
      current_mode = MODE_IDLE;
      setMotor(0, 0);
      Serial.println("\n>>> MODO IDLE (Motor Apagado)");
    }
    
    // Limpiar buffer de caracteres basura (\n, \r, espacios)
    while (Serial.available() > 0) Serial.read();
  }

  unsigned long current_time = millis();
  
  if (current_time - previous_time >= (SAMPLE_TIME_S * 1000.0)) {
    // 2. Lectura segura del encoder
    noInterrupts();
    long current_pulses = pulse_count;
    interrupts();

    // Calcular Posición y Velocidad Actual
    float current_position_rad = (current_pulses / CPR_BASE) * (2.0 * PI);
    
    long delta_pulses = current_pulses - previous_pulses;
    float current_velocity_rad_s = (delta_pulses / CPR_BASE) * (2.0 * PI) / SAMPLE_TIME_S;

    float error = 0.0;
    float pwm_float = 0.0;

    // 3. EJECUCIÓN DEL PID SEGÚN EL MODO
    if (current_mode == MODE_POSITION) {
      error = setpoint - current_position_rad;
      error_integral += error * SAMPLE_TIME_S;
      
      // Anti-windup
      if (error_integral > 50.0) error_integral = 50.0;
      if (error_integral < -50.0) error_integral = -50.0;

      pwm_float = (kp_pos * error) + (ki_pos * error_integral);
    } 
    else if (current_mode == MODE_VELOCITY) {
      error = setpoint - current_velocity_rad_s;
      error_integral += error * SAMPLE_TIME_S;
      
      // Anti-windup velocidad
      if (error_integral > 100.0) error_integral = 100.0;
      if (error_integral < -100.0) error_integral = -100.0;

      pwm_float = (kp_vel * error) + (ki_vel * error_integral);
    }

    // 4. APLICAR POTENCIA
    if (current_mode != MODE_IDLE) {
      int dir = 1;
      if (pwm_float < 0) {
        dir = -1;
        pwm_float = -pwm_float; // Valor absoluto
      }

      int pwm_val = (int)pwm_float;
      if (pwm_val > 255) pwm_val = 255;
      
      // Deadband para evitar zumbidos (Más útil en control de posición fina)
      if (pwm_val < 30 && current_mode == MODE_POSITION) { 
        pwm_val = 0;
        dir = 0;
      }

      setMotor(dir, pwm_val);

      // 5. Telemetría
      Serial.print("Mode:"); 
      Serial.print(current_mode == MODE_POSITION ? "POS" : "VEL");
      Serial.print(" SP:"); 
      Serial.print(setpoint);
      
      if (current_mode == MODE_POSITION) {
        Serial.print(" PosReal:"); 
        Serial.print(current_position_rad);
      } else {
        Serial.print(" VelReal:"); 
        Serial.print(current_velocity_rad_s);
      }
      
      Serial.print(" PWM:"); 
      Serial.println(dir * pwm_val);
    }

    // Actualizar estados
    previous_pulses = current_pulses;
    previous_time = current_time;
  }
}
