// ============================================================
// HERRAMIENTA FINAL (END-EFFECTOR) ROLL-PITCH  v3.0
// Robot Recolector de Naranjas "CitriBot RRP"
// ============================================================
//
// ARQUITECTURA:
//   - Pitch (Cabeceo): Servo MG995 montado INTEGRADO en el bracket
//       Eje de rotación en Y (inclina la herramienta adelante/atrás)
//       El cuerpo del servo está atornillado a la cara +Y del bracket.
//       Un muñón (journal) en la cara -Y da el segundo apoyo del pivote.
//       El servo horn se conecta a una horquilla del brazo robot.
//   - Roll  (Torsión): NEMA 17 + Correa GT2 + Hollow Shaft
//   - Neumática: Sleeved Routing (manguera PU por dentro del eje)
//
// v3.0: PIEZA ÚNICA
//   Motor NEMA 17 y Servo MG995 montados lado a lado en el mismo
//   soporte. No hay cuna separada ni horquilla (yoke) separada.
//   El servo horn sale por la cara +Y y se conecta al eslabón del brazo.
//
// UNIDADES: milímetros (mm)
// MATERIAL: PETG, 100% infill, >= 4 perímetros
// ============================================================
//
// Para exportar a STL: poner PIEZA = 1 y presionar F6
// Para visualizar el ensamble con referencias: poner PIEZA = 0 y F5

PIEZA = 0; // 0 = Ensamble, 1 = Pieza para imprimir

/* ==================== PARÁMETROS GLOBALES ==================== */

// --- Construcción ---
pared = 5;           // Grosor de pared estructural (mm)
tol   = 0.3;         // Tolerancia de impresión FDM

// --- Motor Paso a Paso NEMA 17 (Roll / Torsión) ---
nema_cara      = 42.3;
nema_tornillos = 31;
nema_boss_d    = 22.5;
nema_m3        = 3.3;
nema_cuerpo_h  = 40;    // Solo para visualización

// --- Servo Motor MG995 (Pitch / Cabeceo) ---
servo_largo       = 41;
servo_ancho       = 20;
servo_alto_bajo   = 27;    // Cuerpo debajo de las tabs
servo_alto_arriba = 10;    // Cuerpo arriba de las tabs (donde sale eje)
servo_tab_grosor  = 2.5;
servo_dist_holes  = 49.5;  // Entre agujeros de las tabs
servo_d_hole      = 4.5;
servo_offset_eje  = 10;    // Centro del eje desde el borde del cuerpo

// --- Rodamientos 608ZZ ---
rod_od   = 22 + tol;
rod_h    = 7;
rod_bore = 8;

// --- Eje Hueco ---
eje_od = rod_bore;
eje_id = 5;

// --- Layout ---
dist_ejes = 55;        // NEMA 17 <-> Hollow Shaft
altura_c  = 55;        // Altura del bracket

// --- Zona de Correa GT2 ---
soporte_lat_w      = 10;
polea_motriz_d     = 20;
polea_conducida_d  = 40;
polea_h            = 10;
correa_ventana_w   = 15;

// --- Pivote Integrado ---
// El eje de pitch es el eje Y del bracket.
// El servo MG995 se atornilla en la cara +Y.
// Un muñón (journal) sobresale de la cara -Y.
// Ambos a la misma posición (X, Z) para ser coaxiales.
pivot_x = dist_ejes * 0.45;  // Posición X del eje de pitch
pivot_z = 42;                  // Posición Z (servo body debe caber arriba de z=0)
journal_d     = 16;            // Diámetro del muñón
journal_largo = 12;            // Largo del muñón (cuánto sobresale de la cara -Y)
journal_pin_d = 4.3;           // M4 + tolerancia

// Cálculos derivados del servo (posición exacta del cuerpo)
// El eje del servo está en (pivot_x, +Y_exterior, pivot_z)
// El cuerpo se extiende debajo del eje.
servo_body_x_start = pivot_x - servo_offset_eje;         // Borde izquierdo del cuerpo
servo_body_x_center = servo_body_x_start + servo_largo / 2; // Centro del cuerpo
z_tab_level = pivot_z - servo_alto_arriba - servo_tab_grosor; // Z donde están las tabs

/* ==================== MÓDULOS AUXILIARES ==================== */

module placa_nema17() {
    mitad = nema_cara / 2;
    difference() {
        translate([-mitad, -mitad, 0])
            cube([nema_cara, nema_cara, pared]);
        cylinder(h = pared * 3, d = nema_boss_d, center = true, $fn = 60);
        for (i = [-1, 1], j = [-1, 1])
            translate([i * nema_tornillos / 2, j * nema_tornillos / 2, 0])
                cylinder(h = pared * 3, d = nema_m3, center = true, $fn = 24);
    }
}

module sede_rodamiento(invertido = false) {
    housing_d = rod_od + pared * 2;
    r = invertido ? [180, 0, 0] : [0, 0, 0];
    rotate(r)
    difference() {
        cylinder(h = rod_h + pared, d = housing_d, $fn = 60);
        translate([0, 0, pared])
            cylinder(h = rod_h + 1, d = rod_od, $fn = 60);
        translate([0, 0, -1])
            cylinder(h = rod_h + pared + 2, d = rod_bore + tol, $fn = 60);
    }
}

/* ==================== PIEZA ÚNICA: END-EFFECTOR INTEGRADO ==================== */

module end_effector() {
    mitad = nema_cara / 2;
    hd = rod_od + pared * 2;
    col_x = dist_ejes - hd / 2 - pared;

    // Placa de montaje del servo: posiciones de los agujeros de las tabs
    tab_hole_1_x = servo_body_x_center - servo_dist_holes / 2;
    tab_hole_2_x = servo_body_x_center + servo_dist_holes / 2;
    servo_plate_margen = 8;

    difference() {
        union() {
            // ===========================================================
            // ESTRUCTURA EN "C" CON CENTRO ABIERTO PARA CORREA GT2
            // ===========================================================

            // [A] Placa de montaje NEMA 17 (base, z = 0)
            placa_nema17();

            // [B] Puente inferior (conecta motor con hollow shaft)
            translate([-mitad, -mitad, 0])
                cube([dist_ejes + hd / 2 + mitad, nema_cara, pared]);

            // [C] Rodamiento INFERIOR
            translate([dist_ejes, 0, 0])
                sede_rodamiento(invertido = false);

            // [D] Soporte lateral IZQUIERDO (-Y, borde)
            translate([col_x, -mitad, 0])
                cube([pared, soporte_lat_w, altura_c]);

            // [E] Soporte lateral DERECHO (+Y, borde)
            translate([col_x, mitad - soporte_lat_w, 0])
                cube([pared, soporte_lat_w, altura_c]);

            // [F] Costilla lado motor (altura completa)
            translate([-mitad, -mitad, 0])
                cube([pared, nema_cara, altura_c]);

            // [G] Puente superior (tapa, span completo)
            translate([-mitad, -mitad, altura_c - pared])
                cube([dist_ejes + hd / 2 + mitad, nema_cara, pared]);

            // [H] Rodamiento SUPERIOR (invertido)
            translate([dist_ejes, 0, altura_c - pared])
                sede_rodamiento(invertido = true);

            // ===========================================================
            // MONTAJE DEL SERVO MG995 (INTEGRADO, cara +Y)
            // ===========================================================
            // El servo se atornilla directamente a esta placa.
            // Su cuerpo queda por fuera del bracket (en +Y).
            // El servo horn sale hacia +Y y se conecta al brazo del robot.

            // [I] Placa de montaje del Servo (cara +Y)
            // Superficie reforzada donde se atornillan las tabs del MG995
            translate([tab_hole_1_x - servo_plate_margen,
                       mitad - pared,
                       z_tab_level - servo_plate_margen])
                cube([tab_hole_2_x - tab_hole_1_x + servo_plate_margen * 2,
                      pared * 2,
                      servo_tab_grosor + servo_plate_margen * 2]);

            // [J] Nervio vertical izquierdo de refuerzo (conecta placa servo con puente inf.)
            translate([tab_hole_1_x - servo_plate_margen,
                       mitad - pared,
                       pared])
                cube([pared, pared * 2, z_tab_level - servo_plate_margen - pared]);

            // [K] Nervio vertical derecho de refuerzo (conecta placa servo con puente sup.)
            translate([tab_hole_2_x + servo_plate_margen - pared,
                       mitad - pared,
                       z_tab_level + servo_tab_grosor + servo_plate_margen])
                cube([pared, pared * 2,
                      altura_c - pared - (z_tab_level + servo_tab_grosor + servo_plate_margen)]);

            // ===========================================================
            // MUÑÓN / JOURNAL (cara -Y, segundo apoyo del pivote)
            // ===========================================================
            // Cilindro que sobresale de la cara -Y del bracket.
            // Coaxial con el eje del servo para definir el eje de Pitch.
            // Se inserta en una horquilla o agujero del brazo robot.

            // [L] Journal boss
            translate([pivot_x, -mitad, pivot_z])
                rotate([90, 0, 0])
                    cylinder(h = journal_largo, d = journal_d, $fn = 50);

            // [M] Refuerzo del journal (conecta boss con la pared -Y)
            translate([pivot_x - journal_d / 2, -mitad - journal_largo, pivot_z - journal_d / 2])
                cube([journal_d, journal_largo, journal_d]);
        }

        // ===========================================================
        // SUSTRACCIONES
        // ===========================================================

        // [N] Agujeros para las tabs del servo (atraviesan la placa +Y)
        for (tab_x = [tab_hole_1_x, tab_hole_2_x])
            translate([tab_x, mitad - pared - 1, z_tab_level + servo_tab_grosor / 2])
                rotate([-90, 0, 0])
                    cylinder(h = pared * 2 + 2, d = servo_d_hole, $fn = 24);

        // [O] Abertura para el cuerpo del servo ARRIBA de las tabs
        // (zona donde sale el eje, necesita holgura contra la placa)
        translate([servo_body_x_start - tol,
                   mitad - pared - 1,
                   z_tab_level + servo_tab_grosor])
            cube([servo_largo + tol * 2,
                  pared * 2 + 2,
                  servo_alto_arriba + 2]);

        // [P] Abertura para el cuerpo del servo DEBAJO de las tabs
        // (el cuerpo rectangular cuelga afuera)
        translate([servo_body_x_start - tol,
                   mitad - 1,
                   z_tab_level - servo_alto_bajo - 1])
            cube([servo_largo + tol * 2,
                  pared * 2 + 2,
                  servo_alto_bajo + 1]);

        // [Q] Agujero pasante del Journal (M4, para pin del pivote)
        translate([pivot_x, -mitad + 1, pivot_z])
            rotate([90, 0, 0])
                cylinder(h = journal_largo + pared + 2,
                         d = journal_pin_d, $fn = 30);

        // [R] Ranura para cables del NEMA 17
        translate([-mitad - 1, -8, pared])
            cube([pared + 2, 16, 15]);

        // [S] Ventana en el puente superior para instalación de la correa GT2
        translate([pared * 2, -correa_ventana_w / 2, altura_c - pared - 1])
            cube([dist_ejes - pared * 4, correa_ventana_w, pared + 2]);
    }
}

/* ==================== VISUALIZACIÓN DE ENSAMBLE ==================== */

module ensamble_completo() {
    // Altura central de la zona de poleas
    polea_z = (rod_h + pared + altura_c - pared - rod_h) / 2;
    mitad = nema_cara / 2;

    // --- Pieza imprimible (azul) ---
    color("SteelBlue", 0.85)
        end_effector();

    // --- Servo MG995 (naranja, referencia) ---
    // Montado en la cara +Y, cuerpo cuelga por fuera, eje apunta en +Y
    color("DarkOrange", 0.7) {
        // Cuerpo debajo de las tabs
        translate([servo_body_x_start, mitad + pared, z_tab_level - servo_alto_bajo])
            cube([servo_largo, servo_ancho, servo_alto_bajo]);
        // Cuerpo arriba de las tabs (zona del eje)
        translate([servo_body_x_start, mitad + pared, z_tab_level + servo_tab_grosor])
            cube([servo_largo, servo_ancho, servo_alto_arriba]);
        // Tabs (pestañas)
        translate([servo_body_x_start - servo_tab_grosor, mitad, z_tab_level])
            cube([servo_largo + servo_tab_grosor * 2, servo_ancho + pared, servo_tab_grosor]);
    }

    // --- Motor NEMA 17 (gris oscuro, referencia) ---
    color("DimGray", 0.4)
        translate([-nema_cara / 2, -nema_cara / 2, -nema_cuerpo_h])
            cube([nema_cara, nema_cara, nema_cuerpo_h]);

    // --- Eje hueco / Hollow Shaft (celeste, referencia) ---
    color("LightBlue", 0.5)
        translate([dist_ejes, 0, -20])
            difference() {
                cylinder(h = altura_c + 35, d = eje_od, $fn = 40);
                cylinder(h = altura_c + 40, d = eje_id, center = true, $fn = 40);
            }

    // --- Polea CONDUCIDA en hollow shaft (dorado) ---
    color("Gold", 0.5)
        translate([dist_ejes, 0, polea_z - polea_h / 2])
            cylinder(h = polea_h, d = polea_conducida_d, $fn = 50);

    // --- Polea MOTRIZ en eje NEMA 17 (dorado) ---
    color("Gold", 0.5)
        translate([0, 0, polea_z - polea_h / 2])
            cylinder(h = polea_h, d = polea_motriz_d, $fn = 50);

    // --- Correa GT2 (verde, simplificada) ---
    color("DarkGreen", 0.3)
        translate([0, -3, polea_z - 3])
            cube([dist_ejes, 6, 6]);

    // --- Pin del Journal (rojo, referencia) ---
    color("Red", 0.6)
        translate([pivot_x, -mitad - journal_largo - 3, pivot_z])
            rotate([-90, 0, 0])
                cylinder(h = journal_largo + 6,
                         d = 4, $fn = 20);
}

/* ==================== RENDERIZADO ==================== */

if (PIEZA == 0) {
    ensamble_completo();   // Visualización con referencias
} else if (PIEZA == 1) {
    end_effector();        // Pieza para exportar a STL
}
