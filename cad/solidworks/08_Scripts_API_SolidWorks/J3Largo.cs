using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// J3Largo.exe <root> [parts|e2|asm300|asm400|top ...]   (v6, 2026-10-22)
// Prismatico J3 de carrera larga: NEMA 17 + husillo T8 (avance 8 mm) + tubo de aluminio 30x30x2 que se retrae
// por detras del hombro. Carrera 350 mm: la ventosa (alineada con el brazo) va de 227.8 a 577.8 mm del eje J2.
//
// Toda la geometria esta en coordenadas de la pieza E2 (CB-301): eje J2 = eje Z en (x=-80, y=0),
// eje del prismatico = eje X (y=0, z=0), z=-35 cara de E2 contra la torre, z=+35 cara exterior (tapa AS5600).
// u = x + 80 = distancia al eje J2 a lo largo del brazo. Home de J3 (q3 = 0) = retraido.
class J3Largo
{
    // ---- cadena de cotas a lo largo del eje del prismatico (x de E2) ----
    const double XFront = 13;            // extremo delantero de E2 (u = 93)
    const double XFlIn = 15, XFlOut = 25; // brida de E3 retraida (cara exterior a u = 105)
    const double XTubeRear = -445;       // tubo 30x30x2 L460: x -445..15 retraido
    const double TubeL = 460;
    const double XMotor = -491.5;        // cara del NEMA 17 = cara trasera de la placa de E2
    const double XPlate = -485.5;        // cara interior de la placa (6 mm); el canal empieza aca
    const double Ch = 15.5;              // canal 31 x 31 (tubo 30 x 30, 0.5 mm de juego por lado)
    const double Tail = 27.5;            // cola de E2 55 x 55
    const double XNutFl = -448.5;        // cara trasera de la brida de la tuerca T8 (retraido)
    const double XScrewRear = -466.5, ScrewL = 400;
    const double XCoupler = -479.5, CouplerL = 25;
    public const double Stroke = 350;

    static string R;
    static string P(string sub, string file) { return Path.Combine(R, sub, file); }
    const string COM = "05_Comerciales", HW = "05_Comerciales\\Tornilleria\\";
    static double[] V(params double[] v) { return v; }

    static void Main(string[] args)
    {
        R = args[0];
        SW.Connect();
        SW.CloseMine();
        var which = args.Skip(1).ToList();
        Func<string, bool> want = k => which.Count == 0 || which.Contains(k);
        if (want("parts")) { Tubo(); Brida(); Portatuerca(); Tuerca(); Husillo(); Acople(); Nema17(); }
        if (want("e2")) E2();
        if (want("asm300")) Asm300();
        if (want("asm400")) Asm400();
        if (want("top")) Top();
    }

    // ================= piezas nuevas =================

    static void Tubo()
    {
        PB.NewPart();
        PB.Sketch('R'); PB.Rect(-15, -15, 15, 15); PB.Rect(-13, -13, 13, 13);
        PB.Extrude(XTubeRear, TubeL, false, "Tubo_30x30x2_L460");
        PB.Sketch('T'); PB.Circle(XFlIn - 10, 0, 3.2); PB.Circle(XTubeRear + 10, 0, 3.2);
        PB.Extrude(-16, 32, true, "Agujeros_M3_tapones");
        PB.PartColor(0.84, 0.85, 0.88);
        PB.Prop("Descripcion", "Eslabon E3: tubo cuadrado de aluminio 30 x 30 x 2, largo 460 mm (prismatico J3)");
        PB.Prop("Material", "Aluminio 6063");
        PB.Prop("Notas", "Coordenadas de E2, J3 retraido. Desliza en el canal 31 x 31 de E2 (cinta de PTFE o patines si hay juego). " +
            "2 agujeros D3.2 pasantes: M3 a la brida CB-402 (adelante) y al portatuerca CB-403 (atras).");
        PB.Report("Tubo E3");
        PB.Save(P("04_Prismatico_E3_J3", "CB-401_Tubo_E3_Aluminio_30x30x2.SLDPRT"));
        SW.CloseMine();
    }

    static void Brida()
    {
        PB.NewPart();
        PB.Sketch('R'); PB.RRect(-40, -40, 40, 40, 6); PB.Extrude(XFlIn, XFlOut - XFlIn, false, "Placa_80x80x10");
        PB.Sketch('R'); PB.Rect(-12.9, -12.9, 12.9, 12.9); PB.Extrude(XFlIn - 20, 20, false, "Tapon_25.8_en_el_tubo");
        // 4 x M5 en rombo de radio 28.5 (mismo patron que la horquilla CB-601)
        PB.Sketch('R'); PB.Circle(0, 28.5, 5.4); PB.Circle(0, -28.5, 5.4); PB.Circle(28.5, 0, 5.4); PB.Circle(-28.5, 0, 5.4);
        PB.Extrude(XFlIn - 1, 12, true, "Agujeros_4xM5_rombo_r28.5");
        PB.Sketch('T'); PB.Circle(XFlIn - 10, 0, 3.2); PB.Extrude(-16, 32, true, "Agujero_M3_tubo");
        PB.PartColor(0.95, 0.55, 0.1);
        PB.Prop("Descripcion", "Brida del extremo de E3 para la herramienta (horquilla CB-601), con tapon que entra en el tubo");
        PB.Prop("Material", "PETG impreso, 100 % de relleno en el tapon");
        PB.Prop("Notas", "Coordenadas de E2, J3 retraido (cara exterior a 105 mm del eje J2). 4 x M5 + tuerca en la horquilla. " +
            "Tapon 25.8 x 25.8 x 20 dentro del tubo, pegado con epoxi + 1 M3 pasante.");
        PB.Report("Brida E3");
        PB.Save(P("04_Prismatico_E3_J3", "CB-402_Brida_E3_Herramienta.SLDPRT"));
        SW.CloseMine();
    }

    static void Portatuerca()
    {
        PB.NewPart();
        PB.Sketch('R'); PB.Rect(-12.9, -12.9, 12.9, 12.9); PB.Extrude(XTubeRear, 20, false, "Tapon_25.8x20");
        PB.Sketch('R'); PB.Circle(0, 0, 10.5); PB.Extrude(XTubeRear - 1, 22, true, "Paso_tuerca_D10.5");
        PB.Sketch('R'); foreach (double a in new[] { 45.0, 135.0, 225.0, 315.0 }) PB.Circle(8 * Math.Cos(a * Math.PI / 180), 8 * Math.Sin(a * Math.PI / 180), 2.5);
        PB.Extrude(XTubeRear - 1, 11, true, "Pilotos_4xM3_brida_tuerca");
        PB.Sketch('T'); PB.Circle(XTubeRear + 10, 0, 3.2); PB.Extrude(-16, 32, true, "Agujero_M3_tubo");
        PB.PartColor(0.95, 0.55, 0.1);
        PB.Prop("Descripcion", "Portatuerca T8 en el extremo trasero del tubo de E3");
        PB.Prop("Material", "PETG impreso, 100 % de relleno");
        PB.Prop("Notas", "Coordenadas de E2, J3 retraido. La brida de la tuerca T8 se atornilla a la cara trasera con 4 x M3x10 (autorroscante en pilotos D2.5). " +
            "Pegado con epoxi en el tubo + 1 M3 pasante.");
        PB.Report("Portatuerca");
        PB.Save(P("04_Prismatico_E3_J3", "CB-403_Portatuerca_T8.SLDPRT"));
        SW.CloseMine();
    }

    static void Tuerca()
    {
        PB.NewPart();
        PB.Sketch('R'); PB.Circle(0, 0, 22); var fl = PB.Extrude(XNutFl, 3.5, false, "Brida_D22x3.5");
        PB.Sketch('R'); PB.Circle(0, 0, 10.2); PB.Extrude(XNutFl + 3.5, 11.5, false, "Cuerpo_D10.2");
        PB.Sketch('R'); PB.Circle(0, 0, 8); PB.Extrude(XNutFl - 1, 17, true, "Rosca_T8");
        PB.Sketch('R'); foreach (double a in new[] { 45.0, 135.0, 225.0, 315.0 }) PB.Circle(8 * Math.Cos(a * Math.PI / 180), 8 * Math.Sin(a * Math.PI / 180), 3.5);
        PB.Extrude(XNutFl - 1, 5.5, true, "Agujeros_4xD3.5");
        PB.PartColor(0.80, 0.62, 0.28);
        PB.Prop("Descripcion", "Tuerca de brida para husillo T8 (laton), paso 2 mm, 4 entradas, avance 8 mm");
        PB.Prop("Notas", "Brida D22 x 3.5, cuerpo D10.2, 4 agujeros D3.5 sobre D16. Coordenadas de E2, J3 retraido.");
        PB.Report("Tuerca T8");
        PB.Save(P(COM, "CB-512_Tuerca_T8_Brida.SLDPRT"));
        SW.CloseMine();
    }

    static void Husillo()
    {
        PB.NewPart();
        PB.Sketch('R'); PB.Circle(0, 0, 8); PB.Extrude(XScrewRear, ScrewL, false, "Husillo_D8_L400");
        PB.PartColor(0.78, 0.79, 0.81);
        PB.Prop("Descripcion", "Husillo trapezoidal T8 de 400 mm, avance 8 mm (4 entradas x paso 2)");
        PB.Prop("Notas", "Rosca no modelada. Coordenadas de E2: x -466.5..-66.5. Apoyado en el acople del NEMA 17; el otro extremo queda libre dentro del tubo de E3.");
        PB.Report("Husillo");
        PB.Save(P(COM, "CB-511_Husillo_T8_L400.SLDPRT"));
        SW.CloseMine();
    }

    static void Acople()
    {
        PB.NewPart();
        PB.Sketch('R'); PB.Circle(0, 0, 19); PB.Extrude(XCoupler, CouplerL, false, "Cuerpo_D19x25");
        PB.Sketch('R'); PB.Circle(0, 0, 5); PB.Extrude(XCoupler - 1, 13.5, true, "Agujero_D5_motor");
        PB.Sketch('R'); PB.Circle(0, 0, 8); PB.Extrude(XCoupler + 12, 14, true, "Agujero_D8_husillo");
        PB.PartColor(0.84, 0.85, 0.88);
        PB.Prop("Descripcion", "Acople flexible 5 x 8 mm, D19 x 25 (NEMA 17 -> husillo T8)");
        PB.Prop("Notas", "Coordenadas de E2. Eje del motor 12 mm adentro, husillo 13 mm adentro.");
        PB.Report("Acople");
        PB.Save(P(COM, "CB-513_Acople_Flexible_5x8.SLDPRT"));
        SW.CloseMine();
    }

    static void Nema17()
    {
        PB.NewPart();
        PB.Sketch('R'); PB.RRect(-21.15, -21.15, 21.15, 21.15, 4); var c1 = PB.Extrude(XMotor - 10, 10, false, "Tapa_delantera");
        PB.Sketch('R'); PB.RRect(-21.15, -21.15, 21.15, 21.15, 4); var mid = PB.Extrude(XMotor - 30, 20, false, "Estator");
        PB.Sketch('R'); PB.RRect(-21.15, -21.15, 21.15, 21.15, 4); var c2 = PB.Extrude(XMotor - 40, 10, false, "Tapa_trasera");
        PB.Sketch('R'); PB.Rect(-8, 21.15, 8, 26.5); var con = PB.Extrude(XMotor - 40, 9, false, "Conector");
        PB.Sketch('R'); PB.Circle(0, 0, 22); PB.Extrude(XMotor, 2, false, "Buje_D22x2");
        PB.Sketch('R'); PB.Circle(0, 0, 5); var sh = PB.Extrude(XMotor, 24, false, "Eje_D5x24");
        PB.Sketch('R'); PB.Rect(2.0, -3, 3.5, 3); PB.Extrude(XMotor + 9, 16, true, "Plano_D_eje");
        PB.Sketch('R'); foreach (double a in new[] { -15.5, 15.5 }) foreach (double b in new[] { -15.5, 15.5 }) PB.Circle(a, b, 2.5);
        PB.Extrude(XMotor, -4.5, true, "Agujeros_4xM3_prof4.5");
        PB.PartColor(0.84, 0.85, 0.88);
        PB.Color(mid, 0.07, 0.07, 0.08); PB.Color(con, 0.95, 0.95, 0.95); PB.Color(sh, 0.78, 0.79, 0.81);
        PB.Prop("Descripcion", "Motor paso a paso NEMA 17 de 40 mm (tipo 17HS4401, 1.7 A, ~4.2 kg.cm) - prismatico J3");
        PB.Prop("Notas", "Coordenadas de E2: cara del motor en x = -491.5. 4 x M3 en cuadrado de 31 mm, buje D22 x 2, eje D5 x 24 con plano.");
        PB.Report("NEMA 17");
        PB.Save(P(COM, "CB-505_Motor_NEMA17_Prismatico.SLDPRT"));
        SW.CloseMine();
    }

    // ================= E2 (CB-301) =================

    static bool Has(ModelDoc2 m, string feat) { return ((PartDoc)m).FeatureByName(feat) != null; }

    static void E2()
    {
        var m = SW.Open(P("03_Hombro_E2_J2", "CB-301_Eslabon_E2_mod5840.SLDPRT"));
        PB.Use(m);
        if (Has(m, "CB_v6_Corte_frente_u93")) { Console.WriteLine("E2 ya tiene los cambios v6"); SW.CloseMine(); return; }
        PB.Report("E2 antes");
        // 1) se corta el frente (canal del Rx-rcp) y el boss trasero D66
        PB.Sketch('F'); PB.Rect(XFront, -60, 220, 60); PB.Extrude(-60, 120, true, "CB_v6_Corte_frente_u93");
        PB.Sketch('F'); PB.Rect(-220, -60, -125, 60); PB.Extrude(-60, 120, true, "CB_v6_Corte_boss_trasero");
        // 2) se rellenan el alojamiento del NEMA 14 y el resto del canal viejo
        PB.Sketch('R'); PB.Rect(-25.6, -27.1, 25.6, 27.1); PB.Extrude(-125, XFront + 125, false, "CB_v6_Relleno_cavidades");
        // 3) ventana D40 de la tapa-soporte del AS5600 (como estaba)
        PB.Sketch('F'); PB.Circle(-80, 0, 40); PB.Extrude(14.2, 21, true, "CB_v6_Ventana_AS5600_D40");
        // 4) cola 55 x 55 hasta la placa del motor
        PB.Sketch('R'); PB.Rect(-Tail, -Tail, Tail, Tail); PB.Extrude(XMotor, -100 - XMotor, false, "CB_v6_Cola_55x55");
        // 5) canal 31 x 31 para el tubo de E3, abierto adelante
        PB.Sketch('R'); PB.Rect(-Ch, -Ch, Ch, Ch); PB.Extrude(XPlate, XFront - XPlate + 3, true, "CB_v6_Canal_31x31");
        // 6) placa del NEMA 17: paso D23 del buje y 4 x M3 con alojamiento de cabeza desde el canal
        PB.Sketch('R'); PB.Circle(0, 0, 23); PB.Extrude(XMotor - 1, 8, true, "CB_v6_Paso_buje_NEMA17_D23");
        PB.Sketch('R'); foreach (double a in new[] { -15.5, 15.5 }) foreach (double b in new[] { -15.5, 15.5 }) PB.Circle(a, b, 3.4);
        PB.Extrude(XMotor - 1, 8, true, "CB_v6_Agujeros_4xM3_NEMA17");
        PB.Sketch('R'); foreach (double a in new[] { -15.5, 15.5 }) foreach (double b in new[] { -15.5, 15.5 }) PB.Circle(a, b, 6.5);
        PB.Extrude(XMotor + 3, 3.5, true, "CB_v6_Alojamiento_cabezas_M3");
        // 7) tunel para los cables del AS5600: de la ventana D40 hasta el extremo trasero, sale por la cara exterior
        PB.Sketch('R'); PB.Rect(18.5, -5, 23.5, 5); PB.Extrude(XPlate + 10, -95 - (XPlate + 10), true, "CB_v6_Tunel_cables_AS5600");
        PB.Sketch('T'); PB.Rect(XPlate + 10, 18.5, XPlate + 20, Tail + 1); PB.Extrude(-5, 10, true, "CB_v6_Salida_cables_AS5600");
        PB.Report("E2 despues");
        PB.Prop("Descripcion", "Eslabon E2 (brazo del hombro): cubo del Rx-rcp-003 adaptado al 5840 + cola con el prismatico J3 de carrera larga");
        PB.Prop("Modificaciones_CB", "Cubo recortado 4.5 mm (5840); tapa-soporte AS5600 (3 pilotos). v6: se corto el frente a 93 mm del eje J2 y el boss trasero, " +
            "se rellenaron el alojamiento del NEMA 14 y el canal viejo; cola 55 x 55 hasta 411.5 mm detras del eje J2 con canal 31 x 31 para el tubo de E3, " +
            "placa trasera de 6 mm para el NEMA 17 (paso D23, 4 x M3x6 desde el canal) y tunel 10 x 5 para los cables del AS5600.");
        PB.Prop("Material", "PETG impreso (504.5 mm de largo: partir en 3 tramos)");
        int e = 0, w = 0;
        m.ForceRebuild3(false);
        bool ok = m.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w);
        Console.WriteLine((ok ? "saved " : "SAVE FAIL ") + m.GetPathName() + " e=" + e + " w=" + w);
        SW.CloseMine();
    }

    // ================= ensamblajes =================

    static Component2 Add(ModelDoc2 m, string path, Xf x)
    {
        var a = (AssemblyDoc)m;
        int e = 0, w = 0;
        SW.App.DocumentVisible(false, (int)swDocumentTypes_e.swDocPART);
        SW.App.OpenDoc6(path, (int)swDocumentTypes_e.swDocPART, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref e, ref w);
        SW.App.DocumentVisible(true, (int)swDocumentTypes_e.swDocPART);
        var c = (Component2)a.AddComponent5(path, (int)swAddComponentConfigOptions_e.swAddComponentConfigOptions_CurrentSelectedConfig, "", false, "", 0, 0, 0);
        if (c == null) throw new Exception("AddComponent failed " + path);
        c.Transform2 = x.ToMath();
        m.ClearSelection2(true); c.Select4(false, null, false); a.FixComponent(); m.ClearSelection2(true);
        return c;
    }
    static void Del(ModelDoc2 m, Component2 c)
    {
        string n = c.Name2;
        m.ClearSelection2(true);
        c.Select4(false, null, false);
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

    static Xf E2Pose()
    {
        var m = SW.Open(P("03_Hombro_E2_J2", "CB-300_Hombro_E2_J2.SLDASM"));
        var x = Xf.FromComp(Comps(m).First(c => c.Name2.StartsWith("CB-301")));
        SW.App.CloseDoc(m.GetTitle());
        return x;
    }

    static void Asm300()
    {
        var m = SW.Open(P("03_Hombro_E2_J2", "CB-300_Hombro_E2_J2.SLDASM"));
        ((AssemblyDoc)m).ResolveAllLightWeightComponents(false);
        var e2 = Xf.FromComp(Comps(m).First(c => c.Name2.StartsWith("CB-301")));
        var inv = e2.Inv();
        foreach (var c in Comps(m))
        {
            string n = c.Name2;
            if (n.StartsWith("CB-302") || n.StartsWith("CB-505")) Del(m, c);
            else if (n.StartsWith("HW_M3x6"))
            {
                var p = inv.Apply(Xf.FromComp(c).T);
                if (Math.Abs(p[0] + 37.2) < 1.0) Del(m, c);   // los 4 tornillos del NEMA 14
            }
        }
        Add(m, P(COM, "CB-505_Motor_NEMA17_Prismatico.SLDPRT"), e2);
        Add(m, P(COM, "CB-513_Acople_Flexible_5x8.SLDPRT"), e2);
        Add(m, P(COM, "CB-511_Husillo_T8_L400.SLDPRT"), e2);
        // M3x6 (apoyo de la cabeza en el origen, cana en -Y): cabeza en el alojamiento, cana hacia el motor (-x)
        foreach (double a in new[] { -15.5, 15.5 }) foreach (double b in new[] { -15.5, 15.5 })
                Add(m, HWp("HW_M3x6_Allen_DIN912"), Xf.Rows(V(0, 1, 0), V(1, 0, 0), V(0, 0, -1), V(XMotor + 3, a, b)).Then(e2));
        var cpm = m.Extension.CustomPropertyManager[""];
        cpm.Add3("Descripcion", (int)swCustomInfoType_e.swCustomInfoText, "Eslabon 2 (hombro): E2 + NEMA 17 + acople + husillo T8 + tapa-soporte AS5600", 2);
        SaveAsm(m);
        SW.CloseMine();
    }
    static string HWp(string n) { return Path.Combine(R, HW + n + ".SLDPRT"); }

    static void Asm400()
    {
        var e2 = E2Pose();
        double[] ax = { e2.R[0], e2.R[1], e2.R[2] };   // eje X de E2 en el ensamblaje (direccion del brazo)
        var m = SW.Open(P("04_Prismatico_E3_J3", "CB-400_Prismatico_E3_J3.SLDASM"));
        var a = (AssemblyDoc)m;
        a.ResolveAllLightWeightComponents(false);
        var oldE3 = Comps(m).FirstOrDefault(c => c.Name2.StartsWith("CB-401_Eslabon_E3_v2"));
        var tool = Comps(m).First(c => c.Name2.StartsWith("CB-600"));
        if (oldE3 != null)
        {
            // cara exterior de la placa del E3 viejo (coordenadas de E3 v2) -> coordenadas de E2
            var f = e2.Inv().Apply(Xf.FromComp(oldE3).Apply(V(-120.245, 238.221, -431.516)));
            Console.WriteLine("cara vieja de E3 en E2: " + string.Join(", ", f.Select(v => v.ToString("0.###"))));
            double d = XFlOut - f[0];
            var t = Xf.FromComp(tool).Then(Xf.Tr(d * ax[0], d * ax[1], d * ax[2]));
            m.ClearSelection2(true); tool.Select4(false, null, false); a.UnfixComponent(); m.ClearSelection2(true);
            tool.Transform2 = t.ToMath();
            m.ClearSelection2(true); tool.Select4(false, null, false); a.FixComponent(); m.ClearSelection2(true);
            Console.WriteLine("herramienta movida " + d.ToString("0.###") + " mm a lo largo del brazo");
            Del(m, oldE3);
        }
        Add(m, P("04_Prismatico_E3_J3", "CB-401_Tubo_E3_Aluminio_30x30x2.SLDPRT"), e2);
        Add(m, P("04_Prismatico_E3_J3", "CB-402_Brida_E3_Herramienta.SLDPRT"), e2);
        Add(m, P("04_Prismatico_E3_J3", "CB-403_Portatuerca_T8.SLDPRT"), e2);
        Add(m, P(COM, "CB-512_Tuerca_T8_Brida.SLDPRT"), e2);
        // M3x10 (cabeza arriba en el origen, cana en +Y): cabeza detras de la brida de la tuerca, cana hacia +x
        foreach (double ang in new[] { 45.0, 135.0, 225.0, 315.0 })
        {
            double y = 8 * Math.Cos(ang * Math.PI / 180), z = 8 * Math.Sin(ang * Math.PI / 180);
            Add(m, HWp("HW_M3x10"), Xf.Rows(V(0, 1, 0), V(1, 0, 0), V(0, 0, -1), V(XNutFl - 2.6, y, z)).Then(e2));
        }
        var cpm = m.Extension.CustomPropertyManager[""];
        cpm.Add3("Descripcion", (int)swCustomInfoType_e.swCustomInfoText, "Eslabon 3 (prismatico J3, en home = retraido): tubo 30x30x2 + brida + portatuerca + tuerca T8 + herramienta", 2);
        SaveAsm(m);
        SW.CloseMine();
    }

    // ================= ensamblaje general: relaciones de J3 =================

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
                if (ar < areaMin || ar > areaMax) continue;
                Console.WriteLine("  cara ok: " + what + " (" + ar.ToString("0") + " mm2)");
                return (Entity)comp.GetCorrespondingEntity(f);
            }
        throw new Exception("cara no encontrada: " + what);
    }
    // plano de referencia de la pieza normal al eje dado (0 X, 1 Y, 2 Z)
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
        Feature f = (Feature)M.FirstFeature();
        while (f != null)
        {
            if (f.GetTypeName2() == "MateGroup")
                for (Feature sf = (Feature)f.GetFirstSubFeature(); sf != null; sf = (Feature)sf.GetNextSubFeature()) r.Add(sf);
            f = (Feature)f.GetNextFeature();
        }
        return r;
    }

    static void Top()
    {
        M = SW.Open(Path.Combine(R, "CB-000_CitriBot_Ensamblaje_General.SLDASM"));
        A = (AssemblyDoc)M;
        A.ResolveAllLightWeightComponents(false);
        foreach (var mt in Mates().Where(x => x.Name.StartsWith("J3_")).ToList())
        {
            string n = mt.Name;
            M.ClearSelection2(true); mt.Select2(false, 0);
            Console.WriteLine("  relacion borrada " + n + " " + M.Extension.DeleteSelection2(0));
        }
        M.ClearSelection2(true);
        var cs = ((object[])A.GetComponents(true)).Cast<Component2>().ToArray();
        var c300 = cs.First(c => c.Name2.StartsWith("CB-300"));
        var c400 = cs.First(c => c.Name2.StartsWith("CB-400"));
        var e2 = Child(c300, "CB-301");
        var brida = Child(c400, "CB-402");
        // J3: el plano medio y el plano "superior" de la brida coinciden con los de E2 (todo modelado en coordenadas de E2) -> solo queda la traslacion en X
        Mate("J3_plano_medio", (int)swMateType_e.swMateCOINCIDENT, RefPlane(brida, 2), RefPlane(e2, 2));
        Mate("J3_plano_eje", (int)swMateType_e.swMateCOINCIDENT, RefPlane(brida, 1), RefPlane(e2, 1));
        // limite de carrera: frente de E2 (x=13) <-> cara interior de la brida (x=15 retraido): 2 .. 352 mm
        Mate("J3_limite_carrera", (int)swMateType_e.swMateDISTANCE,
            FindPlaneFace(brida, 0, -1, XFlIn, 4000, 6500, "brida cara interior x=15"),
            FindPlaneFace(e2, 0, 1, XFront, 2000, 9000, "frente de E2 x=13"), 2 + Stroke / 2, 2 + Stroke, 2);
        M.EditRebuild3();
        foreach (var mt in Mates()) { bool w; Console.WriteLine("  " + mt.Name + " err=" + mt.GetErrorCode2(out w)); }
        var cpm = M.Extension.CustomPropertyManager[""];
        cpm.Add3("Descripcion", (int)swCustomInfoType_e.swCustomInfoText,
            "CitriBot - ensamblaje general v6 (base BCN3D Moveo + torre 2M1D mod + E2 con prismatico J3 de 330 mm + herramienta)", 2);
        SaveAsm(M);
        SW.App.CloseDoc(M.GetTitle());
    }
}
