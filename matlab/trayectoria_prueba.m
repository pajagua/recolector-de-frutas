%TRAYECTORIA_PRUEBA Ciclo de cosecha de prueba para el modelo de Simscape CB_001_Telescopico.
%   Resuelve la cinemática inversa de cada punto del ciclo y deja en el workspace:
%     qpuntos  3xN  [q1; q2; q3] de cada punto (rad, rad, m)
%     tiempos  1xN  instante en que el brazo llega a cada punto (s)
%   Van al bloque Polynomial Trajectory (Waypoints = qpuntos, Time points = tiempos).
%   Entre un punto y otro el brazo se mueve articulación por articulación, como el
%   firmware: una recta en XYZ entre dos puntos lejanos pasaría por adentro del
%   alcance mínimo del brazo.
%
%   Los puntos están en el marco de la base (SR0 = origen de CB-001, z hacia arriba).
%   La herramienta queda fija en la pose del CAD (q4 = 1,944°, q5 = 0), igual que en
%   el modelo de Simscape.

robot = citribot_robot([deg2rad(1.944) 0]);

%          x (m)   y (m)   z (m)   t (s)
ciclo = [ 0.156   0.103   0.412    0     % inicio (pose de la exportación, q3 = 0)
          0.271   0.058   0.433    2     % frente a la fruta
          0.428   0.148   0.517    4     % en la fruta
          0.428   0.148   0.517    5     % agarra (1 s quieto)
          0.231   0.035   0.412    7     % retrocede con la fruta
          0.032  -0.225   0.277   10     % sobre la canasta
          0.032  -0.225   0.277   11     % suelta (1 s quieto)
          0.156   0.103   0.412   13 ];  % vuelve al inicio

ik = inverseKinematics('RigidBodyTree', robot);
pesos = [0 0 0 1 1 1];                       % solo importa la posición
q = [deg2rad(60) deg2rad(30) 0.1];          % primera suposición
n = size(ciclo, 1);
qpuntos = zeros(3, n);
for k = 1:n
    q = ik('ventosa', trvec2tform(ciclo(k, 1:3)), pesos, q);   % parte de la solución anterior
    T = getTransform(robot, q, 'ventosa');
    err = norm(T(1:3, 4)' - ciclo(k, 1:3)) * 1000;
    if err > 1
        warning('Punto %d fuera de alcance: queda a %.1f mm.', k, err);
    end
    qpuntos(:, k) = q';
    fprintf('%d  t=%4.1f s   q1=%7.2f°  q2=%6.2f°  q3=%6.1f mm   error %.2f mm\n', ...
            k, ciclo(k, 4), rad2deg(q(1)), rad2deg(q(2)), q(3) * 1000, err);
end
tiempos = ciclo(:, 4)';
