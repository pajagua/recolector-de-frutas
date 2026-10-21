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