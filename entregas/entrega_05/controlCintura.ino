// =================
// Open-loop Control
// =================

#define MOTOR_IN1  8
#define MOTOR_IN2  9
#define MOTOR_ENA  10

// Parametros del Motor y limites de duty cycle
#define PWM_MAX         255
#define VELOCIDAD_BAJA  80
#define VELOCIDAD_MEDIA 150
#define VELOCIDAD_ALTA  230

// Tiempo (ms)
#define TIEMPO_GIRO_MS     2000
#define TIEMPO_PAUSA_MS    1000
#define TIEMPO_ARRANQUE_MS 3000

// =================  
// Funciones de Control
// =================

// Setea el duty cycle y el sentido de giro
void girarCintura(int velocidadPWM, bool sentidoHorario) {
  velocidadPWM = constrain(velocidadPWM, 0, PWM_MAX);

  if (sentidoHorario) {
    digitalWrite(MOTOR_IN1, HIGH);
    digitalWrite(MOTOR_IN2, LOW);
  } else {
    digitalWrite(MOTOR_IN1, LOW);
    digitalWrite(MOTOR_IN2, HIGH);
  }
  
  analogWrite(MOTOR_ENA, velocidadPWM);
}

// Frenado por inercia
void frenarCintura() {
  digitalWrite(MOTOR_IN1, LOW);
  digitalWrite(MOTOR_IN2, LOW);
  analogWrite(MOTOR_ENA, 0);
}

// Frenado electromagnético
void frenadoActivoCintura() {
  digitalWrite(MOTOR_IN1, HIGH);
  digitalWrite(MOTOR_IN2, HIGH);
  analogWrite(MOTOR_ENA, PWM_MAX);
}

// Control open-loop por tiempo
void girarCinturaTiempo(int velocidadPWM, bool sentidoHorario, unsigned long duracionMs) {
  girarCintura(velocidadPWM, sentidoHorario);
  delay(duracionMs);
  frenarCintura();
}

// Arranque suave para mitigar picos de corriente
void arranqueSuave(int velocidadObjetivo, bool sentidoHorario, int rampaTiempoMs) {
  velocidadObjetivo = constrain(velocidadObjetivo, 0, PWM_MAX);
  
  int pasos = velocidadObjetivo > 0 ? velocidadObjetivo : 1;
  int retardoPorPaso = rampaTiempoMs / pasos;
  if (retardoPorPaso < 1) retardoPorPaso = 1;

  for (int pwm = 0; pwm <= velocidadObjetivo; pwm++) {
    girarCintura(pwm, sentidoHorario);
    delay(retardoPorPaso);
  }
}

// Frenado suave para evitar backlash o estrés mecánico
void frenadoSuave(int velocidadActual, bool sentidoHorario, int rampaTiempoMs) {
  velocidadActual = constrain(velocidadActual, 0, PWM_MAX);
  
  int pasos = velocidadActual > 0 ? velocidadActual : 1;
  int retardoPorPaso = rampaTiempoMs / pasos;
  if (retardoPorPaso < 1) retardoPorPaso = 1;

  for (int pwm = velocidadActual; pwm >= 0; pwm--) {
    girarCintura(pwm, sentidoHorario);
    delay(retardoPorPaso);
  }
  frenarCintura();
}

// ===================
// Setup & Main Loop
// ===================

void setup() {
  Serial.begin(115200);
  Serial.println("=== Control Cintura ===");

  pinMode(MOTOR_IN1, OUTPUT);
  pinMode(MOTOR_IN2, OUTPUT);
  pinMode(MOTOR_ENA, OUTPUT);

  // Estado inicial seguro
  frenarCintura();
  delay(TIEMPO_ARRANQUE_MS);
}

void loop() {
  // --- FASE 1: CW ---
  Serial.println("Fase 1: CW @ Media");
  girarCinturaTiempo(VELOCIDAD_MEDIA, true, TIEMPO_GIRO_MS);
  delay(TIEMPO_PAUSA_MS);

  // --- FASE 2: CCW ---
  Serial.println("Fase 2: CCW @ Media");
  girarCinturaTiempo(VELOCIDAD_MEDIA, false, TIEMPO_GIRO_MS);
  delay(TIEMPO_PAUSA_MS);

  // --- FASE 3: Comparación de velocidad ---
  Serial.println("Fase 3: Comparación de velocidad");
  girarCinturaTiempo(VELOCIDAD_BAJA, true, TIEMPO_GIRO_MS);
  delay(TIEMPO_PAUSA_MS);
  girarCinturaTiempo(VELOCIDAD_MEDIA, true, TIEMPO_GIRO_MS);
  delay(TIEMPO_PAUSA_MS);
  girarCinturaTiempo(VELOCIDAD_ALTA, true, TIEMPO_GIRO_MS);
  delay(TIEMPO_PAUSA_MS);

  // --- FASE 4: Ramping ---
  Serial.println("Fase 4: Arranque suave y frenado suave");
  arranqueSuave(VELOCIDAD_ALTA, true, 1000);
  delay(1000);
  frenadoSuave(VELOCIDAD_ALTA, true, 1000);
  delay(TIEMPO_PAUSA_MS);

  // --- FASE 5: Comparación de frenado ---
  Serial.println("Fase 5: Comparación de frenado");
  
  // Coast
  girarCintura(VELOCIDAD_ALTA, true);
  delay(1000);
  frenarCintura();
  delay(3000);

  // Short Brake
  girarCintura(VELOCIDAD_ALTA, true);
  delay(1000);
  frenadoActivoCintura();
  delay(2000);
  frenarCintura();

  delay(5000);
}
