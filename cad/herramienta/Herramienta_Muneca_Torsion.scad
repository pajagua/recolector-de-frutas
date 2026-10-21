// ============================================================
// HERRAMIENTA FINAL: MUÑECA (PITCH) + TORSIÓN + SUCCIÓN   v4.2
// Robot Recolector de Naranjas "CitriBot RRP"  (P-RRP-RR)
// ============================================================
//
// Basado en "StepperMotorBracket" (carpeta Diseño): caja con el
// servo de torsión adentro y el eje hueco sobre rodamientos 6806.
//
// ARQUITECTURA:
//   - q5 Muñeca (Pitch): Servo MG996R en un brazo de la horquilla.
//       El eje de pitch es el eje X. Del otro lado hay un pivote
//       libre con rodamiento 608, así el eje del servo no carga
//       el peso en voladizo.
//   - q6 Torsión (Roll): Servo MG996R dentro de la caja + correa
//       GT2 hacia el eje hueco. El eje de torsión es el eje Z.
//   - Succión: la manguera de aire va por fuera del brazo. Pasa por la
//       guía del borde de la placa, se mete por debajo de la placa y baja
//       por el eje hueco hasta un racor comercial (rosca macho 1/4 a
//       manguera) que va trabado en el hexágono del fondo del eje. Su rosca
//       asoma por abajo y la ventosa (JXD SAB40 o SAF-40, rosca hembra G1/4)
//       se enrosca ahí. El fondo del eje queda apretado entre el racor y la
//       ventosa. El aire no toca el plástico impreso y nada pasa por
//       dentro del prismático.
//   - Los ejes de pitch y torsión se cortan sobre el eje del prismático.
//
// POSICIÓN NATURAL (pitch = 0):
//   La ventosa queda perpendicular al prismático (pitch_natural = 90).
//   Con el brazo horizontal apunta hacia abajo, y el peso de la naranja
//   pasa por el eje de pitch: el servo de muñeca no hace fuerza.
//   Con pitch = -90 la ventosa queda alineada con el prismático, hacia
//   adelante. El rango útil va de pitch_min a pitch_max.
//
// MONTAJE AL PRISMÁTICO:
//   La horquilla (soporte en U) es la pieza que va en la punta del
//   prismático. Su placa es maciza: se apoya plana contra el extremo de
//   E3 (la pieza que sale y entra del brazo) y se sujeta con 4 tornillos.
//   Con brida_agujeros = 0 la placa sale sin ningún agujero, para
//   perforarla a medida. El eje de pitch (X) tiene que quedar horizontal,
//   paralelo al eje del hombro, y la guía de la manguera hacia arriba.
//   Con montado = 1 el ensamble se dibuja así, como va en el robot.
//
// ORIGEN: eje de torsión, Z = 0 en la cara de abajo de la caja.
//         Las piezas se modelan con la ventosa hacia -Z y la placa de la
//         horquilla arriba (+Z, hacia el prismático).
//
// PIEZAS (variable PIEZA):
//   0 = Ensamble con referencias (F5)
//   1 = Caja               5 = Anillo de retención del eje
//   2 = Tapa               6 = Polea del servo
//   3 = Porta-servo        7 = Horquilla de muñeca
//   4 = Eje hueco + polea  8 = Separador del pivote libre
//   Las piezas 1-8 salen ya orientadas para imprimir (F6 y exportar STL).
//
// TORNILLERÍA:
//   M3 x 10 (rosca en el plástico) : pestañas de los 2 servos (8),
//                                    porta-servo al piso (2)
//   M3 x 12 (rosca en el plástico) : tapa (4)
//   M3 x 8                         : anillo al eje (2, no usar más largos)
//   Horn de plástico (HORN = 1)    : tornillos chicos del kit del servo, por los
//                                    agujeros del horn, a la polea y a la caja
//   Horn metálico (HORN = 2)       : M3 x 8, polea al horn (4) y horn a la caja (4)
//   M5 + tuerca                    : brida de la horquilla a E3 (4), largo según E3
//   M8 x 25 + tuerca               : pivote libre (1)
//   Rodamientos                    : 6806 (30x42x7) x2, 608 (8x22x7) x1
//   Correa                         : GT2 cerrada de 6 mm, ver correa_dientes
//   Manguera                       : flexible (silicona o PU), ver manguera_d
//   Racor                          : recto, rosca macho 1/4 (G o R) a manguera,
//                                    tipo PC6-02. Medirlo: ver parámetros racor_*
//   Ventosa                        : JXD SAB40 (fuelle) o SAF-40 (plana), G1/4 hembra
//
// ORDEN DE ARMADO (importa: después no hay acceso):
//   1. Con la caja vacía: atornillar el horn de muñeca a la pared +X y
//      poner la tuerca M8 en su alojamiento de la pared -X.
//   2. Rodamientos 6806, tapa, eje hueco desde arriba y anillo desde abajo.
//      El eje deja atrapada la tuerca M8.
//   3. Encajar la manguera en el racor, bajarlo por dentro del eje hasta
//      que el hexágono calce en el fondo y enroscar la ventosa desde abajo
//      sobre la rosca que asoma (teflón en la rosca).
//   4. Porta-servo con el servo de torsión, polea, correa y tensar.
//   5. Montar el servo de muñeca en la horquilla y la horquilla en E3.
//      Llevar ese servo al centro de su recorrido.
//   6. Meter la caja entre los brazos, corrida hacia el pivote libre, y
//      deslizarla sobre el estriado del servo de muñeca con la caja en la
//      posición natural. Así el centro del servo coincide con pitch = 0.
//   7. Separador, 608 y tornillo M8. La caja queda atrapada entre los dos
//      brazos, así que el tornillo central de ese horn no hace falta.
//   8. Encajar la manguera en la guía de la placa, dejando una curva
//      holgada entre la guía y el eje, y llevarla por fuera del brazo.
//
// UNIDADES: milímetros (mm)
// MATERIAL: PETG, >= 4 perímetros. Eje hueco y poleas al 100 %.
// ============================================================

PIEZA = 0;

// Solo para el ensamble (PIEZA = 0)
pitch   = 0;  // q5, grados desde la posición natural, entre pitch_min y pitch_max
giro    = 0;  // q6, grados
montado = 0;  // 0 = ejes de diseño (prismático vertical), 1 = como va en el robot (prismático horizontal)

// Cantidad de lados con que se dibuja cada círculo. Más alto = curvas más
// lisas y render más lento. No cambia las medidas.
$fn = 72;

/* ==================== PARÁMETROS GLOBALES ==================== */

// --- Construcción ---
pared = 4;           // Grosor de pared estructural
tol   = 0.3;         // Tolerancia de impresión FDM
piloto_m3 = 2.6;     // Agujero para M3 autorroscante en PETG
paso_m3   = 3.4;     // Agujero pasante para M3

// --- Rodamiento del eje hueco: 6806 (McMaster 5972K217) ---
rod_id      = 30;
rod_od      = 42;
rod_h       = 7;
rod_ajuste  = 0.15;  // Holgura diametral del alojamiento (ajustar con pieza de prueba)
rod_labio   = 1.5;   // Tope que apoya solo la pista exterior
rod_labio_d = 38.4;  // Paso libre sobre la pista interior
rod_pared   = 4;     // Pared del alojamiento
rod_margen  = 6;     // Entre el alojamiento y la pared -X (lugar para la tuerca del pivote)

// --- Servo (medidas del plano del fabricante: 40 x 20 x 36.5) ---
servo_l       = 40.0;  // Largo del cuerpo
servo_a       = 20.0;  // Ancho del cuerpo
servo_tab_l   = 52.8;  // Largo total con pestañas
servo_tab_e   = 2.5;   // Grosor de las pestañas (el plano no lo acota: medir)
servo_h_tab   = 26.5;  // Base -> cara de abajo de las pestañas
servo_h_caja  = 36.5;  // Base -> tope de la carcasa
servo_h_estria = 38.2; // Base -> donde apoya el horn. Sale del plano: 26.5 + 16.6 - alto del horn de plástico
servo_eje_off = 10.0;  // Centro del cuerpo -> eje de salida (el plano no lo acota: medir)
servo_ag_l    = 47.8;  // Entre agujeros de pestañas, a lo largo
servo_ag_a    = 10.2;  // Entre agujeros de pestañas, a lo ancho

// --- Horn: disco que va sobre el estriado del servo ---
// Define la altura de las poleas y la separación de la horquilla: elegir bien y medir.
HORN = 1;                             // 1 = disco de plástico que trae el servo (el del plano)
                                      // 2 = disco metálico de 20 mm con 4 roscas M3
horn_d        = (HORN == 1) ? 24  : 20;   // Diámetro del disco
horn_e        = (HORN == 1) ? 2.2 : 2;    // Grosor del disco
horn_h        = (HORN == 1) ? 4.9 : 7;    // Alto total (disco + cubo)
horn_agujeros = (HORN == 1) ? 0   : 4;    // 0 = sin agujeros: se perfora usando el horn como plantilla
horn_pcd      = 14;                       // Círculo de las roscas M3 del disco metálico
horn_centro   = 9.5;                      // Paso para el cubo y el tornillo central

// --- Transmisión GT2 de la torsión ---
// Relación = dientes_servo / dientes_eje. Con 66/66 el eje gira lo mismo
// que el servo (~180 grados). El eje de 30 mm obliga a >= 60 dientes en el eje.
gt2_paso       = 2;
dientes_eje    = 66;
dientes_servo  = 66;
correa_dientes = 116;  // Correa cerrada de 232 mm. Cambiarla recalcula la distancia entre ejes
correa_ancho   = 6;
holgura_diente = 1.13; // Ensanche del hueco del diente para impresión FDM
polea_diente_h = correa_ancho + 1;
polea_brida_r  = 1.5;  // Cuánto sobresale la brida sobre los dientes
polea_brida_h  = 0.8;
polea_cubo_h   = 4;    // Cubo del eje que apoya en la pista interior del 6806 de arriba
ajuste_correa  = 3;    // Recorrido del porta-servo para tensar (+/-)

// --- Eje hueco ---
eje_ajuste    = 0.1;   // Holgura diametral contra la pista interior
eje_cubo_d    = 35;    // Menor que la pista exterior, para no rozarla
eje_nariz     = 10;    // Cuánto baja el eje por debajo de la caja
eje_paso      = 20.5;  // Paso interior: deja bajar el racor y la manguera desde arriba
eje_fondo     = 3.5;   // Fondo del eje: queda apretado entre el hexágono del racor y la ventosa

// --- Ventosa JXD: SAB40 (fuelle) o SAF-40 (plana), rosca hembra G1/4 ---
ventosa_largo   = 28.8;  // Alto total. SAB40: 28.8, SAF-40: 21
ventosa_r       = 22.5;  // Radio del labio. SAB40: 45/2, SAF-40: 40/2
ventosa_rosca_l = 7;     // Profundidad de la rosca hembra (plano de la SAF-40)

// --- Racor recto: rosca macho 1/4 a manguera (tipo PC6-02) ---
// Medidas típicas: verificar con el racor en la mano antes de imprimir.
racor_rosca_d = 13.2;   // Diámetro exterior de la rosca 1/4 (G o R)
racor_rosca_l = 8;      // Largo de la rosca macho
racor_af      = 14;     // Hexágono, entre caras
racor_hex_h   = 6;      // Alto del hexágono
racor_alto    = 26;     // Alto total sin la rosca (hexágono + cuerpo + boca)
racor_cuerpo_d = 12.5;  // Diámetro del cuerpo sobre el hexágono

// --- Manguera ---
manguera_d      = 6;     // Diámetro exterior de la manguera
manguera_radio  = 15;    // Radio mínimo de curvatura (PU 6x4: 15, silicona: 10). Sube la placa

// --- Anillo de retención ---
anillo_h = 8;
anillo_d = 40;

// --- Porta-servo ---
porta_pared = 3;

// --- Caja ---
piso_e       = 4;
tapa_placa   = 4;
servo_holgura_lat = 2.5;  // Entre las pestañas del servo y las paredes
hueco_horn   = 9;         // Entre el porta-servo y la pared +X (cabezas de los tornillos del horn)
horn_rebaje  = 1.5;       // Asiento del disco del horn en la pared +X
horn_refuerzo = 3;        // Engrosado interior de la pared bajo el horn

// --- Pivote libre: 608 + tornillo M8 ---
piv_rod_od   = 22;
piv_rod_h    = 7;
piv_tornillo = 8.3;
piv_tuerca_af = 13.2;   // Entre caras de la tuerca M8 + holgura
piv_tuerca_h  = 5;      // Profundidad del alojamiento de la tuerca
piv_refuerzo  = 6;      // Engrosado interior de la pared -X
holgura_libre = 5;      // Juego de armado: tiene que superar el largo del estriado

// --- Horquilla de muñeca ---
pitch_natural = 90;     // Ángulo entre la ventosa y el prismático en la posición natural
pitch_min     = -90;    // Rango de pitch desde la natural (-90 = ventosa hacia adelante)
pitch_max     = 25;     // Más allá la ventosa se acerca a la placa: el despeje se recalcula
holgura_giro  = 3;
placa_e       = 8;
brazo_e       = 7;      // Brazo del servo
brazo_ancho   = 40;
cartela       = 12;     // Cuña entre brazo y placa
brida_lado    = 80;     // Brida de E3: 80 x 80
brida_pcd     = 57;     // E3 tiene 4 agujeros sobre un círculo de 57 mm
brida_giro    = 0;      // Girar el patrón si cambia E3
brida_m       = 5.4;    // M5. Los agujeros de E3 hoy miden 10.2: ajustar una de las dos piezas
brida_tuerca_af = 8.3;
brida_tuerca_h  = 4.2;
brida_agujeros = 1;     // 1 = 4 agujeros con tuerca para E3, 0 = placa sin ningún agujero
guia_manguera = 1;      // 1 = clip para la manguera en el borde -Y de la placa
guia_pared    = 2.5;
guia_ancho    = 8;

// --- Extremo del prismático (E3), solo para dibujarlo en el ensamble ---
e3_brida_e = 26;            // Grosor de la brida de E3
e3_tubo    = [45.4, 53.4];  // Sección exterior del tubo de E3

/* ==================== VALORES DERIVADOS ==================== */

function d_paso(z) = z * gt2_paso / PI;       // Diámetro primitivo
function d_ext(z)  = d_paso(z) - 0.508;       // Diámetro exterior de la polea GT2

// Distancia entre ejes que deja la correa justa
correa_l  = correa_dientes * gt2_paso;
_b        = correa_l - PI * (d_paso(dientes_eje) + d_paso(dientes_servo)) / 2;
dist_ejes = (_b + sqrt(_b * _b - 2 * pow(d_paso(dientes_eje) - d_paso(dientes_servo), 2))) / 4;

// El cuerpo del servo queda centrado en Y; su eje de salida cae en (servo_x, -servo_eje_off)
servo_x = sqrt(dist_ejes * dist_ejes - servo_eje_off * servo_eje_off);

// Alto del tope en cono (45 grados) del alojamiento de abajo
rod_cono = (rod_od + rod_ajuste - rod_labio_d) / 2;

// Base del servo -> cara exterior del disco del horn ya colocado
servo_h_horn = servo_h_estria + horn_h;

// Alturas: todo sale de apoyar el servo de torsión casi en el piso
polea_h    = 2 * (polea_brida_h + polea_brida_r) + polea_diente_h;
z_servo    = piso_e + 0.5;
z_tab      = z_servo + servo_h_tab;
z_polea    = z_servo + servo_h_horn - horn_e;   // Base de las dos poleas
z_tapa_top = z_polea - polea_cubo_h;
caja_h     = z_tapa_top - rod_h - rod_labio;    // Tope de las paredes
z_eje_top  = z_polea + polea_h;

// Altura del eje de pitch: lo más alto que deja el refuerzo del horn en la pared
piv_z      = caja_h - (horn_d + 8) / 2 - 0.5;

// Planta de la caja
x_in0 = -(rod_od / 2 + rod_margen);
x_in1 = servo_x + servo_a / 2 + tol + porta_pared + ajuste_correa + hueco_horn;
w_in  = servo_tab_l + 2 * servo_holgura_lat;
x0    = x_in0 - pared;
x1    = x_in1 + pared;
w_out = w_in + 2 * pared;

// La tapa cubre solo la zona del eje; la del servo queda abierta
x_tapa1 = servo_x - servo_a / 2 - tol - porta_pared - ajuste_correa - 1;
tapa_tornillos = [for (sy = [-1, 1]) each [[x_in0 + 4, sy * (w_in / 2 - 4)],
                                           [x_tapa1 - 5, sy * (w_in / 2 - 4)]]];

// Racor: su hexágono apoya en el fondo del eje y la rosca asoma por abajo
z_fondo       = -eje_nariz + eje_fondo;          // Cara donde apoya el hexágono del racor
rosca_ventosa = racor_rosca_l - eje_fondo;       // Lo que entra la rosca en la ventosa
ventosa_paso  = racor_rosca_d + 0.8;             // Paso de la rosca por el fondo del eje

// Porta-servo
porta_lx  = servo_a + 2 * tol + 2 * porta_pared;
porta_ly  = servo_tab_l + 2;
porta_h   = z_tab - piso_e;
porta_fij = (porta_ly + servo_l + 2 * tol) / 4;   // Y de los tornillos al piso (centro de cada taco)

// Horquilla: posiciones en X de los brazos
brazo_libre_e = piv_rod_h + 1;
xl1 = x0 - holgura_libre;                 // Cara interior del brazo libre
xl0 = xl1 - brazo_libre_e;
xs1 = x1 - horn_rebaje + (servo_h_horn - servo_h_tab - servo_tab_e);  // Cara exterior del brazo del servo
xs0 = xs1 - brazo_e;

// Giro real de la caja respecto de la horquilla (0 = ventosa alineada con el prismático)
ang_min = pitch_natural + pitch_min;
ang_max = pitch_natural + pitch_max;

// Despeje entre el eje de pitch y la placa: el punto que más se acerca a la placa
// en todo el rango. Puntos [y, z] medidos desde el eje de pitch.
brida_polea_r = max(d_ext(dientes_eje), d_ext(dientes_servo)) / 2 + polea_brida_r;
_z_labio = -piv_z - eje_nariz - ventosa_largo;
_puntos = [[ brida_polea_r, z_eje_top - piv_z],
           [-brida_polea_r, z_eje_top - piv_z],
           [-servo_eje_off - brida_polea_r, z_eje_top - piv_z],
           [-servo_eje_off + brida_polea_r, z_eje_top - piv_z],
           [ w_out / 2, z_tapa_top - piv_z],
           [-w_out / 2, z_tapa_top - piv_z],
           [ w_out / 2, -piv_z],
           [-w_out / 2, -piv_z],
           [ anillo_d / 2, -piv_z - anillo_h],
           [-anillo_d / 2, -piv_z - anillo_h],
           [ ventosa_r, _z_labio],
           [-ventosa_r, _z_labio]];
_angulos = concat([for (a = [ang_min : 2 : ang_max]) a], [ang_max]);
_despeje_giro = max([for (p = _puntos, a = _angulos) p[0] * sin(a) + p[1] * cos(a)]) + holgura_giro;
// Cuando la boca del eje mira hacia la placa, la manguera dobla entre las dos:
// necesita lugar para esa curva
_despeje_manguera = (ang_min < 45) ? z_eje_top - piv_z + manguera_radio + manguera_d / 2 + 1 : 0;
despeje = max(_despeje_giro, _despeje_manguera);
z_placa = piv_z + despeje;                // Cara de abajo de la placa

// Guía de la manguera: centro del clip, medido desde el borde -Y de la placa
guia_dist = manguera_radio + 3;

echo(str("Distancia entre ejes = ", dist_ejes, " mm"));
echo(str("Caja (X x Y x Z) = ", x1 - x0, " x ", w_out, " x ", caja_h, " mm"));
echo(str("Giro del eje por cada 180 grados de servo = ", 180 * dientes_servo / dientes_eje, " grados"));
echo(str("Cara de E3 al eje de pitch = ", despeje + placa_e, " mm"));
echo(str("Eje de pitch al labio de la ventosa = ", -_z_labio, " mm"));
echo(str("Rosca del racor que entra en la ventosa = ", rosca_ventosa, " mm"));
if (rosca_ventosa > ventosa_rosca_l)
    echo("ATENCION: la rosca del racor toca fondo en la ventosa antes de apretar. Subir eje_fondo.");
if (rosca_ventosa < 3)
    echo("ATENCION: entra muy poca rosca en la ventosa. Bajar eje_fondo o usar un racor de rosca más larga.");

/* ==================== MÓDULOS AUXILIARES ==================== */

// Hueco de un diente GT2 de 2 mm, visto desde afuera de la polea.
// Perfil aproximado (según el script paramétrico de poleas de droftarts).
module perfil_gt2() {
    polygon([[0.747183, -0.5], [0.747183, 0], [0.647876, 0.037218], [0.598311, 0.130528],
             [0.578556, 0.238423], [0.547158, 0.343077], [0.504649, 0.443762], [0.451556, 0.53975],
             [0.358229, 0.636924], [0.2484, 0.707276], [0.127259, 0.750044], [0, 0.76447],
             [-0.127259, 0.750044], [-0.2484, 0.707276], [-0.358229, 0.636924], [-0.451556, 0.53975],
             [-0.504797, 0.443762], [-0.547291, 0.343077], [-0.578605, 0.238423], [-0.598311, 0.130528],
             [-0.648009, 0.037218], [-0.747183, 0], [-0.747183, -0.5]]);
}

// Polea GT2 con las dos bridas en cono a 45 grados (imprime sin soportes)
module polea_gt2(dientes) {
    de = d_ext(dientes);
    rb = de / 2 + polea_brida_r;
    z1 = polea_brida_h;
    z2 = z1 + polea_brida_r;
    z3 = z2 + polea_diente_h;
    z4 = z3 + polea_brida_r;
    r_diente = sqrt(pow(de / 2, 2) - pow(0.747183 * holgura_diente, 2));
    difference() {
        union() {
            cylinder(r = rb, h = z1);
            translate([0, 0, z1]) cylinder(r1 = rb, r2 = de / 2, h = polea_brida_r);
            translate([0, 0, z2]) cylinder(d = de, h = polea_diente_h);
            translate([0, 0, z3]) cylinder(r1 = de / 2, r2 = rb, h = polea_brida_r);
            translate([0, 0, z4]) cylinder(r = rb, h = polea_brida_h);
        }
        for (i = [0 : dientes - 1])
            rotate([0, 0, i * 360 / dientes])
                translate([0, -r_diente, z2 - 0.01])
                    linear_extrude(height = polea_diente_h + 0.02)
                        scale([holgura_diente, 1]) perfil_gt2();
    }
}

// Alojamiento hexagonal para tuerca, eje en Z
module hueco_tuerca(af, h) {
    cylinder(d = af / cos(30), h = h, $fn = 6);
}

// Ranura para tensar, eje en Z, alargada en X
module ranura(d, largo, h) {
    hull() for (sx = [-1, 1]) translate([sx * largo / 2, 0, 0]) cylinder(d = d, h = h);
}

/* ==================== PIEZA 1: CAJA ==================== */

module caja() {
    difference() {
        union() {
            // [A] Piso
            translate([x0, -w_out / 2, 0]) cube([x1 - x0, w_out, piso_e]);

            // [B] Paredes
            difference() {
                translate([x0, -w_out / 2, 0]) cube([x1 - x0, w_out, caja_h]);
                translate([x_in0, -w_in / 2, piso_e]) cube([x_in1 - x_in0, w_in, caja_h]);
            }

            // [C] Alojamiento del 6806 de abajo
            cylinder(d = rod_od + 2 * rod_pared, h = rod_h + rod_cono + 1);

            // [D] Torres donde atornilla la tapa
            for (p = tapa_tornillos)
                translate([p[0] - 4, p[1] - 4, 0]) cube([8, 8, caja_h]);

            // [E] Refuerzo interior del pivote libre (aloja la tuerca M8).
            //     Baja hasta el piso para no dejar voladizo al imprimir.
            hull() {
                translate([x_in0 - 0.01, 0, piv_z]) rotate([0, 90, 0])
                    cylinder(d = 22, h = piv_refuerzo);
                translate([x_in0 - 0.01, -11, 0]) cube([piv_refuerzo, 22, 1]);
            }

            // [F] Refuerzo interior bajo el horn de muñeca, también hasta el piso
            hull() {
                translate([x_in1 + 0.01, 0, piv_z]) rotate([0, -90, 0])
                    cylinder(d = horn_d + 8, h = horn_refuerzo);
                translate([x_in1 - horn_refuerzo, -(horn_d + 8) / 2, 0])
                    cube([horn_refuerzo + 0.01, horn_d + 8, 1]);
            }
        }

        // [G] 6806 de abajo: entra desde afuera. El tope queda arriba y en cono
        //     a 45 grados para imprimir sin soportes.
        translate([0, 0, -1]) cylinder(d = rod_od + rod_ajuste, h = rod_h + 1);
        translate([0, 0, rod_h - 0.01])
            cylinder(d1 = rod_od + rod_ajuste, d2 = rod_labio_d, h = rod_cono);
        translate([0, 0, rod_h]) cylinder(d = rod_labio_d, h = rod_cono + 2);

        // [H] Ranuras del porta-servo (tensado de la correa)
        for (sy = [-1, 1])
            translate([servo_x, sy * porta_fij, -1])
                ranura(paso_m3, 2 * ajuste_correa, piso_e + 2);

        // [I] Pilotos de la tapa
        for (p = tapa_tornillos)
            translate([p[0], p[1], caja_h - 12]) cylinder(d = piloto_m3, h = 13);

        // [J] Pivote libre: paso del M8 y tuerca atrapada por dentro
        translate([x0 - 1, 0, piv_z]) rotate([0, 90, 0])
            cylinder(d = piv_tornillo, h = pared + piv_refuerzo + 2);
        translate([x_in0 + piv_refuerzo - piv_tuerca_h, 0, piv_z]) rotate([0, 90, 0])
            hueco_tuerca(piv_tuerca_af, piv_tuerca_h + 1);

        // [K] Horn de muñeca: asiento del disco, paso central y tornillos.
        //     Con el disco de plástico no hay agujeros: se marcan con el horn puesto.
        translate([x1 - horn_rebaje, 0, piv_z]) rotate([0, 90, 0])
            cylinder(d = horn_d + 0.6, h = horn_rebaje + 1);
        translate([x_in1 - horn_refuerzo - 1, 0, piv_z]) rotate([0, 90, 0]) {
            cylinder(d = horn_centro, h = pared + horn_refuerzo + 2);
            if (horn_agujeros > 0)
                for (i = [0 : horn_agujeros - 1])
                    rotate([0, 0, 45 + i * 360 / horn_agujeros]) translate([horn_pcd / 2, 0, 0])
                        cylinder(d = paso_m3, h = pared + horn_refuerzo + 2);
        }

        // [L] Salida del cable del servo de torsión
        translate([servo_x - 6, w_in / 2 - 1, caja_h - 7]) cube([12, pared + 2, 8]);
    }
}

/* ==================== PIEZA 2: TAPA ==================== */

module tapa() {
    difference() {
        union() {
            translate([x0, -w_out / 2, caja_h]) cube([x_tapa1 - x0, w_out, tapa_placa]);
            translate([0, 0, caja_h]) cylinder(d = rod_od + 2 * rod_pared, h = rod_h + rod_labio);
        }
        // 6806 de arriba: entra desde arriba, el tope queda abajo
        translate([0, 0, caja_h + rod_labio]) cylinder(d = rod_od + rod_ajuste, h = rod_h + 1);
        translate([0, 0, caja_h - 1]) cylinder(d = rod_labio_d, h = rod_labio + 2);
        for (p = tapa_tornillos)
            translate([p[0], p[1], caja_h - 1]) cylinder(d = paso_m3, h = tapa_placa + 2);
    }
}

/* ==================== PIEZA 3: PORTA-SERVO ==================== */
// Origen local: centro del cuerpo del servo, sobre el piso de la caja.

module porta_servo() {
    difference() {
        translate([-porta_lx / 2, -porta_ly / 2, 0]) cube([porta_lx, porta_ly, porta_h]);

        // Bolsillo del cuerpo del servo
        translate([-(servo_a / 2 + tol), -(servo_l / 2 + tol), -1])
            cube([servo_a + 2 * tol, servo_l + 2 * tol, porta_h + 2]);

        // Pilotos de las pestañas
        for (sx = [-1, 1], sy = [-1, 1])
            translate([sx * servo_ag_a / 2, sy * servo_ag_l / 2, porta_h - 12])
                cylinder(d = piloto_m3, h = 13);

        // Pilotos de la fijación al piso (tornillos desde abajo)
        for (sy = [-1, 1])
            translate([0, sy * porta_fij, -1]) cylinder(d = piloto_m3, h = 11);

        // Ventanas bajas: paso del cable por cualquiera de los dos lados
        translate([-porta_lx / 2 - 1, -(servo_l / 2 - 5), -1])
            cube([porta_lx + 2, servo_l - 10, 10]);
    }
}

/* ==================== PIEZA 4: EJE HUECO + POLEA ==================== */

module eje_hueco() {
    d = rod_id - eje_ajuste;
    hex_d = (racor_af + 0.6) / cos(30);         // Hexágono del racor, con holgura
    hex_h = racor_hex_h + 0.5;
    cono  = (eje_paso - racor_af - 0.6) / 2;    // Transición a 45 grados (imprime sin soportes)
    difference() {
        union() {
            // El eje y el cubo entran 0.1 mm en la polea: así se funden en un solo sólido
            translate([0, 0, -eje_nariz]) cylinder(d = d, h = z_polea + eje_nariz + 0.1);
            translate([0, 0, z_tapa_top]) cylinder(d = eje_cubo_d, h = polea_cubo_h + 0.1);
            translate([0, 0, z_polea]) polea_gt2(dientes_eje);
        }
        // Paso de la rosca del racor por el fondo
        translate([0, 0, -eje_nariz - 1]) cylinder(d = ventosa_paso, h = eje_fondo + 2);
        // Alojamiento hexagonal: el racor no gira al enroscar la ventosa ni al torcer.
        // Las caras quedan hacia X, donde entran los tornillos del anillo.
        translate([0, 0, z_fondo]) rotate([0, 0, 30])
            cylinder(d = hex_d, h = hex_h + 0.01, $fn = 6);
        hull() {
            translate([0, 0, z_fondo + hex_h]) rotate([0, 0, 30])
                cylinder(d = hex_d, h = 0.01, $fn = 6);
            translate([0, 0, z_fondo + hex_h + cono]) cylinder(d = eje_paso, h = 0.01);
        }
        // Paso del racor y la manguera hasta arriba
        translate([0, 0, z_fondo + hex_h + cono - 0.01])
            cylinder(d = eje_paso, h = z_eje_top - z_fondo);
        // Pilotos ciegos de los 2 tornillos del anillo (no llegan al paso interior)
        for (a = [0, 180])
            rotate([0, 0, a]) translate([d / 2 - 4, 0, -anillo_h / 2]) rotate([0, 90, 0])
                cylinder(d = piloto_m3, h = 5);
    }
}

/* ==================== PIEZA 5: ANILLO DE RETENCIÓN ==================== */
// Va por debajo de la caja. Su resalte toca solo la pista interior del 6806.

module anillo() {
    difference() {
        union() {
            translate([0, 0, -anillo_h]) cylinder(d = anillo_d, h = anillo_h - 0.6);
            translate([0, 0, -1]) cylinder(d = eje_cubo_d - 1, h = 1);
        }
        translate([0, 0, -anillo_h - 1]) cylinder(d = rod_id + 0.2, h = anillo_h + 2);
        for (a = [0, 180])
            rotate([0, 0, a]) translate([rod_id / 2 - 1, 0, -anillo_h / 2]) rotate([0, 90, 0])
                cylinder(d = paso_m3, h = anillo_d);
    }
}

/* ==================== PIEZA 6: POLEA DEL SERVO ==================== */
// Origen local: eje del servo, Z = 0 en la base de la polea.
// Se apoya sobre el disco del horn y se atornilla a sus 4 agujeros M3.

module polea_servo() {
    difference() {
        polea_gt2(dientes_servo);
        translate([0, 0, -0.01]) cylinder(d = horn_d + 0.6, h = horn_e + 0.01);
        // Acceso al tornillo central del servo
        translate([0, 0, -1]) cylinder(d = 7, h = polea_h + 2);
        // Tornillos al disco metálico. Con el de plástico se atornilla desde
        // abajo, por los agujeros del horn, antes de calzarlo en el servo.
        if (horn_agujeros > 0)
            for (i = [0 : horn_agujeros - 1])
                rotate([0, 0, 45 + i * 360 / horn_agujeros]) translate([horn_pcd / 2, 0, 0]) {
                    translate([0, 0, -1]) cylinder(d = paso_m3, h = polea_h + 2);
                    translate([0, 0, polea_h - 3.2]) cylinder(d = 6.2, h = 4);
                }
    }
}

/* ==================== PIEZA 7: HORQUILLA DE MUÑECA ==================== */

module horquilla() {
    servo_cz = piv_z + servo_eje_off;   // Centro del cuerpo del servo de muñeca

    difference() {
        union() {
            // [A] Placa: brida de E3 unida a la franja que llega a los dos brazos
            translate([0, 0, z_placa]) linear_extrude(height = placa_e)
                hull() {
                    square([brida_lado, brida_lado], center = true);
                    translate([xl0 - cartela, -brazo_ancho / 2])
                        square([xs1 - xl0 + 2 * cartela, brazo_ancho]);
                }

            // [B] Brazo libre (rodamiento 608)
            hull() {
                translate([xl0, 0, piv_z]) rotate([0, 90, 0])
                    cylinder(d = brazo_ancho, h = brazo_libre_e);
                translate([xl0, -brazo_ancho / 2, z_placa - 1])
                    cube([brazo_libre_e, brazo_ancho, 1.1]);
            }

            // [C] Brazo del servo de muñeca
            hull() {
                translate([xs0, 0, piv_z]) rotate([0, 90, 0])
                    cylinder(d = brazo_ancho, h = brazo_e);
                translate([xs0, -brazo_ancho / 2, z_placa - 1])
                    cube([brazo_e, brazo_ancho, 1.1]);
            }

            // [D] Cuñas por el lado de afuera de cada brazo
            for (c = [[xl0, -1], [xs1, 1]])
                translate([c[0], brazo_ancho / 2, z_placa]) rotate([90, 0, 0])
                    linear_extrude(height = brazo_ancho)
                        polygon([[0, 0.01], [c[1] * cartela, 0.01], [0, -cartela]]);

            // [J] Guía de la manguera: clip abierto, fuera del borde -Y de la placa
            if (guia_manguera == 1)
                translate([0, -brida_lado / 2, z_placa]) linear_extrude(height = placa_e)
                    difference() {
                        union() {
                            translate([-guia_ancho / 2, -guia_dist]) square([guia_ancho, guia_dist + 0.01]);
                            translate([0, -guia_dist]) circle(d = manguera_d + 2 * guia_pared);
                        }
                        translate([0, -guia_dist]) circle(d = manguera_d + 0.4);
                        // Boca del clip: la manguera entra a presión desde afuera
                        translate([-(manguera_d - 1.5) / 2, -guia_dist - manguera_d - guia_pared])
                            square([manguera_d - 1.5, manguera_d + guia_pared]);
                    }
        }

        // [E] 608: entra desde la cara interior; el tope queda afuera
        translate([xl1 - piv_rod_h, 0, piv_z]) rotate([0, 90, 0])
            cylinder(d = piv_rod_od + rod_ajuste, h = piv_rod_h + 1);
        translate([xl0 - 1, 0, piv_z]) rotate([0, 90, 0])
            cylinder(d = piv_rod_od - 4, h = brazo_libre_e + 2);

        // [F] Paso del cuerpo del servo de muñeca
        translate([xs0 - 1, -(servo_a / 2 + tol), servo_cz - servo_l / 2 - tol])
            cube([brazo_e + 2, servo_a + 2 * tol, servo_l + 2 * tol]);

        // [G] Pilotos de las pestañas del servo de muñeca
        for (sy = [-1, 1], sz = [-1, 1])
            translate([xs0 - 1, sy * servo_ag_a / 2, servo_cz + sz * servo_ag_l / 2])
                rotate([0, 90, 0]) cylinder(d = piloto_m3, h = brazo_e + 2);

        // [H] Unión a E3: tornillos y tuercas atrapadas por abajo (no sobresalen).
        //     La placa no tiene ningún otro agujero.
        if (brida_agujeros == 1)
            for (a = [0 : 90 : 270])
                rotate([0, 0, a + brida_giro]) translate([brida_pcd / 2, 0, z_placa]) {
                    translate([0, 0, -1]) cylinder(d = brida_m, h = placa_e + 2);
                    translate([0, 0, -1]) hueco_tuerca(brida_tuerca_af, brida_tuerca_h + 1);
                }
    }
}

/* ==================== PIEZA 8: SEPARADOR DEL PIVOTE LIBRE ==================== */
// Llena el juego de armado entre la caja y el 608. Toca solo la pista interior.

module separador() {
    difference() {
        cylinder(d = 12, h = holgura_libre - 0.3);
        translate([0, 0, -1]) cylinder(d = piv_tornillo, h = holgura_libre + 2);
    }
}

/* ==================== REFERENCIAS (NO SE IMPRIMEN) ==================== */

// Origen local: eje de salida, Z = 0 en la base. El cuerpo se extiende hacia +Y.
module servo_ref() {
    translate([-servo_a / 2, servo_eje_off - servo_l / 2, 0]) cube([servo_a, servo_l, servo_h_caja]);
    translate([-servo_a / 2, servo_eje_off - servo_tab_l / 2, servo_h_tab])
        cube([servo_a, servo_tab_l, servo_tab_e]);
    cylinder(d = 13.3, h = servo_h_caja + 0.8);
    cylinder(d = 5.9, h = servo_h_horn - horn_e + 0.5);
    translate([0, 0, servo_h_estria]) cylinder(d = 9, h = horn_h);
    translate([0, 0, servo_h_horn - horn_e]) cylinder(d = horn_d, h = horn_e);
}

module rodamiento_ref(od, id, h) {
    difference() {
        cylinder(d = od, h = h);
        translate([0, 0, -1]) cylinder(d = id, h = h + 2);
    }
}

// Ventosa JXD SAB40 (perfil aproximado del catálogo). Origen: cara de arriba del
// inserto metálico con rosca hembra G1/4, que apoya contra el fondo del eje. Cuelga hacia -Z.
module ventosa_ref() {
    rotate_extrude()
        polygon([[6.6, 0], [9.5, 0], [9.5, -ventosa_rosca_l], [12, -ventosa_rosca_l], [16, -11],
                 [13, -15], [17, -18], [ventosa_r, -ventosa_largo + 2.3], [ventosa_r, -ventosa_largo],
                 [ventosa_r - 2.5, -ventosa_largo], [14, -20], [10, -17], [3, -12], [3, -ventosa_rosca_l],
                 [6.6, -ventosa_rosca_l]]);
}

// Racor recto: rosca macho abajo (asoma bajo el eje), hexágono y cuerpo con la boca de la manguera.
// Origen: cara de abajo del hexágono.
module racor_ref() {
    translate([0, 0, -racor_rosca_l]) cylinder(d = racor_rosca_d, h = racor_rosca_l);
    cylinder(d = racor_af / cos(30), h = racor_hex_h, $fn = 6);
    cylinder(d = racor_cuerpo_d, h = racor_alto);
}

module correa_ref() {
    r1 = d_paso(dientes_eje) / 2;
    r2 = d_paso(dientes_servo) / 2;
    translate([0, 0, z_polea + polea_h / 2 - correa_ancho / 2])
        linear_extrude(height = correa_ancho)
            difference() {
                hull() { circle(r = r1 + 0.6); translate([servo_x, -servo_eje_off]) circle(r = r2 + 0.6); }
                hull() { circle(r = r1 - 0.8); translate([servo_x, -servo_eje_off]) circle(r = r2 - 0.8); }
            }
}

/* ==================== VISUALIZACIÓN DE ENSAMBLE ==================== */

// Extremo del prismático: brida y un tramo del tubo de E3, con los 4 tornillos
// que lo unen a la placa de la horquilla
module e3_ref() {
    z0 = z_placa + placa_e;
    color("Gray", 0.35)
        translate([0, 0, z0]) {
            linear_extrude(height = e3_brida_e) square([brida_lado, brida_lado], center = true);
            translate([0, 0, e3_brida_e]) linear_extrude(height = 70) square(e3_tubo, center = true);
        }
    if (brida_agujeros == 1)
        color("Red")
            for (a = [0 : 90 : 270])
                rotate([0, 0, a + brida_giro]) translate([brida_pcd / 2, 0, z_placa + brida_tuerca_h]) {
                    cylinder(d = 5, h = placa_e - brida_tuerca_h + e3_brida_e);
                    translate([0, 0, placa_e - brida_tuerca_h + e3_brida_e]) cylinder(d = 8.5, h = 5);
                }
}

// Tramo de manguera entre dos puntos, con un punto de control (curva de Bézier)
module tramo_manguera(a, c, b) {
    pts = [for (t = [0 : 0.1 : 1.001]) (1 - t) * (1 - t) * a + 2 * (1 - t) * t * c + t * t * b];
    for (i = [0 : len(pts) - 2])
        hull() {
            translate(pts[i]) sphere(d = manguera_d, $fn = 16);
            translate(pts[i + 1]) sphere(d = manguera_d, $fn = 16);
        }
}

// Manguera por fuera del brazo: sale del eje hueco, dobla bajo la placa,
// asoma por el borde -Y, entra en la guía y sigue a lo largo de E3
module manguera_ref() {
    ang = pitch_natural + pitch;
    h   = z_eje_top - piv_z;
    u   = [0, -sin(ang), cos(ang)];                       // Dirección del eje, ya girado
    a   = [0, 0, piv_z] + h * u;                          // Boca del eje hueco
    zr  = z_placa - manguera_d / 2 - 1;                   // Altura del tramo bajo la placa
    p1  = [0, -brida_lado / 2 - 2, zr];                   // Asoma por el borde de la placa
    b   = [0, -brida_lado / 2 - guia_dist, z_placa + placa_e / 2];   // Centro del clip
    // La boca mira hacia la placa si, siguiendo el eje, se llega a la placa antes que a su borde
    bajo_placa = u[2] > 0.05 && a[1] + ((zr - a[2]) / u[2]) * u[1] > -brida_lado / 2;
    color("DeepSkyBlue") {
        if (bajo_placa) {
            tramo_manguera(a, a + ((zr - a[2]) / u[2]) * u, p1);
            tramo_manguera(p1, [0, b[1], zr], b);
        } else {
            // La boca ya mira hacia afuera: una sola curva hasta la guía
            tramo_manguera(a, a + ((b[1] - a[1]) / u[1]) * u, b);
        }
        translate(b) cylinder(d = manguera_d, h = placa_e / 2 + e3_brida_e + 70, $fn = 16);
    }
}

// Todo lo que gira con la torsión (q6)
module conjunto_eje() {
    color("Gold", 0.9) eje_hueco();
    color("Goldenrod") anillo();
    color("Silver") translate([0, 0, z_fondo]) rotate([0, 0, 30]) racor_ref();
    color("DeepSkyBlue")
        translate([0, 0, z_fondo + racor_alto - 3])
            cylinder(d = manguera_d, h = z_eje_top + 4 - (z_fondo + racor_alto - 3));
    color("RoyalBlue", 0.9) translate([0, 0, -eje_nariz]) ventosa_ref();
}

// Todo lo que gira con el pitch (q5)
module conjunto_caja() {
    color("SteelBlue", 0.85) caja();
    color("LightSteelBlue", 0.85) tapa();
    color("CadetBlue") translate([servo_x, 0, piso_e]) porta_servo();
    color("DarkOrange", 0.8) translate([servo_x, -servo_eje_off, z_servo]) servo_ref();
    color("Gold", 0.9) translate([servo_x, -servo_eje_off, z_polea]) polea_servo();
    color("DarkGreen", 0.6) correa_ref();
    color("Silver") {
        rodamiento_ref(rod_od, rod_id, rod_h);
        translate([0, 0, z_tapa_top - rod_h]) rodamiento_ref(rod_od, rod_id, rod_h);
    }
    rotate([0, 0, giro]) conjunto_eje();
}

module ensamble_completo() {
    // --- Fijo al prismático ---
    color("Tomato", 0.85) horquilla();
    color("DarkOrange", 0.8)
        translate([xs1 + servo_h_tab + servo_tab_e, 0, piv_z])
            rotate([0, -90, 0]) rotate([0, 0, -90]) servo_ref();
    color("Silver")
        translate([xl1 - piv_rod_h, 0, piv_z]) rotate([0, 90, 0])
            rodamiento_ref(piv_rod_od, 8, piv_rod_h);
    color("White")
        translate([x0, 0, piv_z]) rotate([0, -90, 0]) separador();
    e3_ref();
    manguera_ref();

    // --- Gira con el pitch ---
    translate([0, 0, piv_z]) rotate([pitch_natural + pitch, 0, 0]) translate([0, 0, -piv_z])
        conjunto_caja();
}

/* ==================== RENDERIZADO ==================== */

if (PIEZA == 0) {
    if (montado == 1)
        // E3 hacia -X (el brazo), eje de pitch sobre Y (horizontal) y guía de la
        // manguera hacia arriba. En la posición natural la ventosa apunta hacia abajo.
        multmatrix([[0, 0, -1, 0], [1, 0, 0, 0], [0, -1, 0, 0], [0, 0, 0, 1]])
            ensamble_completo();
    else
        ensamble_completo();
} else if (PIEZA == 1) {
    caja();
} else if (PIEZA == 2) {
    translate([0, 0, -caja_h]) tapa();
} else if (PIEZA == 3) {
    porta_servo();
} else if (PIEZA == 4) {
    // Polea contra la cama, el eje hacia arriba
    translate([0, 0, z_eje_top]) rotate([180, 0, 0]) eje_hueco();
} else if (PIEZA == 5) {
    translate([0, 0, anillo_h]) anillo();
} else if (PIEZA == 6) {
    // Asiento del horn hacia arriba
    translate([0, 0, polea_h]) rotate([180, 0, 0]) polea_servo();
} else if (PIEZA == 7) {
    // Cara de la brida contra la cama, los brazos hacia arriba
    translate([0, 0, z_placa + placa_e]) rotate([180, 0, 0]) horquilla();
} else if (PIEZA == 8) {
    separador();
}
