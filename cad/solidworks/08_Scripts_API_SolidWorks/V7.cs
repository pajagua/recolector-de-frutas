using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// V7.exe <root> [hw|partsA|e2A|asmA|topA|partsB|e2B|asmB|topB ...]   (v7, 2026-10-27)
// Dos variantes del prismatico J3, mas cortas que la v6. El hombro (5840 en direct drive + AS5600) y la cintura no cambian.
//   A: una etapa. Tubo 30x30x2 L440 movido por una correa GT2 (lazo sobre el tubo); NEMA 17 arriba del cubo de E2.
//   B: telescopio de 2 etapas. Tubo intermedio 30x30x2 L275 (lo mueve la correa GT2 del NEMA) + tubo interior
//      20x20x1.5 L265 que avanza el doble gracias a una segunda correa anclada a E2 (multiplicador 2:1).
// Carrera 340 mm en las dos (brida en la misma posicion que la v6: la ventosa va de 227.8 a 567.8 mm del eje J2).
//
// Coordenadas de E2 (CB-301), igual que J3Largo.cs: eje J2 = eje Z en (x=-80, y=0), eje del prismatico = eje X,
// y hacia "arriba" del brazo, z = -35 cara de E2 contra la torre, z = +35 cara exterior (tapa del AS5600).
class V7
{
    static string R;
    const string DA = "10_v7A_Prismatico_Correa", DB = "11_v7B_Prismatico_Telescopico", COM = "05_Comerciales";
    static string P(string sub, string file) { return Path.Combine(R, sub, file); }
    static double[] V(params double[] v) { return v; }
    public const double Stroke = 340;
    const double XFront = 13, XFlIn = 15;

    // ---- A ----
    const double A_XMotor = -58, A_YFace = 47, A_XIdler = -442, A_YbPulley = 21, A_YcBelt = 25.5;
    const double A_XTubeRear = -425, A_TubeL = 440, A_XEnd = -457;
    // ---- B ----
    const double B_XMotor = -40, B_YFace = 50, B_XIdler = -252, B_YbPulley = 25.5, B_YcBelt = 30;
    const double B_XMidRear = -262, B_MidL = 275, B_XInRear = -250, B_InL = 265, B_XEnd = -284;
    const double B_XPulR = -267, B_XPulF = 6, B_YPul = 17.3;   // poleas 16T del multiplicador (eje paralelo a Z)
    const double B_XAnchor0 = -16, B_XAnchor1 = -8;             // anclaje de la correa 2:1 en E2

    static void Main(string[] args)
    {
        R = args[0];
        SW.Connect();
        SW.CloseMine();
        var which = args.Skip(1).ToList();
        Func<string, bool> want = k => which.Count == 0 || which.Contains(k);
        Directory.CreateDirectory(Path.Combine(R, DA)); Directory.CreateDirectory(Path.Combine(R, DB));
        if (want("hw")) { Polea20(); Loca20(); Loca16(); }
        if (want("placa")) PlacaNema();
        if (want("partsA")) { TuboA(); TaponA(); CorreaLazo(P(DA, "CB-441A_Correa_GT2_6mm_J3.SLDPRT"), A_XIdler, A_XMotor, A_YcBelt, "A"); }
        if (want("e2A")) E2A();
        if (want("asmA")) { Asm300("A"); Asm400A(); }
        if (want("asm300A")) Asm300("A");
        if (want("topA")) Top("A");
        if (want("fixB")) { TuboInterior(); BujeInterior(); }
        if (want("muesca")) MuescaMedio();
        if (want("rebajeA")) RebajeAFix();
        if (want("partsB")) { TuboMedio(); TaponMedio(); SoportePoleaDel(); BujeDel(); PinzaEtapa1(); TuboInterior(); BujeInterior(); BridaB();
                              CorreaLazo(P(DB, "CB-441B_Correa_GT2_6mm_Etapa1.SLDPRT"), B_XIdler, B_XMotor, B_YcBelt, "B"); Correa21(); }
        if (want("e2B")) E2B();
        if (want("asmB")) { Asm300("B"); Asm410B(); Asm400B(); }
        if (want("asm300B")) Asm300("B");
        if (want("topB")) Top("B");
    }

    // ================= comerciales (coordenadas locales: eje = +Y, y = 0 en la cara de la pestaña inferior) =================
    static void Cyl(double y0, double h, double d, string name) { PB.Sketch('T'); PB.Circle(0, 0, d); PB.Extrude(y0, h, false, name); }
    static void Polea20()
    {
        PB.NewPart();
        Cyl(0, 1, 16, "Pestana_inferior_D16"); Cyl(1, 7, 12.2, "Dientes_20T_D12.2"); Cyl(8, 1, 16, "Pestana_superior_D16"); Cyl(9, 7, 13, "Cubo_D13x7");
        PB.Sketch('T'); PB.Circle(0, 0, 5); PB.Extrude(-1, 18, true, "Agujero_D5");
        PB.PartColor(0.84, 0.85, 0.88);
        PB.Prop("Descripcion", "Polea GT2 20 dientes, agujero 5 mm, para correa de 6 mm (2 prisioneros M3 en el cubo)");
        PB.Prop("Notas", "Eje local +Y: pestana inferior y 0..1, dientes 1..8, pestana 8..9, cubo 9..16. Diametro primitivo 12.73 mm.");
        PB.Report("Polea GT2 20T"); PB.Save(P(COM, "HW_Polea_GT2_20T_D5.SLDPRT")); SW.CloseMine();
    }
    static void Loca20()
    {
        PB.NewPart();
        Cyl(0, 1, 18, "Pestana_inferior_D18"); Cyl(1, 7, 12.2, "Dientes_20T_D12.2"); Cyl(8, 1, 18, "Pestana_superior_D18");
        PB.Sketch('T'); PB.Circle(0, 0, 5); PB.Extrude(-1, 11, true, "Agujero_D5");
        PB.PartColor(0.84, 0.85, 0.88);
        PB.Prop("Descripcion", "Polea loca GT2 20 dientes con rodamientos, agujero 5 mm, correa de 6 mm");
        PB.Prop("Notas", "Eje local +Y, ancho 9 mm. Diametro primitivo 12.73 mm.");
        PB.Report("Polea loca 20T"); PB.Save(P(COM, "HW_Polea_Loca_GT2_20T_D5.SLDPRT")); SW.CloseMine();
    }
    static void Loca16()
    {
        PB.NewPart();
        Cyl(0, 1, 13.5, "Pestana_inferior_D13.5"); Cyl(1, 7, 9.7, "Dientes_16T_D9.7"); Cyl(8, 1, 13.5, "Pestana_superior_D13.5");
        PB.Sketch('T'); PB.Circle(0, 0, 3); PB.Extrude(-1, 11, true, "Agujero_D3");
        PB.PartColor(0.84, 0.85, 0.88);
        PB.Prop("Descripcion", "Polea loca GT2 16 dientes con rodamientos, agujero 3 mm, correa de 6 mm");
        PB.Prop("Notas", "Eje local +Y, ancho 9 mm. Diametro primitivo 10.19 mm.");
        PB.Report("Polea loca 16T"); PB.Save(P(COM, "HW_Polea_Loca_GT2_16T_D3.SLDPRT")); SW.CloseMine();
    }

        // placa soporte del NEMA 17 (coordenadas locales: apoyo en y = 0, espesor 5 hacia +y, centrada en el eje del motor)
    static void PlacaNema()
    {
        PB.NewPart();
        PB.Sketch('T'); PB.RRect(-31, -31, 31, 31, 4); PB.Extrude(0, 5, false, "Placa_62x62x5");
        PB.Sketch('T'); PB.Circle(0, 0, 23); PB.Extrude(-1, 7, true, "Paso_buje_D23");
        PB.Sketch('T'); foreach (double a in new[] { -15.5, 15.5 }) foreach (double b in new[] { -15.5, 15.5 }) PB.Circle(a, b, 3.4);
        PB.Extrude(-1, 7, true, "Agujeros_4xM3_motor");
        PB.Sketch('T'); foreach (double a in new[] { -15.5, 15.5 }) foreach (double b in new[] { -15.5, 15.5 }) PB.Circle(a, b, 6.5);
        PB.Extrude(-1, 4, true, "Cajeado_cabezas_M3_abajo");
        PB.Sketch('T'); foreach (double a in new[] { -25.5, 25.5 }) foreach (double b in new[] { -25.5, 25.5 }) PB.Circle(a, b, 3.4);
        PB.Extrude(-1, 7, true, "Agujeros_4xM3_a_E2");
        PB.PartColor(0.95, 0.55, 0.1);
        PB.Prop("Descripcion", "Placa soporte del NEMA 17 del prismatico sobre el cubo de E2 (v7A y v7B)");
        PB.Prop("Material", "PETG impreso, 100 % de relleno");
        PB.Prop("Notas", "El motor se atornilla primero a la placa (4 x M3x6, cabezas en el cajeado de abajo). Despues la placa va a E2 con 4 x M3x10 en insertos termicos M3 (agujeros D4.2 de 7 mm en E2).");
        PB.Report("Placa NEMA"); PB.Save(P("03_Hombro_E2_J2", "CB-306_Placa_Soporte_NEMA17_Prismatico.SLDPRT")); SW.CloseMine();
    }

    // ================= correas (coordenadas de E2, dibujadas con J3 retraido) =================
    // lazo cerrado con poleas de eje vertical (Y): tramos rectos en z = +-(6.2..7.6), ancho 6 en y
    static void Racetrack(double x1, double x2, double r)
    {
        PB.Line(x1, -r, x2, -r); PB.Arc(x2, 0, x2, -r, x2, r); PB.Line(x2, r, x1, r); PB.Arc(x1, 0, x1, r, x1, -r);
    }
    static void CorreaLazo(string path, double x1, double x2, double yc, string v)
    {
        PB.NewPart();
        PB.Sketch('T'); Racetrack(x1, x2, 7.6); Racetrack(x1, x2, 6.2);
        PB.Extrude(yc - 3, 6, false, "Correa_GT2_6mm");
        PB.PartColor(0.08, 0.08, 0.09);
        double L = 2 * (x2 - x1) + Math.PI * 12.73;
        PB.Prop("Descripcion", "Correa GT2 de 6 mm, abierta, " + Math.Ceiling(L / 10) * 10 + " mm (lazo de " + L.ToString("0") + " mm entre centros, extremos en la pinza)");
        PB.Prop("Notas", "Coordenadas de E2, J3 retraido. Variante " + v + ". Dibujo simplificado (sin dientes).");
        PB.Report("Correa " + v); PB.Save(path); SW.CloseMine();
    }
    static void HalfRing(double cx, double cy, double r1, double r2, bool front)
    {
        // media corona alrededor de (cx, cy) en el plano XY: lado -x (atras) o +x (adelante)
        if (!front)
        {
            PB.Arc(cx, cy, cx, cy + r2, cx, cy - r2); PB.Line(cx, cy - r2, cx, cy - r1);
            PB.Arc(cx, cy, cx, cy + r1, cx, cy - r1); PB.Line(cx, cy + r1, cx, cy + r2);
        }
        else
        {
            PB.Arc(cx, cy, cx, cy - r2, cx, cy + r2); PB.Line(cx, cy + r2, cx, cy + r1);
            PB.Arc(cx, cy, cx, cy - r1, cx, cy + r1); PB.Line(cx, cy - r1, cx, cy - r2);
        }
    }
    static void Correa21()
    {
        double r1 = 4.95, r2 = 6.35, yT0 = B_YPul + r1, yT1 = B_YPul + r2, yB0 = B_YPul - r2, yB1 = B_YPul - r1;
        PB.NewPart();
        PB.Sketch('F'); PB.Rect(B_XPulR, yT0, B_XAnchor0, yT1); PB.Extrude(-3, 6, false, "Tramo_superior_trasero");
        PB.Sketch('F'); PB.Rect(B_XAnchor1, yT0, B_XPulF, yT1); PB.Extrude(-3, 6, false, "Tramo_superior_delantero");
        PB.Sketch('F'); PB.Rect(B_XPulR, yB0, B_XPulF, yB1); PB.Extrude(-3, 6, false, "Tramo_inferior_al_tubo_interior");
        PB.Sketch('F'); HalfRing(B_XPulR, B_YPul, r1, r2, false); PB.Extrude(-3, 6, false, "Vuelta_polea_trasera");
        PB.Sketch('F'); HalfRing(B_XPulF, B_YPul, r1, r2, true); PB.Extrude(-3, 6, false, "Vuelta_polea_delantera");
        PB.PartColor(0.08, 0.08, 0.09);
        double L = (B_XAnchor0 - B_XPulR) + (B_XPulF - B_XAnchor1) + (B_XPulF - B_XPulR) + Math.PI * 10.19;
        PB.Prop("Descripcion", "Correa GT2 de 6 mm del multiplicador 2:1, abierta, ~" + Math.Ceiling(L / 10) * 10 + " mm: los dos extremos van al anclaje de E2 y el tramo inferior a la pinza del tubo interior");
        PB.Prop("Notas", "Coordenadas de E2, dibujada con J3 retraido (en el CAD no acompana el movimiento). Poleas 16T en el tubo intermedio.");
        PB.Report("Correa 2:1"); PB.Save(P(DB, "CB-442B_Correa_GT2_6mm_Multiplicador.SLDPRT")); SW.CloseMine();
    }

    // ================= A: piezas =================
    static void Tubo(double zy, double t, double x0, double L, string name)
    {
        PB.Sketch('R'); PB.Rect(-zy, -zy, zy, zy); PB.Rect(-zy + t, -zy + t, zy - t, zy - t); PB.Extrude(x0, L, false, name);
    }
    static void TuboA()
    {
        PB.NewPart();
        Tubo(15, 2, A_XTubeRear, A_TubeL, "Tubo_30x30x2_L440");
        PB.Sketch('T'); PB.Circle(XFlIn - 10, 0, 3.2); PB.Circle(A_XTubeRear + 10, 0, 3.2); PB.Extrude(-16, 32, true, "Agujeros_M3_tapones");
        PB.PartColor(0.84, 0.85, 0.88);
        PB.Prop("Descripcion", "Eslabon E3 (v7A): tubo cuadrado de aluminio 30 x 30 x 2, largo 440 mm");
        PB.Prop("Material", "Aluminio 6063");
        PB.Prop("Notas", "Coordenadas de E2, J3 retraido: x -425..15. Desliza en el canal 31 x 31 de E2. 2 agujeros D3.2: brida CB-402 (adelante) y tapon CB-422A (atras).");
        PB.Report("Tubo A"); PB.Save(P(DA, "CB-421A_Tubo_E3_Aluminio_30x30x2_L440.SLDPRT")); SW.CloseMine();
    }
    static void TaponA()
    {
        double x0 = A_XTubeRear;
        PB.NewPart();
        PB.Sketch('R'); PB.Rect(-12.9, -12.9, 12.9, 12.9); PB.Extrude(x0, 20, false, "Tapon_25.8x20");
        PB.Sketch('R'); PB.Rect(-15, -15, 15, 15); PB.Extrude(x0 - 5, 5, false, "Tapa_30x30x5");
        PB.Sketch('R'); PB.Rect(2, 15, 11, 30); PB.Extrude(x0 - 5, 18, false, "Pinza_correa");
        PB.Sketch('R'); PB.Rect(6.0, 21.5, 7.8, 30.5); PB.Extrude(x0 - 6, 20, true, "Ranura_correa_1.8");
        PB.Sketch('T'); PB.Circle(x0 + 10, 0, 3.2); PB.Extrude(-16, 32, true, "Agujero_M3_tubo");
        PB.PartColor(0.95, 0.55, 0.1);
        PB.Prop("Descripcion", "Tapon trasero del tubo de E3 (v7A) con la pinza de la correa GT2");
        PB.Prop("Material", "PETG impreso, 100 % de relleno");
        PB.Prop("Notas", "Coordenadas de E2, J3 retraido. Pegado con epoxi + 1 M3 pasante. Los dos extremos de la correa entran en la ranura de 1.8 mm con los dientes enfrentados y se aseguran con un precinto.");
        PB.Report("Tapon A"); PB.Save(P(DA, "CB-422A_Tapon_Trasero_Pinza_Correa.SLDPRT")); SW.CloseMine();
    }

    // ================= E2 modificado (copia de CB-301 v6) =================
    static ModelDoc2 CopyOpen(string src, string dst)
    {
        if (File.Exists(dst)) throw new Exception("ya existe " + dst + " (borrarlo a mano para regenerar)");
        File.Copy(src, dst);
        File.SetAttributes(dst, FileAttributes.Normal);
        return SW.Open(dst);
    }
    static void SavePart(ModelDoc2 m)
    {
        int e = 0, w = 0;
        m.ForceRebuild3(false);
        bool ok = m.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w);
        Console.WriteLine((ok ? "saved " : "SAVE FAIL ") + m.GetPathName() + " e=" + e + " w=" + w);
    }
    // la base de E2 termina 5 mm debajo de la cara del motor: ahi va la placa soporte CB-306 (motor atornillado en la mesa)
    static void MotorHoles(double xm, double y0, double yFace)
    {
        double top = yFace - 5;
        PB.Sketch('T'); PB.Circle(xm, 0, 8); PB.Extrude(y0, top - y0 + 1, true, "CB_v7_Paso_eje_NEMA17_D8");
        PB.Sketch('T'); foreach (double a in new[] { -25.5, 25.5 }) foreach (double b in new[] { -25.5, 25.5 }) PB.Circle(xm + a, b, 4.2);
        PB.Extrude(top - 7, 8, true, "CB_v7_Insertos_4xM3_placa_NEMA17");
    }
    static void E2A()
    {
        var m = CopyOpen(P("03_Hombro_E2_J2", "CB-301_Eslabon_E2_mod5840.SLDPRT"), P(DA, "CB-301A_Eslabon_E2_v7A_Correa.SLDPRT"));
        PB.Use(m); PB.Report("E2 v6");
        PB.Sketch('F'); PB.Rect(-560, -80, A_XEnd, 80); PB.Extrude(-80, 160, true, "CB_v7A_Corte_cola_u377");
        PB.Sketch('R'); PB.Rect(-27.5, -27.5, 27.5, 36); PB.Extrude(A_XEnd, 5, false, "CB_v7A_Pared_trasera");
        PB.Sketch('R'); PB.Rect(-27.5, 27.5, 27.5, 36); PB.Extrude(A_XEnd + 5, -100 - (A_XEnd + 5), false, "CB_v7A_Cola_alta_y36");
        PB.Sketch('T'); PB.Rect(A_XMotor - 31, -31, A_XMotor + 31, 31); PB.Extrude(30, A_YFace - 5 - 30, false, "CB_v7A_Base_placa_NEMA17");
        RebajeA();
        PB.Sketch('R'); PB.Rect(-11, 15.5, 11, 31); PB.Extrude(A_XEnd + 5, (A_XMotor + 12) - (A_XEnd + 5), true, "CB_v7A_Ranura_correa_22x15.5");
        PB.Sketch('T'); PB.Rect(A_XMotor - 18, -18, A_XMotor + 18, 18); PB.Extrude(31, 10, true, "CB_v7A_Bolsillo_polea");
        MotorHoles(A_XMotor, 40, A_YFace);
        PB.Sketch('T'); PB.Circle(A_XIdler, 0, 5.3); PB.Extrude(-30, 70, true, "CB_v7A_Eje_polea_loca_M5");
        PB.Sketch('T'); PB.Rect(A_XEnd + 10, 18.5, A_XEnd + 20, 28); PB.Extrude(-5, 10, true, "CB_v7A_Salida_cables_AS5600");
        PB.Report("E2 v7A");
        PB.Prop("Descripcion", "Eslabon E2 (v7A): cubo del hombro (5840 + AS5600, sin cambios) + cola corta con canal 31 x 31 y ranura para la correa GT2");
        PB.Prop("Modificaciones_CB", "Sobre la v6: cola cortada a 377 mm detras del eje J2 con pared trasera de 5 mm; techo de la cola subido a y = 36; " +
            "ranura 22 x 15.5 sobre el canal para el lazo de la correa; base de la placa del NEMA 17 sobre el cubo (y = 42, 4 insertos M3; cara del motor a y = 47, eje a 22 mm delante de J2) con bolsillo para la polea; " +
            "agujero de la polea loca (M5 vertical) a 362 mm detras de J2; salida del tunel de cables del AS5600 movida.");
        PB.Prop("Material", "PETG impreso (420 mm: partir en 2 tramos)");
        SavePart(m); SW.CloseMine();
    }
    static void E2B()
    {
        var m = CopyOpen(P("03_Hombro_E2_J2", "CB-301_Eslabon_E2_mod5840.SLDPRT"), P(DB, "CB-301B_Eslabon_E2_v7B_Telescopico.SLDPRT"));
        PB.Use(m); PB.Report("E2 v6");
        PB.Sketch('F'); PB.Rect(-560, -80, B_XEnd, 80); PB.Extrude(-80, 160, true, "CB_v7B_Corte_cola_u204");
        PB.Sketch('R'); PB.Rect(-27.5, -27.5, 27.5, 40.5); PB.Extrude(B_XEnd, 5, false, "CB_v7B_Pared_trasera");
        PB.Sketch('R'); PB.Rect(-27.5, 27.5, 27.5, 40.5); PB.Extrude(B_XEnd + 5, -100 - (B_XEnd + 5), false, "CB_v7B_Cola_alta_y40.5");
        PB.Sketch('T'); PB.Rect(B_XMotor - 31, -31, B_XMotor + 31, 31); PB.Extrude(30, B_YFace - 5 - 30, false, "CB_v7B_Base_placa_NEMA17");
        PB.Sketch('R'); PB.Rect(-11, 15.5, 11, 35.5); PB.Extrude(B_XEnd + 5, 40 - (B_XEnd + 5), true, "CB_v7B_Ranura_correas_22x20");
        PB.Sketch('R'); PB.Rect(-4, 24, 4, 35.6); PB.Extrude(B_XAnchor0, B_XAnchor1 - B_XAnchor0, false, "CB_v7B_Anclaje_correa_2a1");
        PB.Sketch('T'); PB.Circle((B_XAnchor0 + B_XAnchor1) / 2, 0, 2.5); PB.Extrude(23, 9, true, "CB_v7B_Piloto_M3_anclaje");
        PB.Sketch('T'); PB.Rect(B_XMotor - 18, -18, B_XMotor + 18, 18); PB.Extrude(35.5, 8.5, true, "CB_v7B_Bolsillo_polea");
        MotorHoles(B_XMotor, 43, B_YFace);
        PB.Sketch('T'); PB.Circle(B_XIdler, 0, 5); PB.Extrude(35, 6, true, "CB_v7B_Pasador_polea_loca_D5");
        PB.Sketch('T'); PB.Rect(B_XEnd + 10, 18.5, B_XEnd + 20, 28); PB.Extrude(-5, 10, true, "CB_v7B_Salida_cables_AS5600");
        PB.Report("E2 v7B");
        PB.Prop("Descripcion", "Eslabon E2 (v7B): cubo del hombro (5840 + AS5600, sin cambios) + manga corta del telescopio con ranura para las dos correas GT2");
        PB.Prop("Modificaciones_CB", "Sobre la v6: cola cortada a 204 mm detras del eje J2 con pared trasera de 5 mm; techo subido a y = 40.5; " +
            "ranura 22 x 20 sobre el canal 31 x 31, abierta adelante (correa de la etapa 1, poleas y correa del multiplicador); " +
            "anclaje de la correa 2:1 a 72 mm delante de J2; base de la placa del NEMA 17 sobre el cubo (y = 45, 4 insertos M3; cara del motor a y = 50, eje a 40 mm delante de J2); " +
            "pasador D5 de la polea loca a 172 mm detras de J2; salida del tunel de cables del AS5600 movida.");
        PB.Prop("Material", "PETG impreso (297 mm: entra en la Ender 3 V2 en diagonal o en 2 tramos)");
        SavePart(m); SW.CloseMine();
    }

    // agrega la muesca trasera al tubo intermedio ya existente (conserva las referencias de los ensamblajes)
    static void MuescaMedio()
    {
        var m = SW.Open(P(DB, "CB-411B_Tubo_Intermedio_30x30x2_L275.SLDPRT"));
        PB.Use(m);
        if (((PartDoc)m).FeatureByName("Muesca_polea_trasera") != null) { Console.WriteLine("ya tiene la muesca"); SW.App.CloseDoc(m.GetTitle()); return; }
        PB.Sketch('T'); PB.Rect(B_XMidRear - 0.5, -4.7, B_XMidRear + 3, 4.7); PB.Extrude(12.5, 3, true, "Muesca_polea_trasera");
        PB.Prop("Notas", "Coordenadas de E2, J3 retraido: x -262..13. Carrera propia 170 mm (la mitad de J3). Ventana de 14.5 x 9.4 en la cara superior para la polea 16T delantera y muesca de 3 x 9.4 atras para la trasera. " +
            "4 agujeros D2.5 roscados M3 en la cara superior (soporte de polea y pinza de la etapa 1): tornillos al ras por dentro. Agujero D3.2 lateral atras: pasador D3 x 30 al ras.");
        PB.Report("Tubo medio");
        SavePart(m); SW.App.CloseDoc(m.GetTitle());
    }

    // el cubo del Rx llega a y = 45..46.7: se rebaja a y = 42 bajo la placa del motor (variante A)
    static void RebajeA()
    {
        PB.Sketch('T'); PB.Rect(A_XMotor - 31, -31, A_XMotor + 31, 31); PB.Extrude(A_YFace - 5, 20, true, "CB_v7A_Rebaje_cubo_y42");
    }
    static void RebajeAFix()
    {
        var m = SW.Open(P(DA, "CB-301A_Eslabon_E2_v7A_Correa.SLDPRT"));
        PB.Use(m);
        if (((PartDoc)m).FeatureByName("CB_v7A_Rebaje_cubo_y42") != null) { Console.WriteLine("ya tiene el rebaje"); SW.App.CloseDoc(m.GetTitle()); return; }
        RebajeA(); PB.Report("E2 v7A");
        SavePart(m); SW.App.CloseDoc(m.GetTitle());
    }

    // ================= B: piezas =================
    static void TuboMedio()
    {
        PB.NewPart();
        Tubo(15, 2, B_XMidRear, B_MidL, "Tubo_30x30x2_L275");
        PB.Sketch('T'); PB.Rect(XFront - 14, -4.7, XFront + 0.5, 4.7); PB.Extrude(12.5, 3, true, "Ventana_polea_delantera");
        PB.Sketch('T'); PB.Rect(B_XMidRear - 0.5, -4.7, B_XMidRear + 3, 4.7); PB.Extrude(12.5, 3, true, "Muesca_polea_trasera");
        PB.Sketch('F'); PB.Circle(B_XMidRear + 6, 0, 3.2); PB.Extrude(-16, 32, true, "Agujero_M3_tapon_trasero");
        PB.Sketch('T'); PB.Circle(XFront - 23, 0, 2.5); PB.Circle(XFront - 17, 0, 2.5);
        PB.Circle(B_XMidRear + 20, 7.2, 2.5); PB.Circle(B_XMidRear + 25, 7.2, 2.5); PB.Extrude(12.5, 3, true, "Agujeros_M3_roscados_soportes");
        PB.PartColor(0.70, 0.74, 0.80);
        PB.Prop("Descripcion", "Tramo intermedio del telescopio J3 (v7B): tubo cuadrado de aluminio 30 x 30 x 2, largo 275 mm");
        PB.Prop("Material", "Aluminio 6063");
        PB.Prop("Notas", "Coordenadas de E2, J3 retraido: x -262..13. Carrera propia 170 mm (la mitad de J3). Ventana de 14.5 x 9.4 en la cara superior para la polea 16T delantera y muesca de 3 x 9.4 atras para la trasera. " +
            "4 agujeros D2.5 roscados M3 en la cara superior (soporte de polea y pinza de la etapa 1): tornillos al ras por dentro.");
        PB.Report("Tubo medio"); PB.Save(P(DB, "CB-411B_Tubo_Intermedio_30x30x2_L275.SLDPRT")); SW.CloseMine();
    }
    static void TaponMedio()
    {
        double x0 = B_XMidRear;
        PB.NewPart();
        PB.Sketch('R'); PB.Rect(-12.9, -12.9, 12.9, 12.9); PB.Extrude(x0, 12, false, "Tapon_25.8x12");
        PB.Sketch('R'); PB.Rect(-15, -15, 15, 15); PB.Extrude(x0 - 14, 14, false, "Cuerpo_30x30");
        PB.Sketch('R'); PB.Rect(5, 15, 10.5, 21.5); PB.Rect(-10.5, 15, -5, 21.5); PB.Extrude(x0 - 14, 14, false, "Orejas_eje_polea");
        PB.Sketch('R'); PB.Rect(-4, 9.5, 4, 13.5); PB.Extrude(x0 - 1, 14, true, "Paso_correa_inferior");
        PB.Sketch('F'); PB.Rect(x0 - 13, 9, x0 + 2.5, 30); PB.Extrude(-4.8, 9.6, true, "Alojamiento_polea_16T");
        PB.Sketch('F'); PB.Circle(B_XPulR, B_YPul, 3.2); PB.Extrude(-12, 24, true, "Eje_M3_polea");
        PB.Sketch('F'); PB.Circle(x0 + 6, 0, 3.2); PB.Extrude(-16, 32, true, "Agujero_M3_tubo");
        PB.PartColor(0.95, 0.55, 0.1);
        PB.Prop("Descripcion", "Tapon trasero del tramo intermedio (v7B): lleva la polea 16T trasera del multiplicador 2:1");
        PB.Prop("Material", "PETG impreso, 100 % de relleno");
        PB.Prop("Notas", "Coordenadas de E2, J3 retraido. Pegado en el tubo + 1 M3 pasante (no estorba: el tubo interior nunca llega aca). Eje M3x25 + tuerca autoblocante.");
        PB.Report("Tapon medio"); PB.Save(P(DB, "CB-412B_Tapon_Trasero_Intermedio_Polea.SLDPRT")); SW.CloseMine();
    }
    static void SoportePoleaDel()
    {
        PB.NewPart();
        PB.Sketch('T'); PB.Rect(XFront - 26, -7.8, XFront - 14, 7.8); PB.Extrude(15, 2, false, "Base");
        PB.Sketch('F'); PB.Rect(XFront - 14, 15, XFront, 24); PB.Extrude(4.8, 3, false, "Oreja_z+");
        PB.Sketch('F'); PB.Rect(XFront - 14, 15, XFront, 24); PB.Extrude(-7.8, 3, false, "Oreja_z-");
        PB.Sketch('F'); PB.Circle(B_XPulF, B_YPul, 3.2); PB.Extrude(-9, 18, true, "Eje_M3_polea");
        PB.Sketch('T'); PB.Circle(XFront - 23, 0, 3.4); PB.Circle(XFront - 17, 0, 3.4); PB.Extrude(14, 4, true, "Agujeros_M3");
        PB.PartColor(0.95, 0.55, 0.1);
        PB.Prop("Descripcion", "Soporte de la polea 16T delantera del multiplicador 2:1 (v7B), sobre el tramo intermedio");
        PB.Prop("Material", "PETG impreso, 100 % de relleno");
        PB.Prop("Notas", "Coordenadas de E2, J3 retraido. 2 x M3x6 en la cara superior del tubo (roscados, al ras por dentro) + epoxi. Eje M3x20 + tuerca autoblocante.");
        PB.Report("Soporte polea del"); PB.Save(P(DB, "CB-413B_Soporte_Polea_Delantera.SLDPRT")); SW.CloseMine();
    }
    static void BujeDel()
    {
        PB.NewPart();
        PB.Sketch('R'); PB.Poly(-13, -13, 13, -13, 13, 13, 10.2, 13, 10.2, -10.2, -10.2, -10.2, -10.2, 13, -13, 13);
        PB.Extrude(XFront - 13, 13, false, "Buje_U_20.4");
        PB.PartColor(0.95, 0.95, 0.95);
        PB.Prop("Descripcion", "Buje delantero del tramo intermedio (v7B): guia el tubo interior 20 x 20 (abierto arriba para la correa)");
        PB.Prop("Material", "PETG impreso o PTFE/UHMW mecanizado");
        PB.Prop("Notas", "Coordenadas de E2. Pegado dentro del frente del tubo 30 x 30 x 2. Juego 0.2 mm por lado con el tubo interior.");
        PB.Report("Buje del"); PB.Save(P(DB, "CB-414B_Buje_Delantero_Intermedio.SLDPRT")); SW.CloseMine();
    }
    static void PinzaEtapa1()
    {
        double x0 = B_XMidRear + 17;   // base x -245..-222, mordazas x -233..-222
        PB.NewPart();
        PB.Sketch('R'); PB.Rect(3.8, 15, 10.5, 18); PB.Extrude(x0, 23, false, "Base");
        PB.Sketch('R'); PB.Rect(3.8, 15, 10.5, 34.5); PB.Extrude(x0 + 12, 11, false, "Mordazas");
        PB.Sketch('R'); PB.Rect(6.0, 26, 7.8, 35); PB.Extrude(x0 + 11, 13, true, "Ranura_correa_1.8");
        PB.Sketch('T'); PB.Circle(B_XMidRear + 20, 7.2, 3.4); PB.Circle(B_XMidRear + 25, 7.2, 3.4); PB.Extrude(14, 5, true, "Agujeros_M3");
        PB.PartColor(0.95, 0.55, 0.1);
        PB.Prop("Descripcion", "Pinza de la correa de la etapa 1 (v7B): une el tramo intermedio con la correa del NEMA 17");
        PB.Prop("Material", "PETG impreso, 100 % de relleno");
        PB.Prop("Notas", "Coordenadas de E2, J3 retraido. 2 x M3x6 en la cara superior del tubo (roscados, al ras por dentro) + epoxi. Extremos de la correa en la ranura de 1.8 mm + precinto.");
        PB.Report("Pinza 1"); PB.Save(P(DB, "CB-415B_Pinza_Correa_Etapa1.SLDPRT")); SW.CloseMine();
    }
    static void TuboInterior()
    {
        PB.NewPart();
        Tubo(10, 1.5, B_XInRear + 2, B_InL - 2, "Tubo_20x20x1.5_L263");
        PB.Sketch('F'); PB.Circle(XFlIn - 10, 0, 3.2); PB.Circle(B_XInRear + 10, 0, 3.2); PB.Extrude(-11, 22, true, "Agujeros_M3_laterales");
        PB.PartColor(0.84, 0.85, 0.88);
        PB.Prop("Descripcion", "Eslabon E3 (v7B): tubo interior del telescopio, aluminio 20 x 20 x 1.5, largo 263 mm");
        PB.Prop("Material", "Aluminio 6063");
        PB.Prop("Notas", "Coordenadas de E2, J3 retraido: x -248..15. Agujeros laterales (eje z) para que la correa pase libre por arriba.");
        PB.Report("Tubo interior"); PB.Save(P(DB, "CB-421B_Tubo_Interior_20x20x1.5_L263.SLDPRT")); SW.CloseMine();
    }
    static void BujeInterior()
    {
        double x0 = B_XInRear;
        PB.NewPart();
        PB.Sketch('R'); PB.Rect(-12.8, -12.8, 12.8, 12.8); PB.Extrude(x0, 2, false, "Placa_trasera_2mm");
        PB.Sketch('R'); PB.Rect(-8.4, -8.4, 8.4, 8.4); PB.Extrude(x0 + 2, 16, false, "Tapon_16.8");
        PB.Sketch('R'); PB.Rect(-12.8, -12.8, 12.8, 12.8); PB.Rect(-10, -10, 10, 10); PB.Extrude(x0 + 2, 13, false, "Patines_25.6");
        PB.Sketch('R'); PB.Rect(-4.5, 9.9, 4.5, 13); PB.Extrude(x0 - 3, 19, true, "Canal_correa");
        PB.Sketch('F'); PB.Circle(x0 + 10, 0, 3.2); PB.Extrude(-14, 28, true, "Agujero_M3_tubo");
        PB.PartColor(0.95, 0.95, 0.95);
        PB.Prop("Descripcion", "Buje trasero del tubo interior (v7B): patines que deslizan en el intermedio + pinza del tramo inferior de la correa 2:1");
        PB.Prop("Material", "PETG impreso, 100 % de relleno (o UHMW)");
        PB.Prop("Notas", "Coordenadas de E2, J3 retraido. La correa pasa por el canal de 9 mm y se fija al tubo con una placa dentada y 2 x M2x6 (no modelados). M3 lateral pasante.");
        PB.Report("Buje interior"); PB.Save(P(DB, "CB-422B_Buje_Trasero_Interior_Pinza_2a1.SLDPRT")); SW.CloseMine();
    }
    static void BridaB()
    {
        PB.NewPart();
        PB.Sketch('R'); PB.RRect(-40, -40, 40, 40, 6); PB.Extrude(XFlIn, 10, false, "Placa_80x80x10");
        PB.Sketch('R'); PB.Rect(-8.4, -8.4, 8.4, 8.4); PB.Extrude(XFlIn - 20, 20, false, "Tapon_16.8_en_el_tubo");
        PB.Sketch('R'); PB.Circle(0, 28.5, 5.4); PB.Circle(0, -28.5, 5.4); PB.Circle(28.5, 0, 5.4); PB.Circle(-28.5, 0, 5.4);
        PB.Extrude(XFlIn - 1, 12, true, "Agujeros_4xM5_rombo_r28.5");
        PB.Sketch('F'); PB.Circle(XFlIn - 10, 0, 3.2); PB.Extrude(-11, 22, true, "Agujero_M3_tubo");
        PB.PartColor(0.95, 0.55, 0.1);
        PB.Prop("Descripcion", "Brida del extremo de E3 (v7B) para la herramienta, con tapon para el tubo 20 x 20 x 1.5");
        PB.Prop("Material", "PETG impreso, 100 % de relleno en el tapon");
        PB.Prop("Notas", "Igual que CB-402 (cara exterior a 105 mm del eje J2 con J3 retraido) pero con tapon de 16.8 y M3 lateral.");
        PB.Report("Brida B"); PB.Save(P(DB, "CB-432B_Brida_E3_Herramienta_20x20.SLDPRT")); SW.CloseMine();
    }

    // ================= ensamblajes =================
    static Component2 Add(ModelDoc2 m, string path, Xf x, bool fix = true)
    {
        var a = (AssemblyDoc)m;
        int e = 0, w = 0;
        int type = path.ToUpper().EndsWith(".SLDASM") ? (int)swDocumentTypes_e.swDocASSEMBLY : (int)swDocumentTypes_e.swDocPART;
        SW.App.DocumentVisible(false, type);
        SW.App.OpenDoc6(path, type, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref e, ref w);
        SW.App.DocumentVisible(true, type);
        var c = (Component2)a.AddComponent5(path, (int)swAddComponentConfigOptions_e.swAddComponentConfigOptions_CurrentSelectedConfig, "", false, "", 0, 0, 0);
        if (c == null) throw new Exception("AddComponent failed " + path);
        c.Transform2 = x.ToMath();
        if (fix) { m.ClearSelection2(true); c.Select4(false, null, false); a.FixComponent(); m.ClearSelection2(true); }
        return c;
    }
    static void Del(ModelDoc2 m, Component2 c)
    {
        string n = c.Name2;
        m.ClearSelection2(true); c.Select4(false, null, false);
        bool ok = m.Extension.DeleteSelection2(0);
        Console.WriteLine("  borrado " + n + " " + ok);
        m.ClearSelection2(true);
    }
    static Component2[] Comps(ModelDoc2 m) { return ((object[])((AssemblyDoc)m).GetComponents(true)).Cast<Component2>().ToArray(); }
    static void SaveAsm(ModelDoc2 m)
    {
        m.ForceRebuild3(false);
        int e = 0, w = 0;
        bool ok = m.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w);
        Console.WriteLine((ok ? "saved " : "SAVE FAIL ") + m.GetPathName() + " e=" + e + " w=" + w);
    }
    static void Replace(ModelDoc2 m, Component2 c, string path)
    {
        var a = (AssemblyDoc)m;
        m.ClearSelection2(true); c.Select4(false, null, false);
        bool ok = a.ReplaceComponents2(path, "", false, 0, true);
        Console.WriteLine("  reemplazo " + c.Name2 + " -> " + Path.GetFileName(path) + " " + ok);
        m.ClearSelection2(true);
    }
    static void Desc(ModelDoc2 m, string d)
    {
        m.Extension.CustomPropertyManager[""].Add3("Descripcion", (int)swCustomInfoType_e.swCustomInfoText, d, 2);
    }
    // NEMA 17 (CB-505, modelado con la cara en x = -491.5 y el eje hacia +x) con la cara en y = yFace mirando hacia -y
    static Xf NemaVertical(double xc, double yFace) { return Xf.Rows(V(0, -1, 0), V(1, 0, 0), V(0, 0, 1), V(xc, yFace - 491.5, 0)); }
    // polea de eje local Y -> eje Z de E2, centrada en z = 0
    static Xf EjeZ(double xc, double yc) { return Xf.Rows(V(1, 0, 0), V(0, 0, 1), V(0, -1, 0), V(xc, yc, -4.5)); }

    static void Asm300(string v)
    {
        string dir = v == "A" ? DA : DB;
        string dst = P(dir, "CB-300" + v + "_Hombro_E2_v7" + v + ".SLDASM");
        if (File.Exists(dst)) throw new Exception("ya existe " + dst);
        File.Copy(P("03_Hombro_E2_J2", "CB-300_Hombro_E2_J2.SLDASM"), dst); File.SetAttributes(dst, FileAttributes.Normal);
        var m = SW.Open(dst);
        ((AssemblyDoc)m).ResolveAllLightWeightComponents(false);
        var e2c = Comps(m).First(c => c.Name2.StartsWith("CB-301"));
        var e2 = Xf.FromComp(e2c);
        Replace(m, e2c, P(dir, v == "A" ? "CB-301A_Eslabon_E2_v7A_Correa.SLDPRT" : "CB-301B_Eslabon_E2_v7B_Telescopico.SLDPRT"));
        var inv = e2.Inv();
        foreach (var c in Comps(m))
        {
            string n = c.Name2;
            if (n.StartsWith("CB-505") || n.StartsWith("CB-513") || n.StartsWith("CB-511")) Del(m, c);
            else if (n.StartsWith("HW_M3x6") && inv.Apply(Xf.FromComp(c).T)[0] < -400) Del(m, c);   // tornillos del NEMA 17 de la v6
        }
        double xm = v == "A" ? A_XMotor : B_XMotor, yf = v == "A" ? A_YFace : B_YFace, xi = v == "A" ? A_XIdler : B_XIdler, yb = v == "A" ? A_YbPulley : B_YbPulley;
        Add(m, P("03_Hombro_E2_J2", "CB-306_Placa_Soporte_NEMA17_Prismatico.SLDPRT"), Xf.Tr(xm, yf - 5, 0).Then(e2));
        Add(m, P(COM, "CB-505_Motor_NEMA17_Prismatico.SLDPRT"), NemaVertical(xm, yf).Then(e2));
        Add(m, P(COM, "HW_Polea_GT2_20T_D5.SLDPRT"), Xf.Tr(xm, yb, 0).Then(e2));
        Add(m, P(COM, "HW_Polea_Loca_GT2_20T_D5.SLDPRT"), Xf.Tr(xi, yb, 0).Then(e2));
        if (v == "A") Add(m, P(DA, "CB-441A_Correa_GT2_6mm_J3.SLDPRT"), e2);
        else { Add(m, P(DB, "CB-441B_Correa_GT2_6mm_Etapa1.SLDPRT"), e2); Add(m, P(DB, "CB-442B_Correa_GT2_6mm_Multiplicador.SLDPRT"), e2); }
        Desc(m, v == "A" ? "Eslabon 2 (hombro) v7A: E2 con cola corta + placa CB-306 y NEMA 17 arriba del cubo + polea, polea loca y correa GT2 + tapa-soporte AS5600"
                         : "Eslabon 2 (hombro) v7B: E2 con manga corta + placa CB-306 y NEMA 17 arriba del cubo + poleas y correas GT2 (etapa 1 y multiplicador) + tapa-soporte AS5600");
        SaveAsm(m); SW.CloseMine();
    }
    static Xf E2Pose(string v)
    {
        var m = SW.Open(P(v == "A" ? DA : DB, "CB-300" + v + "_Hombro_E2_v7" + v + ".SLDASM"));
        var x = Xf.FromComp(Comps(m).First(c => c.Name2.StartsWith("CB-301")));
        SW.App.CloseDoc(m.GetTitle());
        return x;
    }
    static ModelDoc2 Copy400(string v)
    {
        string dst = P(v == "A" ? DA : DB, "CB-400" + v + "_Prismatico_E3_v7" + v + ".SLDASM");
        if (File.Exists(dst)) throw new Exception("ya existe " + dst);
        File.Copy(P("04_Prismatico_E3_J3", "CB-400_Prismatico_E3_J3.SLDASM"), dst); File.SetAttributes(dst, FileAttributes.Normal);
        var m = SW.Open(dst);
        ((AssemblyDoc)m).ResolveAllLightWeightComponents(false);
        return m;
    }
    static void Asm400A()
    {
        var e2 = E2Pose("A");
        var m = Copy400("A");
        foreach (var c in Comps(m))
        {
            string n = c.Name2;
            if (n.StartsWith("CB-401") || n.StartsWith("CB-403") || n.StartsWith("CB-512") || n.StartsWith("HW_M3x10")) Del(m, c);
        }
        Add(m, P(DA, "CB-421A_Tubo_E3_Aluminio_30x30x2_L440.SLDPRT"), e2);
        Add(m, P(DA, "CB-422A_Tapon_Trasero_Pinza_Correa.SLDPRT"), e2);
        Desc(m, "Eslabon 3 (prismatico J3 v7A, home = retraido): tubo 30x30x2 L440 + brida CB-402 + tapon con pinza de correa + herramienta");
        SaveAsm(m); SW.CloseMine();
    }
    static void Asm400B()
    {
        var e2 = E2Pose("B");
        var m = Copy400("B");
        foreach (var c in Comps(m))
        {
            string n = c.Name2;
            if (n.StartsWith("CB-401") || n.StartsWith("CB-402") || n.StartsWith("CB-403") || n.StartsWith("CB-512") || n.StartsWith("HW_M3x10")) Del(m, c);
        }
        Add(m, P(DB, "CB-421B_Tubo_Interior_20x20x1.5_L263.SLDPRT"), e2);
        Add(m, P(DB, "CB-422B_Buje_Trasero_Interior_Pinza_2a1.SLDPRT"), e2);
        Add(m, P(DB, "CB-432B_Brida_E3_Herramienta_20x20.SLDPRT"), e2);
        Desc(m, "Eslabon 3 (tubo interior del telescopio J3 v7B, home = retraido): tubo 20x20x1.5 L265 + buje trasero con pinza 2:1 + brida + herramienta");
        SaveAsm(m); SW.CloseMine();
    }
    // tramo intermedio: ensamblaje nuevo en coordenadas de E2
    static void Asm410B()
    {
        string dst = P(DB, "CB-410B_Tramo_Intermedio_v7B.SLDASM");
        if (File.Exists(dst)) throw new Exception("ya existe " + dst);
        string tpl = SW.App.GetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swDefaultTemplateAssembly);
        var m = (ModelDoc2)SW.App.NewDocument(tpl, 0, 0, 0);
        SW.Created.Add(m.GetTitle());
        var id = new Xf();
        Add(m, P(DB, "CB-411B_Tubo_Intermedio_30x30x2_L275.SLDPRT"), id);
        Add(m, P(DB, "CB-412B_Tapon_Trasero_Intermedio_Polea.SLDPRT"), id);
        Add(m, P(DB, "CB-413B_Soporte_Polea_Delantera.SLDPRT"), id);
        Add(m, P(DB, "CB-414B_Buje_Delantero_Intermedio.SLDPRT"), id);
        Add(m, P(DB, "CB-415B_Pinza_Correa_Etapa1.SLDPRT"), id);
        Add(m, P(COM, "HW_Polea_Loca_GT2_16T_D3.SLDPRT"), EjeZ(B_XPulR, B_YPul));
        Add(m, P(COM, "HW_Polea_Loca_GT2_16T_D3.SLDPRT"), EjeZ(B_XPulF, B_YPul));
        Desc(m, "Tramo intermedio del telescopio J3 (v7B, home = retraido): tubo 30x30x2 L275 + tapon con polea 16T + soporte con polea 16T + buje delantero + pinza de la etapa 1. Coordenadas de E2.");
        int e = 0, w = 0;
        m.ForceRebuild3(false);
        bool ok = m.Extension.SaveAs3(dst, 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, null, ref e, ref w);
        Console.WriteLine((ok ? "saved " : "SAVE FAIL ") + dst + " e=" + e);
        SW.CloseMine();
    }

    // ================= ensamblaje general =================
    static ModelDoc2 M; static AssemblyDoc A;
    static bool Near(double a, double b, double tol = 0.05) { return Math.Abs(a - b) < tol; }
    static Component2 Child(Component2 parent, string fileStart)
    {
        foreach (Component2 c in (object[])parent.GetChildren())
            if (Path.GetFileName(c.GetPathName()).StartsWith(fileStart, StringComparison.OrdinalIgnoreCase)) return c;
        return null;
    }
    static Entity FindPlaneFace(Component2 comp, int axis, double sign, double at, double areaMin, double areaMax, string what)
    {
        var part = (PartDoc)comp.GetModelDoc2();
        foreach (Body2 b in (object[])part.GetBodies2((int)swBodyType_e.swSolidBody, true))
            foreach (Face2 f in (object[])b.GetFaces())
            {
                var s = (Surface)f.GetSurface();
                if (!s.IsPlane()) continue;
                double[] n = (double[])f.Normal;
                if (Math.Abs(n[axis] - sign) > 1e-6) continue;
                double[] bx = (double[])f.GetBox();
                if (!Near(bx[axis] * 1000, at) || !Near(bx[axis + 3] * 1000, at)) continue;
                double ar = f.GetArea() * 1e6;
                if (ar < areaMin || ar > areaMax) { Console.WriteLine("  (cara descartada " + what + ": " + ar.ToString("0") + " mm2)"); continue; }
                Console.WriteLine("  cara ok: " + what + " (" + ar.ToString("0") + " mm2)");
                return (Entity)comp.GetCorrespondingEntity(f);
            }
        throw new Exception("cara no encontrada: " + what);
    }
    static Feature RefPlane(Component2 comp, int axis)
    {
        Feature f = (Feature)comp.FirstFeature();
        while (f != null)
        {
            if (f.GetTypeName2() == "RefPlane")
            {
                var rp = (RefPlane)f.GetSpecificFeature2();
                double[] t = (double[])((MathTransform)rp.Transform).ArrayData;
                if (Math.Abs(Math.Abs(t[6 + axis]) - 1) < 1e-6) return f;
            }
            f = (Feature)f.GetNextFeature();
        }
        throw new Exception("sin plano normal a " + axis + " en " + comp.Name2);
    }
    static Feature Mate(string name, int type, object e1, object e2, double dist = 0, double dmax = 0, double dmin = 0)
    {
        M.ClearSelection2(true);
        var sd = (SelectData)((SelectionMgr)M.SelectionManager).CreateSelectData();
        sd.Mark = 1;
        bool s1 = e1 is Entity ? ((Entity)e1).Select4(true, sd) : ((Feature)e1).Select2(true, 1);
        bool s2 = e2 is Entity ? ((Entity)e2).Select4(true, sd) : ((Feature)e2).Select2(true, 1);
        int err;
        var mt = (Mate2)A.AddMate5(type, (int)swMateAlign_e.swMateAlignCLOSEST, false, dist / 1000, dmax / 1000, dmin / 1000, 0, 0, 0, 0, 0, false, false, 0, out err);
        Console.WriteLine("relacion " + name + ": sel " + s1 + "/" + s2 + " err=" + err + (mt == null ? " FALLO" : ""));
        M.ClearSelection2(true);
        if (mt == null) return null;
        Feature mg = null; Feature f = (Feature)M.FirstFeature();
        while (f != null) { if (f.GetTypeName2() == "MateGroup") mg = f; f = (Feature)f.GetNextFeature(); }
        Feature last = null; Feature sf = (Feature)mg.GetFirstSubFeature();
        while (sf != null) { last = sf; sf = (Feature)sf.GetNextSubFeature(); }
        if (last != null) last.Name = name;
        return last;
    }
    static List<Feature> Mates()
    {
        var r = new List<Feature>();
        for (Feature f = (Feature)M.FirstFeature(); f != null; f = (Feature)f.GetNextFeature())
            if (f.GetTypeName2() == "MateGroup")
                for (Feature sf = (Feature)f.GetFirstSubFeature(); sf != null; sf = (Feature)sf.GetNextSubFeature()) r.Add(sf);
        return r;
    }

    static void Top(string v)
    {
        string dir = v == "A" ? DA : DB;
        string dst = Path.Combine(R, "CB-000" + v + "_CitriBot_v7" + v + (v == "A" ? "_Correa" : "_Telescopico") + ".SLDASM");
        if (File.Exists(dst)) throw new Exception("ya existe " + dst);
        File.Copy(Path.Combine(R, "CB-000_CitriBot_Ensamblaje_General.SLDASM"), dst); File.SetAttributes(dst, FileAttributes.Normal);
        M = SW.Open(dst); A = (AssemblyDoc)M;
        A.ResolveAllLightWeightComponents(false);
        foreach (var mt in Mates().Where(x => x.Name.StartsWith("J3_")).ToList())
        {
            string n = mt.Name; M.ClearSelection2(true); mt.Select2(false, 0);
            Console.WriteLine("  relacion borrada " + n + " " + M.Extension.DeleteSelection2(0));
        }
        var cs = Comps(M);
        Replace(M, cs.First(c => c.Name2.StartsWith("CB-300")), P(dir, "CB-300" + v + "_Hombro_E2_v7" + v + ".SLDASM"));
        Replace(M, Comps(M).First(c => c.Name2.StartsWith("CB-400")), P(dir, "CB-400" + v + "_Prismatico_E3_v7" + v + ".SLDASM"));
        M.EditRebuild3();
        cs = Comps(M);
        var c300 = cs.First(c => c.Name2.StartsWith("CB-300"));
        var c400 = cs.First(c => c.Name2.StartsWith("CB-400"));
        var e2 = Child(c300, "CB-301");
        var brida = Child(c400, v == "A" ? "CB-402" : "CB-432B");
        Mate("J3_plano_medio", (int)swMateType_e.swMateCOINCIDENT, RefPlane(brida, 2), RefPlane(e2, 2));
        Mate("J3_plano_eje", (int)swMateType_e.swMateCOINCIDENT, RefPlane(brida, 1), RefPlane(e2, 1));
        var front = FindPlaneFace(e2, 0, 1, XFront, 2000, 9000, "frente de E2 x=13");
        Mate("J3_limite_carrera", (int)swMateType_e.swMateDISTANCE,
            FindPlaneFace(brida, 0, -1, XFlIn, 4000, 6500, "brida cara interior x=15"), front, 2, 2 + Stroke, 2);
        if (v == "B")
        {
            var c410 = Add(M, P(DB, "CB-410B_Tramo_Intermedio_v7B.SLDASM"), Xf.FromComp(e2), false);
            M.EditRebuild3();
            var mid = Child(c410, "CB-411B");
            front = FindPlaneFace(e2, 0, 1, XFront, 2000, 9000, "frente de E2 x=13");
            Mate("J3B_intermedio_plano_medio", (int)swMateType_e.swMateCOINCIDENT, RefPlane(mid, 2), RefPlane(e2, 2));
            Mate("J3B_intermedio_plano_eje", (int)swMateType_e.swMateCOINCIDENT, RefPlane(mid, 1), RefPlane(e2, 1));
            Mate("J3B_intermedio_limite", (int)swMateType_e.swMateDISTANCE,
                FindPlaneFace(mid, 0, 1, XFront, 150, 300, "frente del intermedio x=13"), front, 0, Stroke / 2, 0);
        }
        M.EditRebuild3();
        foreach (var mt in Mates()) { bool w; Console.WriteLine("  " + mt.Name + " err=" + mt.GetErrorCode2(out w)); }
        Desc(M, v == "A" ? "CitriBot v7A: base BCN3D + torre 2M1D mod + hombro 5840 (sin cambios) + prismatico J3 de una etapa con correa GT2 (carrera 340) + herramienta"
                         : "CitriBot v7B: base BCN3D + torre 2M1D mod + hombro 5840 (sin cambios) + prismatico J3 telescopico 2:1 con correas GT2 (carrera 340) + herramienta");
        SaveAsm(M);
        SW.App.CloseDoc(M.GetTitle());
    }
}
