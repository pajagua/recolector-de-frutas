%PRUEBA_IK Cinemática directa e inversa del brazo CitriBot.
%   Correr desde la carpeta matlab/ del repo.

robot = citribot_robot();
showdetails(robot)

%% 1. Cinemática directa: control contra el CAD
% Pose en que se midieron los sistemas de referencia en SolidWorks:
% q1 = 60.54°, q2 = 32.35°, q3 = 0, q4 = 1.94°, q5 = 0. Ahí el labio de la
% ventosa está en (156.2, 103.3, 412.3) mm en coordenadas de la base.
qCAD = [deg2rad(60.54) deg2rad(32.35) 0 deg2rad(1.94) 0];
T = getTransform(robot, qCAD, 'ventosa');
fprintf('TCP en la pose del CAD: (%.1f, %.1f, %.1f) mm  (CAD: 156.2, 103.3, 412.3)\n', T(1:3, 4) * 1000);

figure; show(robot, qCAD, 'Frames', 'on'); axis equal; title('Pose del CAD');

%% 2. Cinemática inversa: llevar la ventosa a un punto
% Se pide solo la posición del labio (pesos de orientación en 0): con 5
% articulaciones el brazo no puede fijar posición y orientación completas a la vez.
ik = inverseKinematics('RigidBodyTree', robot);
pesos = [0 0 0 1 1 1];             % [orientación(3) posición(3)]
q0 = homeConfiguration(robot);

objetivo = [0.30 0.15 0.40];       % punto en metros (X, Y, Z desde la base)
[q, info] = ik('ventosa', trvec2tform(objetivo), pesos, q0);
Tq = getTransform(robot, q, 'ventosa');
fprintf('IK: %s, error de posición %.2f mm\n', info.Status, norm(Tq(1:3, 4)' - objetivo) * 1000);
fprintf('  q1 = %.1f°, q2 = %.1f°, q3 = %.1f mm, q4 = %.1f°, q5 = %.1f°\n', ...
    rad2deg(q(1)), rad2deg(q(2)), q(3) * 1000, rad2deg(q(4)), rad2deg(q(5)));

figure; show(robot, q, 'Frames', 'off'); hold on
plot3(objetivo(1), objetivo(2), objetivo(3), 'ro', 'MarkerSize', 10, 'LineWidth', 2);
axis equal; title('Cinemática inversa');

%% 3. Recorrido: varios puntos seguidos (animación)
puntos = [0.30 0.15 0.40; 0.35 -0.10 0.35; 0.25 0.20 0.50; 0.10 0.40 0.30; 0.30 0.15 0.40];
figure; ax = gca;
qi = q0;
for k = 1:size(puntos, 1)
    p = puntos(k, :);
    qi = ik('ventosa', trvec2tform(p), pesos, qi);     % arranca desde la solución anterior
    show(robot, qi, 'Parent', ax, 'PreservePlot', false, 'Frames', 'off'); hold(ax, 'on');
    plot3(ax, p(1), p(2), p(3), 'ro'); axis(ax, 'equal'); drawnow; pause(0.8);
end
