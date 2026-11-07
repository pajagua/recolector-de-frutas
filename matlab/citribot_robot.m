function robot = citribot_robot()
%CITRIBOT_ROBOT Brazo CitriBot como rigidBodyTree (Robotics System Toolbox).
%   robot = citribot_robot() arma el brazo con los parámetros de Denavit-Hartenberg
%   del CAD (docs/sistemas_de_referencia.md). Unidades: metros y radianes.
%
%   Variables de articulación (en este orden):
%     q1  J1 cintura   (rad)  giro desde el eje X de la base
%     q2  J2 hombro    (rad)  elevación de E2 sobre la horizontal
%     q3  J3 prismático (m)   carrera, 0 = retraído ... 0,34
%     q4  J4 cabeceo   (rad)  0 = ventosa perpendicular al brazo
%     q5  J5 torsión   (rad)
%   El último cuerpo, 'ventosa', es el labio de la ventosa (TCP).
%
%   Tabla DH estándar  A_i = Rz(theta) Tz(d) Tx(a) Rx(alfa):
%     i  tipo  theta        d (m)            a   alfa
%     1  R     q1           0.3555           0   90°
%     2  R     q2 + 90°     0.0830           0   90°
%     3  P     0            0.1726 + q3      0   90°
%     4  R     q4 + 90°    -0.0022           0  -90°
%     5  R     q5           0.0412           0    0

    %       tipo  theta_off   d_const   a   alfa       limites
    dh = { 'R',   0,          0.3555,   0,  pi/2,      [-pi pi];
           'R',   pi/2,       0.0830,   0,  pi/2,      deg2rad([-45 45]);
           'P',   0,          0.1726,   0,  pi/2,      [0 0.34];
           'R',   pi/2,      -0.0022,   0, -pi/2,      deg2rad([-90 90]);
           'R',   0,          0.0412,   0,  0,         [-pi pi] };
    nombres = {'cintura', 'E2', 'E3', 'caja', 'eje_hueco'};
    juntas  = {'J1', 'J2', 'J3', 'J4', 'J5'};

    robot = rigidBodyTree('DataFormat', 'row', 'MaxNumBodies', 6);
    padre = robot.BaseName;
    sufijo = eye(4);                 % parte fija que queda después de la articulación anterior
    for i = 1:size(dh, 1)
        [tipo, toff, d, a, alfa, lim] = dh{i, :};
        if tipo == 'R'
            prefijo = rotz_(toff);                     % Rz(offset) y después gira la articulación
            post    = trz_(d) * trx_(a) * rotx_(alfa);
            j = rigidBodyJoint(juntas{i}, 'revolute');
        else
            prefijo = rotz_(toff) * trz_(d);           % Tz(d fijo) y después desliza la articulación
            post    = trx_(a) * rotx_(alfa);
            j = rigidBodyJoint(juntas{i}, 'prismatic');
        end
        j.PositionLimits = lim;
        j.HomePosition = 0;
        setFixedTransform(j, sufijo * prefijo);
        b = rigidBody(nombres{i});
        b.Joint = j;
        addBody(robot, b, padre);
        padre = nombres{i};
        sufijo = post;
    end
    tcp = rigidBody('ventosa');
    setFixedTransform(tcp.Joint, sufijo);
    addBody(robot, tcp, padre);
end

function T = rotz_(t), T = [cos(t) -sin(t) 0 0; sin(t) cos(t) 0 0; 0 0 1 0; 0 0 0 1]; end
function T = rotx_(t), T = [1 0 0 0; 0 cos(t) -sin(t) 0; 0 sin(t) cos(t) 0; 0 0 0 1]; end
function T = trz_(d),  T = [eye(3) [0; 0; d]; 0 0 0 1]; end
function T = trx_(a),  T = [eye(3) [a; 0; 0]; 0 0 0 1]; end
