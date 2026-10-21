% -------------------------------------------------------------------------
% DATA ACQUISITION SCRIPT - VELOCITY CONTROL (HIL)
% Proyecto: Robot Recolector de Naranjas - Robótica 1
% -------------------------------------------------------------------------
clear all; close all; clc;
% 'clear all' NO borra timers: se eliminan los que hayan quedado de corridas anteriores
delete(timerfindall);

% 1. PARAMETROS DE HARDWARE Y ENLACE SERIAL
% -------------------------------------------------------------------------
port = "COM3";
baudRate = 115200;

% 1b. PERFIL DE REFERENCIA (secuencia de escalones de velocidad)
% -------------------------------------------------------------------------
% Cada valor se mantiene durante STEP_DURATION_S segundos.
% El ultimo valor deberia ser 0 para que el motor termine detenido.
SETPOINT_SEQUENCE = [10 20 15 5 -10 -20 0];   % rad/s
STEP_DURATION_S   = 4;                          % s por escalon
REPEAT_SEQUENCE   = false;                      % true = repetir en bucle (ignora el 0 final hasta cerrar la ventana)

disp(">>> Inicializando conexion asincrona con " + port + " para Control de Velocidad...");
s = serialport(port, baudRate);
configureTerminator(s, "LF"); 

% 2. CONFIGURACION DE LA INTERFAZ GRAFICA (GUI)
% -------------------------------------------------------------------------
fig = figure('Name', 'Analisis de Respuesta Transitoria PID (Velocidad)', 'NumberTitle', 'off');

% Eje Y Primario (Velocidad Angular)
yyaxis left;
line_setpoint = animatedline('Color', 'r', 'LineStyle', '--', 'LineWidth', 1.5, 'DisplayName', 'Setpoint (rad/s)');
line_vel = animatedline('Color', 'b', 'LineWidth', 2, 'DisplayName', 'Velocidad Real (rad/s)');
ylabel('Velocidad Angular (rad/s)');
grid on;

% Eje Y Secundario (Senal de Control - PWM)
yyaxis right;
line_pwm = animatedline('Color', 'k', 'LineStyle', ':', 'LineWidth', 1, 'DisplayName', 'PWM (0-255)');
ylabel('Senal de Control (PWM)');
ylim([0 300]); 

xlabel('Muestras (Ts = 20 ms)');
title('Hardware-in-the-Loop: Step Response del Motor DC (Velocidad)');
legend('Location', 'best');

% Estructura de punteros para el callback
userData = struct('line_setpoint', line_setpoint, 'line_vel', line_vel, 'line_pwm', line_pwm, 'sample_count', 0);
fig.UserData = userData;

% 3. CONFIGURACION DEL CALLBACK (INTERRUPCION POR SOFTWARE)
% -------------------------------------------------------------------------
configureCallback(s, "terminator", @(src, event) readSerialCallback(src, event, fig));

% BUGFIX: Pausa para sortear el Hardware Reset del bootloader Arduino Uno
disp(">>> Esperando 2.5 segundos a que inicie el firmware...");
pause(2.5);

% 4. ACTUACION: TIMER QUE RECORRE LA SECUENCIA DE SETPOINTS
% -------------------------------------------------------------------------
% El timer corre en paralelo al callback serial: uno escribe, el otro lee.
if REPEAT_SEQUENCE
    n_tasks = inf;
else
    n_tasks = numel(SETPOINT_SEQUENCE);
end

sp_timer = timer( ...
    'ExecutionMode', 'fixedRate', ...          % Periodo constante entre escalones
    'Period',        STEP_DURATION_S, ...
    'TasksToExecute', n_tasks, ...
    'BusyMode',      'queue', ...
    'TimerFcn',      @(t, ~) sendNextSetpoint(t, s, SETPOINT_SEQUENCE));

% Al cerrar la ventana: detener timer, parar motor y liberar el COM3
fig.CloseRequestFcn = @(f, ~) onCloseFigure(f, s, sp_timer);

disp(">>> Enlace establecido. Iniciando secuencia de setpoints:");
disp("    " + strjoin(string(SETPOINT_SEQUENCE), " -> ") + " rad/s, cada " + STEP_DURATION_S + " s");
start(sp_timer);   % El primer escalon se envia inmediatamente (StartDelay = 0)

% -------------------------------------------------------------------------
% FUNCION DE SERVICIO DE INTERRUPCION (ISR DEL PUERTO SERIAL)
% -------------------------------------------------------------------------
function readSerialCallback(src, ~, fig)
    rawData = readline(src);
    data = str2double(split(rawData, ','));
    
    if length(data) == 3 && all(~isnan(data))
        
        if ~isvalid(fig)
            configureCallback(src, "off");
            disp(">>> Graficador cerrado. Recepcion detenida.");
            return;
        end
        
        handles = fig.UserData;
        handles.sample_count = handles.sample_count + 1;
        t = handles.sample_count;
        
        % Inyeccion de datos al motor grafico
        addpoints(handles.line_setpoint, t, data(1));
        addpoints(handles.line_vel, t, data(2));
        addpoints(handles.line_pwm, t, data(3));
        
        fig.UserData = handles;
        drawnow limitrate;
    end
end

% -------------------------------------------------------------------------
% CALLBACK DEL TIMER: ENVIA EL SIGUIENTE SETPOINT DE LA SECUENCIA
% -------------------------------------------------------------------------
function sendNextSetpoint(t, s, sequence)
    % TasksExecuted cuenta desde 1. mod() permite repetir la secuencia en bucle.
    idx = mod(t.TasksExecuted - 1, numel(sequence)) + 1;
    sp  = sequence(idx);
    try
        writeline(s, string(sp));
        fprintf(">>> [t = %5.1f s] Nuevo setpoint: %g rad/s\n", ...
                (t.TasksExecuted - 1) * t.Period, sp);
    catch
        stop(t);   % Puerto cerrado: no seguir enviando
    end
end

% -------------------------------------------------------------------------
% CIERRE LIMPIO: detener timer, mandar 0 rad/s, liberar puerto y ventana
% -------------------------------------------------------------------------
function onCloseFigure(fig, s, sp_timer)
    try
        stop(sp_timer);
        delete(sp_timer);
    catch
    end
    try
        configureCallback(s, "off");
        writeline(s, "0");   % Seguridad: dejar el motor detenido
    catch
    end
    delete(fig);
    disp(">>> Ventana cerrada. Motor detenido y timer eliminado.");
end