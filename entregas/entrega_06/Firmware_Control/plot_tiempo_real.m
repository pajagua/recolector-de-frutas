% SCRIPT: Lectura en tiempo real del Arduino Mega para Identificación
% Asegúrate de no tener el "Serial Monitor" del IDE de Arduino abierto
% porque bloqueará el puerto COM.

clear all; close all; clc;

% Limpiar puertos serie previos que hayan quedado bloqueados
if ~isempty(serialportfind)
    delete(serialportfind);
end

% -----------------------------------------------------------
% CONFIGURACIÓN (¡Cambiar por tu puerto real, ej: 'COM3' o 'COM5'!)
% -----------------------------------------------------------
puerto = 'COM3'; 
baudRate = 115200;

try
    arduino = serialport(puerto, baudRate);
catch
    error('No se pudo abrir el puerto %s. Verifica que el IDE de Arduino esté cerrado.', puerto);
end

configureTerminator(arduino, "LF"); % Arduino envía líneas terminadas en Line Feed (\n)

% -----------------------------------------------------------
% CONFIGURACIÓN DE LA GRÁFICA (*Real-time Plot*)
% -----------------------------------------------------------
figure('Name', 'Real-Time Step Response', 'NumberTitle', 'off');
hLine = animatedline('Color', 'b', 'LineWidth', 2);
xlabel('Muestras (*Sample Time* = 10ms)');
ylabel('Velocidad Angular (rad/s)');
title('Identificación del Motor en Lazo Abierto');
grid on;

% Parámetros de la prueba
muestrasMaximas = 300; % 300 muestras * 10ms = 3 segundos de grabación
velocidad_array = zeros(1, muestrasMaximas);

disp('Leyendo datos del Arduino... (espera los 3 segundos del delay inicial)');

% -----------------------------------------------------------
% BUCLE DE LECTURA EN TIEMPO REAL
% -----------------------------------------------------------
for i = 1:muestrasMaximas
    try
        % Lee una línea de texto completa del Arduino
        datosString = readline(arduino); 
        
        % Filtrar la cadena "Velocity_Rad_s:123.45" para quedarse con el número
        if contains(datosString, ':')
            valorStr = extractAfter(datosString, ':');
            velocidadActual = str2double(valorStr);
            
            % Guardar en el array para tu análisis posterior
            velocidad_array(i) = velocidadActual;
            
            % Agregar el punto a la gráfica animada
            addpoints(hLine, i, velocidadActual);
            
            % Dibujar en pantalla (limitrate evita que MATLAB se congele)
            drawnow limitrate; 
        end
    catch ME
        disp('Transmisión interrumpida o error de lectura.');
        break;
    end
end

% -----------------------------------------------------------
% LIMPIEZA Y CIERRE
% -----------------------------------------------------------
clear arduino;
disp('Prueba finalizada.');
disp('-> Ya tienes la matriz "velocidad_array" guardada en el Workspace de MATLAB.');
disp('-> Puedes usar "max(velocidad_array)" para hallar K, y buscar el 63.2% para tau.');
