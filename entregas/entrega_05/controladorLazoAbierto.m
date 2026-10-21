%% ============================================================
%  SIMULACIÓN DE CONTROL EN LAZO ABIERTO — CitriBot RRP
%  Robótica I — Universidad Nacional de Asunción
%  ============================================================
%  Este script simula el comportamiento de los 3 tipos de motores
%  del brazo CitriBot controlados en lazo abierto.
%
%  Modelos utilizados:
%    Motor DC:     Función de transferencia de 1er orden
%    Stepper:      Respuesta ideal (escalón de posición)
%    Servo MG995:  Respuesta de 2do orden subamortiguada
%  ============================================================

clear; clc; close all;

%% =================== PARÁMETROS DEL SISTEMA ===================

% --- Motor DC 37Dx70.1L (con caja reductora) ---
% Modelamos el motor DC como un sistema de primer orden:
%   G(s) = K / (tau*s + 1)
% donde:
%   K   = ganancia estática (velocidad final / voltaje aplicado)
%   tau = constante de tiempo (tiempo para alcanzar 63.2% de la vel. final)

dc.K   = 100;       % [RPM/V] Ganancia del motor (aprox.)
dc.tau = 0.15;      % [s] Constante de tiempo mecánica + eléctrica
dc.Vmax = 12;       % [V] Voltaje máximo de alimentación

% --- Motor Paso a Paso NEMA 17 ---
% En lazo abierto, el stepper se modela como un integrador ideal:
% cada pulso incrementa la posición en un ángulo fijo (1.8°/paso).
% La velocidad depende de la frecuencia de pulsos.

stepper.step_angle = 1.8;          % [°/paso] Ángulo por paso completo
stepper.microstep  = 16;           % Factor de microstepping (A4988)
stepper.angle_per_ustep = stepper.step_angle / stepper.microstep;  % 0.1125°
stepper.pulse_delay_us = 800;      % [µs] Retardo entre pulsos
stepper.pulse_freq = 1e6 / (2 * stepper.pulse_delay_us); % [Hz] Frecuencia de pulsos

% --- Servo MG995 ---
% Modelamos el servo como un sistema de 2do orden:
%   G(s) = wn^2 / (s^2 + 2*zeta*wn*s + wn^2)
% El MG995 tiene una velocidad de ~0.2s/60° → wn ≈ 30 rad/s

servo.wn   = 30;     % [rad/s] Frecuencia natural
servo.zeta = 0.7;    % Factor de amortiguamiento (ligeramente subamortiguado)
servo.max_angle = 180; % [°] Rango máximo

%% =================== SIMULACIÓN TEMPORAL ===================
dt = 0.001;          % [s] Paso de simulación (1 ms)
t_total = 10;        % [s] Tiempo total de simulación
t = 0:dt:t_total;    % Vector de tiempo
N = length(t);

%% ---------- 1. SIMULACIÓN DEL MOTOR DC ----------
% Secuencia de comandos PWM (lazo abierto):
%   0-2s:   PWM = 0     (detenido)
%   2-4s:   PWM = 50%   (velocidad media adelante)
%   4-5s:   PWM = 0     (freno)
%   5-7s:   PWM = -30%  (velocidad baja reversa)
%   7-10s:  PWM = 0     (detenido)

% Señal de comando (porcentaje de PWM, -100 a +100)
pwm_cmd = zeros(1, N);
pwm_cmd(t >= 2 & t < 4) = 50;      % 50% adelante
pwm_cmd(t >= 5 & t < 7) = -30;     % 30% reversa

% Voltaje aplicado al motor
V_motor = (pwm_cmd / 100) * dc.Vmax;

% Simular respuesta del motor DC (Euler hacia adelante)
% dw/dt = (1/tau) * (K*V - w)
dc_vel = zeros(1, N);   % Velocidad angular [RPM]
dc_pos = zeros(1, N);   % Posición angular [°] (integral de velocidad)

for i = 2:N
    dw = (1/dc.tau) * (dc.K * V_motor(i-1) - dc_vel(i-1));
    dc_vel(i) = dc_vel(i-1) + dw * dt;
    dc_pos(i) = dc_pos(i-1) + dc_vel(i-1) * (360/60) * dt;  % RPM → °/s
end

%% ---------- 2. SIMULACIÓN DEL STEPPER NEMA 17 ----------
% Secuencia de comandos (lazo abierto):
%   0-1s:   Detenido
%   1-3s:   Avanzar 90° en sentido horario
%   3-4s:   Detenido
%   4-6s:   Retroceder 90° en sentido antihorario
%   6-10s:  Detenido

% Calcular cuántos pasos para 90°
steps_for_90 = round(90 / stepper.angle_per_ustep);
% Tiempo que tarda en dar esos pasos
time_for_90 = steps_for_90 * (2 * stepper.pulse_delay_us * 1e-6);

stepper_pos = zeros(1, N);         % Posición acumulada [°]
stepper_vel_cmd = zeros(1, N);     % Velocidad comandada [°/s]

% Velocidad angular del stepper cuando está en movimiento
stepper_speed = stepper.angle_per_ustep * stepper.pulse_freq;  % [°/s]

% Avance (1s hasta completar 90°)
t_start_fwd = 1.0;
t_end_fwd = t_start_fwd + time_for_90;
stepper_vel_cmd(t >= t_start_fwd & t < t_end_fwd) = stepper_speed;

% Retroceso (4s hasta completar 90°)
t_start_rev = 4.0;
t_end_rev = t_start_rev + time_for_90;
stepper_vel_cmd(t >= t_start_rev & t < t_end_rev) = -stepper_speed;

% Integrar para obtener posición
for i = 2:N
    stepper_pos(i) = stepper_pos(i-1) + stepper_vel_cmd(i-1) * dt;
end

%% ---------- 3. SIMULACIÓN DEL SERVO MG995 ----------
% Secuencia de comandos (lazo abierto):
%   0-1s:   Posición 90° (centro)
%   1-3s:   Ir a 0°
%   3-5s:   Ir a 180°
%   5-7s:   Ir a 90° (centro)
%   7-10s:  Mantener 90°

servo_cmd = 90 * ones(1, N);       % Comando de ángulo [°]
servo_cmd(t >= 1 & t < 3) = 0;
servo_cmd(t >= 3 & t < 5) = 180;
servo_cmd(t >= 5) = 90;

% Simular respuesta del servo (sistema de 2do orden)
% d²θ/dt² + 2*zeta*wn*dθ/dt + wn²*θ = wn²*θ_cmd
servo_pos = zeros(1, N);           % Posición real del servo [°]
servo_vel = zeros(1, N);           % Velocidad del servo [°/s]
servo_pos(1) = 90;                 % Posición inicial

for i = 2:N
    error = servo_cmd(i-1) - servo_pos(i-1);
    accel = servo.wn^2 * error - 2 * servo.zeta * servo.wn * servo_vel(i-1);
    servo_vel(i) = servo_vel(i-1) + accel * dt;
    servo_pos(i) = servo_pos(i-1) + servo_vel(i-1) * dt;
end

%% =================== GRÁFICAS ===================

figure('Name', 'Control en Lazo Abierto — CitriBot RRP', ...
    'Position', [100 50 900 750], 'Color', 'w');

% --- Motor DC ---
subplot(3,2,1);
plot(t, pwm_cmd, 'b-', 'LineWidth', 1.5);
xlabel('Tiempo [s]'); ylabel('PWM [%]');
title('Motor DC — Comando PWM (Entrada)');
grid on; ylim([-60 60]);

subplot(3,2,2);
yyaxis left;
plot(t, dc_vel, 'r-', 'LineWidth', 1.5);
ylabel('Velocidad [RPM]');
yyaxis right;
plot(t, dc_pos, 'g-', 'LineWidth', 1.2);
ylabel('Posición [°]');
xlabel('Tiempo [s]');
title('Motor DC — Respuesta (Velocidad y Posición)');
grid on; legend('Velocidad', 'Posición', 'Location', 'best');

% --- Stepper NEMA 17 ---
subplot(3,2,3);
plot(t, stepper_vel_cmd, 'b-', 'LineWidth', 1.5);
xlabel('Tiempo [s]'); ylabel('Velocidad [°/s]');
title('Stepper NEMA 17 — Velocidad Comandada');
grid on;

subplot(3,2,4);
plot(t, stepper_pos, 'r-', 'LineWidth', 1.5);
xlabel('Tiempo [s]'); ylabel('Posición [°]');
title('Stepper NEMA 17 — Posición Acumulada');
grid on;

% --- Servo MG995 ---
subplot(3,2,5);
stairs(t, servo_cmd, 'b-', 'LineWidth', 1.5);
xlabel('Tiempo [s]'); ylabel('Ángulo [°]');
title('Servo MG995 — Comando de Ángulo (Entrada)');
grid on; ylim([-10 200]);

subplot(3,2,6);
plot(t, servo_pos, 'r-', 'LineWidth', 1.5);
hold on;
stairs(t, servo_cmd, 'b--', 'LineWidth', 1);
xlabel('Tiempo [s]'); ylabel('Ángulo [°]');
title('Servo MG995 — Respuesta Real vs Comando');
legend('Respuesta real', 'Comando', 'Location', 'best');
grid on; ylim([-10 200]);

sgtitle('CONTROL EN LAZO ABIERTO — CitriBot RRP', 'FontSize', 14, 'FontWeight', 'bold');

%% =================== ANÁLISIS ADICIONAL ===================
% Imprimir resumen de la simulación

fprintf('\n========================================\n');
fprintf('  RESUMEN DE SIMULACIÓN — LAZO ABIERTO\n');
fprintf('========================================\n\n');

fprintf('MOTOR DC (37Dx70.1L):\n');
fprintf('  Ganancia K = %.0f RPM/V\n', dc.K);
fprintf('  Constante de tiempo tau = %.3f s\n', dc.tau);
fprintf('  Velocidad máxima teórica = %.0f RPM (a %dV)\n', dc.K*dc.Vmax, dc.Vmax);
fprintf('  Tiempo de establecimiento (5%%) ≈ %.3f s\n\n', 3*dc.tau);

fprintf('STEPPER NEMA 17:\n');
fprintf('  Ángulo por micro-paso = %.4f°\n', stepper.angle_per_ustep);
fprintf('  Frecuencia de pulsos = %.0f Hz\n', stepper.pulse_freq);
fprintf('  Velocidad angular = %.1f °/s (%.1f RPM)\n', stepper_speed, stepper_speed/6);
fprintf('  Tiempo para 90° = %.3f s (%d micro-pasos)\n\n', time_for_90, steps_for_90);

fprintf('SERVO MG995:\n');
fprintf('  Frecuencia natural wn = %.0f rad/s\n', servo.wn);
fprintf('  Amortiguamiento zeta = %.2f\n', servo.zeta);
fprintf('  Tiempo de establecimiento (2%%) ≈ %.3f s\n', 4/(servo.zeta*servo.wn));
fprintf('  Sobrepaso máximo ≈ %.1f%%\n', 100*exp(-pi*servo.zeta/sqrt(1-servo.zeta^2)));

%% =================== DIAGRAMA DE BLOQUES ===================
% Visualizar la diferencia entre lazo abierto y lazo cerrado

figure('Name', 'Lazo Abierto vs Lazo Cerrado', ...
    'Position', [150 100 800 400], 'Color', 'w');

% Texto explicativo con anotaciones
annotation('textbox', [0.05 0.55 0.9 0.4], ...
    'String', {'\bf LAZO ABIERTO (Esta entrega):', '', ...
    '  Comando → [Controlador/Arduino] → [Driver] → [Motor] → Movimiento', '', ...
    '  ❌ No hay sensor midiendo la salida', ...
    '  ❌ No hay corrección de errores', ...
    '  ✅ Simple de implementar', ...
    '  ✅ No requiere sensores adicionales'}, ...
    'FontSize', 11, 'EdgeColor', [0.2 0.6 0.2], 'LineWidth', 2, ...
    'BackgroundColor', [0.9 1 0.9]);

annotation('textbox', [0.05 0.05 0.9 0.4], ...
    'String', {'\bf LAZO CERRADO (Siguiente entrega):', '', ...
    '  Comando → [Controlador] → [Driver] → [Motor] → Movimiento', ...
    '                    ↑                                      │', ...
    '                    └──────── [Sensor/Encoder] ←────────┘', '', ...
    '  ✅ Corrige errores automáticamente', ...
    '  ✅ Mayor precisión', ...
    '  ❌ Requiere sensores (encoders, IMU, etc.)', ...
    '  ❌ Más complejo de diseñar y sintonizar'}, ...
    'FontSize', 11, 'EdgeColor', [0.8 0.2 0.2], 'LineWidth', 2, ...
    'BackgroundColor', [1 0.9 0.9]);

sgtitle('Comparación: Lazo Abierto vs Lazo Cerrado', 'FontSize', 14, 'FontWeight', 'bold');
