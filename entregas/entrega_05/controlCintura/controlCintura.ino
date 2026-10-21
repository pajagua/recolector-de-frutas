// ============================================================
// CONTROL EN LAZO ABIERTO — Motor de Cintura (JGA25-370)
// CitriBot RRP — Robótica I — Universidad Nacional de Asunción
// ============================================================
// Este código controla ÚNICAMENTE el motor DC JGA25-370 que
// acciona la articulación de "cintura" (rotación de la base)
// del brazo robótico CitriBot.
//
// Hardware utilizado:
//   - Arduino UNO / MEGA
//   - Driver L298N (puente H doble)
//   - Motor DC JGA25-370 (6V/12V con caja reductora)
//
// Conexionado L298N:
//   L298N IN1  → Arduino Pin 8   (dirección A)
//   L298N IN2  → Arduino Pin 9   (dirección B)
//   L298N ENA  → Arduino Pin 10  (PWM para velocidad)
//   L298N OUT1 → Terminal (+) del motor JGA25-370
//   L298N OUT2 → Terminal (-) del motor JGA25-370
//   L298N VCC  → Fuente externa 12V
//   L298N GND  → GND compartido con Arduino y fuente
//   L298N 5V   → Puede alimentar al Arduino (o dejar desconectado
//                 si el Arduino tiene su propia alimentación)
//
// Modo de operación (LAZO ABIERTO):
//   Se envían señales PWM al L298N sin ningún tipo de
//   retroalimentación. No se verifica si el motor giró
//   la cantidad deseada. Si hay una carga excesiva o el
//   motor se traba, el Arduino no lo detecta.
// ============================================================

// ===================== PINES DEL L298N =====================
// Definimos los pines del Arduino conectados al driver L298N.
// Estos pines controlan la dirección y velocidad del motor.

#define MOTOR_IN1  8    // Pin digital 8 → IN1 del L298N (control de dirección)
#define MOTOR_IN2  9    // Pin digital 9 → IN2 del L298N (control de dirección)
#define MOTOR_ENA  10   // Pin PWM 10 → ENA del L298N (control de velocidad)
// NOTA: ENA debe estar en un pin PWM (3, 5, 6, 9, 10, 11 en Arduino UNO)
// para poder variar la velocidad con analogWrite().

// =================== PARÁMETROS DEL MOTOR ===================
// Especificaciones típicas del JGA25-370 (varían según el modelo exacto):
//   - Voltaje nominal: 6V o 12V DC
//   - RPM sin carga: depende de la relación de engranajes (ej: ~280 RPM a 12V)
//   - Torque máximo: depende del modelo (ej: ~2.2 kg·cm)
//   - Encoder (si lo tiene): 11 pulsos por revolución del eje del motor
//
// El L298N tiene una caída de voltaje interna de ~2V, así que si
// alimentamos con 12V, el motor recibe efectivamente ~10V.

#define PWM_MAX        255   // Valor máximo de PWM (8 bits = 0 a 255)
#define VELOCIDAD_BAJA  80   // PWM bajo (~31% del máximo) — giro lento
#define VELOCIDAD_MEDIA 150  // PWM medio (~59% del máximo) — giro moderado
#define VELOCIDAD_ALTA  230  // PWM alto (~90% del máximo) — giro rápido

// ===================== TIEMPOS DE DEMO =====================
// Tiempos en milisegundos para cada fase de la demostración.
// En lazo abierto, controlamos el motor por TIEMPO, no por posición.
// Es decir: "gira a X velocidad durante Y segundos".

#define TIEMPO_GIRO_MS     2000  // Duración de cada movimiento (2 segundos)
#define TIEMPO_PAUSA_MS    1000  // Pausa entre movimientos (1 segundo)
#define TIEMPO_ARRANQUE_MS 3000  // Espera inicial antes de empezar la demo

// ============================================================
//                    FUNCIONES AUXILIARES
// ============================================================

// -------------------- girarCintura --------------------
// Función: girarCintura
// Descripción: Hace girar el motor de cintura en una dirección
//   específica y a una velocidad determinada por PWM.
//
// Parámetros:
//   velocidadPWM - Valor de 0 a 255 que determina la velocidad
//                  (0 = detenido, 255 = máxima velocidad)
//   sentidoHorario - true = giro horario (CW)
//                    false = giro antihorario (CCW)
//
// Funcionamiento del L298N:
//   IN1=HIGH, IN2=LOW  → Motor gira en un sentido (horario)
//   IN1=LOW,  IN2=HIGH → Motor gira en el otro sentido (antihorario)
//   IN1=LOW,  IN2=LOW  → Motor detenido (freno por inercia)
//   IN1=HIGH, IN2=HIGH → Motor frenado (freno activo/corto)
//   ENA = PWM          → Controla la velocidad (duty cycle)
// ============================================================
void girarCintura(int velocidadPWM, bool sentidoHorario) {
  // Paso 1: Limitar el valor de PWM al rango válido (0-255)
  velocidadPWM = constrain(velocidadPWM, 0, PWM_MAX);
  // constrain(valor, min, max) asegura que el valor esté dentro del rango.
  // Si velocidadPWM fuera 300, lo reduce a 255. Si fuera -10, lo sube a 0.

  // Paso 2: Establecer la dirección de giro usando IN1 e IN2
  if (sentidoHorario) {
    // Giro horario: IN1 = HIGH, IN2 = LOW
    digitalWrite(MOTOR_IN1, HIGH);  // Activar IN1 (polo positivo al motor)
    digitalWrite(MOTOR_IN2, LOW);   // Desactivar IN2 (polo negativo al motor)
  } else {
    // Giro antihorario: IN1 = LOW, IN2 = HIGH
    digitalWrite(MOTOR_IN1, LOW);   // Desactivar IN1
    digitalWrite(MOTOR_IN2, HIGH);  // Activar IN2 (invierte la polaridad)
  }
  // Al invertir IN1/IN2, se invierte la polaridad del voltaje
  // aplicado al motor, lo que invierte el sentido de giro.

  // Paso 3: Aplicar la velocidad mediante PWM en el pin ENA
  analogWrite(MOTOR_ENA, velocidadPWM);
  // analogWrite() genera una señal PWM con frecuencia ~490 Hz (en pin 10).
  // El duty cycle es: (velocidadPWM / 255) * 100%
  // Ejemplo: analogWrite(10, 150) → duty cycle = 58.8%
  // El L298N "recorta" el voltaje de alimentación según el duty cycle.
  // Si VCC = 12V y duty = 59%, el motor recibe ~7V efectivos (menos la caída del L298N).
}

// -------------------- frenarCintura --------------------
// Función: frenarCintura
// Descripción: Detiene el motor de cintura.
//   Usa frenado por inercia (coast): simplemente deja de aplicar
//   voltaje y el motor se detiene gradualmente por fricción.
//
// Alternativa (frenado activo/brake):
//   Si quisieras un frenado más brusco, podrías poner
//   IN1=HIGH, IN2=HIGH, lo que cortocircuita los terminales
//   del motor a través del L298N, generando un freno electromagnético.
// ============================================================
void frenarCintura() {
  // Poner ambos pines de dirección en LOW = motor en "coast" (giro libre)
  digitalWrite(MOTOR_IN1, LOW);  // Desactivar IN1
  digitalWrite(MOTOR_IN2, LOW);  // Desactivar IN2
  // Con IN1=LOW e IN2=LOW, el puente H desconecta el motor.
  // El motor se detendrá gradualmente por su propia inercia y fricción.

  // Poner PWM en 0 para asegurar que no haya señal de habilitación
  analogWrite(MOTOR_ENA, 0);     // ENA = 0 → driver deshabilitado
  // Aunque con IN1=IN2=LOW ya no hay corriente, poner ENA=0 es
  // una buena práctica para asegurar que el driver esté completamente apagado.
}

// -------------------- frenadoActivoCintura --------------------
// Función: frenadoActivoCintura
// Descripción: Frena el motor de forma activa (brake).
//   Cortocircuita los terminales del motor a través del L298N,
//   lo que genera un freno electromagnético más rápido.
// ============================================================
void frenadoActivoCintura() {
  // Poner ambos pines en HIGH = freno activo (short brake)
  digitalWrite(MOTOR_IN1, HIGH);  // Activar IN1
  digitalWrite(MOTOR_IN2, HIGH);  // Activar IN2
  // Con IN1=HIGH e IN2=HIGH, ambos lados del puente H conectan
  // los terminales del motor a GND, creando un cortocircuito
  // que frena el motor rápidamente.

  // Habilitar el driver para que el frenado sea efectivo
  analogWrite(MOTOR_ENA, PWM_MAX); // ENA al máximo para frenado completo
}

// -------------------- girarCinturaTiempo --------------------
// Función: girarCinturaTiempo
// Descripción: Gira el motor durante un tiempo específico y luego frena.
//   Esta es la forma típica de control en lazo abierto para un motor DC:
//   "gira a esta velocidad durante este tiempo".
//
// Parámetros:
//   velocidadPWM   - Velocidad de giro (0-255)
//   sentidoHorario - Dirección de giro (true=CW, false=CCW)
//   duracionMs     - Tiempo de giro en milisegundos
//
// ¡IMPORTANTE! En lazo abierto, el ángulo real girado depende de:
//   - La velocidad (PWM)
//   - El tiempo
//   - La carga (peso/fricción)
//   - El voltaje de la fuente
//   - La temperatura del motor
// Cualquier cambio en estos factores altera el resultado final.
// ============================================================
void girarCinturaTiempo(int velocidadPWM, bool sentidoHorario, unsigned long duracionMs) {
  // Paso 1: Iniciar el giro del motor
  girarCintura(velocidadPWM, sentidoHorario);
  // El motor empieza a girar en la dirección y velocidad indicadas.

  // Paso 2: Esperar el tiempo indicado (bloqueante)
  delay(duracionMs);
  // delay() detiene la ejecución del programa durante 'duracionMs' milisegundos.
  // Durante este tiempo, el motor sigue girando.
  // NOTA: delay() es bloqueante — el Arduino NO puede hacer nada más durante la espera.

  // Paso 3: Frenar el motor al terminar el tiempo
  frenarCintura();
  // Después de que transcurre el tiempo, detenemos el motor.
}

// -------------------- arranqueSuave --------------------
// Función: arranqueSuave
// Descripción: Incrementa la velocidad del motor gradualmente
//   desde 0 hasta la velocidad objetivo, evitando picos de
//   corriente bruscos que podrían dañar el motor o el driver.
//
// Parámetros:
//   velocidadObjetivo - Velocidad final deseada (0-255)
//   sentidoHorario    - Dirección de giro
//   rampaTiempoMs     - Tiempo total para alcanzar la velocidad objetivo
//
// El arranque suave (soft start) es importante porque:
//   1. Reduce el pico de corriente inicial (puede ser 5-10× la corriente nominal)
//   2. Protege los engranajes de la caja reductora del JGA25-370
//   3. Reduce el desgaste mecánico
//   4. El L298N tiene un límite de corriente (~2A continuo, ~3A pico)
// ============================================================
void arranqueSuave(int velocidadObjetivo, bool sentidoHorario, int rampaTiempoMs) {
  // Limitar la velocidad objetivo al rango válido
  velocidadObjetivo = constrain(velocidadObjetivo, 0, PWM_MAX);

  // Calcular el retardo entre cada incremento de PWM
  // Si la rampa es de 500ms y la velocidad objetivo es 200,
  // necesitamos 200 incrementos, cada uno de 500/200 = 2.5ms
  int retardoPorPaso = rampaTiempoMs / velocidadObjetivo;
  // retardoPorPaso = tiempo_total / cantidad_de_pasos
  // Esto determina qué tan rápido sube la velocidad.

  // Si el retardo calculado es 0 (velocidad muy alta o rampa muy corta),
  // forzamos un mínimo de 1ms para que la rampa sea al menos perceptible
  if (retardoPorPaso < 1) retardoPorPaso = 1;

  // Incrementar la velocidad gradualmente desde 0 hasta el objetivo
  for (int pwm = 0; pwm <= velocidadObjetivo; pwm++) {
    girarCintura(pwm, sentidoHorario);  // Aplicar la velocidad actual
    delay(retardoPorPaso);               // Esperar antes de incrementar
  }
  // Al finalizar el bucle, el motor está girando a la velocidad objetivo.
  // La velocidad se incrementó suavemente, paso a paso.
}

// -------------------- frenadoSuave --------------------
// Función: frenadoSuave
// Descripción: Reduce la velocidad del motor gradualmente
//   desde la velocidad actual hasta 0, evitando frenados bruscos.
//
// Parámetros:
//   velocidadActual - Velocidad actual del motor (0-255)
//   sentidoHorario  - Dirección actual de giro
//   rampaTiempoMs   - Tiempo total para frenar completamente
// ============================================================
void frenadoSuave(int velocidadActual, bool sentidoHorario, int rampaTiempoMs) {
  // Limitar la velocidad al rango válido
  velocidadActual = constrain(velocidadActual, 0, PWM_MAX);

  // Calcular el retardo entre cada decremento
  int retardoPorPaso = rampaTiempoMs / velocidadActual;
  if (retardoPorPaso < 1) retardoPorPaso = 1;

  // Decrementar la velocidad gradualmente desde la actual hasta 0
  for (int pwm = velocidadActual; pwm >= 0; pwm--) {
    girarCintura(pwm, sentidoHorario);  // Aplicar la velocidad decreciente
    delay(retardoPorPaso);               // Esperar antes de decrementar
  }

  // Asegurar que el motor esté completamente detenido al final
  frenarCintura();
}

// ============================================================
//                         SETUP
// ============================================================
// La función setup() se ejecuta UNA SOLA VEZ al encender el
// Arduino o al presionar el botón de reset.
// Aquí configuramos los pines y dejamos todo listo para operar.
// ============================================================
void setup() {
  // --- Inicializar comunicación serial para monitoreo/debug ---
  Serial.begin(115200);
  // Serial.begin(baudrate) abre el puerto serial a 115200 baudios.
  // Esto permite enviar mensajes al Monitor Serial del IDE de Arduino
  // para ver qué está haciendo el programa en tiempo real.

  Serial.println("=== CitriBot RRP — Control Cintura (JGA25-370) ===");
  // println() envía un texto seguido de un salto de línea al Monitor Serial.

  Serial.println("Motor: JGA25-370 con caja reductora");
  Serial.println("Driver: L298N (Puente H)");
  Serial.println("Modo: LAZO ABIERTO (sin retroalimentacion)");
  Serial.println("Iniciando configuracion de pines...");

  // --- Configurar pines del L298N como salidas ---
  pinMode(MOTOR_IN1, OUTPUT);
  // pinMode(pin, modo) configura un pin como entrada (INPUT) o salida (OUTPUT).
  // OUTPUT significa que el Arduino puede poner el pin en HIGH (5V) o LOW (0V).
  // IN1 del L298N necesita recibir una señal digital del Arduino.

  pinMode(MOTOR_IN2, OUTPUT);
  // IN2 del L298N también necesita ser una salida digital.

  pinMode(MOTOR_ENA, OUTPUT);
  // ENA del L298N recibe una señal PWM para controlar la velocidad.
  // Los pines PWM en Arduino UNO son: 3, 5, 6, 9, 10, 11.
  // En Arduino MEGA, hay más pines PWM disponibles.

  // --- Asegurar que el motor esté detenido al iniciar ---
  frenarCintura();
  // Es CRÍTICO detener el motor al arrancar. Si el Arduino se resetea
  // mientras el motor estaba girando, queremos que se detenga inmediatamente
  // para evitar accidentes o daños mecánicos.

  Serial.println("Pines configurados correctamente.");
  Serial.println("Motor detenido (estado seguro).");
  Serial.println("");

  // --- Esperar antes de iniciar la demostración ---
  Serial.print("Iniciando demo en ");
  Serial.print(TIEMPO_ARRANQUE_MS / 1000);  // Convertir ms a segundos
  Serial.println(" segundos...");
  // Serial.print() es igual que println() pero SIN salto de línea al final.
  // Esto permite concatenar mensajes en la misma línea.

  delay(TIEMPO_ARRANQUE_MS);
  // Esperamos 3 segundos para dar tiempo a:
  // 1. Verificar las conexiones físicas
  // 2. Abrir el Monitor Serial
  // 3. Alejarse del robot si es necesario
}

// ============================================================
//                      LOOP PRINCIPAL
//               SECUENCIA DE DEMOSTRACIÓN
// ============================================================
// Esta secuencia demuestra el control en lazo abierto del motor
// JGA25-370 (cintura) usando el driver L298N.
//
// La secuencia consiste en:
//   FASE 1: Giro horario a velocidad media
//   FASE 2: Giro antihorario a velocidad media
//   FASE 3: Tres velocidades diferentes (baja, media, alta)
//   FASE 4: Arranque y frenado suave
//   FASE 5: Frenado activo vs. frenado por inercia
//
// NOTA SOBRE LAZO ABIERTO:
//   El Arduino NO sabe cuántos grados giró la cintura.
//   Solo sabe cuánto tiempo aplicó voltaje y a qué duty cycle.
//   Si la carga cambia (ej: el brazo sostiene un objeto más pesado),
//   la cintura girará más lento o menos grados con los mismos comandos.
// ============================================================
void loop() {

  // ========== FASE 1: GIRO HORARIO ==========
  Serial.println("--- FASE 1: Giro horario a velocidad media ---");
  // Informamos por serial qué fase está ejecutándose.

  Serial.print("  Girando CW a PWM=");
  Serial.print(VELOCIDAD_MEDIA);
  Serial.print(" durante ");
  Serial.print(TIEMPO_GIRO_MS);
  Serial.println(" ms...");

  girarCinturaTiempo(VELOCIDAD_MEDIA, true, TIEMPO_GIRO_MS);
  // Gira en sentido horario (true) a velocidad media (150/255 ≈ 59%)
  // durante 2000 ms (2 segundos). Luego frena automáticamente.

  Serial.println("  Motor detenido.");
  delay(TIEMPO_PAUSA_MS);
  // Pausa de 1 segundo entre movimientos para observar el resultado.

  // ========== FASE 2: GIRO ANTIHORARIO ==========
  Serial.println("--- FASE 2: Giro antihorario a velocidad media ---");

  Serial.print("  Girando CCW a PWM=");
  Serial.print(VELOCIDAD_MEDIA);
  Serial.print(" durante ");
  Serial.print(TIEMPO_GIRO_MS);
  Serial.println(" ms...");

  girarCinturaTiempo(VELOCIDAD_MEDIA, false, TIEMPO_GIRO_MS);
  // Gira en sentido antihorario (false) con los mismos parámetros.
  // Idealmente, la cintura regresa a su posición original.
  // PERO en lazo abierto, no hay garantía de que sea exactamente igual:
  // la fricción estática al arrancar puede ser diferente en cada sentido.

  Serial.println("  Motor detenido.");
  delay(TIEMPO_PAUSA_MS);

  // ========== FASE 3: DIFERENTES VELOCIDADES ==========
  Serial.println("--- FASE 3: Comparacion de velocidades ---");

  // --- Velocidad baja ---
  Serial.print("  Velocidad BAJA (PWM=");
  Serial.print(VELOCIDAD_BAJA);
  Serial.println(")...");
  girarCinturaTiempo(VELOCIDAD_BAJA, true, TIEMPO_GIRO_MS);
  // PWM = 80/255 ≈ 31%. El motor gira lentamente.
  // A velocidades muy bajas, el motor podría no arrancar si la carga
  // es alta, porque el torque generado no supera la fricción estática.
  delay(TIEMPO_PAUSA_MS);

  // --- Velocidad media ---
  Serial.print("  Velocidad MEDIA (PWM=");
  Serial.print(VELOCIDAD_MEDIA);
  Serial.println(")...");
  girarCinturaTiempo(VELOCIDAD_MEDIA, true, TIEMPO_GIRO_MS);
  // PWM = 150/255 ≈ 59%. Velocidad moderada, buen compromiso
  // entre velocidad y torque.
  delay(TIEMPO_PAUSA_MS);

  // --- Velocidad alta ---
  Serial.print("  Velocidad ALTA (PWM=");
  Serial.print(VELOCIDAD_ALTA);
  Serial.println(")...");
  girarCinturaTiempo(VELOCIDAD_ALTA, true, TIEMPO_GIRO_MS);
  // PWM = 230/255 ≈ 90%. Velocidad casi máxima.
  // El motor consume más corriente y genera más calor.
  delay(TIEMPO_PAUSA_MS);

  // ========== FASE 4: ARRANQUE Y FRENADO SUAVE ==========
  Serial.println("--- FASE 4: Arranque suave + Frenado suave ---");

  // Arranque suave: la velocidad sube gradualmente de 0 a VELOCIDAD_ALTA
  Serial.println("  Arranque suave (rampa 1s)...");
  arranqueSuave(VELOCIDAD_ALTA, true, 1000);
  // La velocidad aumenta linealmente durante 1000 ms (1 segundo).
  // Esto reduce el pico de corriente inicial y protege los engranajes.

  // Mantener velocidad constante por 1 segundo
  Serial.println("  Manteniendo velocidad...");
  delay(1000);

  // Frenado suave: la velocidad baja gradualmente de VELOCIDAD_ALTA a 0
  Serial.println("  Frenado suave (rampa 1s)...");
  frenadoSuave(VELOCIDAD_ALTA, true, 1000);
  // La velocidad disminuye linealmente durante 1000 ms.
  // El motor se detiene suavemente sin sacudidas.

  Serial.println("  Motor detenido suavemente.");
  delay(TIEMPO_PAUSA_MS);

  // ========== FASE 5: FRENADO ACTIVO vs INERCIA ==========
  Serial.println("--- FASE 5: Comparacion de tipos de frenado ---");

  // Frenado por inercia (coast)
  Serial.println("  Giro rapido + FRENADO POR INERCIA...");
  girarCintura(VELOCIDAD_ALTA, true);  // Arrancar a velocidad alta
  delay(1000);                          // Girar durante 1 segundo
  frenarCintura();                      // Frenado por inercia (coast)
  Serial.println("  [Inercia] Motor soltado — se detiene gradualmente.");
  // En frenado por inercia, el motor se desconecta del driver
  // y se detiene solo por la fricción mecánica. Tarda más en parar.
  delay(3000);  // Esperar a que se detenga completamente

  // Frenado activo (brake)
  Serial.println("  Giro rapido + FRENADO ACTIVO (brake)...");
  girarCintura(VELOCIDAD_ALTA, true);  // Arrancar a velocidad alta
  delay(1000);                          // Girar durante 1 segundo
  frenadoActivoCintura();               // Frenado activo (short brake)
  Serial.println("  [Brake] Motor frenado activamente — se detiene rapido.");
  // En frenado activo, los terminales del motor se cortocircuitan
  // a través del L298N, generando un freno electromagnético.
  // El motor se detiene mucho más rápido.
  delay(2000);  // Esperar un poco menos (frena más rápido)

  // Asegurar que el motor esté detenido al final
  frenarCintura();

  // ========== FIN DE SECUENCIA ==========
  Serial.println("");
  Serial.println("=== SECUENCIA COMPLETA ===");
  // Imprimimos una línea vacía y el mensaje de fin.

  Serial.println("Resumen de la demo:");
  Serial.println("  Fase 1: Giro CW a velocidad media");
  Serial.println("  Fase 2: Giro CCW a velocidad media");
  Serial.println("  Fase 3: Comparacion de 3 velocidades (baja/media/alta)");
  Serial.println("  Fase 4: Arranque y frenado suave (rampas)");
  Serial.println("  Fase 5: Frenado por inercia vs frenado activo");
  Serial.println("");
  Serial.println("Esperando 5 segundos para repetir la secuencia...");
  Serial.println("================================================\n");

  delay(5000);
  // Espera 5 segundos antes de repetir toda la secuencia.
  // El loop() se ejecuta indefinidamente, así que la demo
  // se repite una y otra vez hasta que se desconecte el Arduino.
}
