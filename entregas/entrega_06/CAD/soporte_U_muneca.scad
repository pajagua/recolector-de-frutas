// =====================================================================
//  soporte_U_muneca.scad
//  Visualizacion conceptual de la articulacion de muneca (pitch joint)
//  con soporte en U (clevis / horquilla) + idler bearing 608ZZ.
//  Proyecto: Robot Recolector de Naranjas - Robotica 1 (FIUNA)
// ---------------------------------------------------------------------
//  IDEA CLAVE:
//   - El servo MG996R transmite el TORQUE a traves del servo horn.
//   - La carga radial (peso de herramienta + naranja) se reparte entre
//     el spline del servo (lado izquierdo) y un rodamiento 608ZZ
//     (lado derecho). Se elimina el voladizo (cantilever).
// ---------------------------------------------------------------------
//  Convencion de ejes (global):
//   X = eje de giro de la muneca (pitch axis), pasa por el origen
//   Y = direccion del eje prismatico (el brazo "avanza" hacia +Y)
//   Z = vertical (la ventosa apunta hacia -Z en pose neutra)
// ---------------------------------------------------------------------
//  USO:
//   F5 = preview con colores.
//   View > Animate (FPS = 10, Steps = 60) para ver la muneca oscilando.
//   Para exportar STL: dejar visible solo la pieza deseada
//   (mostrar_fijo / mostrar_movil), poner animar = false y F6.
//  ADVERTENCIA: las cotas del MG996R son APROXIMADAS. Verificar con
//   calibre o con tu modelo de Fusion 360 antes de imprimir.
// =====================================================================

/* [Visualizacion] */
animar           = true;  // true: usa $t (View > Animate)
pitch_manual     = 0;     // [deg] angulo de pitch si animar = false
pitch_amplitud   = 45;    // [deg] amplitud de la animacion
mostrar_fijo     = true;  // horquilla fija + servo de muneca + rodamiento
mostrar_movil    = true;  // bloque herramienta + horn + tornilleria movil
mostrar_contexto = true;  // eslabon prismatico simulado (solo referencia)
corte_seccion    = false; // true: corta el modelo en z > 0 para ver el pivote

$fn = 64;
eps = 0.01;  // pequeno solape para evitar caras coincidentes en CSG

/* [Servo MG996R - VERIFICAR CON CALIBRE] */
srv_L          = 40.7;  // largo del cuerpo [mm]
srv_W          = 19.7;  // ancho del cuerpo [mm]
srv_H          = 37.0;  // alto del cuerpo sin spline [mm] (aprox.)
srv_flange_L   = 54.5;  // largo total incluyendo pestanas [mm]
srv_flange_t   = 2.5;   // espesor de pestanas [mm]
srv_flange_z   = 26.5;  // base del servo -> cara inferior de pestana [mm] (aprox.)
srv_hole_dx    = 49.5;  // separacion agujeros de montaje (a lo largo) [mm]
srv_hole_dy    = 10.0;  // separacion agujeros de montaje (a lo ancho) [mm]
srv_spline_off = 10.0;  // centro del spline -> extremo cercano del cuerpo [mm]
srv_spline_d   = 6.0;   // diametro del spline 25T [mm]
srv_spline_h   = 4.0;   // altura del spline sobre la tapa [mm]
horn_d         = 20.0;  // diametro del servo horn redondo metalico [mm]
horn_t         = 3.0;   // espesor del horn [mm]
horn_pcd       = 14.0;  // diametro del circulo de agujeros del horn [mm]

/* [Rodamiento 608ZZ] */
brg_OD           = 22.0;  // diametro exterior [mm]
brg_ID           = 8.0;   // diametro interior [mm]
brg_W            = 7.0;   // ancho [mm]
brg_inner_race_d = 11.0;  // diametro del espaciador: toca SOLO la pista interna

/* [Tornilleria M8 del pivote] */
m8_d      = 8.0;   // diametro nominal [mm]
m8_head_d = 13.0;  // cabeza hexagonal (entre caras aprox.) [mm]
m8_head_h = 5.5;   // alto de cabeza [mm]
m8_L      = 40.0;  // largo del bulon sin cabeza [mm] -> M8x40
m8_nut_af = 13.0;  // tuerca: distancia entre caras [mm]
m8_nut_h  = 6.5;   // tuerca: alto [mm]
washer_t  = 1.5;   // arandela pequena d = brg_inner_race_d bajo la cabeza [mm]

/* [Bloque herramienta (placeholder de tu BracketParaServo)] */
tool_w = 50;   // ancho en X (distancia entre caras de pivote) [mm]
tool_d = 60;   // profundidad en Y [mm]
tool_h = 40;   // alto en Z [mm]
tube_y = 15;   // posicion en Y del hollow shaft dentro del bloque [mm]

/* [Horquilla fija (clevis)] */
arm_t_srv   = 5;    // espesor del brazo lado servo [mm]
arm_t_brg   = 10;   // espesor del brazo lado rodamiento [mm] (> brg_W)
arm_h       = 46;   // alto de los brazos [mm]
back_t      = 8;    // espesor de la placa trasera [mm]
clear_rot   = 3;    // holgura radial bloque <-> placa trasera al rotar [mm]
pris_hole_d = 4.3;  // agujeros pasantes M4 hacia el eje prismatico [mm]
pris_hole_s = 30;   // separacion del patron cuadrado de agujeros [mm]

// =====================================================================
//  COTAS DERIVADAS (no editar: se calculan a partir de lo anterior)
// =====================================================================

// Altura de la tapa del servo por encima de la cara superior de pestana
srv_top_over_flange = srv_H - srv_flange_z - srv_flange_t;

// --- Cadena de cotas en X, lado IZQUIERDO (servo) ---
// cara izq. del bloque -> horn -> spline -> tapa del servo -> pestanas
x_horn_in = -tool_w/2;                              // cara del horn que toca el bloque
x_srv_top = x_horn_in - horn_t - srv_spline_h;      // tapa superior del servo
xL_out    = x_srv_top - srv_top_over_flange;        // cara exterior brazo izq. (apoyo de pestanas)
xL_in     = xL_out + arm_t_srv;                     // cara interior brazo izq.
gap_lat   = -xL_in - tool_w/2;                      // holgura lateral bloque <-> brazo

// --- Lado DERECHO (rodamiento), misma holgura lateral ---
xR_in     = tool_w/2 + gap_lat;                     // cara interior brazo der.
xR_out    = xR_in + arm_t_brg;                      // cara exterior brazo der.
x_brg_in  = xR_out - brg_W;                         // cara interior del 608ZZ (enrasado afuera)

// --- Placa trasera: debe quedar fuera del radio barrido por el bloque ---
R_sweep    = sqrt(pow(tool_d/2, 2) + pow(tool_h/2, 2)); // radio de las esquinas del bloque
y_bp_front = -(R_sweep + clear_rot);                // cara frontal de la placa trasera
y_bp_rear  = y_bp_front - back_t;                   // cara trasera (contra el prismatico)
x_center   = (xL_out + xR_out)/2;                   // centro de la horquilla en X

// --- Extension del cuerpo del servo en Y (spline ubicado en y = 0) ---
srv_y_min = -(srv_L - srv_spline_off);
srv_y_max =  srv_spline_off;
srv_y_c   = (srv_y_min + srv_y_max)/2;              // centro del cuerpo en Y

// --- Angulo de pitch actual ---
pitch = animar ? pitch_amplitud * sin(360 * $t) : pitch_manual;

// --- Chequeos de consistencia geometrica (salen en la consola) ---
assert(xL_in < x_srv_top, "La tapa del servo no atraviesa el brazo: revisar cotas del servo");
assert(gap_lat > 2,       "Holgura lateral insuficiente entre bloque y brazos");
assert(arm_t_brg > brg_W + 1, "El brazo del rodamiento debe ser mas grueso que el 608ZZ");
echo(str("Holgura lateral = ", gap_lat, " mm | Ancho total horquilla = ",
         xR_out - xL_out, " mm | Radio barrido bloque = ", R_sweep, " mm"));

// =====================================================================
//  MODULOS: PIEZA FIJA
// =====================================================================

// Brazo de horquilla con perfil tipo "gota": hull entre la zona trasera
// (unida a la placa) y un disco centrado en el eje de pitch.
module brazo(x0, t) {
    hull() {
        translate([x0, y_bp_rear, -arm_h/2]) cube([t, back_t, arm_h]);
        translate([x0, 0, 0]) rotate([0, 90, 0]) cylinder(r = arm_h/2, h = t);
    }
}

// Pieza FIJA (se imprime): placa trasera + brazo servo + brazo rodamiento
module horquilla_fija() {
    difference() {
        union() {
            // Placa trasera: se atornilla al eslabon prismatico
            translate([xL_out, y_bp_rear, -arm_h/2])
                cube([xR_out - xL_out, back_t, arm_h]);
            brazo(xL_out, arm_t_srv);   // brazo izquierdo (servo)
            brazo(xR_in,  arm_t_brg);   // brazo derecho (rodamiento)
        }
        // Ventana para el cuerpo del servo (holgura 0.3 mm por lado).
        // Las pestanas del servo se apoyan en la cara EXTERIOR del brazo.
        translate([xL_out - 1, srv_y_min - 0.3, -srv_W/2 - 0.3])
            cube([arm_t_srv + 2, srv_L + 0.6, srv_W + 0.6]);
        // 4 agujeros M3 pasantes para las pestanas del servo
        for (sy = [-1, 1], sz = [-1, 1])
            translate([xL_out - 1, srv_y_c + sy*srv_hole_dx/2, sz*srv_hole_dy/2])
                rotate([0, 90, 0]) cylinder(d = 3.4, h = arm_t_srv + 2);
        // Alojamiento del 608ZZ (press-fit desde el exterior)
        translate([x_brg_in, 0, 0]) rotate([0, 90, 0])
            cylinder(d = brg_OD + 0.1, h = brg_W + 1);
        // Labio interior: paso del espaciador sin tocarlo
        translate([xR_in - 1, 0, 0]) rotate([0, 90, 0])
            cylinder(d = brg_inner_race_d + 3, h = arm_t_brg + 2);
        // 4 agujeros M4 hacia el eje prismatico (atraviesan la placa en Y)
        for (sx = [-1, 1], sz = [-1, 1])
            translate([x_center + sx*pris_hole_s/2, y_bp_rear - 1, sz*pris_hole_s/2])
                rotate([-90, 0, 0]) cylinder(d = pris_hole_d, h = back_t + 2);
    }
}

// Servo MG996R simplificado en coordenadas LOCALES:
//   largo en X_l, ancho en Y_l, alto en Z_l; base en z_l = 0;
//   spline centrado en (x_l, y_l) = (0, 0).
module servo_mg996r() {
    color([0.12, 0.12, 0.12]) difference() {
        union() {
            translate([srv_y_min, -srv_W/2, 0]) cube([srv_L, srv_W, srv_H]);       // cuerpo
            translate([srv_y_c - srv_flange_L/2, -srv_W/2, srv_flange_z])
                cube([srv_flange_L, srv_W, srv_flange_t]);                          // pestanas
        }
        for (sx = [-1, 1], sy = [-1, 1])
            translate([srv_y_c + sx*srv_hole_dx/2, sy*srv_hole_dy/2, srv_flange_z - 1])
                cylinder(d = 3.4, h = srv_flange_t + 2);
    }
    color("Gold") translate([0, 0, srv_H - eps])
        cylinder(d = srv_spline_d, h = srv_spline_h + eps);                         // spline
}

// Coloca el servo en coordenadas GLOBALES mediante una rotacion ciclica:
//   X_l -> +Y,  Y_l -> +Z,  Z_l -> +X  (la tapa del servo mira hacia +X)
module servo_colocado() {
    multmatrix([[0, 0, 1, x_srv_top - srv_H],
                [1, 0, 0, 0],
                [0, 1, 0, 0],
                [0, 0, 0, 1]])
        servo_mg996r();
}

// Rodamiento 608ZZ (anillo simple, eje en X, cara interior en x = 0)
module rodamiento_608() {
    color("Silver") difference() {
        rotate([0, 90, 0]) cylinder(d = brg_OD, h = brg_W);
        translate([-1, 0, 0]) rotate([0, 90, 0]) cylinder(d = brg_ID, h = brg_W + 2);
    }
}

// =====================================================================
//  MODULOS: PIEZA MOVIL (rota con el pitch)
// =====================================================================

// Bloque de la herramienta (aqui va tu BracketParaServo con el servo de
// torsion, las poleas GT2 y el hollow shaft). Solo se modelan las
// interfaces con la muneca.
module bloque_herramienta() {
    difference() {
        cube([tool_w, tool_d, tool_h], center = true);
        // 4 agujeros para tornillos del horn (M3 en insertos termicos)
        for (a = [45 : 90 : 315])
            translate([-tool_w/2 - 1, horn_pcd/2*cos(a), horn_pcd/2*sin(a)])
                rotate([0, 90, 0]) cylinder(d = 2.6, h = 10);
        // Rebaje central para el tornillo del horn
        translate([-tool_w/2 - 1, 0, 0]) rotate([0, 90, 0]) cylinder(d = 7, h = 6);
        // Agujero ciego para el bulon M8 desde la cara derecha
        translate([tool_w/2 - 20, 0, 0]) rotate([0, 90, 0])
            cylinder(d = m8_d + 0.4, h = 21);
        // Nut trap hexagonal + ranura de insercion desde arriba
        translate([tool_w/2 - 14, 0, 0]) {
            rotate([0, 90, 0])
                cylinder(d = m8_nut_af/cos(30) + 0.3, h = m8_nut_h + 0.4, $fn = 6);
            translate([0, -(m8_nut_af + 0.3)/2, 0])
                cube([m8_nut_h + 0.4, m8_nut_af + 0.3, tool_h]);
        }
        // Paso vertical del hollow shaft
        translate([0, tube_y, -tool_h]) cylinder(d = 16.4, h = 2*tool_h);
    }
}

// Servo horn redondo de aluminio (atornillado al bloque, montado en el spline)
module horn() {
    color("Silver") difference() {
        translate([x_horn_in - horn_t, 0, 0]) rotate([0, 90, 0])
            cylinder(d = horn_d, h = horn_t);
        for (a = [45 : 90 : 315])
            translate([x_horn_in - horn_t - 1, horn_pcd/2*cos(a), horn_pcd/2*sin(a)])
                rotate([0, 90, 0]) cylinder(d = 3, h = horn_t + 2);
    }
}

// Stack del pivote: espaciador + 608ZZ (pista interna) + arandela + bulon.
// Al apretar la tuerca, la pista INTERNA queda apretada y gira con el
// bloque; la pista EXTERNA queda fija en el brazo.
module tornilleria_pivote() {
    // Espaciador: del bloque a la pista interna del 608 (NO toca la externa)
    color("White") translate([tool_w/2, 0, 0]) rotate([0, 90, 0]) difference() {
        cylinder(d = brg_inner_race_d, h = x_brg_in - tool_w/2);
        translate([0, 0, -1]) cylinder(d = m8_d + 0.4, h = 100);
    }
    // Arandela pequena bajo la cabeza (toca solo la pista interna)
    color("Silver") translate([xR_out, 0, 0]) rotate([0, 90, 0]) difference() {
        cylinder(d = brg_inner_race_d, h = washer_t);
        translate([0, 0, -1]) cylinder(d = m8_d + 0.4, h = 10);
    }
    // Bulon M8: cabeza hexagonal + vastago
    color([0.6, 0.6, 0.65]) {
        translate([xR_out + washer_t, 0, 0]) rotate([0, 90, 0])
            cylinder(d = m8_head_d, h = m8_head_h, $fn = 6);
        translate([xR_out + washer_t - m8_L, 0, 0]) rotate([0, 90, 0])
            cylinder(d = m8_d, h = m8_L);
    }
    // Tuerca M8 alojada en el nut trap del bloque
    color([0.6, 0.6, 0.65]) translate([tool_w/2 - 14 + 0.2, 0, 0]) rotate([0, 90, 0])
        difference() {
            cylinder(d = m8_nut_af/cos(30), h = m8_nut_h, $fn = 6);
            translate([0, 0, -1]) cylinder(d = m8_d, h = 10);
        }
}

// Hollow shaft + vacuum cup tipo fuelle (bellows), solo referencia visual
module tubo_y_ventosa() {
    color("LightGray") translate([0, tube_y, -tool_h/2 - 25]) difference() {
        cylinder(d = 16, h = tool_h + 25 + 8);
        translate([0, 0, -1]) cylinder(d = 10, h = tool_h + 40);
    }
    color([0.15, 0.15, 0.15]) for (i = [0 : 2])
        translate([0, tube_y, -tool_h/2 - 25 - 7*(i + 1)])
            cylinder(d1 = 30, d2 = 22, h = 7);
}

// =====================================================================
//  CONTEXTO (no se imprime)
// =====================================================================

// Eslabon prismatico simulado (tubo cuadrado 40x40) detras de la placa
module contexto_prismatico() {
    color([0.35, 0.35, 0.40, 0.5])
        translate([x_center - 20, y_bp_rear - 80, -20]) difference() {
            cube([40, 80, 40]);
            translate([3, -1, 3]) cube([34, 82, 34]);
        }
}

// =====================================================================
//  ENSAMBLE
// =====================================================================
module ensamble() {
    if (mostrar_fijo) {
        color([0.78, 0.78, 0.82]) horquilla_fija();
        servo_colocado();
        translate([x_brg_in, 0, 0]) rodamiento_608();
    }
    if (mostrar_movil)
        rotate([pitch, 0, 0]) {          // todo lo movil gira alrededor del eje X
            color([1.0, 0.55, 0.10, 0.85]) bloque_herramienta();
            horn();
            tornilleria_pivote();
            tubo_y_ventosa();
        }
    if (mostrar_contexto) contexto_prismatico();
}

if (corte_seccion)
    difference() {
        ensamble();
        translate([-500, -500, 0]) cube(1000);   // elimina la mitad z > 0
    }
else
    ensamble();
