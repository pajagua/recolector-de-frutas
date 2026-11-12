%MASAS_REALES Masas reales para el modelo de Simscape CB_001_Telescopico.
%   Las piezas del CAD no tienen material asignado, así que SolidWorks exportó todas
%   con 1000 kg/m3 (el tubo de aluminio de 30 x 30 sale con 61 g en vez de 166 g).
%   Este script:
%     1. carga los datos de la exportación (smiData del DataFile),
%     2. multiplica la masa y las inercias de cada pieza por su densidad real
%        (las impresas, por la del PETG y la fracción maciza según el relleno),
%     3. suma lo que el brazo solo (CB-001) no tiene: NEMA 17 de J3, servos de la
%        herramienta, motorreductor de J2 y, si con_naranja = true, la naranja,
%     4. guarda smiData en simulink/brazo_telescopico/CB_001_masas_reales.mat.
%   En el modelo: Model Explorer > CB_001_Telescopico > Model Workspace >
%   Data source: MAT-File con ese archivo > Reinitialize from Source.
%
%   Lo que se suma se reparte como la pieza en la que se monta (mismo centro de masa,
%   inercia escalada). Las fracciones macizas de las impresas son estimaciones:
%   conviene pesar las piezas reales y corregir esta tabla.

con_naranja = true;     % peor caso: la naranja va en la ventosa todo el ciclo

raiz = fileparts(fileparts(mfilename('fullpath')));
carpeta = fullfile(raiz, 'simulink', 'brazo_telescopico');
run(fullfile(carpeta, 'CB_001_Brazo_Solo_sin_limites_DataFile.m'));   % crea smiData

PETG = 1.27;            % g/cm3
densidades = {          % patrón del ID de la pieza (regexp; gana el primero), g/cm3
    'CB-411B|CB-421B',                    2.70        % tubos de aluminio
    'HW_Polea',                           2.70        % poleas GT2 de aluminio
    'HW_|CB-506|CB-615|CB-616|CB-623',    7.85        % acero: tornillería, rodamientos, eje
    'CB-504',                             7.50        % imán de neodimio
    'CB-503',                             1.85        % placa del AS5600
    'CB-441B|CB-442B',                    1.20        % correas GT2
    'CB-618',                             1.15        % ventosa de goma
    'CB-619',                             3.00        % racor (latón y plástico)
    'CB-516',                             1.50        % microswitch
    'CB-614',                             1.40        % horn de plástico del servo
    'CB-301B',                            PETG * 0.5  % E2: cubo al 50 %, manga al 25 %
    'CB-306|CB-41[2-5]B|CB-422B|CB-432B|CB-605|CB-606|CB-621|CB-622', PETG   % impresas al 100 %
    'CB-',                                PETG * 0.6  % resto de las impresas (4 perímetros)
};
extras = {              % pieza donde se monta, masa kg, qué es
    'CB-306', 0.280, 'NEMA 17 de J3 (17HS4401)'
    'CB-601', 0.055, 'servo MG996R de la muñeca'
    'CB-602', 0.055, 'servo MG996R de la torsión'
    'CB-210', 0.350, 'motorreductor 5840 de J2'
};
if con_naranja
    extras(end + 1, :) = {'CB-618', 0.180, 'naranja'};
end

antes = 0; despues = 0;
for k = 1:numel(smiData.Solid)
    s = smiData.Solid(k);
    if strlength(s.ID) == 0, continue; end
    f = NaN;
    for d = 1:size(densidades, 1)
        if ~isempty(regexp(s.ID, densidades{d, 1}, 'once')), f = densidades{d, 2}; break; end
    end
    if isnan(f), error('Sin densidad para %s', s.ID); end
    m0 = s.mass;
    m = m0 * f;
    for e = 1:size(extras, 1)
        if startsWith(s.ID, extras{e, 1} + "_"), m = m + extras{e, 2}; end
    end
    escala = m / m0;
    smiData.Solid(k).mass = m;
    smiData.Solid(k).MoI = s.MoI * escala;
    smiData.Solid(k).PoI = s.PoI * escala;
    antes = antes + m0; despues = despues + m;
end
fprintf('Masa de las piezas (una vez cada una): %.2f kg exportado -> %.2f kg real\n', antes, despues);

archivo = fullfile(carpeta, 'CB_001_masas_reales.mat');
save(archivo, 'smiData');
fprintf('Guardado %s\n', archivo);
