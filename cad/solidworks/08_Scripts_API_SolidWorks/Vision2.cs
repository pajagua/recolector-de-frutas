using System;
using System.IO;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Vision2.exe <root> <stl_base> <stl_tapa> [importar|adaptador|asm150|stl ...]   (2026-10-27)
// Reemplaza la barra estéreo propia (CB-154 barra + 2 x CB-155) por el soporte StereoVision_03 del usuario
// (CB-156 base + 2 x CB-157 tapa: cada C270 se cuelga de la pared con su pinza de monitor; 71 mm entre lentes).
// CB-154 pasa a ser el adaptador entre el pivote del cabezal y la base del soporte. Mástil, pie y cabezal no cambian.
class Vision2
{
    static string R;
    const string BASE = "01_Base_J0", EL = "05_Comerciales\\Electronica";
    static string P(string sub, string file) { return Path.Combine(R, sub, file); }
    static double[] V(params double[] v) { return v; }

    const double FX = -290, FY = 222, H = 638;
    const double YAW = -17.96, PITCH = 32.09;          // recalculados con la lente del soporte nuevo
    const double TX = 40.3, TZ = 8;                    // soporte en el marco barra: pared (y_h = 0) en x = TX, base en z = TZ
    const double ZONA1 = 22, ZONA2 = 93;               // centros de los dos tramos del soporte (x_h): 71 mm entre lentes
    const double LENS_Y = -6.5, LENS_Z = 50;           // origen de la C270 en el marco del soporte (aprox.: medir)

    static void Main(string[] args)
    {
        R = args[0];
        SW.Connect(); SW.CloseMine();
        var w = args.Skip(3).ToList();
        Func<string, bool> want = k => w.Count == 0 || w.Contains(k);
        if (want("importar")) { Importar(args[1], "CB-156_Soporte_Estereo_C270_Base", "Base del soporte estereo StereoVision_03 (pared de 5.3 mm para las pinzas de las C270, 71 mm entre centros)");
                                Importar(args[2], "CB-157_Soporte_Estereo_C270_Tapa", "Tapa del soporte estereo StereoVision_03 (traba la pinza de una C270; 2 por soporte)"); }
        if (want("basenativa")) BaseNativa();
        if (want("adaptador")) Adaptador();
        if (want("asm150")) Asm150();
        if (want("stl")) Stl();
    }

    static void Importar(string stl, string nombre, string desc)
    {
        SW.App.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swImportStlVrmlModelType, (int)swImportStlVrmlModelType_e.swImportStlVrmlModelType_Solid);
        SW.App.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swImportStlVrmlUnits, (int)swLengthUnit_e.swMM);
        int err = 0; var m = (ModelDoc2)SW.App.LoadFile4(stl, "r", null, ref err);
        if (m == null) throw new Exception("no se pudo importar " + stl + " err " + err);
        SW.Created.Add(m.GetTitle());
        var bs = (object[])((PartDoc)m).GetBodies2((int)swBodyType_e.swSolidBody, true);
        Console.WriteLine(nombre + ": cuerpos solidos " + (bs == null ? 0 : bs.Length) + (bs != null ? " " + SW.Box((double[])((Body2)bs[0]).GetBodyBox()) : ""));
        m.MaterialPropertyValues = new double[] { 0.95, 0.55, 0.1, 1, 1, 0.4, 0.3, 0, 0 };
        var cpm = m.Extension.CustomPropertyManager[""];
        cpm.Add3("Descripcion", (int)swCustomInfoType_e.swCustomInfoText, desc, 2);
        cpm.Add3("Material", (int)swCustomInfoType_e.swCustomInfoText, "PETG impreso", 2);
        cpm.Add3("Notas", (int)swCustomInfoType_e.swCustomInfoText, "Importado del STL del usuario (StereoVision_03, malla). Para imprimir: cad/imprimir/" + nombre + ".stl", 2);
        int e = 0, ww = 0;
        bool ok = m.Extension.SaveAs3(P(BASE, nombre + ".SLDPRT"), 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, null, ref e, ref ww);
        Console.WriteLine((ok ? "saved " : "SAVE FAIL ") + nombre + " e=" + e);
        SW.App.CloseDoc(m.GetTitle());
    }

    // base del soporte remodelada nativa con las cotas del STL (el STL de 24 714 triángulos no entra como sólido)
    static void BaseNativa()
    {
        PB.NewPart();
        PB.Sketch('F'); PB.Rect(0, -10, 44, 25.3); PB.Rect(71, -10, 115, 25.3); PB.Extrude(0, 3, false, "Placa_tramos_camaras_3mm");
        PB.Sketch('F'); PB.Rect(44, -9, 71, 24.3); PB.Extrude(0, 2, false, "Placa_centro_2mm");
        PB.Sketch('F'); PB.Rect(0, 0, 44, 5.3); PB.Rect(71, 0, 115, 5.3); PB.Extrude(0, 18, false, "Pared_5.3_pinzas");
        PB.Sketch('F'); PB.Rect(44, 1, 71, 4.3); PB.Extrude(0, 10, false, "Pared_centro");
        PB.Sketch('F'); foreach (var x in new[] { 0.0, 38.5, 71.0, 109.5 }) PB.Rect(x, 0, x + 5.5, 5.3); PB.Extrude(18, 7, false, "Postes");
        PB.Sketch('F'); foreach (var x in new[] { 2.75, 41.25, 73.75, 112.25 }) PB.Circle(x, 2.65, 3.25); PB.Extrude(18, 8, true, "Agujeros_postes_D3.25");
        PB.Sketch('F'); foreach (double x in new[] { 50.0, 65.0 }) foreach (double y in new[] { -6.0, 15.0 }) PB.Circle(x, y, 3.2); PB.Extrude(-1, 5, true, "Agujeros_4xM3_adaptador");
        PB.PartColor(0.95, 0.55, 0.1);
        PB.Prop("Descripcion", "Base del soporte estereo StereoVision_03: cada C270 se cuelga con su pinza de la pared de 5.3 mm; 71 mm entre centros de tramo");
        PB.Prop("Material", "PETG impreso");
        PB.Prop("Notas", "Remodelada con las cotas del STL del usuario (para imprimir: cad/imprimir/CB-156_Soporte_Estereo_C270_Base.stl). " +
            "4 agujeros D3.2 en la placa del centro para el adaptador CB-154 (4 x M3x12 + tuerca).");
        PB.Report("Base soporte"); PB.Save(P(BASE, "CB-156_Soporte_Estereo_C270_Base.SLDPRT")); SW.App.CloseDoc(PB.M.GetTitle());
    }

    // adaptador (marco barra: origen en el pivote, x adelante, y izquierda, z arriba)
    static void Adaptador()
    {
        PB.NewPart();
        PB.Sketch('F'); PB.RRect(TX - 25.8, -60.5, TX + 11.5, 60.5, 4); PB.Extrude(0, TZ, false, "Placa_121x37x8");
        PB.Sketch('F'); PB.Rect(-10, -5, 10, 5); PB.Extrude(-10, TZ + 10, false, "Lengueta_pivote");
        PB.Sketch('F'); PB.Rect(9, -5, TX - 25, 5); PB.Extrude(0, TZ, false, "Union_lengueta_placa");
        PB.Sketch('T'); PB.Circle(0, 0, 5.3); PB.Extrude(-6, 12, true, "Pivote_M5");
        // 4 x M3 en la zona del medio del soporte (x_h 50 y 65; y_h -6 y +15) -> marco barra (x = TX - y_h, y = x_h - 57.5)
        PB.Sketch('F'); foreach (double xb in new[] { TX + 6, TX - 15 }) foreach (double yb in new[] { -7.5, 7.5 }) PB.Circle(xb, yb, 3.4);
        PB.Extrude(-1, TZ + 2, true, "Agujeros_4xM3");
        PB.Sketch('F'); foreach (double xb in new[] { TX + 6, TX - 15 }) foreach (double yb in new[] { -7.5, 7.5 }) Hex(xb, yb, 3.3);
        PB.Extrude(-1, 4, true, "Alojamiento_tuercas_M3");
        PB.Sketch('F'); PB.Rect(TX - 25.8, -60.5, TX + 10.5, -57.8); PB.Rect(TX - 25.8, 57.8, TX + 10.5, 60.5); PB.Extrude(TZ, 3, false, "Topes_extremos_soporte");
        PB.PartColor(0.95, 0.55, 0.1);
        PB.Prop("Descripcion", "Adaptador del soporte estereo StereoVision_03 al pivote de cabeceo del cabezal del mastil");
        PB.Prop("Material", "PETG impreso 100 %");
        PB.Prop("Notas", "La base del soporte (CB-156) apoya en la placa entre los dos topes y se fija con 4 x M3x12 + tuerca (agujeros ya hechos en la base). Pivote M5x30 + tuerca autoblocante.");
        PB.Report("Adaptador"); PB.Save(P(BASE, "CB-154_Adaptador_Soporte_Estereo.SLDPRT")); SW.App.CloseDoc(PB.M.GetTitle());
    }

    static void Hex(double cx, double cy, double r)
    {
        var p = new double[12];
        for (int i = 0; i < 6; i++) { p[2 * i] = cx + r * Math.Cos(Math.PI / 3 * i); p[2 * i + 1] = cy + r * Math.Sin(Math.PI / 3 * i); }
        PB.Poly(p);
    }
    static double Rad(double d) { return d * Math.PI / 180; }
    static Xf Mast() { double c = Math.Cos(Rad(YAW)), s = Math.Sin(Rad(YAW)); return Xf.Rows(V(c, s, 0), V(-s, c, 0), V(0, 0, 1), V(FX, FY, 0)); }
    static Xf Bar() { double c = Math.Cos(Rad(PITCH)), s = Math.Sin(Rad(PITCH)); return Xf.Rows(V(c, 0, -s), V(0, 1, 0), V(s, 0, c), V(0, 0, H + 15)).Then(Mast()); }
    static Xf Holder() { return Xf.Rows(V(0, 1, 0), V(-1, 0, 0), V(0, 0, 1), V(TX, -57.5, TZ)).Then(Bar()); }
    static Xf Cam(double xz) { return Xf.Rows(V(0, -1, 0), V(1, 0, 0), V(0, 0, 1), V(xz, LENS_Y, LENS_Z)).Then(Holder()); }

    static Component2 Add(ModelDoc2 m, string path, Xf x)
    {
        var a = (AssemblyDoc)m; int e = 0, w = 0;
        SW.App.DocumentVisible(false, (int)swDocumentTypes_e.swDocPART);
        SW.App.OpenDoc6(path, (int)swDocumentTypes_e.swDocPART, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref e, ref w);
        SW.App.DocumentVisible(true, (int)swDocumentTypes_e.swDocPART);
        var c = (Component2)a.AddComponent5(path, (int)swAddComponentConfigOptions_e.swAddComponentConfigOptions_CurrentSelectedConfig, "", false, "", 0, 0, 0);
        if (c == null) throw new Exception("AddComponent fallo " + path);
        c.Transform2 = x.ToMath();
        m.ClearSelection2(true); c.Select4(false, null, false); a.FixComponent(); m.ClearSelection2(true);
        return c;
    }
    // rearma CB-150 en el mismo archivo (CB-100 lo sigue encontrando)
    static void Asm150()
    {
        var m = SW.Open(P(BASE, "CB-150_Modulo_Camaras.SLDASM"));
        int er = 0; SW.App.ActivateDoc3(m.GetTitle(), false, 0, ref er);
        var a = (AssemblyDoc)m; a.ResolveAllLightWeightComponents(false);
        foreach (Component2 c in ((object[])a.GetComponents(true)).Cast<Component2>().ToList())
        { m.ClearSelection2(true); c.Select4(false, null, false); m.Extension.DeleteSelection2(0); }
        m.ClearSelection2(true);
        Add(m, P(BASE, "CB-151_Pie_Mastil_Camaras.SLDPRT"), Mast());
        Add(m, P(BASE, "CB-152_Mastil_Camaras_20x20x1.5.SLDPRT"), Mast());
        Add(m, P(BASE, "CB-153_Cabezal_Mastil.SLDPRT"), Mast());
        Add(m, P(BASE, "CB-154_Adaptador_Soporte_Estereo.SLDPRT"), Bar());
        Add(m, P(BASE, "CB-156_Soporte_Estereo_C270_Base.SLDPRT"), Holder());
        foreach (double x0 in new[] { 0.0, 71.0 }) Add(m, P(BASE, "CB-157_Soporte_Estereo_C270_Tapa.SLDPRT"), Xf.Tr(x0, 0, 25).Then(Holder()));
        foreach (double xz in new[] { ZONA2, ZONA1 })
        {
            Add(m, P(EL, "CB-515_Camara_Logitech_C270.SLDPRT"), Cam(xz));
            var lens = Xf.Tr(2, 0, 0).Then(Cam(xz)).T;
            var o = Cam(xz).Apply(V(0, 0, 0)); var f = Cam(xz).Apply(V(1, 0, 0));
            Console.WriteLine((xz == ZONA2 ? "izq" : "der") + " lente (marco arbol): " + string.Join(", ", lens.Select(v => v.ToString("0.0"))) +
                "  eje: " + string.Join(", ", new[] { f[0] - o[0], f[1] - o[1], f[2] - o[2] }.Select(v => v.ToString("0.0000"))));
        }
        m.Extension.CustomPropertyManager[""].Add3("Descripcion", (int)swCustomInfoType_e.swCustomInfoText,
            "Modulo de camaras estereo: mastil en la esquina trasera del tablero + soporte StereoVision_03 con 2 x C270 (71 mm entre lentes). Marco: origen en el eje J1 sobre el tablero, x hacia el arbol.", 2);
        int e = 0, w = 0; m.ForceRebuild3(false);
        bool ok = m.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w);
        Console.WriteLine((ok ? "saved " : "SAVE FAIL ") + m.GetPathName() + " e=" + e + " w=" + w);
        SW.App.CloseDoc(m.GetTitle());
    }
    static void Stl()
    {
        var m = SW.Open(P(BASE, "CB-154_Adaptador_Soporte_Estereo.SLDPRT"));
        int e = 0, w = 0;
        string dst = Path.Combine(R, "..", "imprimir", "CB-154_Adaptador_Soporte_Estereo.stl");
        bool ok = m.Extension.SaveAs3(Path.GetFullPath(dst), 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent | (int)swSaveAsOptions_e.swSaveAsOptions_Copy, null, null, ref e, ref w);
        Console.WriteLine((ok ? "stl " : "STL FAIL ") + Path.GetFullPath(dst));
        SW.App.CloseDoc(m.GetTitle());
    }
}
