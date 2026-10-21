/*
 * CONTROLADOR PID DE POSICIÓN EN LAZO CERRADO (Closed-Loop)
 * 
 * Arquitectura del Sistema:
 * 1. Módulo ISR: Captura la posición real del encoder (cuadratura).
 * 2. Módulo de Temporización: Utiliza un "Ticker" por software (millis) para ejecutar el 
 *    algoritmo PID a un `dt` constante.
 * 3. Módulo PID: Calcula el error entre el Setpoint y la Posición Real, y aplica la 
 *    ecuación de control discreta.
 * 4. Módulo de Actuación (Saturation & Deadband): Limita la señal de control al rango del 
 *    PWM (0-255) y establece una banda muerta para evitar ruido y sobrecalentamiento del L298N.
 */

#include <Arduino.h>

// -------------------------------------------------------------
// DEFINICIÓN DE PINES (Arduino Mega 2560)
// -------------------------------------------------------------
const int ENC_A = 2; // Pin de interrupción externa 0
const int ENC_B = 3; // Pin digital normal o interrupción 1

const int ENA = 9; // Pin PWM
const int IN1 = 8; 
const int IN2 = 7; 

// -------------------------------------------------------------
// CONSTANTES DEL SISTEMA
// -------------------------------------------------------------
// Nota: Como queremos la posición final del brazo, aquí SÍ usamos la resolución a la salida del gearbox.
const float TOTAL_CPR = 132.0; // CPR base (11) * Gear Ratio (12)
const float SAMPLE_TIME_S = 0.01; // Tiempo de muestreo: 10 ms (100 Hz)

// -------------------------------------------------------------
// GANANCIAS DEL PID (Tuning parameters - Planta de Posición MATLAB)
// -------------------------------------------------------------
float kp = 1.0843; 
float ki = 2.5465;
float kd = 0.0; // El PID Tuner arrojó n/a (Controlador PI, sin acción derivativa)

// Variables de estado del PID
float error_integral = 0.0;
float previous_error = 0.0;
float setpoint_rad = 0.0; 
bool motor_enabled = false; // El motor arranca apagado hasta recibir el primer comando

volatile long pulse_count = 0; 
unsigned long previous_time = 0; 

// -------------------------------------------------------------
// RUTINA DE SERVICIO DE INTERRUPCIÓN (ISR)
// -------------------------------------------------------------
void readEncoder() {
  int aVal = digitalRead(ENC_A);
  int bVal = digitalRead(ENC_B);
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
  Serial.println("\n--- SISTEMA PID INICIADO ---");
  Serial.println("El motor esta apagado. Envia un angulo en GRADOS (ej: 90, -45, 180)");
  Serial.println("por el monitor serial para establecer el setpoint y arrancar.");
  
  pinMode(ENC_A, INPUT_PULLUP);
  pinMode(ENC_B, INPUT_PULLUP);
  attachInterrupt(digitalPinToInterrupt(ENC_A), readEncoder, CHANGE);

  pinMode(IN1, OUTPUT);
  pinMode(IN2, OUTPUT);
  pinMode(ENA, OUTPUT);
  
  previous_time = millis();
}

// -------------------------------------------------------------
// FUNCIÓN PARA APLICAR POTENCIA AL MOTOR (Actuation Module)
// -------------------------------------------------------------
void setMotor(int dir, int pwm_val) {
  // Configurar dirección en el L298N
  if (dir == 1) { // Forward
    digitalWrite(IN1, HIGH);
    digitalWrite(IN2, LOW);
  } else if (dir == -1) { // Reverse
    digitalWrite(IN1, LOW);
    digitalWrite(IN2, HIGH);
  } else { // Stop
    digitalWrite(IN1, LOW);
    digitalWrite(IN2, LOW);
  }
  
  // Escribir señal PWM
  analogWrite(ENA, pwm_val);
}

// -------------------------------------------------------------
// LOOP PRINCIPAL
// -------------------------------------------------------------
void loop() {
  // 1. INTERFAZ DE USUARIO (Lectura de Comandos por Serial)
  if (Serial.available() > 0) {
    float input_degrees = Serial.parseFloat(); // Lee el número enviado
    
    // Limpiar el buffer de caracteres invisibles como \n o \r
    while (Serial.available() > 0) {
      Serial.read();
    }
    
    if (input_degrees != 0.0 || (input_degrees == 0.0 && Serial.available() == 0)) {
      setpoint_rad = input_degrees * (PI / 180.0); // Convertir a radianes
      motor_enabled = true; // Activar el PID
      
      Serial.print("\n>>> NUEVO SETPOINT: ");
      Serial.print(input_degrees);
      Serial.println(" grados. Arrancando motor...");
    }
  }

  unsigned long current_time = millis();
  
  if (current_time - previous_time >= (SAMPLE_TIME_S * 1000.0)) {
    
    // 2. Lectura segura del encoder
    noInterrupts();
    long current_pulses = pulse_count;
    interrupts();

    // 2. Convertir pulsos a posición angular real (Radianes)
    float current_position_rad = (current_pulses / TOTAL_CPR) * (2.0 * PI);

    // 3. Calcular Error
    float error = setpoint_rad - current_position_rad;

    // 4. Términos PID (Discretizados)
    float proportional = kp * error;
    
    error_integral += error * SAMPLE_TIME_S;
    float integral = ki * error_integral;
    
    float derivative = kd * (error - previous_error) / SAMPLE_TIME_S;

    // 5. Calcular Señal de Control u(t)
    float control_signal = proportional + integral + derivative;

    // 6. Actuación: Lógica de dirección y Saturation / Anti-windup
    int dir = 1; // 1 = adelante, -1 = atrás
    if (control_signal < 0) {
      dir = -1;
      control_signal = -control_signal; // Valor absoluto
    }

    // Limitación (Saturation)
    int pwm_val = (int)control_signal;
    if (pwm_val > 255) {
      pwm_val = 255;
      // Anti-windup clamping: no acumular más integral si ya saturamos
      error_integral -= error * SAMPLE_TIME_S; 
    }

    // Deadband (Banda Muerta): Si el PWM es menor al voltaje que vence la inercia, 
    // lo mandamos a 0 para que el L298N no empiece a "zumbar" y recalentar.
    if (pwm_val < 30) {
      pwm_val = 0;
      dir = 0;
    }

    // Seguridad: Si el usuario no ha enviado comandos aún, forzar el apagado.
    if (!motor_enabled) {
      pwm_val = 0;
      dir = 0;
      error_integral = 0.0; // Evitar que acumule error mientras está estacionario
    }

    // Aplicar comando de motor
    setMotor(dir, pwm_val);

    // 7. Telemetría y actualización (Formato CSV para el script de MATLAB)
    if (motor_enabled) {
      Serial.print(setpoint_rad);
      Serial.print(",");
      Serial.print(current_position_rad);
      Serial.print(",");
      Serial.println(pwm_val);
    }

    previous_error = error;
    previous_time = current_time;
  }
}
