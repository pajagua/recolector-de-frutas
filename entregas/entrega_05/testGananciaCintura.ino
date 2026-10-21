// ============================================================
// TEST DE GANANCIA (K) — Motor JGA25-370
// ============================================================
// Este programa te ayudará a determinar la ganancia (K) de tu 
// motor JGA25-370 de forma experimental y sencilla.
//
// ¿Qué es la ganancia K?
// Es la cantidad de RPM (Revoluciones por Minuto) que gira el 
// motor por cada Voltio que le aplicas. Su unidad es [RPM/V].
// ============================================================

// Pines del L298N (Los mismos de tu configuración)
#define MOTOR_IN1  8
#define MOTOR_IN2  9
#define MOTOR_ENA  10

// --- PARÁMETROS DE LA PRUEBA ---
// Puedes cambiar estos valores para hacer diferentes pruebas
int pwmPrueba = 255;            // PWM a aplicar durante la prueba (0-255)
int tiempoPruebaMs = 10000;     // Tiempo de la prueba en milisegundos (10 segundos)

void setup() {
  Serial.begin(115200);
  
  pinMode(MOTOR_IN1, OUTPUT);
  pinMode(MOTOR_IN2, OUTPUT);
  pinMode(MOTOR_ENA, OUTPUT);

  // Asegurarse de que el motor esté detenido
  digitalWrite(MOTOR_IN1, LOW);
  digitalWrite(MOTOR_IN2, LOW);
  analogWrite(MOTOR_ENA, 0);

  Serial.println("=== TEST PARA DETERMINAR LA GANANCIA (K) DEL MOTOR ===");
  Serial.println("");
  Serial.println("INSTRUCCIONES PREVIAS:");
  Serial.println("1. Ponle un trocito de cinta adhesiva de color al eje del motor.");
  Serial.println("   (Esto es para que puedas ver y contar las vueltas facilmente).");
  Serial.println("2. Prepara un cronometro o simplemente mantente muy atento.");
  Serial.println("3. Cuando estes listo para contar, envia CUALQUIER LETRA y dale a Enter");
  Serial.println("   en esta ventana del Monitor Serial.");
  Serial.println("");
  Serial.print("La prueba durara EXACTAMENTE ");
  Serial.print(tiempoPruebaMs / 1000);
  Serial.println(" segundos.");
  Serial.print("El PWM aplicado sera de: ");
  Serial.println(pwmPrueba);
  Serial.println("========================================================");
}

void loop() {
  // Esperar a que el usuario envíe algo por el Monitor Serial
  if (Serial.available() > 0) {
    // Limpiar el buffer de entrada
    while (Serial.available() > 0) {
      Serial.read();
    }

    Serial.println("\n>>> PREPARATE...");
    delay(1000);
    Serial.println("3...");
    delay(1000);
    Serial.println("2...");
    delay(1000);
    Serial.println("1...");
    delay(1000);

    Serial.println("\n¡YA! CUENTA LAS VUELTAS...");

    // 1. Arrancar el motor
    digitalWrite(MOTOR_IN1, HIGH);
    digitalWrite(MOTOR_IN2, LOW);
    analogWrite(MOTOR_ENA, pwmPrueba);

    // 2. Esperar el tiempo exacto de la prueba (10 segundos)
    delay(tiempoPruebaMs);

    // 3. Detener el motor bruscamente (frenado activo para mayor precision)
    digitalWrite(MOTOR_IN1, HIGH);
    digitalWrite(MOTOR_IN2, HIGH);
    analogWrite(MOTOR_ENA, 255);
    delay(200); // Mantener el freno un instante
    
    // Apagar el driver
    digitalWrite(MOTOR_IN1, LOW);
    digitalWrite(MOTOR_IN2, LOW);
    analogWrite(MOTOR_ENA, 0);

    Serial.println("¡TIEMPO! MOTOR DETENIDO.");
    Serial.println("========================================================");
    Serial.println("CÁLCULO DE LA GANANCIA (K):");
    Serial.println("1. Anota cuantas vueltas completas contaste (llamemosle 'N').");
    Serial.println("2. Calcula las RPM:");
    Serial.println("   RPM = N * 6");
    Serial.println("   (Multiplicamos por 6 porque 10 segundos caben 6 veces en un minuto).");
    Serial.println("");
    Serial.println("3. Calcula el Voltaje Efectivo (V_efectivo) que le llego al motor:");
    Serial.print("   V_efectivo = (");
    Serial.print(pwmPrueba);
    Serial.println(" / 255) * (12V - 2V de caida del L298N)");
    float v_efectivo = ((float)pwmPrueba / 255.0) * (12.0 - 2.0);
    Serial.print("   V_efectivo = ");
    Serial.print(v_efectivo);
    Serial.println(" Voltios");
    Serial.println("");
    Serial.println("4. Finalmente, calcula la ganancia (K):");
    Serial.println("   K = RPM / V_efectivo");
    Serial.println("========================================================");
    Serial.println("\nPara hacer otra prueba, envia otra letra por el Monitor Serial.");
  }
}
