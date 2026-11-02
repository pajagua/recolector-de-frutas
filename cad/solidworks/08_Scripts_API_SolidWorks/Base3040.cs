using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Base3040.exe <root solidworks> [tablero|patas|bandeja|camaras ...]   (v7.7, 2026-11-02)
// Tablero de 400 x 300 en lugar del 550 x 550 del BCN3D.
// Marco "árbol" (el de CB-150): origen en el eje J1 sobre la cara del tablero, x hacia el árbol, y a lo largo del riel, z arriba.
// En el mundo de CB-100 / CB-000: x_arbol = (cos -112.5°, sin -112.5°, 0), y_arbol = (cos -22.5°, sin -22.5°, 0).
//   - Tablero: x de -115 a +185 (borde del árbol a 185 mm del eje J1), y de -200 a +200 (400 a lo largo del riel).
//   - J1, lazy susan y módulo del motor de J1 no se mueven (sus agujeros quedan en el mismo lugar respecto de J1).
//   - Bandeja de electrónica: del lado -y, girada 90° (largo a lo largo de x): x -58 … 152, y -195 … -95.
//   - Mástil de las cámaras: esquina trasera +y, pie en (-80, 130), apuntado al centro de la esfera (385, 0, 270).
//   - Patas: en las 4 esquinas nuevas, con la misma separación al borde (pernos a 25 mm de cada borde).
class Base3040
{
    static string R;
    const string BASE = "01_Base_J0";
    static string P(string f) { return Path.Combine(R, BASE, f); }
    static double[] V(params double[] v) { return v; }

    // marco árbol en el mundo de CB-100
    static readonly double[] X = { Math.Cos(Rad(-112.5)), Math.Sin(Rad(-112.5)), 0 };
    static readonly double[] Y = { Math.Cos(Rad(-22.5)), Math.Sin(Rad(-22.5)), 0 };
    static Xf Arbol() { return Xf.Rows(X, Y, V(0, 0, 1), V(0, 0, 0)); }
    static double Rad(double d) { return d * Math.PI / 180; }
    static double[] W(double xt, double ys, double z = 0) { return Arbol().Apply(V(xt, ys, z)); }

    const double X0 = -115, X1 = 185, Y0 = -200, Y1 = 200, ESP = 16, INSET = 25;
    // esquinas viejas y nuevas (marco árbol)
    static readonly double[][] VIEJAS = { V(-355.24, 275), V(194.76, 275), V(194.76, -275), V(-355.24, -275) };
    static readonly double[][] NUEVAS = { V(X0, Y1), V(X1, Y1), V(X1, Y0), V(X0, Y0) };

    // bandeja: centro y giro (marco árbol). Vieja: centro (139.27, -5.8), x local hacia -y. Nueva: x local hacia +x.
    static Xf BandejaVieja() { return Xf.Rows(V(0, -1, 0), V(1, 0, 0), V(0, 0, 1), V(139.27, -5.8, 0)); }
    const double BX = 47;   // centro de la bandeja en x (27 chocaba la tornilleria con la pata trasera)
    static Xf BandejaNueva() { return Xf.Rows(V(1, 0, 0), V(0, 1, 0), V(0, 0, 1), V(BX, -145, 0)); }

    // cámaras (ver Vision2.cs)
    const double H = 638, TX = 40.3, TZ = 8;
    const double FX0 = -290, FY0 = 222, YAW0 = -17.96, PITCH0 = 32.09;
    const double FX1 = -80, FY1 = 130, YAW1 = -15.62, PITCH1 = 43.82;   // aim.py: eje de las lentes por (385, 0, 270)
    static Xf Mast(double fx, double fy, double yaw) { double c = Math.Cos(Rad(yaw)), s = Math.Sin(Rad(yaw)); return Xf.Rows(V(c, s, 0), V(-s, c, 0), V(0, 0, 1), V(fx, fy, 0)); }
    static Xf Bar(double fx, double fy, double yaw, double pitch) { double c = Math.Cos(Rad(pitch)), s = Math.Sin(Rad(pitch)); return Xf.Rows(V(c, 0, -s), V(0, 1, 0), V(s, 0, c), V(0, 0, H + 15)).Then(Mast(fx, fy, yaw)); }

    static void Main(string[] a)
    {
        R = a[0];
        SW.Connect();
        var w = a.Skip(1).ToList();
        Func<string, bool> want = k => w.Count == 0 || w.Contains(k);
        if (want("tablero")) Tablero();
        if (want("patas")) Patas();
        if (want("bandeja")) Bandeja();
        if (want("camaras")) Camaras();
        if (want("ajuste")) Ajuste();
    }

    // ---------- tablero nuevo, modelado en el marco árbol ----------
    static void Tablero()
    {
        PB.NewPart();
        PB.Sketch('F'); PB.Rect(X0, Y0, X1, Y1); PB.Extrude(-ESP, ESP, false, "Tablero_400x300x16");
        PB.Sketch('F'); PB.Circle(0, 0, 35); PB.Extrude(-ESP - 1, ESP + 2, true, "Paso_eje_J1_D35");
        PB.Sketch('F'); PB.Circle(0, 147.41, 42); PB.Extrude(-ESP - 1, ESP + 2, true, "Paso_motor_37D_D42");
        PB.Sketch('F'); foreach (double sx in new[] { -1.0, 1.0 }) foreach (double sy in new[] { -1.0, 1.0 }) PB.Circle(sx * 51.26, sy * 51.26, 4.5);
        PB.Extrude(-ESP - 1, ESP + 2, true, "Placa_J1_1M2A_4xD4.5");
        PB.Sketch('F'); foreach (double sx in new[] { -1.0, 1.0 }) foreach (double y in new[] { 178.19, 171.74, 113.39, 106.94 }) PB.Circle(sx * 32.4, y, 4.3);
        PB.Extrude(-ESP - 1, ESP + 2, true, "Modulo_motor_J1_8xD4.3");
        Ranuras();
        PB.Sketch('F');
        foreach (var h in new[] { V(169.26, 92.2), V(109.26, 92.2), V(179.26, -65.8), V(99.26, -65.8) })
        { var n = BandejaVieja().Inv().Then(BandejaNueva()).Apply(V(h[0], h[1], 0)); PB.Circle(n[0], n[1], 4.5); Console.WriteLine("  bandeja agujero " + n[0].ToString("0.0") + ", " + n[1].ToString("0.0")); }
        PB.Extrude(-ESP - 1, ESP + 2, true, "Bandeja_electronica_4xD4.5");
        PB.Sketch('F'); foreach (var c in NUEVAS) PB.Circle(c[0] - Math.Sign(c[0] - 35) * INSET, c[1] - Math.Sign(c[1]) * INSET, 5.5);
        PB.Extrude(-ESP - 1, ESP + 2, true, "Patas_4xD5.5");
        PB.PartColor(0.76, 0.6, 0.42);
        PB.Prop("Descripcion", "Tablero de madera 400 x 300 x 16 (base del robot)");
        PB.Prop("Material", "Madera (MDF o fenolico) 16 mm");
        PB.Prop("Notas", "v7.7: reemplaza al tablero 550 x 550 del BCN3D. Origen en el eje J1 sobre la cara de arriba, x hacia el arbol " +
            "(borde a 185 mm), y a lo largo del riel (400 mm). Agujeros: eje J1 D35, motor 37D D42, placa 1M2A, modulo del motor J1, bandeja, 4 patas (M5).");
        PB.Report("CB-111 nuevo");
        PB.Save(P("CB-111_Tablero_Madera_400x300x16.SLDPRT")); SW.Created.Add(PB.M.GetTitle());
        string nuevoTitulo = PB.M.GetTitle();

        var m = SW.Open(P("CB-110_Base_Tablero_Patas.SLDASM")); var asm = (AssemblyDoc)m;
        int er = 0; SW.App.ActivateDoc3(m.GetTitle(), false, 0, ref er);
        asm.ResolveAllLightWeightComponents(false);
        var tab = ((object[])asm.GetComponents(true)).Cast<Component2>().First(c => c.Name2.StartsWith("CB-111"));
        m.ClearSelection2(true); tab.Select4(false, null, false);
        bool ok = asm.ReplaceComponents2(P("CB-111_Tablero_Madera_400x300x16.SLDPRT"), "", false, 0, true);
        Console.WriteLine("reemplazo del tablero " + ok);
        tab = ((object[])asm.GetComponents(true)).Cast<Component2>().First(c => c.Name2.StartsWith("CB-111"));
        Poner(m, tab, Arbol());
        MoverPatas(m, asm, "CB-112");
        Guardar(m);
        SW.App.CloseDoc(nuevoTitulo);
    }

    static void Poner(ModelDoc2 m, Component2 c, Xf x)
    {
        var asm = (AssemblyDoc)m;
        m.ClearSelection2(true); c.Select4(false, null, false); asm.UnfixComponent();
        c.Transform2 = x.ToMath();
        m.ClearSelection2(true); c.Select4(false, null, false); asm.FixComponent(); m.ClearSelection2(true);
    }

    static void Guardar(ModelDoc2 m)
    {
        int e = 0, w = 0; m.ForceRebuild3(false);
        bool ok = m.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w);
        Console.WriteLine((ok ? "guardado " : "NO GUARDADO ") + Path.GetFileName(m.GetPathName()) + " e=" + e + " w=" + w);
    }

    // traslada cada componente cuyo origen está cerca de una esquina vieja (a menos de 'radio') a la esquina nueva
    static void MoverPatas(ModelDoc2 m, AssemblyDoc asm, params string[] prefijos)
    {
        var inv = Arbol().Inv();
        foreach (var c in ((object[])asm.GetComponents(true)).Cast<Component2>().Where(c => prefijos.Any(p => c.Name2.StartsWith(p))).ToList())
        {
            var x = Xf.FromComp(c); var t = inv.Apply(x.T);
            int k = Enumerable.Range(0, 4).OrderBy(i => Math.Pow(t[0] - VIEJAS[i][0], 2) + Math.Pow(t[1] - VIEJAS[i][1], 2)).First();
            double d = Math.Sqrt(Math.Pow(t[0] - VIEJAS[k][0], 2) + Math.Pow(t[1] - VIEJAS[k][1], 2));
            if (d > 90) continue;
            var dw = Arbol().Apply(V(NUEVAS[k][0] - VIEJAS[k][0], NUEVAS[k][1] - VIEJAS[k][1], 0));
            Poner(m, c, x.Then(Xf.Tr(dw[0], dw[1], 0)));
            Console.WriteLine("  " + c.Name2 + " esquina " + k + " (estaba a " + d.ToString("0") + " mm)");
        }
    }

    // ranuras de tensado del módulo del motor de J1 (entre cada par de agujeros)
    static void Ranuras()
    {
        PB.Sketch('F');
        foreach (double sx in new[] { -1.0, 1.0 }) foreach (var yy in new[] { V(171.74, 178.19), V(106.94, 113.39) })
            PB.Rect(sx * 32.4 - 2.15, yy[0], sx * 32.4 + 2.15, yy[1]);
        PB.Extrude(-ESP - 1, ESP + 2, true, "Ranuras_modulo_motor_J1");
    }

    // corrección sobre lo ya hecho: ranuras del motor, agujeros de la bandeja con BX = 47, y la bandeja corrida +20 en x
    static void Ajuste()
    {
        var p = SW.Open(P("CB-111_Tablero_Madera_400x300x16.SLDPRT")); PB.Use(p);
        int er = 0; SW.App.ActivateDoc3(p.GetTitle(), false, 0, ref er);
        if (((PartDoc)p).FeatureByName("Ranuras_modulo_motor_J1") == null)
        {
            p.ClearSelection2(true);
            p.Extension.SelectByID2("Bandeja_electronica_4xD4.5", "BODYFEATURE", 0, 0, 0, false, 0, null, 0);
            p.Extension.DeleteSelection2((int)swDeleteSelectionOptions_e.swDelete_Absorbed);
            Ranuras();
            PB.Sketch('F');
            foreach (var h in new[] { V(169.26, 92.2), V(109.26, 92.2), V(179.26, -65.8), V(99.26, -65.8) })
            { var n = BandejaVieja().Inv().Then(BandejaNueva()).Apply(V(h[0], h[1], 0)); PB.Circle(n[0], n[1], 4.5); Console.WriteLine("  bandeja agujero " + n[0].ToString("0.0") + ", " + n[1].ToString("0.0")); }
            PB.Extrude(-ESP - 1, ESP + 2, true, "Bandeja_electronica_4xD4.5");
            PB.Report("CB-111");
            int e = 0, w = 0; p.ForceRebuild3(false); Console.WriteLine("tablero guardado " + p.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w));
        }
        var m = SW.Open(P("CB-100_Base_J0.SLDASM")); var asm = (AssemblyDoc)m;
        SW.App.ActivateDoc3(m.GetTitle(), false, 0, ref er);
        asm.ResolveAllLightWeightComponents(false);
        var c = ((object[])asm.GetComponents(true)).Cast<Component2>().First(k => k.Name2.StartsWith("CB-140"));
        var t = Arbol().Inv().Apply(Xf.FromComp(c).T);
        Console.WriteLine("bandeja en x = " + t[0].ToString("0.0") + ", y = " + t[1].ToString("0.0"));
        if (Math.Abs(t[0] - BX) > 0.5)
        {
            var d = Arbol().Apply(V(BX - t[0], 0, 0));
            Poner(m, c, Xf.FromComp(c).Then(Xf.Tr(d[0], d[1], 0)));
            Guardar(m);
        }
    }

    // ---------- CB-100: tornillería de las patas y bandeja ----------
    static void Patas()
    {
        var m = SW.Open(P("CB-100_Base_J0.SLDASM")); var asm = (AssemblyDoc)m;
        int er = 0; SW.App.ActivateDoc3(m.GetTitle(), false, 0, ref er);
        asm.ResolveAllLightWeightComponents(false);
        MoverPatas(m, asm, "HW_M5x30", "HW_Tuerca_M5", "HW_Arandela_M5");
        Guardar(m);
    }

    static void Bandeja()
    {
        var m = SW.Open(P("CB-100_Base_J0.SLDASM")); var asm = (AssemblyDoc)m;
        int er = 0; SW.App.ActivateDoc3(m.GetTitle(), false, 0, ref er);
        asm.ResolveAllLightWeightComponents(false);
        // la bandeja y su tornillería (si está suelta en CB-100): todo lo que cae dentro de la huella vieja
        var mov = BandejaVieja().Inv().Then(BandejaNueva());
        var a2w = Arbol(); var w2a = a2w.Inv();
        var delta = w2a.Then(mov).Then(a2w);   // movimiento en el mundo
        foreach (var c in ((object[])asm.GetComponents(true)).Cast<Component2>().ToList())
        {
            var t = w2a.Apply(Xf.FromComp(c).T);
            bool esBandeja = c.Name2.StartsWith("CB-140");
            bool tornillo = (c.Name2.StartsWith("HW_M4x30") || c.Name2.StartsWith("HW_Tuerca_M4") || c.Name2.StartsWith("HW_Arandela_M4"))
                            && t[0] > 85 && t[0] < 195 && t[1] > -115 && t[1] < 104;
            if (!esBandeja && !tornillo) continue;
            Poner(m, c, Xf.FromComp(c).Then(delta));
            Console.WriteLine("  movido " + c.Name2);
        }
        Guardar(m);
    }

    // ---------- CB-150: mástil en el pie nuevo, re-apuntado ----------
    static void Camaras()
    {
        var m = SW.Open(P("CB-150_Modulo_Camaras.SLDASM")); var asm = (AssemblyDoc)m;
        int er = 0; SW.App.ActivateDoc3(m.GetTitle(), false, 0, ref er);
        asm.ResolveAllLightWeightComponents(false);
        var mast = Mast(FX0, FY0, YAW0).Inv().Then(Mast(FX1, FY1, YAW1));
        var bar = Bar(FX0, FY0, YAW0, PITCH0).Inv().Then(Bar(FX1, FY1, YAW1, PITCH1));
        foreach (var c in ((object[])asm.GetComponents(true)).Cast<Component2>().ToList())
        {
            bool fijo = c.Name2.StartsWith("CB-151") || c.Name2.StartsWith("CB-152") || c.Name2.StartsWith("CB-153");
            Poner(m, c, Xf.FromComp(c).Then(fijo ? mast : bar));
            Console.WriteLine("  " + c.Name2 + (fijo ? " mastil" : " cabezal"));
        }
        m.Extension.CustomPropertyManager[""].Add3("Descripcion", (int)swCustomInfoType_e.swCustomInfoText,
            "Modulo de camaras estereo: mastil en la esquina trasera +y del tablero 400 x 300 (pie en x -80, y 130) + soporte StereoVision_03 con 2 x C270 (71 mm entre lentes). " +
            "Rumbo -15.6 grados, 43.8 grados hacia abajo, al centro de la esfera. Marco: origen en el eje J1 sobre el tablero, x hacia el arbol.", 2);
        Guardar(m);
    }
}
