using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Finales.exe <root solidworks> [partes|pca|base|j3|torre|tablero ...] [rehacer]   (v7.11, 2026-11-06)
// Finales de carrera de J0 (riel), J1 (cintura) y J3 (prismático), y el PCA9685 de los servos.
//   partes : CB-516 microswitch con palanca (cuerpo 19.8 x 6.4 x 9.5, agujeros Ø2.35 a 9.5, a 2.9 de la base; opera a 10.2 sobre
//            los agujeros), CB-517 PCA9685 (62.7 x 25.3, agujeros a 56.7 x 19.3), tornillería M2, M2.5 y tirafondos 3.5 x 16,
//            y los soportes impresos CB-142, CB-161, CB-162 y CB-307.
//   pca    : CB-142 soporte del PCA9685 sobre el tablero, al lado de la bandeja (marco árbol x -148 … -79, y -94 … -51), en CB-140.
//   base   : CB-160 = poste del final de carrera de J1 (CB-161, en -x, bajo el borde de la torre) + soporte del de J0 (CB-162, bajo
//            el tablero en el borde -y, palanca hacia -y), dentro de CB-100.
//   j3     : CB-307 escuadra en el fondo del canal de E2 (base en el piso, atravesada por el eje M5 de la polea loca, y brazo
//            lateral) con el final de carrera de J3 mirando al tapón CB-403: opera con la cara trasera del tapón en x = -430 (q3 = 0;
//            q3 es la x del origen de la brida CB-402 en E2). Paso de cables Ø6 en la pared trasera de E2.
//   j3mover: rehace la geometría de CB-307 en el mismo archivo (la primera versión suponía el tapón en x = -410 y chocaba con
//            el tubo en el home real) y mueve el switch y sus 2 tornillos en CB-300.
//   torre  : leva de J1 en la base de la torre CB-210: sector de anillo r 76 … 98, z 91.5 … 96.5, de 250° a 308° respecto del
//            brazo; aprieta el final de carrera de J1 (en el marco árbol a 183°) con J1 entre -125° y -67°.
//   tablero: pilotos Ø2.5 x 12 de los tirafondos en CB-111.
//   leva2  : (v7.13, no entra en la lista por defecto) la leva de J1 llegaba a la palanca por la PUNTA libre (J1 nunca pasa de -70°: siempre
//            entra por el mismo lado) y la empujaba a lo largo, hacia la bisagra, con 0.3 mm entre la punta y la cara recta de la leva.
//            Ahora el switch va girado 180° (SwJ1, mismos agujeros) y la leva llega por el lado de la bisagra: la palanca sube hacia la leva y
//            hace de rampa. Leva nueva más corta: de 243.6° a 272.9° respecto del brazo (antes 250 … 308), mismas r y z, rampas de 4 x 4
//            (la cara recta queda 1.3 mm sobre la punta de la palanca). Verificar con VerLevaJ1.cs.
//   girarj1: (v7.13, no entra en la lista por defecto) gira el switch de J1 180° en CB-160 (los M2, las tuercas y el poste no cambian).
class Finales
{
    static string R;
    static string P(string rel) { return Path.Combine(R, rel); }
    static double[] V(params double[] v) { return v; }
    static double Rad(double d) { return d * Math.PI / 180; }
    const string EL = "05_Comerciales\\Electronica\\", TO = "05_Comerciales\\Tornilleria\\", BASE = "01_Base_J0\\", HOM = "03_Hombro_E2_J2\\", CIN = "02_Cintura_J1\\";
    static bool Rehacer;
    // marco árbol (el del tablero CB-111) en el mundo de CB-100 / CB-000
    static Xf Arbol() { return Xf.Rows(V(Math.Cos(Rad(-112.5)), Math.Sin(Rad(-112.5)), 0), V(Math.Cos(Rad(-22.5)), Math.Sin(Rad(-22.5)), 0), V(0, 0, 1), V(0, 0, 0)); }
    static Xf Rows(double[] x, double[] y, double[] z, double[] t) { return Xf.Rows(x, y, z, t); }

    // ---------- posiciones (marco árbol salvo que se diga otra cosa) ----------
    // PCA9685: PCB de x -145 a -82.3, y -85 a -59.7, cara de abajo a z 8 (placa de 3 + postes de 5)
    static readonly double[] PCA0 = { -145, -85, 8 };
    static readonly double[][] PCA_AG = { V(3, 3), V(59.7, 3), V(3, 22.3), V(59.7, 22.3) };
    static readonly double[][] PCA_TIRA = { V(-113.65, -90), V(-113.65, -55) };
    // J1: switch con el espesor en x (-95.2 … -88.8), el largo en y (-9.9 … 9.9), la palanca arriba; base del switch a z 79.2
    // (opera a z 92.3; la leva de la torre baja hasta 91.5)
    // v7.13: girado 180° alrededor de la vertical por (-92, 0): bisagra de la palanca en y -8.7 y punta en y +7.4 (en la v7.11 era al revés,
    // Rows(V(0, 1, 0), V(0, 0, 1), V(1, 0, 0), V(-92, -9.9, 79.2))). Los agujeros quedan en el mismo lugar (y ±4.75, z 82.1).
    static Xf SwJ1() { return Rows(V(0, -1, 0), V(0, 0, 1), V(-1, 0, 0), V(-92, 9.9, 79.2)); }
    static readonly double[][] J1_TIRA = { V(-118, 9.5), V(-118, -9.5) };
    // J0: bajo el tablero, palanca hacia -y (sale 4 mm del borde), espesor en z (-26.4 … -20)
    static Xf SwJ0() { return Rows(V(1, 0, 0), V(0, -1, 0), V(0, 0, -1), V(-9.9, -189.5, -23.2)); }
    static readonly double[][] J0_TIRA = { V(-18, -189), V(18, -189) };
    // J3 (marco de E2 = CB-301): palanca hacia +x (al tapón), largo en y, espesor en z (5.0 … 11.4, al costado del eje M5 en
    // z = 0); base del switch en x -443.1: opera (13.1 sobre la base) en x -430, la cara trasera del tapón con q3 = 0. Con el
    // tope (q3 = -2, tapón en -432) quedan 1.6 mm hasta el cuerpo del switch (x -433.6).
    static Xf SwJ3() { return Rows(V(0, 1, 0), V(1, 0, 0), V(0, 0, -1), V(-443.1, -9.9, 8.2)); }
    // agujeros del switch en su marco (X largo, Y alto)
    static readonly double[][] SW_AG = { V(5.15, 2.9), V(14.65, 2.9) };
    static double[] EnSw(Xf s, double[] xy) { return s.Apply(V(xy[0], xy[1], 0)); }

    static void Main(string[] a)
    {
        R = a[0]; SW.Connect();
        var w = a.Skip(1).ToList(); Rehacer = w.Remove("rehacer");
        Func<string, bool> want = k => w.Count == 0 || w.Contains(k);
        if (want("partes")) Partes();
        if (want("pca")) Pca();
        if (want("base")) Base();
        if (want("j3")) J3();
        if (w.Contains("j3mover")) J3Mover();
        if (want("torre")) Torre();
        if (want("tablero")) Tablero();
        if (w.Contains("leva2")) Leva2();
        if (w.Contains("girarj1")) GirarJ1();
    }

    // ---------------------------------------------------------------- piezas
    static bool Nueva(string rel)
    {
        string p = P(rel);
        if (File.Exists(p))
        {
            if (!Rehacer) { Console.WriteLine("ya existe " + rel); return false; }
            if (SW.App.GetOpenDocumentByName(p) != null) { Console.WriteLine("abierto, no se rehace " + rel); return false; }
            File.Delete(p);
        }
        PB.NewPart(); return true;
    }
    static void Cerrar(string rel, string desc)
    {
        PB.Prop("Descripcion", desc); PB.Report(Path.GetFileName(rel)); PB.M.ForceRebuild3(false);
        int er = 0; SW.App.ActivateDoc3(PB.M.GetTitle(), false, 0, ref er);
        try { PB.M.ShowNamedView2("*Isometric", -1); PB.M.ViewZoomtofit2(); } catch (Exception ex) { Console.WriteLine("  (vista: " + ex.GetType().Name + ")"); }
        int e = 0, w = 0; bool ok = PB.M.Extension.SaveAs3(P(rel), 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, null, ref e, ref w);
        Console.WriteLine((ok ? "saved " : "SAVE FAIL ") + rel + (e != 0 ? " e=" + e : "")); SW.App.CloseDoc(PB.M.GetTitle());
    }
    static void Tornillo(string rel, double dh, double hh, double d, double L, string desc)
    {
        if (!Nueva(rel)) return;
        // eje Z: cabeza de z 0 a hh, cuerpo de z 0 a -L
        PB.Sketch('F'); PB.Circle(0, 0, dh); PB.Extrude(0, hh, false, "Cabeza");
        PB.Sketch('F'); PB.Circle(0, 0, d); PB.Extrude(0, -L, false, "Cuerpo");
        PB.PartColor(0.62, 0.62, 0.64);
        Cerrar(rel, desc);
    }
    static void Partes()
    {
        string rel = EL + "CB-516_Microswitch_Palanca_19.8x6.4.SLDPRT";
        if (Nueva(rel))
        {
            // X 0 … 19.8 (largo), Y 0 … 9.5 (alto, palanca arriba), Z -3.2 … 3.2 (espesor)
            PB.Sketch('F'); PB.Rect(0, 0, 19.8, 9.5); PB.Color(PB.Extrude(-3.2, 6.4, false, "Cuerpo"), 0.12, 0.12, 0.12);
            PB.Sketch('F'); PB.Circle(5.15, 2.9, 2.35); PB.Circle(14.65, 2.9, 2.35); PB.Extrude(-3.4, 6.8, true, "Agujeros_D2.35_a_9.5");
            PB.Sketch('F'); PB.Rect(6.0, 9.5, 8.1, 10.6); PB.Color(PB.Extrude(-1.2, 2.4, false, "Boton"), 0.85, 0.12, 0.12);
            PB.Sketch('F'); PB.Rect(16.6, 9.5, 19.2, 10.6); PB.Color(PB.Extrude(-2.2, 4.4, false, "Pivote_palanca"), 0.7, 0.7, 0.72);
            // palanca libre: punta a 14.4 … 15 sobre la base; opera a 13.1 (10.2 sobre los agujeros)
            PB.Sketch('F'); PB.Poly(18.6, 10.0, 18.6, 10.6, 2.5, 15.0, 2.5, 14.4); PB.Color(PB.Extrude(-2.0, 4.0, false, "Palanca"), 0.78, 0.78, 0.8);
            PB.Sketch('F'); foreach (var x in new[] { 1.85, 10.65, 17.95 }) PB.Rect(x - 0.3, -3.5, x + 0.3, 0);
            PB.Color(PB.Extrude(-1.6, 3.2, false, "Terminales_1_3_2"), 0.8, 0.65, 0.3);
            Cerrar(rel, "Final de carrera: microswitch con palanca 19.8 x 6.4 x 9.5, agujeros D2.35 a 9.5 mm, OP 10.2 sobre los agujeros");
        }
        rel = EL + "CB-517_Modulo_PCA9685.SLDPRT";
        if (Nueva(rel))
        {
            // X 0 … 62.7, Y 0 … 25.3, PCB de Z 0 a 1.6; agujeros a 3 mm de los bordes (56.7 x 19.3)
            PB.Sketch('F'); PB.Rect(0, 0, 62.7, 25.3); PB.Color(PB.Extrude(0, 1.6, false, "PCB"), 0.1, 0.25, 0.6);
            PB.Sketch('F'); foreach (var h in PCA_AG) PB.Circle(h[0], h[1], 2.7); PB.Extrude(-0.2, 2.0, true, "Agujeros_56.7x19.3");
            PB.Sketch('F'); PB.Rect(9.5, 17.2, 53.5, 24.6); PB.Color(PB.Extrude(1.6, 2.5, false, "Tira_16_servos"), 0.1, 0.1, 0.1);
            PB.Sketch('F'); PB.Rect(10.0, 17.6, 53.0, 24.2); PB.Color(PB.Extrude(4.1, 8.0, false, "Pines_servos"), 0.85, 0.7, 0.2);
            PB.Sketch('F'); PB.Rect(26.4, 0.8, 36.4, 8.8); PB.Color(PB.Extrude(1.6, 10, false, "Bornera_V+"), 0.15, 0.45, 0.85);
            PB.Sketch('F'); PB.Rect(5.6, 5.0, 8.1, 20.3); PB.Rect(54.6, 5.0, 57.1, 20.3); PB.Color(PB.Extrude(1.6, 2.5, false, "Headers_control"), 0.1, 0.1, 0.1);
            PB.Sketch('F'); PB.Rect(6.0, 5.4, 7.7, 19.9); PB.Rect(55.0, 5.4, 56.7, 19.9); PB.Color(PB.Extrude(4.1, 6.0, false, "Pines_control"), 0.85, 0.7, 0.2);
            PB.Sketch('F'); PB.Rect(28.5, 11.5, 34.5, 15.0); PB.Color(PB.Extrude(1.6, 1.2, false, "Chip_PCA9685"), 0.1, 0.1, 0.1);
            Cerrar(rel, "Modulo PCA9685 de 16 canales PWM (I2C), 62.7 x 25.3, agujeros a 56.7 x 19.3");
        }
        Tornillo(TO + "HW_M2x10.SLDPRT", 3.8, 1.3, 2, 10, "Tornillo M2 x 10 cabeza redonda (autorroscante en piloto D1.6)");
        Tornillo(TO + "HW_M2x12.SLDPRT", 3.8, 1.3, 2, 12, "Tornillo M2 x 12 cabeza redonda");
        Tornillo(TO + "HW_M2.5x8.SLDPRT", 4.5, 1.7, 2.5, 8, "Tornillo M2.5 x 8 cabeza redonda (autorroscante en piloto D2.1)");
        Tornillo(TO + "HW_Tirafondo_3.5x16.SLDPRT", 7, 2.4, 3.5, 16, "Tirafondo para madera D3.5 x 16 cabeza redonda");
        rel = TO + "HW_Tuerca_M2.SLDPRT";
        if (Nueva(rel))
        {
            double rc = 4 / Math.Sqrt(3); var pts = new List<double>();
            for (int i = 0; i < 6; i++) { pts.Add(rc * Math.Cos(Rad(60 * i + 30))); pts.Add(rc * Math.Sin(Rad(60 * i + 30))); }
            PB.Sketch('F'); PB.Poly(pts.ToArray()); PB.Extrude(0, 1.6, false, "Hexagono");
            PB.Sketch('F'); PB.Circle(0, 0, 2); PB.Extrude(-0.1, 1.8, true, "Agujero");
            PB.PartColor(0.62, 0.62, 0.64);
            Cerrar(rel, "Tuerca M2 (4 entre caras x 1.6)");
        }
        rel = BASE + "CB-142_Soporte_PCA9685.SLDPRT";
        if (Nueva(rel))
        {
            PB.Sketch('F'); PB.RRect(-148, -94, -79, -51, 4); PB.Extrude(0, 3, false, "Placa");
            PB.Sketch('F'); foreach (var h in PCA_AG) PB.Circle(PCA0[0] + h[0], PCA0[1] + h[1], 6.5); PB.Extrude(3, 5, false, "Postes");
            PB.Sketch('F'); foreach (var h in PCA_AG) PB.Circle(PCA0[0] + h[0], PCA0[1] + h[1], 2.1); PB.Extrude(1, 7.5, true, "Pilotos_M2.5");
            PB.Sketch('F'); foreach (var t in PCA_TIRA) PB.Circle(t[0], t[1], 3.9); PB.Extrude(-0.5, 4, true, "Pasantes_tirafondos");
            PB.PartColor(0.95, 0.55, 0.1);
            Cerrar(rel, "Soporte impreso del PCA9685 sobre el tablero (2 tirafondos 3.5 x 16, 4 x M2.5 x 8 al modulo)");
        }
        rel = BASE + "CB-161_Soporte_Final_Carrera_J1.SLDPRT";
        if (Nueva(rel))
        {
            PB.Sketch('F'); PB.RRect(-130, -15, -95.2, 15, 3); PB.Extrude(0, 5, false, "Base");
            PB.Sketch('F'); PB.Rect(-99.2, -12, -95.2, 12); PB.Extrude(0, 89, false, "Pared");
            PB.Sketch('T'); PB.Poly(-99.2, 4, -99.2, 80, -126, 4); PB.Extrude(-2.5, 5, false, "Nervio");
            // colisas verticales (±2 mm) para ajustar la altura del switch; M2 x 12 + tuerca atrás
            var s = SwJ1(); PB.Sketch('R');
            foreach (var h in SW_AG) { var p = EnSw(s, h); PB.Rect(p[2] - 3.2, p[1] - 1.2, p[2] + 3.2, p[1] + 1.2); }
            PB.Extrude(-100.5, 6.5, true, "Colisas_M2");
            PB.Sketch('F'); foreach (var t in J1_TIRA) PB.Circle(t[0], t[1], 3.9); PB.Extrude(-0.5, 6, true, "Pasantes_tirafondos");
            PB.PartColor(0.95, 0.55, 0.1);
            Cerrar(rel, "Poste impreso del final de carrera de J1 (lo aprieta la leva de la torre CB-210); 2 tirafondos 3.5 x 16");
        }
        rel = BASE + "CB-162_Soporte_Final_Carrera_J0.SLDPRT";
        if (Nueva(rel))
        {
            PB.Sketch('F'); PB.RRect(-24, -200, 24, -178, 3); PB.Extrude(-20, 4, false, "Placa");
            var s = SwJ0(); PB.Sketch('F'); foreach (var h in SW_AG) { var p = EnSw(s, h); PB.Circle(p[0], p[1], 1.6); }
            PB.Extrude(-20.5, 4.0, true, "Pilotos_M2");
            PB.Sketch('F'); foreach (var t in J0_TIRA) PB.Circle(t[0], t[1], 3.9); PB.Extrude(-20.5, 5, true, "Pasantes_tirafondos");
            PB.PartColor(0.95, 0.55, 0.1);
            Cerrar(rel, "Placa impresa bajo el tablero para el final de carrera de J0 (provisoria: el riel no esta definido)");
        }
        rel = HOM + "CB-307_Soporte_Final_Carrera_J3.SLDPRT";
        if (Nueva(rel))
        {
            GeometriaJ3();
            Cerrar(rel, DescJ3);
        }
    }
    const string DescJ3 = "Soporte impreso del final de carrera de J3: escuadra en el fondo del canal de E2 (la base la atraviesa el M5 de la polea loca)";
    // marco de E2. Canal 31 x 31 (y, z de -15.5 a 15.5), pared trasera en x -452, eje M5 vertical en x -442, z 0.
    // Base de 2.8 sobre el piso del canal y brazo lateral de 3.8 contra la pared +z; termina en x -434.5 (el tapón llega a -430
    // en el home y a -432 en el tope). Los terminales del switch quedan frente al paso de cables de la pared trasera.
    static void GeometriaJ3()
    {
        PB.Sketch('T'); PB.RRect(-451.8, -15.2, -434.5, 15.2, 1.0); PB.Extrude(-15.2, 2.8, false, "Base");
        PB.Sketch('R'); PB.Rect(11.4, -12.4, 15.2, 12.0); PB.Extrude(-451.8, 17.3, false, "Brazo_switch");
        PB.Sketch('T'); PB.Circle(-442, 0, 5.6); PB.Extrude(-16, 4, true, "Paso_eje_M5_polea_loca");
        var s = SwJ3(); PB.Sketch('F'); foreach (var h in SW_AG) { var p = EnSw(s, h); PB.Circle(p[0], p[1], 1.6); }
        PB.Extrude(11.0, 4.6, true, "Pilotos_M2");
        PB.PartColor(0.95, 0.55, 0.1);
    }
    static void J3Mover()
    {
        // 1) CB-307: borra las operaciones y rehace la geometría en el mismo archivo (mantiene la identidad del documento)
        bool yp; var p = Abrir(HOM + "CB-307_Soporte_Final_Carrera_J3.SLDPRT", out yp); PB.Use(p);
        PB.Report("CB-307 antes");
        foreach (var nom in new[] { "Pilotos_M2", "Canal_cables", "Paso_eje_M5_polea_loca", "Brazo_switch", "Bloque", "Base" })
        {
            var f = ((PartDoc)p).FeatureByName(nom) as Feature; if (f == null) continue;
            p.ClearSelection2(true); f.Select2(false, 0);
            Console.WriteLine("  borrada " + nom + " " + p.Extension.DeleteSelection2((int)swDeleteSelectionOptions_e.swDelete_Absorbed));
        }
        PB.Use(p); GeometriaJ3(); PB.Prop("Descripcion", DescJ3);
        PB.Report("CB-307 despues");
        Guardar(p, yp);
        // 2) CB-300: switch y tornillos a la posición nueva
        bool ya; var m = Abrir(HOM + "CB-300_Hombro_E2_J2.SLDASM", out ya); var asm = (AssemblyDoc)m;
        var cs = ((object[])asm.GetComponents(true)).Cast<Component2>().ToList();
        var x = Xf.FromComp(cs.First(c => c.Name2.StartsWith("CB-301")));
        var s = SwJ3();
        Action<Component2, Xf> poner = (c, t) =>
        {
            m.ClearSelection2(true); c.Select4(false, null, false); asm.UnfixComponent();
            c.Transform2 = t.ToMath();
            m.ClearSelection2(true); c.Select4(false, null, false); asm.FixComponent(); m.ClearSelection2(true);
            Console.WriteLine("  movido " + c.Name2);
        };
        poner(cs.First(c => c.Name2.StartsWith("CB-516")), s.Then(x));
        var torn = cs.Where(c => c.Name2.StartsWith("HW_M2x10")).OrderBy(c => c.Name2).ToList();
        for (int i = 0; i < torn.Count && i < SW_AG.Length; i++) { var q = EnSw(s, SW_AG[i]); poner(torn[i], Torn(V(q[0], q[1], 5.0), V(0, 0, -1)).Then(x)); }
        Guardar(m, ya);
    }

    // ---------------------------------------------------------------- ensamblajes
    static Component2 Add(ModelDoc2 m, string path, Xf x)
    {
        var a = (AssemblyDoc)m; int e = 0, w = 0;
        int t = path.ToUpper().EndsWith(".SLDASM") ? (int)swDocumentTypes_e.swDocASSEMBLY : (int)swDocumentTypes_e.swDocPART;
        if (SW.App.GetOpenDocumentByName(path) == null)
        {
            SW.App.DocumentVisible(false, t);
            SW.App.OpenDoc6(path, t, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref e, ref w);
            SW.App.DocumentVisible(true, t);
        }
        // AddComponents3 funciona también con el ensamblaje abierto sin ventana (AddComponent5 no)
        var tr = new double[16];
        for (int i = 0; i < 9; i++) tr[i] = x.R[i];
        for (int i = 0; i < 3; i++) tr[9 + i] = x.T[i] / 1000;
        tr[12] = 1;
        var r = a.AddComponents3(new[] { path }, tr, new[] { "" }) as object[];
        var c = r == null || r.Length == 0 ? null : (Component2)r[0];
        if (c == null) throw new Exception("AddComponent fallo " + path);
        c.Transform2 = x.ToMath();
        m.ClearSelection2(true); c.Select4(false, null, false); a.FixComponent(); m.ClearSelection2(true);
        Console.WriteLine("  + " + c.Name2);
        return c;
    }
    static bool Tiene(ModelDoc2 m, string pref)
    {
        return ((object[])((AssemblyDoc)m).GetComponents(true)).Cast<Component2>().Any(c => c.Name2.StartsWith(pref));
    }
    static ModelDoc2 Abrir(string rel, out bool ya)
    {
        ya = SW.App.GetOpenDocumentByName(P(rel)) != null;
        bool asm = rel.ToUpper().EndsWith(".SLDASM");
        // los ensamblajes se abren sin ventana: el 2026-11-06 la GPU NVIDIA se desconectó (y SolidWorks se cerró) al mostrar CB-140
        if (asm && !ya) SW.App.DocumentVisible(false, (int)swDocumentTypes_e.swDocASSEMBLY);
        ModelDoc2 m;
        try { m = SW.Open(P(rel)); } finally { if (asm && !ya) SW.App.DocumentVisible(true, (int)swDocumentTypes_e.swDocASSEMBLY); }
        if (!asm) { int er = 0; SW.App.ActivateDoc3(m.GetTitle(), false, 0, ref er); }
        else ((AssemblyDoc)m).ResolveAllLightWeightComponents(false);
        return m;
    }
    static void Guardar(ModelDoc2 m, bool ya)
    {
        int e = 0, w = 0; m.ForceRebuild3(false);
        Console.WriteLine("guardado " + m.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w) + " " + m.GetTitle() + " e=" + e + " w=" + w);
        if (!ya) SW.App.CloseDoc(m.GetTitle());
    }
    // tornillo: eje Z de la pieza hacia la cabeza (dir), cara de apoyo de la cabeza en p
    static Xf Torn(double[] p, double[] dir)
    {
        double[] z = dir, x = Math.Abs(z[2]) > 0.9 ? V(1, 0, 0) : V(0, 0, 1);
        double[] y = { z[1] * x[2] - z[2] * x[1], z[2] * x[0] - z[0] * x[2], z[0] * x[1] - z[1] * x[0] };
        x = new[] { y[1] * z[2] - y[2] * z[1], y[2] * z[0] - y[0] * z[2], y[0] * z[1] - y[1] * z[0] };
        return Rows(x, y, z, p);
    }

    static void Pca()
    {
        bool ya; var m = Abrir(BASE + "CB-140_Modulo_Electronica.SLDASM", out ya);
        if (Tiene(m, "CB-517")) { Console.WriteLine("CB-140 ya tiene el PCA9685"); if (!ya) SW.App.CloseDoc(m.GetTitle()); return; }
        // CB-140 está en CB-100 con los ejes del tablero y el origen en (14, -145) del marco árbol
        var aLocal = Xf.Tr(-14, 145, 0);   // árbol -> CB-140
        Add(m, P(BASE + "CB-142_Soporte_PCA9685.SLDPRT"), aLocal);
        Add(m, P(EL + "CB-517_Modulo_PCA9685.SLDPRT"), Xf.Tr(PCA0[0], PCA0[1], PCA0[2]).Then(aLocal));
        foreach (var h in PCA_AG) Add(m, P(TO + "HW_M2.5x8.SLDPRT"), Torn(V(PCA0[0] + h[0], PCA0[1] + h[1], PCA0[2] + 1.6), V(0, 0, 1)).Then(aLocal));
        foreach (var t in PCA_TIRA) Add(m, P(TO + "HW_Tirafondo_3.5x16.SLDPRT"), Torn(V(t[0], t[1], 3), V(0, 0, 1)).Then(aLocal));
        Guardar(m, ya);
    }

    static void Base()
    {
        string rel = BASE + "CB-160_Finales_Carrera_Base.SLDASM";
        if (!File.Exists(P(rel)))
        {
            string tpl = SW.App.GetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swDefaultTemplateAssembly);
            var n = (ModelDoc2)SW.App.NewDocument(tpl, 0, 0, 0); SW.Created.Add(n.GetTitle());
            // marco árbol
            Add(n, P(BASE + "CB-161_Soporte_Final_Carrera_J1.SLDPRT"), new Xf());
            var s1 = SwJ1(); Add(n, P(EL + "CB-516_Microswitch_Palanca_19.8x6.4.SLDPRT"), s1);
            foreach (var h in SW_AG)
            {
                var p = EnSw(s1, h);
                Add(n, P(TO + "HW_M2x12.SLDPRT"), Torn(V(-88.8, p[1], p[2]), V(1, 0, 0)));
                Add(n, P(TO + "HW_Tuerca_M2.SLDPRT"), Torn(V(-99.2, p[1], p[2]), V(-1, 0, 0)));
            }
            foreach (var t in J1_TIRA) Add(n, P(TO + "HW_Tirafondo_3.5x16.SLDPRT"), Torn(V(t[0], t[1], 5), V(0, 0, 1)));
            Add(n, P(BASE + "CB-162_Soporte_Final_Carrera_J0.SLDPRT"), new Xf());
            var s0 = SwJ0(); Add(n, P(EL + "CB-516_Microswitch_Palanca_19.8x6.4.SLDPRT"), s0);
            foreach (var h in SW_AG) { var p = EnSw(s0, h); Add(n, P(TO + "HW_M2x10.SLDPRT"), Torn(V(p[0], p[1], -26.4), V(0, 0, -1))); }
            foreach (var t in J0_TIRA) Add(n, P(TO + "HW_Tirafondo_3.5x16.SLDPRT"), Torn(V(t[0], t[1], -20), V(0, 0, -1)));
            n.Extension.CustomPropertyManager[""].Add3("Descripcion", (int)swCustomInfoType_e.swCustomInfoText,
                "Finales de carrera de la base: J1 (poste CB-161, lo aprieta la leva de la torre) y J0 (CB-162 bajo el tablero, provisorio)", 2);
            int e = 0, w = 0; n.ForceRebuild3(false);
            Console.WriteLine((n.Extension.SaveAs3(P(rel), 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, null, ref e, ref w) ? "saved " : "SAVE FAIL ") + rel);
            SW.App.CloseDoc(n.GetTitle());
        }
        bool ya; var m = Abrir(BASE + "CB-100_Base_J0.SLDASM", out ya);
        if (Tiene(m, "CB-160")) { Console.WriteLine("CB-100 ya tiene CB-160"); if (!ya) SW.App.CloseDoc(m.GetTitle()); return; }
        Add(m, P(rel), Arbol());
        Guardar(m, ya);
    }

    static void J3()
    {
        bool ya; var m = Abrir(HOM + "CB-300_Hombro_E2_J2.SLDASM", out ya);
        if (!Tiene(m, "CB-307"))
        {
            var e2 = ((object[])((AssemblyDoc)m).GetComponents(true)).Cast<Component2>().First(c => c.Name2.StartsWith("CB-301"));
            var x = Xf.FromComp(e2);
            Add(m, P(HOM + "CB-307_Soporte_Final_Carrera_J3.SLDPRT"), x);
            var s = SwJ3(); Add(m, P(EL + "CB-516_Microswitch_Palanca_19.8x6.4.SLDPRT"), s.Then(x));
            foreach (var h in SW_AG) { var p = EnSw(s, h); Add(m, P(TO + "HW_M2x10.SLDPRT"), Torn(V(p[0], p[1], 5.0), V(0, 0, -1)).Then(x)); }
            Guardar(m, ya);
        }
        else { Console.WriteLine("CB-300 ya tiene CB-307"); if (!ya) SW.App.CloseDoc(m.GetTitle()); }
        // paso de cables en la pared trasera de E2
        bool yp; var e = Abrir(HOM + "CB-301_Eslabon_E2_mod5840.SLDPRT", out yp); PB.Use(e);
        if (((PartDoc)e).FeatureByName("CB_v711_Paso_cables_final_J3") != null) { Console.WriteLine("CB-301 ya tiene el paso"); if (!yp) SW.App.CloseDoc(e.GetTitle()); return; }
        PB.Report("CB-301 antes");
        PB.Sketch('R'); PB.Circle(7.5, -7, 6); PB.Extrude(-458, 7, true, "CB_v711_Paso_cables_final_J3");
        PB.Report("CB-301 despues");
        Guardar(e, yp);
    }

    static void Torre()
    {
        // marco de CB-200: J1 en el origen, z arriba. Dirección del brazo (proyección del eje x de E2) en CB-200: 159.09°
        // (de CB-000: eje x de E2 (-0.1874, 0.9804, 0.0616) y CB-200 girado -58.27°). La torre está en CB-200 con Xt.
        double fwd = 159.09, a1 = fwd + 250, a2 = fwd + 308, ri = 76, ro = 98, z0 = 91.5, z1 = 96.5;
        bool yaA; var asm = Abrir(CIN + "CB-200_Cintura_J1.SLDASM", out yaA);
        var tc = ((object[])((AssemblyDoc)asm).GetComponents(true)).Cast<Component2>().First(k => k.Name2.StartsWith("CB-210"));
        var inv = Xf.FromComp(tc).Inv();
        if (!yaA) SW.App.CloseDoc(asm.GetTitle());
        bool ya; var m = Abrir(CIN + "CB-210_Torre_Cintura_2M1D_mod.SLDPRT", out ya); PB.Use(m);
        if (((PartDoc)m).FeatureByName("CB_v711_Leva_final_carrera_J1") != null) { Console.WriteLine("CB-210 ya tiene la leva"); if (!ya) SW.App.CloseDoc(m.GetTitle()); return; }
        PB.Report("CB-210 antes");
        Func<double, double, double, double[]> L = (r, ang, z) => inv.Apply(V(r * Math.Cos(Rad(ang)), r * Math.Sin(Rad(ang)), z));
        var c = L(0, 0, z0);
        Console.WriteLine(string.Format("eje J1 en la torre: ({0:0.00}, {1:0.00}, {2:0.00})", c[0], c[1], c[2]));
        // boceto en el plano normal a x de la torre (x = arriba): coordenadas (a = z, b = y)
        PB.Sketch('R');
        var o1 = L(ro, a1, z0); var o2 = L(ro, a2, z0); var i1 = L(ri, a1, z0); var i2 = L(ri, a2, z0);
        PB.Line(i1[2], i1[1], o1[2], o1[1]); PB.Line(i2[2], i2[1], o2[2], o2[1]);
        // CCW en el mundo = CW en (z, y) de la torre: el arco CCW en (a, b) va de a2 a a1
        PB.Arc(c[2], c[1], o2[2], o2[1], o1[2], o1[1]);
        PB.Arc(c[2], c[1], i2[2], i2[1], i1[2], i1[1]);
        PB.Extrude(c[0], z1 - z0, false, "CB_v711_Leva_final_carrera_J1");
        PB.Report("CB-210 con leva");
        // rampas de 3 x 3 en las aristas de abajo de las dos puntas
        foreach (var ang in new[] { a1, a2 })
        {
            var q = L(89, ang, z0);
            m.ClearSelection2(true);
            bool s = m.Extension.SelectByID2("", "EDGE", q[0] / 1000, q[1] / 1000, q[2] / 1000, false, 0, null, 0);
            var ch = s ? (Feature)m.FeatureManager.InsertFeatureChamfer(4, (int)swChamferType_e.swChamferDistanceDistance, 0.003, 0, 0.003, 0, 0, 0) : null;
            Console.WriteLine("rampa a " + ang.ToString("0") + "°: " + (ch == null ? "NO (seleccion " + s + ")" : ch.Name));
            if (ch != null) ch.Name = "CB_v711_Rampa_leva_" + (ang == a1 ? "1" : "2");
        }
        PB.Report("CB-210 despues");
        Guardar(m, ya);
    }

    // Leva de J1, versión v7.13 (ver la cabecera): borra la de la v7.11 y la hace de nuevo al final del árbol de operaciones.
    const double LEVA_A1 = 243.6, LEVA_A2 = 272.9, LEVA_RAMPA = 4.0;   // grados respecto del brazo (CCW) y rampa (mm x mm, 45°)
    static void Leva2()
    {
        double fwd = 159.09, a1 = fwd + LEVA_A1, a2 = fwd + LEVA_A2, ri = 76, ro = 98, z0 = 91.5, z1 = 96.5;
        bool yaA; var asm = Abrir(CIN + "CB-200_Cintura_J1.SLDASM", out yaA);
        var tc = ((object[])((AssemblyDoc)asm).GetComponents(true)).Cast<Component2>().First(k => k.Name2.StartsWith("CB-210"));
        var inv = Xf.FromComp(tc).Inv();
        if (!yaA) SW.App.CloseDoc(asm.GetTitle());
        bool ya; var m = Abrir(CIN + "CB-210_Torre_Cintura_2M1D_mod.SLDPRT", out ya); PB.Use(m);
        PB.Report("CB-210 antes");
        foreach (var f in new[] { "CB_v711_Rampa_leva_1", "CB_v711_Rampa_leva_2", "CB_v711_Leva_final_carrera_J1", "CB_v713_Rampa_leva_1", "CB_v713_Rampa_leva_2", "CB_v713_Leva_final_carrera_J1" })
        {
            m.ClearSelection2(true);
            bool s = m.Extension.SelectByID2(f, "BODYFEATURE", 0, 0, 0, false, 0, null, 0);
            if (s) { m.Extension.DeleteSelection2((int)swDeleteSelectionOptions_e.swDelete_Absorbed); Console.WriteLine("  borrado " + f); }
        }
        PB.Use(m);
        PB.Report("CB-210 sin leva");
        Func<double, double, double, double[]> L = (r, ang, z) => inv.Apply(V(r * Math.Cos(Rad(ang)), r * Math.Sin(Rad(ang)), z));
        var c = L(0, 0, z0);
        PB.Sketch('R');
        var o1 = L(ro, a1, z0); var o2 = L(ro, a2, z0); var i1 = L(ri, a1, z0); var i2 = L(ri, a2, z0);
        PB.Line(i1[2], i1[1], o1[2], o1[1]); PB.Line(i2[2], i2[1], o2[2], o2[1]);
        PB.Arc(c[2], c[1], o2[2], o2[1], o1[2], o1[1]);
        PB.Arc(c[2], c[1], i2[2], i2[1], i1[2], i1[1]);
        PB.Extrude(c[0], z1 - z0, false, "CB_v713_Leva_final_carrera_J1");
        PB.Report("CB-210 con leva");
        // Rampas en la arista de ABAJO de cada punta (la que va de la base de la torre, r 80, a r 98, en z 91.5). No con SelectByID2 en un
        // punto: elige la arista según la vista y en la primera corrida puso la rampa de la punta a2 en la arista de arriba. Se busca la arista
        // por sus vértices.
        Func<double[], double[], double> dist = (u, w) => Math.Sqrt((u[0] - w[0]) * (u[0] - w[0]) + (u[1] - w[1]) * (u[1] - w[1]) + (u[2] - w[2]) * (u[2] - w[2]));
        foreach (var ang in new[] { a1, a2 })
        {
            var pOut = L(ro, ang, z0); var pIn = L(80, ang, z0);
            var cuerpo = (Body2)((object[])((PartDoc)m).GetBodies2((int)swBodyType_e.swSolidBody, true))[0];
            Edge arista = null; double mejor = 1e9;
            foreach (Edge e in (object[])cuerpo.GetEdges())
            {
                var v1 = e.GetStartVertex() as Vertex; var v2 = e.GetEndVertex() as Vertex; if (v1 == null || v2 == null) continue;
                var q1 = ((double[])v1.GetPoint()).Select(v => v * 1000).ToArray(); var q2 = ((double[])v2.GetPoint()).Select(v => v * 1000).ToArray();
                double d = Math.Min(dist(q1, pOut) + dist(q2, pIn), dist(q2, pOut) + dist(q1, pIn));
                if (d < mejor) { mejor = d; arista = e; }
            }
            Feature ch = null;
            if (arista != null && mejor < 1.0)
            {
                m.ClearSelection2(true);
                ((Entity)arista).Select4(false, null);
                ch = (Feature)m.FeatureManager.InsertFeatureChamfer(4, (int)swChamferType_e.swChamferDistanceDistance, LEVA_RAMPA / 1000, 0, LEVA_RAMPA / 1000, 0, 0, 0);
            }
            Console.WriteLine("rampa a " + ang.ToString("0.0") + "°: " + (ch == null ? "NO (arista a " + mejor.ToString("0.00") + " mm)" : ch.Name + " (arista a " + mejor.ToString("0.00") + " mm)"));
            if (ch != null) ch.Name = "CB_v713_Rampa_leva_" + (ang == a1 ? "1" : "2");
        }
        PB.Report("CB-210 despues");
        Guardar(m, ya);
    }

    static void GirarJ1()
    {
        bool ya; var m = Abrir(BASE + "CB-160_Finales_Carrera_Base.SLDASM", out ya); var a = (AssemblyDoc)m;
        var sw = ((object[])a.GetComponents(true)).Cast<Component2>().Where(k => k.Name2.StartsWith("CB-516"))
            .FirstOrDefault(k => { var t = Xf.FromComp(k).T; return Math.Abs(t[0] + 92) < 5 && t[2] > 50; });
        if (sw == null) { Console.WriteLine("NO se encontro el switch de J1 en CB-160"); if (!ya) SW.App.CloseDoc(m.GetTitle()); return; }
        Func<string> pal = () =>
        {
            var x = Xf.FromComp(sw); var b = x.Apply(V(18.6, 10.3, 0)); var p = x.Apply(V(2.5, 14.7, 0));
            return string.Format("bisagra ({0:0.0}, {1:0.0}, {2:0.0})  punta ({3:0.0}, {4:0.0}, {5:0.0})", b[0], b[1], b[2], p[0], p[1], p[2]);
        };
        Console.WriteLine("  " + sw.Name2 + " antes:   " + pal());
        m.ClearSelection2(true); sw.Select4(false, null, false); a.UnfixComponent();
        sw.Transform2 = SwJ1().ToMath();
        m.ClearSelection2(true); sw.Select4(false, null, false); a.FixComponent(); m.ClearSelection2(true);
        Console.WriteLine("  " + sw.Name2 + " despues: " + pal());
        Guardar(m, ya);
    }

    static void Tablero()
    {
        bool ya; var m = Abrir(BASE + "CB-111_Tablero_Madera_400x300x16.SLDPRT", out ya); PB.Use(m);
        if (((PartDoc)m).FeatureByName("CB_v711_Pilotos_tirafondos_arriba") != null) { Console.WriteLine("CB-111 ya tiene los pilotos"); if (!ya) SW.App.CloseDoc(m.GetTitle()); return; }
        PB.Report("CB-111 antes");
        PB.Sketch('F'); foreach (var t in PCA_TIRA.Concat(J1_TIRA)) PB.Circle(t[0], t[1], 2.5); PB.Extrude(-14, 14.5, true, "CB_v711_Pilotos_tirafondos_arriba");
        PB.Sketch('F'); foreach (var t in J0_TIRA) PB.Circle(t[0], t[1], 2.5); PB.Extrude(-16.5, 12.5, true, "CB_v711_Pilotos_tirafondos_abajo");
        PB.Report("CB-111 despues");
        Guardar(m, ya);
    }
}
