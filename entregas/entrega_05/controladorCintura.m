%% ============================================================
%  SIMULACIÓN DE CONTROL EN LAZO ABIERTO — Motor de Cintura
%  Motor JGA25-370 con Driver L298N
%  CitriBot RRP — Robótica I — Universidad Nacional de Asunción
%  ============================================================
%  Este script simula el comportamiento del motor DC JGA25-370
%  que acciona la articulación de "cintura" (rotación de la base)
%  del brazo robótico CitriBot, controlado en lazo abierto.
%
%  Modelo utilizado:
%    Motor DC JGA25-370: Función de transferencia de 1er orden
%      G(s) = K / (tau*s + 1)
%    donde:
%      K   = ganancia estática (RPM por Voltio aplicado)
%      tau = constante de tiempo (combinación de inercia + fricción)
%
%  El driver L298N se modela como un amplificador de ganancia unitaria
%  con una caída de voltaje constante (~2V), por lo que el voltaje
%  efectivo al motor es: V_motor = (PWM/255) * (V_fuente - V_drop)
%  ============================================================

%% Limpiar el espacio de trabajo
clear;    % Borra todas las variables del workspace de MATLAB
clc;      % Limpia la ventana de comandos (Command Window)
close all; % Cierra todas las figuras/gráficas abiertas

%% =================== PARÁMETROS DEL SISTEMA ===================
% Aquí definimos las constantes del motor y del driver que usaremos
% en la simulación. Estos valores deben coincidir con el hardware real.

% --- Motor DC JGA25-370 (con caja reductora) ---
% Modelamos el motor DC como un sistema de primer orden:
%   G(s) = K / (tau*s + 1)
%
% La respuesta temporal a un escalón de voltaje V es:
%   w(t) = K * V * (1 - e^(-t/tau))
%
% Esto significa que la velocidad del motor crece exponencialmente
% hasta estabilizarse en K*V RPM.

motor.K   = 23.3;     % [RPM/V] Ganancia del motor con caja reductora
% La ganancia K representa cuántas RPM produce el motor por cada Voltio
% aplicado en régimen permanente (cuando ya se estabilizó).
% Para un JGA25-370 12V ~280 RPM sin carga: K ≈ 280/12 ≈ 23.3 RPM/V

motor.tau = 0.12;     % [s] Constante de tiempo mecánica + eléctrica
% tau es el tiempo que tarda el motor en alcanzar el 63.2% de su
% velocidad final después de aplicar un escalón de voltaje.
% Para motores pequeños con caja reductora, tau típico: 0.05-0.20 s
% Un tau bajo significa respuesta rápida, un tau alto significa lenta.

motor.Vnom = 12;      % [V] Voltaje nominal del motor
% Voltaje para el cual está diseñado el motor.
% NUNCA aplicar más de este voltaje o el motor se dañará.

motor.RPM_noload = 280; % [RPM] Velocidad sin carga a voltaje nominal
% Velocidad máxima teórica cuando no hay ninguna carga en el eje.

% --- Driver L298N ---
driver.V_fuente = 12;   % [V] Voltaje de la fuente de alimentación externa
% El L298N se alimenta con una fuente externa (típicamente 12V).

driver.V_drop = 2.0;    % [V] Caída de voltaje interna del L298N
% El L298N usa transistores bipolares (no MOSFETs), por lo que tiene
% una caída de voltaje significativa (~1.4V a ~2V por canal).
% Esto significa que si la fuente es 12V, el motor recibe ~10V máximo.

driver.V_efectivo = driver.V_fuente - driver.V_drop;
% Voltaje máximo que realmente llega al motor: 12V - 2V = 10V

driver.PWM_max = 255;   % Resolución del PWM de Arduino (8 bits)
% Arduino usa analogWrite() con valores de 0 a 255.
% 0 = 0% duty cycle (motor apagado)
% 255 = 100% duty cycle (motor a máxima velocidad)

% --- Parámetros de carga (cintura del robot) ---
carga.factor = 0.85;   % Factor de reducción por carga [adimensional]
% En la práctica, la cintura del robot tiene una carga (peso del brazo).
% Esto reduce la velocidad real respecto a la velocidad sin carga.
% factor = 1.0 → sin carga, factor = 0.5 → carga al 50% del torque máximo

%% =================== SIMULACIÓN TEMPORAL ===================
% Configuramos los parámetros de la simulación numérica.

dt = 0.001;          % [s] Paso de simulación (1 milisegundo)
% dt determina la resolución temporal de la simulación.
% Valores más pequeños → simulación más precisa pero más lenta.
% 1 ms es un buen compromiso para motores DC.

t_total = 15;        % [s] Tiempo total de simulación
% Simulamos 15 segundos para cubrir todas las fases de la demo.

t = 0:dt:t_total;    % Vector de tiempo desde 0 hasta t_total con paso dt
% Ejemplo: t = [0, 0.001, 0.002, ..., 14.999, 15.000]

N = length(t);       % Cantidad total de muestras en la simulación
% N = t_total / dt + 1 = 15001 muestras

fprintf('Simulacion configurada:\n');          % Imprimir info de configuración
fprintf('  Paso de tiempo: %.3f s\n', dt);     % Mostrar dt
fprintf('  Tiempo total: %.1f s\n', t_total);  % Mostrar tiempo total
fprintf('  Muestras: %d\n\n', N);              % Mostrar número de muestras

%% =================== SEÑAL DE COMANDO (PWM) ===================
% Definimos la secuencia de comandos PWM que el Arduino enviaría
% al L298N en la demostración de lazo abierto.
%
% La señal PWM va de -255 a +255:
%   Valores positivos → giro horario (CW)
%   Valores negativos → giro antihorario (CCW)
%   Cero → motor detenido
%
% Secuencia de la demo:
%   0-1s:     PWM = 0     (motor detenido, esperando inicio)
%   1-3s:     PWM = +150  (giro CW a velocidad media)
%   3-4s:     PWM = 0     (pausa/frenado)
%   4-6s:     PWM = -150  (giro CCW a velocidad media)
%   6-7s:     PWM = 0     (pausa)
%   7-8s:     PWM = +80   (velocidad baja CW)
%   8-9s:     PWM = 0     (pausa)
%   9-10s:    PWM = +150  (velocidad media CW)
%   10-11s:   PWM = 0     (pausa)
%   11-12s:   PWM = +230  (velocidad alta CW)
%   12-13s:   PWM = 0     (pausa)
%   13-14s:   Rampa suave de 0 a +230 (arranque suave)
%   14-15s:   Rampa suave de +230 a 0 (frenado suave)

pwm_cmd = zeros(1, N);  % Inicializar el vector de comandos en cero
% zeros(1, N) crea un vector fila de N elementos, todos con valor 0.

% FASE 1: Giro horario a velocidad media (t = 1s a 3s)
pwm_cmd(t >= 1 & t < 3) = 150;
% Asigna el valor 150 a todas las posiciones del vector donde t está
% entre 1 y 3 segundos. El operador & es AND lógico elemento a elemento.

% FASE 2: Giro antihorario a velocidad media (t = 4s a 6s)
pwm_cmd(t >= 4 & t < 6) = -150;
% Valor negativo indica giro en sentido contrario.

% FASE 3a: Velocidad baja CW (t = 7s a 8s)
pwm_cmd(t >= 7 & t < 8) = 80;
% PWM = 80/255 ≈ 31% del máximo.

% FASE 3b: Velocidad media CW (t = 9s a 10s)
pwm_cmd(t >= 9 & t < 10) = 150;
% PWM = 150/255 ≈ 59% del máximo.

% FASE 3c: Velocidad alta CW (t = 11s a 12s)
pwm_cmd(t >= 11 & t < 12) = 230;
% PWM = 230/255 ≈ 90% del máximo.

% FASE 4: Arranque suave (rampa lineal de 0 a 230) (t = 13s a 14s)
idx_rampa_up = find(t >= 13 & t < 14);
% find() devuelve los índices del vector t donde la condición es verdadera.
% Estos índices corresponden al intervalo de arranque suave.
pwm_cmd(idx_rampa_up) = linspace(0, 230, length(idx_rampa_up));
% linspace(inicio, fin, N) genera N valores equiespaciados entre inicio y fin.
% Esto crea una rampa lineal de 0 a 230 durante 1 segundo.

% FASE 4: Frenado suave (rampa lineal de 230 a 0) (t = 14s a 15s)
idx_rampa_down = find(t >= 14 & t <= 15);
% Índices del intervalo de frenado suave.
pwm_cmd(idx_rampa_down) = linspace(230, 0, length(idx_rampa_down));
% Rampa descendente de 230 a 0 durante 1 segundo.

%% =================== CÁLCULO DEL VOLTAJE AL MOTOR ===================
% Convertimos la señal PWM (0-255) al voltaje real que llega al motor,
% considerando la caída de voltaje del L298N.

V_motor = (pwm_cmd / driver.PWM_max) * driver.V_efectivo;
% Fórmula: V_motor = (PWM / 255) * (V_fuente - V_drop)
%
% Ejemplo: PWM = 150 → V_motor = (150/255) * (12-2) = 0.588 * 10 = 5.88V
%
% Nota: los valores negativos de PWM producen voltajes negativos,
% lo que en la simulación representa giro en sentido opuesto.
% En la realidad, el L298N invierte la polaridad con IN1/IN2.

%% =================== SIMULACIÓN DEL MOTOR DC ===================
% Simulamos la respuesta del motor JGA25-370 usando el método de
% Euler hacia adelante (Forward Euler) para resolver numéricamente
% la ecuación diferencial del motor.
%
% Modelo de primer orden del motor DC:
%   tau * dw/dt + w = K * V
%   → dw/dt = (1/tau) * (K * V - w)
%
% donde:
%   w   = velocidad angular del motor [RPM]
%   V   = voltaje aplicado al motor [V]
%   K   = ganancia estática [RPM/V]
%   tau = constante de tiempo [s]
%
% El método de Euler aproxima la derivada como:
%   dw/dt ≈ (w[i] - w[i-1]) / dt
%   → w[i] = w[i-1] + dw/dt * dt

motor_vel = zeros(1, N);   % Velocidad angular del motor [RPM]
% Inicializamos en cero: el motor arranca detenido.

motor_pos = zeros(1, N);   % Posición angular acumulada [grados]
% La posición se obtiene integrando la velocidad en el tiempo.

motor_corriente = zeros(1, N); % Corriente estimada del motor [A]
% Estimamos la corriente para visualizar el consumo.

for i = 2:N
    % --- Calcular la derivada de la velocidad ---
    dw = (1/motor.tau) * (motor.K * V_motor(i-1) * carga.factor - motor_vel(i-1));
    % dw = (1/tau) * (K * V * factor_carga - w_actual)
    %
    % Este término representa la ecuación diferencial del motor:
    % - K * V * factor_carga: es la velocidad a la que tiende el motor
    %   (velocidad de régimen permanente reducida por la carga)
    % - motor_vel(i-1): es la velocidad actual
    % - La diferencia entre ambas es el "error" que el motor trata de cerrar
    % - (1/tau) determina qué tan rápido cierra ese error
    
    % --- Actualizar velocidad (Euler hacia adelante) ---
    motor_vel(i) = motor_vel(i-1) + dw * dt;
    % Nueva velocidad = velocidad anterior + cambio * paso de tiempo
    % Este es el método de Euler: aproximación de primer orden de la ODE.
    
    % --- Actualizar posición (integración numérica) ---
    motor_pos(i) = motor_pos(i-1) + motor_vel(i-1) * (360/60) * dt;
    % Convertimos RPM a °/s: w[°/s] = w[RPM] * 360/60 = w[RPM] * 6
    % Luego multiplicamos por dt para obtener el incremento angular.
    % Ejemplo: 100 RPM * 6 = 600 °/s, en 1ms: 600 * 0.001 = 0.6°
    
    % --- Estimar corriente del motor ---
    motor_corriente(i) = abs(V_motor(i)) * 0.15;
    % Estimación simplificada de la corriente.
    % En realidad, I = (V - Ke*w) / R, pero usamos una aproximación
    % proporcional al voltaje para fines de visualización.
    % 0.15 A/V es una estimación para el JGA25-370.
end

%% =================== GRÁFICAS ===================
% Creamos una figura con 6 subgráficas para visualizar todos
% los aspectos del control en lazo abierto.

figure('Name', 'Control Lazo Abierto — Cintura JGA25-370 + L298N', ...
    'Position', [50 50 1000 800], 'Color', 'w');
% 'Name' → Título de la ventana de la figura
% 'Position' → [x, y, ancho, alto] en píxeles desde esquina inferior izquierda
% 'Color' → Color de fondo de la figura ('w' = blanco)

% ---- Gráfica 1: Señal PWM de comando ----
subplot(3, 2, 1);
% subplot(filas, columnas, posición) divide la figura en una cuadrícula.
% 3 filas × 2 columnas = 6 subgráficas. Esta es la posición 1 (arriba izquierda).

plot(t, pwm_cmd, 'b-', 'LineWidth', 1.5);
% plot(x, y, formato, propiedades) crea una gráfica 2D.
% 'b-' → línea sólida azul ('b' = blue, '-' = sólida)
% 'LineWidth', 1.5 → grosor de la línea de 1.5 puntos

xlabel('Tiempo [s]');     % Etiqueta del eje X
ylabel('PWM [-255 a 255]'); % Etiqueta del eje Y
title('Señal de Comando PWM (Entrada al L298N)');  % Título de la subgráfica
grid on;                  % Activar la cuadrícula para facilitar la lectura
ylim([-280 280]);         % Límites del eje Y para dar margen visual
% ylim([min, max]) fija los límites verticales de la gráfica.

% Añadir líneas de referencia
hold on;  % Mantener la gráfica actual para añadir más elementos
yline(0, 'k--', 'LineWidth', 0.5);  % Línea horizontal en y=0
% yline(valor, formato, propiedades) dibuja una línea horizontal.
% 'k--' → línea punteada negra ('k' = black, '--' = punteada)
hold off; % Liberar la gráfica

% ---- Gráfica 2: Voltaje efectivo al motor ----
subplot(3, 2, 2);
plot(t, V_motor, 'r-', 'LineWidth', 1.5);
% 'r-' → línea sólida roja
xlabel('Tiempo [s]');
ylabel('Voltaje [V]');
title('Voltaje Efectivo al Motor (después del L298N)');
grid on;
ylim([-12 12]);  % Rango simétrico para ver ambos sentidos de giro

hold on;
yline(0, 'k--', 'LineWidth', 0.5);
% Añadir anotación de la caída de voltaje del L298N
yline(driver.V_efectivo, 'g--', sprintf('V_{max} = %.1fV', driver.V_efectivo));
% sprintf() formatea un string con los valores numéricos.
% '%.1f' → número con 1 decimal. Resultado: "V_{max} = 10.0V"
yline(-driver.V_efectivo, 'g--', sprintf('V_{min} = %.1fV', -driver.V_efectivo));
hold off;

% ---- Gráfica 3: Velocidad del motor ----
subplot(3, 2, 3);
plot(t, motor_vel, 'Color', [0.8 0.2 0.2], 'LineWidth', 1.5);
% 'Color', [R G B] → color personalizado en formato RGB normalizado (0 a 1)
% [0.8 0.2 0.2] = rojo oscuro
xlabel('Tiempo [s]');
ylabel('Velocidad [RPM]');
title('Velocidad Angular del Motor (Respuesta)');
grid on;

hold on;
yline(0, 'k--', 'LineWidth', 0.5);
% Marcar la velocidad máxima teórica
vel_max_efectiva = motor.K * driver.V_efectivo * carga.factor;
% Velocidad máxima considerando la caída del L298N y la carga.
% vel_max = 23.3 RPM/V * 10V * 0.85 = 198.05 RPM
yline(vel_max_efectiva, 'm--', sprintf('%.0f RPM (max efectivo)', vel_max_efectiva));
% 'm--' → línea punteada magenta
hold off;

% ---- Gráfica 4: Posición angular acumulada ----
subplot(3, 2, 4);
plot(t, motor_pos, 'Color', [0.2 0.6 0.2], 'LineWidth', 1.5);
% [0.2 0.6 0.2] = verde oscuro
xlabel('Tiempo [s]');
ylabel('Posicion [grados]');
title('Posicion Angular Acumulada de la Cintura');
grid on;

hold on;
yline(0, 'k--', 'LineWidth', 0.5);
hold off;

% ---- Gráfica 5: Corriente estimada ----
subplot(3, 2, 5);
area(t, motor_corriente, 'FaceColor', [1 0.8 0.4], 'EdgeColor', [0.8 0.5 0], 'LineWidth', 1);
% area() crea una gráfica de área rellena (útil para visualizar magnitudes).
% 'FaceColor' → color de relleno (naranja claro)
% 'EdgeColor' → color del borde (naranja oscuro)
xlabel('Tiempo [s]');
ylabel('Corriente [A]');
title('Corriente Estimada del Motor');
grid on;

hold on;
yline(2.0, 'r--', 'Limite L298N (2A)', 'LineWidth', 1.5);
% Marcamos el límite de corriente continua del L298N (~2A).
% Si la corriente supera este valor, el L298N se sobrecalienta.
hold off;

% ---- Gráfica 6: Comparación comando vs respuesta ----
subplot(3, 2, 6);
% Normalizar el PWM a RPM para comparar con la velocidad real
vel_comandada = (pwm_cmd / driver.PWM_max) * motor.K * driver.V_efectivo * carga.factor;
% Convertimos el PWM a la velocidad que teóricamente debería alcanzar
% el motor en régimen permanente (estado estable).

stairs(t, vel_comandada, 'b--', 'LineWidth', 1.2);
% stairs() crea una gráfica tipo escalón (escalera), ideal para
% representar señales de comando digitales que cambian instantáneamente.
hold on;
plot(t, motor_vel, 'r-', 'LineWidth', 1.5);
% Superponemos la respuesta real del motor.
xlabel('Tiempo [s]');
ylabel('Velocidad [RPM]');
title('Comando vs Respuesta Real del Motor');
legend('Velocidad comandada', 'Velocidad real', 'Location', 'best');
% legend() añade una leyenda para identificar cada curva.
% 'Location', 'best' → MATLAB elige la mejor posición automáticamente.
grid on;
hold off;

% Título general de toda la figura
sgtitle({'CONTROL EN LAZO ABIERTO — Motor de Cintura', ...
    'JGA25-370 + L298N — CitriBot RRP'}, ...
    'FontSize', 14, 'FontWeight', 'bold');
% sgtitle() añade un supertítulo a toda la figura (por encima de los subplot).
% Las llaves {} permiten pasar múltiples líneas de texto.

%% =================== ANÁLISIS DE RESPUESTA TEMPORAL ===================
% Creamos una segunda figura que analiza en detalle la respuesta
% temporal del motor (constante de tiempo, tiempo de establecimiento, etc.)

figure('Name', 'Analisis de Respuesta — JGA25-370', ...
    'Position', [100 100 900 500], 'Color', 'w');

% Simular la respuesta a un escalón puro (para análisis)
t_step = 0:dt:2;              % Vector de tiempo de 0 a 2 segundos
N_step = length(t_step);       % Número de muestras
V_step = driver.V_efectivo;    % Escalón de voltaje (voltaje máximo efectivo)
vel_step = zeros(1, N_step);   % Velocidad para la respuesta al escalón

% Simular la respuesta al escalón usando Euler
for i = 2:N_step
    dw_step = (1/motor.tau) * (motor.K * V_step * carga.factor - vel_step(i-1));
    vel_step(i) = vel_step(i-1) + dw_step * dt;
end

% Velocidad final (régimen permanente)
vel_final = motor.K * V_step * carga.factor;
% vel_final = 23.3 * 10 * 0.85 = 198.05 RPM

subplot(1, 2, 1);
plot(t_step, vel_step, 'r-', 'LineWidth', 2);
hold on;

% Marcar la constante de tiempo (63.2% de la velocidad final)
yline(0.632 * vel_final, 'b--', sprintf('63.2%% = %.1f RPM', 0.632*vel_final), 'LineWidth', 1);
% A t = tau, la velocidad alcanza el 63.2% de su valor final.
% Esta es la definición de la constante de tiempo.

% Marcar el tiempo de establecimiento al 2% (4*tau)
xline(4*motor.tau, 'm--', sprintf('t_s(2%%) = %.2f s', 4*motor.tau), 'LineWidth', 1);
% El tiempo de establecimiento al 2% es ~4*tau.
% Esto significa que después de 4*tau, la velocidad está dentro del 2%
% de su valor final.

% Marcar el tiempo de establecimiento al 5% (3*tau)
xline(3*motor.tau, 'g--', sprintf('t_s(5%%) = %.2f s', 3*motor.tau), 'LineWidth', 1);
% El tiempo de establecimiento al 5% es ~3*tau.

% Marcar la velocidad final
yline(vel_final, 'k--', sprintf('V_{ss} = %.1f RPM', vel_final), 'LineWidth', 1);
% V_ss = velocidad en estado estable (steady state).

% Marcar tau en el eje x
xline(motor.tau, 'c--', sprintf('\\tau = %.3f s', motor.tau), 'LineWidth', 1);

xlabel('Tiempo [s]');
ylabel('Velocidad [RPM]');
title('Respuesta al Escalon del JGA25-370');
grid on;
legend('Velocidad del motor', 'Location', 'southeast');
hold off;

% ---- Diagrama de bloques del sistema ----
subplot(1, 2, 2);
% Usamos un gráfico de texto para mostrar el diagrama de bloques
axis off;  % Desactivar los ejes (no necesitamos ejes para texto)

% Título del diagrama
text(0.5, 0.95, '\bf Diagrama de Bloques — Lazo Abierto', ...
    'HorizontalAlignment', 'center', 'FontSize', 13);
% text(x, y, string, propiedades) coloca texto en coordenadas normalizadas.
% '\bf' → negrita en el texto

% Dibujar el flujo del sistema
text(0.5, 0.80, 'Arduino UNO/MEGA', 'HorizontalAlignment', 'center', ...
    'FontSize', 11, 'BackgroundColor', [0.8 0.9 1], 'EdgeColor', 'b', 'Margin', 5);
% BackgroundColor → color de fondo del cuadro de texto
% EdgeColor → color del borde
% Margin → espacio entre el texto y el borde

text(0.5, 0.70, '\downarrow  PWM (0-255)', 'HorizontalAlignment', 'center', ...
    'FontSize', 10, 'Color', [0 0 0.7]);
% '\downarrow' → flecha hacia abajo en LaTeX

text(0.5, 0.60, 'Driver L298N', 'HorizontalAlignment', 'center', ...
    'FontSize', 11, 'BackgroundColor', [1 0.9 0.8], 'EdgeColor', [0.8 0.4 0], 'Margin', 5);

text(0.5, 0.50, '\downarrow  V_{motor} (0-10V)', 'HorizontalAlignment', 'center', ...
    'FontSize', 10, 'Color', [0 0 0.7]);

text(0.5, 0.40, 'Motor JGA25-370', 'HorizontalAlignment', 'center', ...
    'FontSize', 11, 'BackgroundColor', [0.8 1 0.8], 'EdgeColor', [0 0.6 0], 'Margin', 5);

text(0.5, 0.30, '\downarrow  Torque + Giro', 'HorizontalAlignment', 'center', ...
    'FontSize', 10, 'Color', [0 0 0.7]);

text(0.5, 0.20, 'Cintura del Robot', 'HorizontalAlignment', 'center', ...
    'FontSize', 11, 'BackgroundColor', [1 0.85 0.85], 'EdgeColor', [0.8 0 0], 'Margin', 5);

% Nota sobre lazo abierto
text(0.5, 0.05, '\it Sin retroalimentacion (Lazo Abierto)', ...
    'HorizontalAlignment', 'center', 'FontSize', 10, 'Color', 'r');
% '\it' → itálica en el texto

sgtitle('ANALISIS — Motor JGA25-370 (Cintura)', 'FontSize', 14, 'FontWeight', 'bold');

%% =================== RESUMEN EN CONSOLA ===================
% Imprimimos un resumen completo de la simulación en la ventana de comandos.

fprintf('\n========================================================\n');
fprintf('  RESUMEN DE SIMULACION — MOTOR DE CINTURA (LAZO ABIERTO)\n');
fprintf('========================================================\n\n');
% fprintf() imprime texto formateado en la consola.
% \n = salto de línea
% %s = string, %d = entero, %.Xf = decimal con X decimales

fprintf('MOTOR: JGA25-370 con caja reductora\n');
fprintf('  Voltaje nominal: %d V\n', motor.Vnom);
% %d inserta un número entero en la posición indicada.
fprintf('  Ganancia K: %.1f RPM/V\n', motor.K);
% %.1f inserta un número con 1 decimal.
fprintf('  Constante de tiempo tau: %.3f s\n', motor.tau);
% %.3f inserta un número con 3 decimales.
fprintf('  RPM sin carga (nominal): %d RPM\n', motor.RPM_noload);
fprintf('  RPM maxima efectiva (con carga): %.1f RPM\n', vel_max_efectiva);

fprintf('\nDRIVER: L298N (Puente H doble)\n');
fprintf('  Voltaje fuente: %d V\n', driver.V_fuente);
fprintf('  Caida de voltaje: %.1f V\n', driver.V_drop);
fprintf('  Voltaje efectivo al motor: %.1f V\n', driver.V_efectivo);
fprintf('  Resolucion PWM: %d niveles (8 bits)\n', driver.PWM_max);
fprintf('  Corriente maxima continua: ~2 A\n');

fprintf('\nRESPUESTA TEMPORAL:\n');
fprintf('  Tiempo de establecimiento (5%%): %.3f s\n', 3*motor.tau);
% El %% doble imprime un símbolo de porcentaje literal (% es un carácter
% especial en fprintf, así que %% produce un solo %).
fprintf('  Tiempo de establecimiento (2%%): %.3f s\n', 4*motor.tau);
fprintf('  Velocidad final (estado estable): %.1f RPM\n', vel_final);

fprintf('\nCONEXIONADO FISICO:\n');
fprintf('  Arduino Pin 8  --> L298N IN1 (direccion A)\n');
fprintf('  Arduino Pin 9  --> L298N IN2 (direccion B)\n');
fprintf('  Arduino Pin 10 --> L298N ENA (velocidad PWM)\n');
fprintf('  L298N OUT1     --> Motor JGA25-370 terminal (+)\n');
fprintf('  L298N OUT2     --> Motor JGA25-370 terminal (-)\n');
fprintf('  Fuente 12V     --> L298N VCC\n');
fprintf('  GND comun      --> Arduino GND + L298N GND + Fuente GND\n');

fprintf('\nLIMITACIONES DEL LAZO ABIERTO:\n');
fprintf('  - No se verifica si el motor realmente giro\n');
fprintf('  - La posicion real depende de la carga y la friccion\n');
fprintf('  - No hay correccion automatica de errores\n');
fprintf('  - Cambios en voltaje de fuente afectan la velocidad\n');
fprintf('  - La temperatura del motor cambia su resistencia interna\n');

fprintf('\n========================================================\n');
