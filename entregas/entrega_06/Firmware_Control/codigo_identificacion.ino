/*
 * IDENTIFICACIÓN DE PARÁMETROS DEL MOTOR (Step Response)
 * 
 * Arquitectura del Sistema:
 * 1. Módulo ISR: Lee los pulsos del encoder (canal A y B) usando interrupciones por hardware.
 * 2. Módulo de Actuación: Aplica un escalón (Step) de voltaje máximo constante mediante el L298N.
 * 3. Módulo de Muestreo: Cada `SAMPLE_TIME` milisegundos, calcula la velocidad angular y la envía por Serial.
 * 
 * Objetivo: Graficar la respuesta temporal de la velocidad para extraer empíricamente la Ganancia Estática (K)
 * y la Constante de Tiempo (tau) del eje de salida del sistema completo (motor + reductora).
 */

#include <Arduino.h>

// -------------------------------------------------------------
// DEFINICIÓN DE PINES (Arduino Mega 2560)
// -------------------------------------------------------------
// Encoder Pins (Deben ser pines con soporte de interrupción en Mega)
const int ENC_A = 2; // Pin de interrupción externa 0
const int ENC_B = 3; // Pin digital normal o interrupción externa 1

// L298N Motor Driver Pins
const int ENA = 9; // Pin PWM (Soporta analogWrite)
const int IN1 = 8; // Dirección 1
const int IN2 = 7; // Dirección 2

// -------------------------------------------------------------
// CONSTANTES DEL SISTEMA
// -------------------------------------------------------------
const float CPR_BASE = 132.0; // CPR total a la salida (11 del motor * 12 de reductora)
const unsigned long SAMPLE_TIME_MS = 10; // Tiempo de muestreo: 10 ms (100 Hz)

// -------------------------------------------------------------
// VARIABLES GLOBALES VOLÁTILES (Para ISR)
// -------------------------------------------------------------
volatile long pulse_count = 0; // Contador de pulsos del encoder
long previous_pulses = 0;      // Pulsos en el ciclo anterior
unsigned long previous_time = 0; // Tiempo del ciclo anterior

// -------------------------------------------------------------
// RUTINA DE SERVICIO DE INTERRUPCIÓN (ISR)
// -------------------------------------------------------------
// Se ejecuta cada vez que el canal A cambia de estado (RISING o FALLING)
void readEncoder() {
  int aVal = digitalRead(ENC_A);
  int bVal = digitalRead(ENC_B);
  
  // Lógica de cuadratura para determinar el sentido de giro
  if (aVal == bVal) {
    pulse_count++;
  } else {
    pulse_count--;
  }
}

// -------------------------------------------------------------
// SETUP
// -------------------------------------------------------------
void setup() {
  Serial.begin(115200);
  
  // Configuración de pines de Encoder
  pinMode(ENC_A, INPUT_PULLUP);
  pinMode(ENC_B, INPUT_PULLUP);
  attachInterrupt(digitalPinToInterrupt(ENC_A), readEncoder, CHANGE);

  // Configuración de pines del L298N
  pinMode(IN1, OUTPUT);
  pinMode(IN2, OUTPUT);
  pinMode(ENA, OUTPUT); // Configurar pin PWM como salida

  Serial.println("Iniciando Step Response en 3 segundos...");
  delay(3000); // Dar tiempo para abrir el Serial Plotter

  // Aplicar un escalón (Step) de voltaje máximo al motor
  // Sentido de giro hacia adelante
  digitalWrite(IN1, HIGH);
  digitalWrite(IN2, LOW);
  analogWrite(ENA, 255); // 100% Duty Cycle (Step = Voltaje de la fuente)

  previous_time = millis();
}

// -------------------------------------------------------------
// LOOP PRINCIPAL
// -------------------------------------------------------------
void loop() {
  unsigned long current_time = millis();
  
  // Ejecutar el muestreo estrictamente en cada SAMPLE_TIME_MS
  if (current_time - previous_time >= SAMPLE_TIME_MS) {
    
    // 1. Calcular el Delta de tiempo real en segundos
    float dt = (current_time - previous_time) / 1000.0;
    
    // 2. Extraer pulsos de forma segura desactivando interrupciones brevemente
    noInterrupts();
    long current_pulses = pulse_count;
    interrupts();

    // 3. Calcular deltas
    long delta_pulses = current_pulses - previous_pulses;
    
    // 4. Calcular velocidad en Radianes por segundo (del eje de salida)
    // Velocidad = (Delta_Pulsos / CPR_BASE) * (2 * PI) / dt
    float velocity_rad_s = (delta_pulses / CPR_BASE) * (2.0 * PI) / dt;

    // 5. Enviar por Serial para graficar (formato Telemetry)
    Serial.print("Velocity_Rad_s:");
    Serial.println(velocity_rad_s);

    // Actualizar variables de estado
    previous_pulses = current_pulses;
    previous_time = current_time;
  }
}
