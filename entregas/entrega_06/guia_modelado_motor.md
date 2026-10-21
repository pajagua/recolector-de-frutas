
### Guía Bloque 5: Modelado del Motor DC en Closed-Loop

Como Ingeniero Mecatrónico, diseñar un control de posición o velocidad en *closed-loop* exige que conozcas el modelo de tu actuador. A continuación, tienes el desarrollo paso a paso del modelo matemático.

#### 1. Ecuaciones Fundamentales (Dinámica del Sistema)

Un motor DC acopla dos dominios: el eléctrico (armadura) y el mecánico (rotor y carga). Debemos plantear las ecuaciones diferenciales para ambos.

**Subcircuito Eléctrico (Ley de Kirchhoff):**
El voltaje de entrada se reparte entre la resistencia, la inductancia y la fuerza contraelectromotriz (*back-EMF*).

$$V(t) = R_a i_a(t) + L_a \frac{di_a(t)}{dt} + e(t)$$

Donde la *back-EMF* es directamente proporcional a la velocidad angular:

$$e(t) = K_e \omega(t)$$

*($K_e$ = Constante de velocidad eléctrica, $\omega(t)$ = velocidad angular en rad/s)*

**Subcircuito Mecánico (2da Ley de Newton):**
El torque electromagnético del motor debe vencer la inercia del sistema y la fricción viscosa.

$$J \frac{d\omega(t)}{dt} = T_m(t) - b \omega(t)$$

Donde el torque electromagnético es directamente proporcional a la corriente:

$$T_m(t) = K_t i_a(t)$$

*($K_t$ = Constante de torque, $J$ = Inercia del rotor/carga, $b$ = Fricción viscosa)*

#### 2. Transformada de Laplace

Para diseñar tu controlador, debes llevar el sistema del dominio del tiempo al *s-domain*. Asumimos condiciones iniciales en cero para facilitar el análisis:

**Ecuación Eléctrica en Laplace:**

$$V(s) = R_a I_a(s) + s L_a I_a(s) + K_e \Omega(s)$$

**Ecuación Mecánica en Laplace:**

$$s J \Omega(s) = K_t I_a(s) - b \Omega(s)$$

#### 3. Deducción de la Transfer Function G(s)

Buscamos la relación entre la salida (Velocidad angular $\Omega$) y la entrada (Voltaje V), es decir: $G(s) = \frac{\Omega(s)}{V(s)}$.

**Paso A:** Despejar la corriente $I_a(s)$ de la ecuación mecánica:

$$I_a(s) = \left( \frac{s J + b}{K_t} \right) \Omega(s)$$

**Paso B:** Sustituir $I_a(s)$ en la ecuación eléctrica:

$$V(s) = (R_a + s L_a) \left( \frac{s J + b}{K_t} \right) \Omega(s) + K_e \Omega(s)$$

**Paso C:** Agrupar términos y multiplicar todo por $K_t$ para eliminar fracciones:

$$K_t V(s) = \left[ (R_a + s L_a)(s J + b) + K_e K_t \right] \Omega(s)$$

**Paso D:** Ecuación final (Este es tu modelo completo de segundo orden):

$$G(s) = \frac{\Omega(s)}{V(s)} = \frac{K_t}{(R_a + s L_a)(s J + b) + K_e K_t}$$

#### 4. Simplificación del Modelo (Reducción de Orden)

Para motores pequeños como el JGA25-370 (y casi cualquier motor en proyectos universitarios), la inductancia `L_a` es minúscula (en el rango de los microhenrios). La dinámica eléctrica es rapidísima comparada con la mecánica, por lo que **aproximamos L_a = 0**.

Al hacer `L_a = 0`, la ecuación se simplifica a un sistema de primer orden:
Partimos de las ecuaciones fundamentales en el dominio del tiempo para el modelo electro-mecánico. Dado que $L_a \approx 0$, la ecuación eléctrica (*armature equation*) queda:
$$v_a(t) = R_a i_a(t) + K_e \omega(t)$$

Despejando la corriente de armadura $i_a(t)$:
$$i_a(t) = \frac{v_a(t) - K_e \omega(t)}{R_a}$$

La ecuación mecánica de la dinámica del rotor (*rotor dynamics*) es:
$$J \frac{d\omega(t)}{dt} + b \omega(t) = T_m(t)$$

Sabiendo que el torque electromagnético es directamente proporcional a la corriente, $T_m(t) = K_t i_a(t)$, sustituimos $i_a(t)$ en la ecuación mecánica:
$$J \frac{d\omega(t)}{dt} + b \omega(t) = K_t \left( \frac{v_a(t) - K_e \omega(t)}{R_a} \right)$$

Multiplicando todo por $R_a$ para eliminar los denominadores y simplificar la expresión:
$$J R_a \frac{d\omega(t)}{dt} + b R_a \omega(t) = K_t v_a(t) - K_t K_e \omega(t)$$

Agrupando a la izquierda todos los términos dependientes de la velocidad angular $\omega(t)$:
$$J R_a \frac{d\omega(t)}{dt} + (b R_a + K_t K_e) \omega(t) = K_t v_a(t)$$

Asumiendo condiciones iniciales nulas y aplicando la Transformada de Laplace para pasar al dominio de la frecuencia $s$:
$$s J R_a \Omega(s) + (b R_a + K_t K_e) \Omega(s) = K_t V_a(s)$$

Factorizando $\Omega(s)$:
$$\Omega(s) \left[ s J R_a + b R_a + K_t K_e \right] = K_t V_a(s)$$

Obtenemos la Función de Transferencia (*Transfer Function*) en lazo abierto (*open-loop*), $G(s) = \frac{\Omega(s)}{V_a(s)}$:
$$G(s) = \frac{K_t}{s J R_a + (b R_a + K_t K_e)}$$

Para llevarla a la forma estándar de un sistema de primer orden $G(s) = \frac{K}{\tau s + 1}$, dividimos tanto el numerador como el denominador por el término constante $(b R_a + K_t K_e)$:

$$G(s) = \frac{\frac{K_t}{b R_a + K_t K_e}}{\left( \frac{J R_a}{b R_a + K_t K_e} \right) s + 1}$$

De esta expresión final deducimos de manera analítica que las constantes vitales para tu simulación *State-Space* o PID son:

**Ganancia Estática (K):**
$$K = \frac{K_t}{b R_a + K_t K_e} \quad \left[ \frac{\text{rad/s}}{\text{V}} \right]$$

**Constante de Tiempo ($\tau$):**
$$\tau = \frac{J R_a}{b R_a + K_t K_e} \quad [\text{s}]$$

#### 5. Parameter Identification (Implementación en Hardware Real)

Para tu informe, no asumas los valores de Inercia (`J`) o fricción (`b`), ya que son casi imposibles de calcular a mano. Debes explicar cómo obtendrás las variables `K` y `tau` empíricamente en el laboratorio:

**Método Empírico (*Step Response*):**
1. **Prueba Open-loop:** Aplica un voltaje constante escalón `V_step` (ej. encender el PWM al máximo en tu *driver*) al motor.
2. **Medición del Sensor:** Usa el *encoder* para leer la velocidad en tiempo real y grafícala vs. tiempo.
3. **Cálculo de K:** Observa la velocidad máxima y estable que alcanza el motor (`W_steady`). Tu ganancia será `K = W_steady / V_step`.
4. **Cálculo de tau:** Mide en tu gráfica el tiempo exacto que le tomó al motor llegar al **63.2%** de esa velocidad máxima. Ese tiempo es `tau` (*time constant* de tu planta).

Con esto, ya puedes ir a MATLAB y colocar un bloque `Transfer Fcn` con los valores reales.

#### 6. Representación en State-Space

Otra forma de modelar el sistema, especialmente útil si planeas utilizar técnicas de control avanzado o un controlador óptimo (LQR), es mediante el modelo de variables de estado (*state-space*).

Definimos nuestro vector de estado $x(t)$, nuestra entrada $u(t)$ y nuestra salida $y(t)$. Si el objetivo es un control de posición, la salida será la posición angular $\theta(t)$.
$$x(t) = \begin{bmatrix} \theta(t) \\ \omega(t) \\ i_a(t) \end{bmatrix}, \quad u(t) = v_a(t), \quad y(t) = \theta(t)$$

Las ecuaciones diferenciales fundamentales del motor (sin simplificar $L_a$) son:
1. $\frac{d\theta(t)}{dt} = \omega(t)$
2. $J \frac{d\omega(t)}{dt} = K_t i_a(t) - b \omega(t) \implies \frac{d\omega(t)}{dt} = -\frac{b}{J} \omega(t) + \frac{K_t}{J} i_a(t)$
3. $L_a \frac{di_a(t)}{dt} = v_a(t) - R_a i_a(t) - K_e \omega(t) \implies \frac{di_a(t)}{dt} = -\frac{K_e}{L_a} \omega(t) - \frac{R_a}{L_a} i_a(t) + \frac{1}{L_a} v_a(t)$

Expresando esto en la forma matricial estándar $\dot{x}(t) = A x(t) + B u(t)$ y $y(t) = C x(t) + D u(t)$:

$$
\begin{bmatrix} \dot{\theta}(t) \\ \dot{\omega}(t) \\ \dot{i}_a(t) \end{bmatrix} = 
\begin{bmatrix} 
0 & 1 & 0 \\ 
0 & -\frac{b}{J} & \frac{K_t}{J} \\ 
0 & -\frac{K_e}{L_a} & -\frac{R_a}{L_a} 
\end{bmatrix}
\begin{bmatrix} \theta(t) \\ \omega(t) \\ i_a(t) \end{bmatrix} + 
\begin{bmatrix} 0 \\ 0 \\ \frac{1}{L_a} \end{bmatrix} v_a(t)
$$

$$
y(t) = \begin{bmatrix} 1 & 0 & 0 \end{bmatrix} \begin{bmatrix} \theta(t) \\ \omega(t) \\ i_a(t) \end{bmatrix}
$$

**Simplificación para $L_a \approx 0$:**
Al igual que con la *Transfer Function*, si despreciamos la inductancia, la corriente de armadura $i_a(t)$ deja de ser una variable de estado (pues su derivada desaparece y depende directamente de $v_a$ y $\omega$). El sistema se reduce a un modelo de segundo orden para la posición, donde $x(t) = [\theta(t), \omega(t)]^T$:

$$
\begin{bmatrix} \dot{\theta}(t) \\ \dot{\omega}(t) \end{bmatrix} = 
\begin{bmatrix} 
0 & 1 \\ 
0 & -\left( \frac{b R_a + K_t K_e}{J R_a} \right) 
\end{bmatrix}
\begin{bmatrix} \theta(t) \\ \omega(t) \end{bmatrix} + 
\begin{bmatrix} 0 \\ \frac{K_t}{J R_a} \end{bmatrix} v_a(t)
$$

Esta es la matriz de estado que puedes utilizar en MATLAB con el comando `ss(A, B, C, D)` para simular la dinámica en el tiempo o diseñar un *observer*.

#### 7. Lazo Cerrado (*Closed-Loop*) y Diseño del Controlador PID

Una vez modelado el motor, el objetivo es controlar su posición $\theta(t)$ para que el brazo robótico alcance las naranjas de forma precisa. 

**Estructura del *Closed-Loop*:**
1. **Referencia (*Setpoint*):** La posición angular deseada calculada por la cinemática inversa.
2. **Planta (*Plant*):** El motor GA25-370 modelado por su *Transfer Function* $G(s)$.
3. **Sensor:** El *encoder* de cuadratura que lee la posición real del eje.
4. **Error:** $e(t) = \theta_{ref}(t) - \theta_{real}(t)$.
5. **Controlador:** El algoritmo PID que calcula la señal de control (voltaje/PWM) $u(t)$ en función del error.

**Ecuación del Controlador PID en el *s-domain*:**
$$C(s) = K_p + \frac{K_i}{s} + K_d s$$

La señal de control en el dominio del tiempo será:
$$u(t) = K_p e(t) + K_i \int_{0}^{t} e(\tau) d\tau + K_d \frac{de(t)}{dt}$$

**Arquitectura de Firmware para el Microcontrolador (ESP32):**
Para llevar esta matemática a código (C/C++), no implementamos el PID de golpe. La arquitectura del sistema debe dividirse en módulos:

*   **Módulo de Interrupciones (ISR):** Configurar pines del ESP32 conectados a los canales A y B del *encoder* con interrupciones de hardware (`attachInterrupt()`). Cada flanco de subida/bajada actualizará un contador de pulsos (`pulses`).
*   **Conversión de Unidades:** Una función que traduzca `pulses` a grados o radianes usando la resolución total: `Resolución = CPR_base \times Gear\_Ratio`.
*   **Módulo de Temporización (*Timer/Ticker*):** El PID de posición o velocidad **no puede ejecutarse en el `loop()` libremente**. Debe ejecutarse a una frecuencia de muestreo fija (*sample time* $\Delta t$, por ejemplo, 100 Hz o 10 ms).
*   **Algoritmo PID Discreto:** Las integrales y derivadas matemáticas se aproximan numéricamente:
    *   Integral: $\sum e(k) \Delta t$
    *   Derivada: $\frac{e(k) - e(k-1)}{\Delta t}$
*   **Módulo de Actuación:** La salida del PID $u(k)$ debe limitarse (*saturation* y *deadband*) al voltaje máximo y convertirse en un valor de PWM (`ledcWrite()` en ESP32) respetando el ciclo de trabajo máximo.

***

> ✅ **HARDWARE VALIDATION: GA25-370 (12V - 500 RPM)**
> Excelente, con los parámetros **12V y 500 RPM**, ya podemos establecer las fronteras de diseño electrónico:
> 
> *   **Gear Ratio:** Para llegar a 500 RPM desde un motor base de ~6000 RPM, la reducción es aproximadamente **1:12**.
> *   **Encoder Resolution:** Los sensores Hall de esta serie suelen tener 11 CPR (pulsos por revolución del eje del motor). La resolución en el eje de salida será de aproximadamente **132 pulsos por revolución**. *(Dato crítico para la conversión de unidades en el firmware).*
> *   **Stall Current:** Esta variante en 12V suele tener un consumo de bloqueo entre **1.5A y 2.0A**.
> 
> **Recomendación de Diseño de PCB (*Trade-offs*):**
> *   ❌ **Evitar el L298N:** Es un puente H muy antiguo basado en transistores BJT. Pierde hasta 2V en forma de calor y ocupa mucho espacio.
> *   ❌ **Evitar el TB6612FNG:** Aunque es un MOSFET moderno, su límite continuo es de 1.2A. Al arrancar o bloquear el motor (situaciones normales en un brazo robótico), los picos de hasta 2A podrían destruirlo.
> *   ✅ **Recomendado: DRV8871 o TB67H420FTG:** Estos *motor drivers* manejan picos por encima de los 3.5A, tienen baja resistencia de encendido ($R_{DS(on)}$) y protección térmica integrada.
> 
> **Layout de PCB:** En la etapa de KiCad, deberás rutear la placa asegurándote de que las pistas (*traces*) de potencia del motor tengan un ancho mínimo de **40-50 mils** para soportar con holgura picos de 2A sin recalentamiento.