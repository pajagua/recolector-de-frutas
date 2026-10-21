using System;
using System.IO;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Parts.exe <outdir_root> [which...]   creates the new (non-copied) parts of the CitriBot assembly
class Parts
{
    static string R;   // project root
    static string P(string sub, string file) { return Path.Combine(R, sub, file); }
    const string COM = "05_Comerciales", HW = "05_Comerciales\\Tornilleria";

    static void Main(string[] args)
    {
        R = args[0];
        SW.Connect();
        SW.CloseMine();
        var which = args.Skip(1).ToList();
        Func<string, bool> want = k => which.Count == 0 || which.Contains(k);
        if (want("m37d")) Motor37D();
        if (want("m5840")) Motor5840();
        if (want("as5600")) AS5600();
        if (want("iman")) Iman();
        if (want("m6x10")) Screw(6, 10, 10, 6, P(HW, "HW_M6x10_Allen_DIN912.SLDPRT"), "Tornillo Allen M6x10 DIN 912 (fija el motor 5840 a la torre, 4.5 mm de rosca en la caja)");
        if (want("eje5840")) Eje5840();
        if (want("sop37d")) SoporteMotorJ1();
        if (want("sopas")) SoporteAS5600();
        if (want("buje")) BujeD8();
        if (want("tapa_as")) TapaSoporteAS5600();
        if (want("sopiman")) SoporteIman();
        if (want("m3x6")) Screw(3, 6, 5.5, 3, P(HW, "HW_M3x6_Allen_DIN912.SLDPRT"), "Tornillo Allen M3x6 DIN 912 (NEMA 14 a E2, AS5600 a la tapa)");
        if (want("bandeja")) Bandeja();
    }

    static void Motor37D()
    {
        PB.NewPart();
        // Frame: origin = output shaft axis on the gearbox face; +Y = shaft direction; gearbox axis at x=+7
        double cx = 7;
        PB.Sketch('T'); PB.Circle(cx, 0, 36.8); var gb = PB.Extrude(0, -24.0, false, "Caja_reductora_L24");
        PB.Sketch('T'); PB.Circle(cx, 0, 34.8); var mo = PB.Extrude(-24.0, -30.7, false, "Motor_D34.8");
        PB.Sketch('T'); PB.Circle(cx, 0, 34.0); var en = PB.Extrude(-54.7, -15.4, false, "Tapa_encoder_64CPR");
        PB.Sketch('T'); PB.Circle(0, 0, 12.0); var bo = PB.Extrude(0, 6.0, false, "Buje_D12");
        PB.Sketch('T'); PB.Circle(0, 0, 6.0); var sh = PB.Extrude(6.0, 16.0, false, "Eje_D6");
        PB.Sketch('T'); PB.Rect(2.4, -4, 4.0, 4); PB.Extrude(7.0, 15.0, true, "Plano_D_5.4");
        PB.Sketch('T');
        foreach (double a in new[] { 30, 90, 150, 210, 270, 330 })
        {
            double t = a * Math.PI / 180;
            PB.Circle(cx + 15.5 * Math.Cos(t), 15.5 * Math.Sin(t), 3.0);
        }
        PB.Extrude(0, -3.0, true, "Agujeros_6xM3_prof3");
        PB.PartColor(0.75, 0.75, 0.78);
        PB.Color(mo, 0.35, 0.35, 0.38); PB.Color(en, 0.08, 0.08, 0.08); PB.Color(sh, 0.6, 0.6, 0.62);
        PB.Prop("Descripcion", "Motorreductor DC 37D 90:1 6V/12V con encoder 64 CPR (37Dx70.1L mm) - cintura J1");
        PB.Prop("Fabricante", "Pololu (o equivalente) - cotas segun plano 37D con encoder");
        PB.Prop("Notas", "Origen en el eje de salida sobre la cara de la caja. Eje D 6 mm (plano a 5.4), descentrado 7 mm. 6xM3 en circulo D31, max 3 mm de rosca.");
        PB.Report("37D");
        PB.Save(P(COM, "CB-501_Motor_37D_90-1_Encoder64CPR.SLDPRT"));
        SW.CloseMine();
    }

    static void Motor5840()
    {
        PB.NewPart();
        // Frame: origin = output shaft axis on the gearbox front face; +Z = shaft direction (out of the face);
        // motor along -Y; gearbox 40 (X) x 58 (Y: -39..+19) x 26.8 (Z: -26.8..0)
        PB.Sketch('F'); PB.RRect(-20, -39, 20, 19, 2.5); var gb = PB.Extrude(0, -26.8, false, "Caja_reductora_58x40x26.8");
        PB.Sketch('F'); PB.Circle(0, 0, 18.6); PB.Extrude(0, 4.0, false, "Buje_frontal_D18.6");
        PB.Sketch('F'); PB.Circle(0, 0, 17.0); PB.Extrude(-26.8, -4.0, false, "Tapa_trasera_D17");
        PB.Sketch('F');
        foreach (double x in new[] { -13.85, 13.85 }) { PB.Circle(x, -29.0, 6.0); PB.Circle(x, 10.7, 6.0); }
        PB.Extrude(0, -6.0, true, "Agujeros_4xM6_prof6");
        // motor can, axis along -Y at z=-18 (tangent to the front face plane)
        PB.Sketch('T'); PB.Circle(0, -18.0, 36.0); var mo = PB.Extrude(-39.0, -63.0, false, "Motor_D36x63");
        PB.Sketch('T'); PB.Circle(0, -18.0, 10.0); PB.Extrude(-102.0, -2.0, false, "Tapa_motor");
        PB.Sketch('T'); PB.Rect(-11, -19.5, -8, -16.5); PB.Rect(8, -19.5, 11, -16.5); var tm = PB.Extrude(-102.0, -5.0, false, "Terminales");
        PB.PartColor(0.78, 0.78, 0.8);
        PB.Color(mo, 0.12, 0.12, 0.12); PB.Color(tm, 0.8, 0.6, 0.2);
        PB.Prop("Descripcion", "Motorreductor sinfin-corona 5840-36ZY 12/24 V autoblocante - hombro J2 (direct drive)");
        PB.Prop("Notas", "Cotas del plano del fabricante: caja 40x58x26.8, motor D36x63, eje D8 con plano 1 mm (L15, plano 13), buje D18.6, 4xM6 prof 6 a 27.7 x 39.7. " +
            "El eje de salida D8 esta modelado aparte (CB-506) porque gira con E2. Posicion vertical del eje y de los agujeros medida en la foto (eje a 39 mm del borde del lado motor; pares de agujeros a -29.0 y +10.7 del eje). VERIFICAR CON CALIBRE.");
        PB.Report("5840");
        PB.Save(P(COM, "CB-502_Motor_Sinfin_5840-36ZY.SLDPRT"));
        SW.CloseMine();
    }

    static void AS5600()
    {
        PB.NewPart();
        // Frame: origin = sensor (chip) center on the PCB front face; PCB in XY, chip side +Z
        PB.Sketch('F'); PB.RRect(-11.5, -11.5, 11.5, 11.5, 1.0); var pcb = PB.Extrude(0, -1.6, false, "PCB_23x23");
        PB.Sketch('F'); PB.Circle(-8.0, 8.0, 3.2); PB.Circle(8.0, 8.0, 3.2); PB.Extrude(0, -1.6, true, "Agujeros_2xD3.2");
        PB.Sketch('F'); for (int i = 0; i < 7; i++) PB.Circle(-7.62 + 2.54 * i, -9.5, 1.0); PB.Extrude(0, -1.6, true, "Pads_header_7p");
        PB.Sketch('F'); PB.Rect(-2.45, -1.95, 2.45, 1.95); var chip = PB.Extrude(0, 1.5, false, "AS5600_SOIC8");
        PB.Sketch('F'); PB.Rect(-7, -4, -4, -6); PB.Rect(4, 4, 7, 6); var cap = PB.Extrude(0, 0.8, false, "Componentes_SMD");
        PB.PartColor(0.1, 0.25, 0.65);
        PB.Color(chip, 0.05, 0.05, 0.05); PB.Color(cap, 0.7, 0.6, 0.4);
        PB.Prop("Descripcion", "Modulo encoder magnetico AS5600 (I2C/analogico) - lectura de angulo del hombro J2");
        PB.Prop("Notas", "PCB 23x23x1.6 tipico; ubicacion de agujeros y header aproximada: verificar con la placa comprada. Entrehierro iman-chip 0.5-3 mm.");
        PB.Report("AS5600");
        PB.Save(P(COM, "CB-503_Modulo_AS5600.SLDPRT"));
        SW.CloseMine();
    }

    static void Iman()
    {
        PB.NewPart();
        PB.Sketch('F'); PB.Line(0, -3, 0, 3); PB.Arc(0, 0, 0, 3, 0, -3); PB.Extrude(0, 2.5, false, "Polo_N");
        PB.Sketch('F'); PB.Line(0, 3, 0, -3); PB.Arc(0, 0, 0, -3, 0, 3); var s = PB.Extrude(0, 2.5, false, "Polo_S");
        PB.PartColor(0.75, 0.15, 0.15);
        PB.Color(s, 0.15, 0.25, 0.75);
        PB.Prop("Descripcion", "Iman de neodimio diametral D6 x 2.5 mm (para AS5600)");
        PB.Report("Iman");
        PB.Save(P(COM, "CB-504_Iman_Diametral_D6x2.5.SLDPRT"));
        SW.CloseMine();
    }

    // Socket head screw. Frame: origin = head bearing face center, head along +Y, shank along -Y (same as BCN3D M4xNN)
    static void Screw(double d, double L, double dk, double k, string path, string desc)
    {
        PB.NewPart();
        PB.Sketch('T'); PB.Circle(0, 0, dk); PB.Extrude(0, k, false, "Cabeza");
        PB.Sketch('T'); PB.Circle(0, 0, d); PB.Extrude(0, -L, false, "Cana");
        double s = d * 0.5; // hex socket approx (square for simplicity)
        PB.Sketch('T'); PB.Circle(0, 0, s); PB.Extrude(k, -k * 0.5, true, "Hexagono");
        PB.PartColor(0.2, 0.2, 0.22);
        PB.Prop("Descripcion", desc);
        PB.Report("screw");
        PB.Save(path);
        SW.CloseMine();
    }

    // J1 motor bracket for the 37D (replaces BCN3D 1M3A). Frame: origin = output shaft axis on the board top face, +Y up,
    // motor (gearbox axis) at +X 7 mm. Same footprint (4x M4 at +-32.4) as 1M3A.
    static void SoporteMotorJ1()
    {
        PB.NewPart();
        PB.Sketch('T'); PB.RRect(-38.5, -38.5, 38.5, 38.5, 6); var b = PB.Extrude(0, 5.7, false, "Base_5.7");
        PB.Sketch('T'); PB.Circle(7, 0, 42); PB.Extrude(0, 5.7, true, "Paso_motor_D42");
        PB.Sketch('T'); foreach (double x in new[] { -32.4, 32.4 }) foreach (double z in new[] { -32.4, 32.4 }) PB.Circle(x, z, 4.4);
        PB.Extrude(0, 5.7, true, "Agujeros_4xM4");
        PB.Sketch('T'); PB.Rect(-24, 28, 24, 34); PB.Rect(-24, -34, 24, -28); PB.Extrude(5.7, 49.3, false, "Paredes");
        PB.Sketch('T'); PB.RRect(-24, -34, 30, 34, 4); PB.Extrude(55.0, 5.0, false, "Placa_motor");
        PB.Sketch('T'); PB.Circle(0, 0, 13); PB.Extrude(55.0, 5.0, true, "Paso_buje_D13");
        PB.Sketch('T');
        foreach (double a in new[] { 30, 90, 150, 210, 270, 330 })
        {
            double t = a * Math.PI / 180;
            PB.Circle(7 + 15.5 * Math.Cos(t), 15.5 * Math.Sin(t), 3.4);
        }
        PB.Extrude(55.0, 5.0, true, "Agujeros_6xM3_D31");
        PB.PartColor(0.15, 0.15, 0.15);
        PB.Prop("Descripcion", "Soporte motor cintura J1 para 37D (reemplaza 1M3A del BCN3D Moveo)");
        PB.Prop("Material", "PETG impreso, 40% relleno, 4 perimetros");
        PB.Prop("Notas", "Mismo patron de 4xM4 (64.8 x 64.8) que 1M3A. Motor colgado bajo la placa superior (6xM3x8, max 3 mm en la caja). " +
            "El motor sobresale 15.1 mm bajo la cara del tablero: requiere agujero D42 pasante en el tablero (ya modelado en CB-111).");
        PB.Report("SoporteJ1");
        PB.Save(P("01_Base_J0", "CB-131_Soporte_Motor_J1_37D.SLDPRT"));
        SW.CloseMine();
    }

    // AS5600 bracket. Geometry directly in CB-210 (2M1D) part coordinates: J2 axis along Y at (x=190.171, z=-86.723),
    // tower -Y outer face at y=181.2, E2 outer face at y=103.2 (TE2 top at 100.2).
    static void SoporteAS5600()
    {
        PB.NewPart();
        double zc = -86.723, xa = 190.171;
        // foot against the tower (y 175.2..181.2)
        PB.Sketch('T'); PB.Rect(64, zc - 15, 100, zc + 15); PB.Extrude(175.2, 6.0, false, "Pie_torre");
        // arm parallel to J2 (y 92.2..175.2)
        PB.Sketch('T'); PB.Rect(64, zc - 15, 78, zc + 15); PB.Extrude(92.2, 83.0, false, "Brazo");
        // sensor plate (y 86.2..92.2)
        PB.Sketch('T'); PB.Rect(64, zc - 15, xa + 16, zc + 15); PB.Extrude(86.2, 6.0, false, "Placa_sensor");
        // standoffs for the AS5600 PCB (PCB back face at y=95.0, chip-magnet gap 2.2 mm)
        PB.Sketch('T'); PB.Circle(xa + 8, zc - 8, 6); PB.Circle(xa + 8, zc + 8, 6); PB.Extrude(92.2, 2.8, false, "Separadores_2.8");
        PB.Sketch('T'); PB.Circle(xa + 8, zc - 8, 2.5); PB.Circle(xa + 8, zc + 8, 2.5); PB.Extrude(86.2, 8.8, true, "Agujeros_M3_autorroscante");
        // window for header pins / cable
        PB.Sketch('T'); PB.Rect(xa - 13, zc - 11, xa - 6, zc + 11); PB.Extrude(86.2, 6.0, true, "Ventana_header");
        // M4 holes in the foot
        PB.Sketch('T'); PB.Circle(82, zc - 9, 4.4); PB.Circle(82, zc + 9, 4.4); PB.Extrude(175.2, 6.0, true, "Agujeros_2xM4");
        // gusset between arm and foot
        PB.Sketch('F'); PB.Poly(78, 175.2, 92, 175.2, 78, 161.2); PB.Extrude(zc - 4, 8, false, "Nervio");
        PB.PartColor(0.95, 0.55, 0.1);
        PB.Prop("Descripcion", "Soporte fijo del AS5600 del hombro J2 (iman en la tapa TE2 de E2)");
        PB.Prop("Material", "PETG impreso");
        PB.Prop("Notas", "Modelado en coordenadas de CB-210 (misma transformacion que la torre). Se atornilla a la cara -Y de la torre con 2xM4x30 + tuerca. " +
            "Cruza por debajo del eje J2 a 112-126 mm: limitar J2 para que E2 no apunte hacia abajo (sector +-35 deg).");
        PB.Report("SoporteAS5600");
        PB.Save(P("02_Cintura_J1", "CB-232_Soporte_AS5600_J2.SLDPRT"));
        SW.CloseMine();
    }

    // 5840 output shaft (rotates with E2). Geometry in E2 part coordinates: axis Z at (x=-80, y=0), from the gearbox boss (z=-51) to the tip (z=-36)
    static void Eje5840()
    {
        PB.NewPart();
        PB.Sketch('F'); PB.Circle(-80, 0, 8.0); PB.Extrude(-51, 15, false, "Eje_D8_L15");
        PB.Sketch('F'); PB.Rect(-80 + 3.0, -6, -80 + 6, 6); PB.Extrude(-49, 13.5, true, "Plano_D_1mm_L13");
        PB.PartColor(0.6, 0.6, 0.62);
        PB.Prop("Descripcion", "Eje de salida D8 del motorreductor 5840-36ZY (parte del motor comprado; modelado aparte para que gire con E2 en el ensamblaje)");
        PB.Prop("Notas", "Coordenadas de E2. D8 con plano de 1 mm (a 7.0), largo 15 desde el buje D18.6, plano de 13 mm.");
        PB.Report("Eje5840");
        PB.Save(P(COM, "CB-506_Eje_Salida_5840_D8.SLDPRT"));
        SW.CloseMine();
    }

    // D8 bushing that goes inside the E2 hub (geometry in E2 part coordinates: axis Z at x=-80, y=0, z -50.5..-35)
    static void BujeD8()
    {
        PB.NewPart();
        PB.Sketch('F'); PB.DShape(-80, 0, 12.4, 5.12); PB.Extrude(-50.5, 15.5, false, "Buje_D12.4_plano5.12");
        PB.Sketch('F'); PB.DShape(-80, 0, 8.1, 3.05); PB.Extrude(-50.5, 15.5, true, "Agujero_D8_plano");
        PB.PartColor(0.9, 0.9, 0.9);
        PB.Prop("Descripcion", "Buje con agujero en D para eje D8 del 5840 dentro del cubo de E2 (transmite el torque del hombro)");
        PB.Prop("Material", "PETG/PLA impreso o aluminio torneado");
        PB.Prop("Notas", "Modelado en coordenadas de E2. Exterior en D (D12.4, plano a 5.12) que calza en el cubo en D de E2; interior en D para el eje D8 (plano a 3.0). Planos paralelos, paredes de ~2 mm. Pegar con epoxi.");
        PB.Report("BujeD8");
        PB.Save(P("03_Hombro_E2_J2", "CB-305_Buje_D8_Hombro.SLDPRT"));
        SW.CloseMine();
    }
    // E2 cap that IS the AS5600 holder (rotates with E2). Geometry in E2 part coordinates: J2 axis along Z at (x=-80, y=0),
    // E2 outer face z=35, D40 window from the outer face into the motor bore. PCB flush with the flange top (z=41), chip facing out.
    static void TapaSoporteAS5600()
    {
        PB.NewPart();
        double cx = -80;
        PB.Sketch('F'); PB.Circle(cx, 0, 56); PB.Extrude(35, 6, false, "Brida_D56");
        PB.Sketch('F'); PB.Circle(cx, 0, 39.6); PB.Extrude(22, 13, false, "Espiga_D39.6");
        PB.Sketch('F'); PB.Circle(cx, 0, 30); PB.Extrude(21, 14, true, "Hueco_cables_D30");
        // pocket for the AS5600 PCB (22.96 x 23.03 x 1.58), centre at (-79.716, -0.138)
        PB.Sketch('F'); PB.Rect(-79.716 - 11.8, -0.138 - 11.8, -79.716 + 11.8, -0.138 + 11.8); PB.Extrude(41, -1.6, true, "Alojamiento_PCB_23.6x23.6");
        // slots under the two header rows -> cables go down into E2 (motor bore) and exit at the back of E2
        PB.Sketch('F'); PB.Rect(-86.1, -8.6, -74.8, -5.3); PB.Rect(-86.1, 4.9, -74.8, 8.2); PB.Extrude(34, 5.4, true, "Ranuras_header_cables");
        // pilot holes for the PCB screws (slots of the module at 16.16 x 16.23)
        PB.Sketch('F'); foreach (double x in new[] { -87.8, -71.64 }) foreach (double y in new[] { -8.25, 7.98 }) PB.Circle(x, y, 2.5);
        PB.Extrude(33, 6.4, true, "Pilotos_M3_PCB");
        // 3 screws to E2 (r = 24.5), counterbored
        PB.Sketch('F'); foreach (double a in new[] { 90.0, 210.0, 330.0 }) PB.Circle(cx + 24.5 * Math.Cos(a * Math.PI / 180), 24.5 * Math.Sin(a * Math.PI / 180), 3.4);
        PB.Extrude(34, 8, true, "Agujeros_3xM3");
        PB.Sketch('F'); foreach (double a in new[] { 90.0, 210.0, 330.0 }) PB.Circle(cx + 24.5 * Math.Cos(a * Math.PI / 180), 24.5 * Math.Sin(a * Math.PI / 180), 6.2);
        PB.Extrude(41, -3, true, "Avellanado_3xM3");
        PB.PartColor(0.95, 0.55, 0.1);
        PB.Prop("Descripcion", "Tapa de E2 que es el soporte del AS5600 (reemplaza a TE2). Gira con E2; el iman queda fijo en el soporte CB-232.");
        PB.Prop("Material", "PETG impreso");
        PB.Prop("Notas", "Coordenadas de E2. PCB al ras de la brida (z=41), chip hacia afuera sobre el eje J2. Pines del header hacia adentro por 2 ranuras; " +
            "los cables bajan por la espiga hueca D30 a la cavidad del motor y salen por la parte trasera de E2 junto con los del NEMA 14. 3 x M3x10 a E2, 4 x M3x6 para la placa.");
        PB.Report("TapaAS5600");
        PB.Save(P("03_Hombro_E2_J2", "CB-303_Tapa_E2_Soporte_AS5600.SLDPRT"));
        SW.CloseMine();
    }

    // Fixed magnet holder (replaces the sensor bracket). Geometry in CB-210 tower coordinates (J2 axis along Y at x=190.171, z=-86.723).
    static void SoporteIman()
    {
        PB.NewPart();
        double zc = -86.723, xa = 190.171;
        PB.Sketch('T'); PB.Rect(64, zc - 15, 100, zc + 15); PB.Extrude(175.2, 6.0, false, "Pie_torre");
        PB.Sketch('T'); PB.Rect(64, zc - 15, 78, zc + 15); PB.Extrude(92.2, 83.0, false, "Brazo");
        PB.Sketch('T'); PB.Rect(64, zc - 12, xa + 12, zc + 12); PB.Extrude(86.2, 6.0, false, "Placa");
        PB.Sketch('T'); PB.Circle(xa, zc, 12); PB.Extrude(92.2, 2.0, false, "Boss_iman_D12");
        PB.Sketch('T'); PB.Circle(xa, zc, 6.1); PB.Extrude(94.2, -2.5, true, "Alojamiento_iman_D6.1x2.5");
        PB.Sketch('T'); PB.Circle(82, zc - 9, 4.4); PB.Circle(82, zc + 9, 4.4); PB.Extrude(175.2, 6.0, true, "Agujeros_2xM4");
        PB.Sketch('F'); PB.Poly(78, 175.2, 92, 175.2, 78, 161.2); PB.Extrude(zc - 4, 8, false, "Nervio");
        PB.PartColor(0.95, 0.55, 0.1);
        PB.Prop("Descripcion", "Soporte fijo del iman del AS5600 del hombro J2 (el sensor va en la tapa de E2, CB-303)");
        PB.Prop("Material", "PETG impreso");
        PB.Prop("Notas", "Coordenadas de CB-210. Iman D6x2.5 diametral pegado en el boss, cara a 1.5 mm del chip. Sin cables en este soporte. 2 x M4x30 + tuerca a la torre. " +
            "Pasa por debajo del eje J2: E2 no puede apuntar mas de 55 grados hacia abajo.");
        PB.Report("SoporteIman");
        PB.Save(P("02_Cintura_J1", "CB-232_Soporte_Iman_J2.SLDPRT"));
        SW.CloseMine();
    }

    // Open electronics tray (replaces the BCN3D electronics box). Frame: origin at the tray bottom centre, +Y up, X along the tray (210 x 100).
    // Holds Arduino Mega + RAMPS 1.4 (+A4988) and Sabertooth 2x60.
    static void Bandeja()
    {
        PB.NewPart();
        PB.Sketch('T'); PB.RRect(-105, -50, 105, 50, 6); PB.Extrude(0, 4, false, "Piso_210x100x4");
        double[][] mega = { new[] { -37.16, -25.16 }, new[] { -38.31, 25.13 }, new[] { 40.99, -25.16 }, new[] { 47.52, 25.13 }, new[] { 15.68, 19.85 } };
        PB.Sketch('T'); foreach (var h in mega) PB.Circle(h[0] - 42.06, h[1] - 2, 7); PB.Extrude(4, 6, false, "Separadores_Mega_h6");
        double[][] sab = { new[] { 23.0, -41.3 }, new[] { 93.1, -41.3 }, new[] { 23.0, 41.3 }, new[] { 93.1, 41.3 } };
        PB.Sketch('T'); foreach (var h in sab) PB.Circle(h[0], h[1], 9); PB.Extrude(4, 5, false, "Separadores_Sabertooth_h5");
        PB.Sketch('T'); foreach (var h in mega) PB.Circle(h[0] - 42.06, h[1] - 2, 2.7); foreach (var h in sab) PB.Circle(h[0], h[1], 2.7);
        PB.Extrude(2, 8, true, "Pilotos_M3");
        PB.Sketch('T'); PB.Rect(-105, 47, 105, 50); PB.Extrude(4, 30, false, "Pared_trasera_h30");
        PB.Sketch('T'); PB.Rect(102, -50, 105, 50); PB.Extrude(4, 30, false, "Pared_lateral_h30");
        PB.Sketch('T'); PB.Rect(-60, -50, 105, -47); PB.Extrude(4, 12, false, "Pared_frontal_h12");
        PB.Sketch('F'); PB.Rect(-80, 19, -50, 35); PB.Rect(-20, 19, 10, 35); PB.Rect(40, 19, 80, 35); PB.Extrude(46, 5, true, "Ranuras_cables_traseras");
        PB.Sketch('R'); PB.Rect(-20, 19, 20, 35); PB.Extrude(101, 5, true, "Ranura_cables_motores");
        PB.Sketch('T'); for (int i = 0; i < 6; i++) PB.RRect(32 + i * 9, -30, 37 + i * 9, 30, 2.4); PB.Extrude(-1, 6, true, "Ventilacion");
        PB.Sketch('T'); PB.Circle(-98, -30, 4.5); PB.Circle(-98, 30, 4.5); PB.Circle(60, -40, 4.5); PB.Circle(60, 40, 4.5); PB.Extrude(-1, 6, true, "Agujeros_4xM4_tablero");
        PB.PartColor(0.2, 0.2, 0.22);
        PB.Prop("Descripcion", "Bandeja abierta de electronica: Arduino Mega 2560 + RAMPS 1.4 (+A4988) y Sabertooth 2x60 (reemplaza la caja del BCN3D)");
        PB.Prop("Material", "PETG impreso (210 x 100: entra en la Ender 3 V2)");
        PB.Prop("Notas", "Separadores con piloto D2.7 para M3 autorroscante. Lado del USB/jack del Mega abierto. Se fija al tablero con 4 x M4x30 + arandela + tuerca.");
        PB.Report("Bandeja");
        PB.Save(P("01_Base_J0", "CB-141_Bandeja_Electronica.SLDPRT"));
        SW.CloseMine();
    }
}
