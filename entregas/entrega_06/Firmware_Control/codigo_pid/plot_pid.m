% -------------------------------------------------------------------------
% DATA ACQUISITION SCRIPT - HARDWARE IN THE LOOP (HIL)
% Proyecto: Robot Recolector de Naranjas - Robótica 1
% -------------------------------------------------------------------------
clear all; close all; clc;
% 'clear all' destruye cualquier objeto 'serialport' en el workspace, cerrando el COM3.

% 1. PARAMETROS DE HARDWARE Y ENLACE SERIAL
% -------------------------------------------------------------------------
port = "COM3";
baudRate = 115200;

disp(">>> Inicializando conexion asincrona con " + port + "...");
s = serialport(port, baudRate);
% El 'Terminator' indica el final de la trama de datos (coincide con Serial.println)
configureTerminator(s, "LF"); 

% 2. CONFIGURACION DE LA INTERFAZ GRAFICA (GUI)
% -------------------------------------------------------------------------
fig = figure('Name', 'Analisis de Respuesta Transitoria PID', 'NumberTitle', 'off');

% Eje Y Primario (Posicion Angular)
yyaxis left;
line_setpoint = animatedline('Color', 'r', 'LineStyle', '--', 'LineWidth', 1.5, 'DisplayName', 'Setpoint (rad)');
line_pos = animatedline('Color', 'b', 'LineWidth', 2, 'DisplayName', 'Posicion Real (rad)');
ylabel('Posicion Angular (Radianes)');
grid on;

% Eje Y Secundario (Senal de Control - PWM / Saturation)
yyaxis right;
line_pwm = animatedline('Color', 'k', 'LineStyle', ':', 'LineWidth', 1, 'DisplayName', 'PWM (0-255)');
ylabel('Senal de Control (PWM)');
ylim([0 300]); % Dejamos margen por encima de los 255 del duty cycle maximo

xlabel('Muestras');
title('Hardware-in-the-Loop: Step Response del Motor DC');
legend('Location', 'best');

% Guardamos los punteros a las lineas y el contador de muestras en el objeto figure
% para evitar el uso de variables globales, lo cual es mala practica en MATLAB.
userData = struct('line_setpoint', line_setpoint, 'line_pos', line_pos, 'line_pwm', line_pwm, 'sample_count', 0);
fig.UserData = userData;

% 3. CONFIGURACION DEL CALLBACK (INTERRUPCION POR SOFTWARE)
% -------------------------------------------------------------------------
% La funcion 'readSerialCallback' se ejecutara solo cuando llegue un "\n"
configureCallback(s, "terminator", @(src, event) readSerialCallback(src, event, fig));

% BUGFIX CRITICO: Hardware Reset del Arduino Uno.
% Al abrir el puerto COM, la senal DTR provoca el reinicio del ATmega328P.
% Si enviamos el comando inmediatamente, el bootloader lo ignora.
disp(">>> Esperando 2.5 segundos para que el Arduino Uno termine de bootear...");
pause(2.5);

disp(">>> Enlace establecido. Enviando Setpoint de prueba...");

% 4. ACTUACION: ENVIO DE COMANDO AL CONTROLADOR
% -------------------------------------------------------------------------
% Mandamos el setpoint para encender la maquina de estados del motor
writeline(s, "10000"); 


% -------------------------------------------------------------------------
% FUNCION DE SERVICIO DE INTERRUPCION (ISR DEL PUERTO SERIAL)
% -------------------------------------------------------------------------
function readSerialCallback(src, ~, fig)
    % 1. Extraer la trama de datos del buffer
    rawData = readline(src);
    
    % 2. Parsing: Separar el CSV y convertir a double
    data = str2double(split(rawData, ','));
    
    % 3. Filtrado de ruido: La cadena de inicio del Arduino dice "SISTEMA INICIADO".
    % Al pasarle str2double se vuelve NaN. Solo graficamos si llegaron los 3 datos limpios.
    if length(data) == 3 && all(~isnan(data))
        
        % Seguridad: Si el usuario cerro la ventana grafica, detener el script
        if ~isvalid(fig)
            configureCallback(src, "off");
            disp(">>> Graficador cerrado. Recepcion detenida.");
            return;
        end
        
        % Recuperar los handles graficos y actualizar contador
        handles = fig.UserData;
        handles.sample_count = handles.sample_count + 1;
        t = handles.sample_count;
        
        % Inyectar la posicion X (muestras) e Y (magnitud) al frame grafico
        addpoints(handles.line_setpoint, t, data(1));
        addpoints(handles.line_pos, t, data(2));
        addpoints(handles.line_pwm, t, data(3));
        
        fig.UserData = handles;
        
        % Forzar el renderizado de la GPU minimizando el overhead del sistema
        drawnow limitrate;
    end
end