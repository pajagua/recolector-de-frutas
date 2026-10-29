using System;
using System.IO;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Vision.exe <root> [partes|asm150|base ...]   (2026-10-27)
// Mástil de las cámaras estéreo (2 x Logitech C270), fijo a la base (eslabón 0, se mueve con el riel).
// Marco "árbol" (el del ensamblaje CB-150): origen en el eje J1 sobre la cara del tablero, x hacia el árbol
// (normal al borde de 194.8 mm), y lateral (z x x), z arriba. El brazo, apuntando al árbol, trabaja en y = -82;
// el mástil va en la esquina trasera del lado +y. En el mundo de CB-000: x_arbol = (cos -112.5°, sin -112.5°, 0).
class Vision
{
    static string R;
    const string BASE = "01_Base_J0", EL = "05_Comerciales\\Electronica";
    static string P(string sub, string file) { return Path.Combine(R, sub, file); }
    static double[] V(params double[] v) { return v; }

    // ---- geometría (mm, grados) ----
    const double FX = -290, FY = 222;          // centro del pie del mástil
    const double YAW = -17.96, PITCH = 31.11;  // rumbo hacia la esfera y cabeceo hacia abajo
    const double H = 638;                      // cara superior del bloque del cabezal (pivote a H + 15)
    const double BASELINE = 120;               // distancia entre los centros de las lentes
    const double TREE_ANG = -112.5;            // ángulo del eje x del marco árbol en el mundo de CB-000

    static void Main(string[] args)
    {
        R = args[0];
        SW.Connect();
        SW.CloseMine();
        var w = args.Skip(1).ToList();
        Func<string, bool> want = k => w.Count == 0 || w.Contains(k);
        if (want("partes")) { Camara(); Soporte(); Barra(); Cabezal(); Mastil(); Pie(); }
        if (want("asm150")) Asm150();
        if (want("base")) Base();
    }

    static void Done(string tag, string path) { PB.Report(tag); PB.Save(path); SW.App.CloseDoc(PB.M.GetTitle()); }

    // C270 (marco local: lente en x = 2 sobre el eje x, mira a +x; y a la izquierda; z arriba)
    static void Camara()
    {
        PB.NewPart();
        PB.Sketch('R'); PB.RRect(-16, -36.5, 16, 36.5, 8); var body = PB.Extrude(-22, 22, false, "Cuerpo_73x32x22");
        PB.Sketch('R'); PB.Circle(0, 0, 14); var lens = PB.Extrude(0, 2, false, "Lente_D14");
        PB.Sketch('R'); PB.Circle(0, 0, 4.5); var cab = PB.Extrude(-52, 30, false, "Cable_USB");
        PB.Sketch('R'); PB.Circle(0, 24, 3); PB.Extrude(0, 0.5, false, "LED");
        PB.PartColor(0.08, 0.08, 0.09); PB.Color(lens, 0.25, 0.27, 0.30, 0.9);
        PB.Prop("Descripcion", "Webcam Logitech C270 (720p, foco fijo, ~55 grados de campo diagonal), sin la pinza de monitor");
        PB.Prop("Notas", "Modelo simplificado: cuerpo 73 x 32 x 22, lente D14 centrada (en la real esta algo corrida). Origen en la cara frontal, sobre el eje optico.");
        Done("C270", P(EL, "CB-515_Camara_Logitech_C270.SLDPRT"));
    }
    static void Soporte()
    {
        PB.NewPart();
        PB.Sketch('R'); PB.Rect(-20, -40, -16, 40); PB.Extrude(-24, 22, false, "Base");
        PB.Sketch('R'); PB.Rect(-20, -40, 6, 40); PB.Extrude(-27, 3, false, "Respaldo");
        PB.Sketch('R'); PB.Rect(-16, 36.6, -6, 40); PB.Rect(-16, -40, -6, -36.6); PB.Extrude(-24, 22, false, "Topes_laterales");
        PB.Sketch('R'); PB.Circle(0, 0, 7); PB.Extrude(-28, 5, true, "Paso_cable");
        PB.Sketch('F'); PB.Circle(-13, 25, 3.4); PB.Circle(-13, -25, 3.4); PB.Extrude(-21, 6, true, "Agujeros_2xM3");
        PB.PartColor(0.95, 0.55, 0.1);
        PB.Prop("Descripcion", "Soporte de una C270 sobre la barra estereo (cuna con respaldo y topes laterales)");
        PB.Prop("Material", "PETG impreso, 4 perimetros");
        PB.Prop("Notas", "Coordenadas de la camara. 2 x M3x10 + tuerca a la barra. La camara se asegura con un precinto alrededor del cuerpo y el respaldo.");
        Done("Soporte C270", P(BASE, "CB-155_Soporte_C270.SLDPRT"));
    }
    // barra estéreo (marco barra: origen en el pivote, x hacia adelante, y a la izquierda, z arriba)
    static void Barra()
    {
        PB.NewPart();
        PB.Sketch('F'); PB.Rect(-10, -90, 44, 90); PB.Extrude(16, 8, false, "Placa_180x54x8");
        PB.Sketch('F'); PB.Rect(-10, -5, 10, 5); PB.Extrude(-10, 26, false, "Lengueta_pivote");
        PB.Sketch('T'); PB.Circle(0, 0, 5.3); PB.Extrude(-6, 12, true, "Pivote_M5");
        PB.Sketch('F'); foreach (double y in new[] { 35.0, 85.0, -35.0, -85.0 }) PB.Circle(25, y, 3.4); PB.Extrude(15, 10, true, "Agujeros_4xM3_soportes");
        PB.PartColor(0.95, 0.55, 0.1);
        PB.Prop("Descripcion", "Barra estereo: dos C270 a " + BASELINE + " mm entre lentes, paralelas, con lengueta para el pivote de cabeceo");
        PB.Prop("Material", "PETG impreso 100 % (o perfil de aluminio con la lengueta impresa): tiene que ser rigida, si se tuerce hay que recalibrar");
        Done("Barra", P(BASE, "CB-154_Barra_Estereo.SLDPRT"));
    }
    // cabezal (marco mástil: origen en el tablero, a = adelante (rumbo), b = izquierda, z arriba)
    static void Cabezal()
    {
        PB.NewPart();
        PB.Sketch('F'); PB.Rect(-15, -15, 15, 15); PB.Extrude(H - 30, 30, false, "Bloque_30x30x30");
        PB.Sketch('F'); PB.Rect(-10.2, -10.2, 10.2, 10.2); PB.Extrude(H - 31, 26, true, "Encastre_tubo_20");
        PB.Sketch('F'); PB.Rect(-12, 5.5, 12, 11.5); PB.Rect(-12, -11.5, 12, -5.5); PB.Extrude(H, 25, false, "Orejas_pivote");
        PB.Sketch('T'); PB.Circle(0, H + 15, 5.3); PB.Extrude(-12, 24, true, "Pivote_M5");
        PB.Sketch('R'); PB.Circle(H - 18, 0, 3.3); PB.Extrude(0, 16, true, "Prisionero_M4");
        PB.PartColor(0.95, 0.55, 0.1);
        PB.Prop("Descripcion", "Cabezal del mastil: encastre del tubo 20 x 20 y orejas del pivote de cabeceo (M5) de la barra estereo");
        PB.Prop("Material", "PETG impreso 100 %");
        PB.Prop("Notas", "Marco del mastil (girado " + YAW + " grados hacia la esfera). Prisionero M4 contra el tubo. Pivote: M5x30 + tuerca autoblocante, apretar en el angulo de " + PITCH + " grados.");
        Done("Cabezal", P(BASE, "CB-153_Cabezal_Mastil.SLDPRT"));
    }
    static void Mastil()
    {
        PB.NewPart();
        PB.Sketch('F'); PB.Rect(-10, -10, 10, 10); PB.Rect(-8.5, -8.5, 8.5, 8.5); PB.Extrude(6, H - 11, false, "Tubo_20x20x1.5");
        PB.PartColor(0.84, 0.85, 0.88);
        PB.Prop("Descripcion", "Mastil de las camaras: tubo cuadrado de aluminio 20 x 20 x 1.5, largo " + (H - 11) + " mm");
        PB.Prop("Material", "Aluminio 6063");
        Done("Mastil", P(BASE, "CB-152_Mastil_Camaras_20x20x1.5.SLDPRT"));
    }
    static void Pie()
    {
        PB.NewPart();
        PB.Sketch('F'); PB.RRect(-30, -30, 30, 30, 5); PB.Extrude(0, 6, false, "Placa_60x60x6");
        PB.Sketch('F'); PB.Rect(-15, -15, 15, 15); PB.Extrude(6, 30, false, "Encastre");
        PB.Sketch('F'); PB.Rect(-10.2, -10.2, 10.2, 10.2); PB.Extrude(6, 31, true, "Encastre_tubo_20");
        PB.Sketch('F'); foreach (double a in new[] { -22.0, 22.0 }) foreach (double b in new[] { -22.0, 22.0 }) PB.Circle(a, b, 4.5); PB.Extrude(-1, 8, true, "Agujeros_4_tirafondos");
        PB.Sketch('R'); PB.Circle(21, 0, 3.3); PB.Extrude(0, 16, true, "Prisionero_M4");
        PB.PartColor(0.95, 0.55, 0.1);
        PB.Prop("Descripcion", "Pie del mastil de las camaras, atornillado al tablero en la esquina trasera");
        PB.Prop("Material", "PETG impreso 100 %");
        PB.Prop("Notas", "4 tirafondos 4 x 12 (no pasantes: debajo esta la pata de la base). Prisionero M4 contra el tubo.");
        Done("Pie", P(BASE, "CB-151_Pie_Mastil_Camaras.SLDPRT"));
    }

    // ---- transformaciones ----
    static double Rad(double d) { return d * Math.PI / 180; }
    static Xf Mast() { double c = Math.Cos(Rad(YAW)), s = Math.Sin(Rad(YAW)); return Xf.Rows(V(c, s, 0), V(-s, c, 0), V(0, 0, 1), V(FX, FY, 0)); }
    static Xf Bar() { double c = Math.Cos(Rad(PITCH)), s = Math.Sin(Rad(PITCH)); return Xf.Rows(V(c, 0, -s), V(0, 1, 0), V(s, 0, c), V(0, 0, H + 15)).Then(Mast()); }
    static Xf Cam(double side) { return Xf.Tr(38, side * BASELINE / 2, 44).Then(Bar()); }
    public static Xf TreeToWorld() { double c = Math.Cos(Rad(TREE_ANG)), s = Math.Sin(Rad(TREE_ANG)); return Xf.Rows(V(c, s, 0), V(-s, c, 0), V(0, 0, 1), V(0, 0, 0)); }

    static Component2 Add(ModelDoc2 m, string path, Xf x)
    {
        var a = (AssemblyDoc)m; int e = 0, w = 0;
        int type = path.ToUpper().EndsWith(".SLDASM") ? (int)swDocumentTypes_e.swDocASSEMBLY : (int)swDocumentTypes_e.swDocPART;
        SW.App.DocumentVisible(false, type);
        SW.App.OpenDoc6(path, type, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref e, ref w);
        SW.App.DocumentVisible(true, type);
        var c = (Component2)a.AddComponent5(path, (int)swAddComponentConfigOptions_e.swAddComponentConfigOptions_CurrentSelectedConfig, "", false, "", 0, 0, 0);
        if (c == null) throw new Exception("AddComponent fallo " + path);
        c.Transform2 = x.ToMath();
        m.ClearSelection2(true); c.Select4(false, null, false); a.FixComponent(); m.ClearSelection2(true);
        return c;
    }
    static void Asm150()
    {
        string dst = P(BASE, "CB-150_Modulo_Camaras.SLDASM");
        if (File.Exists(dst)) throw new Exception("ya existe " + dst);
        string tpl = SW.App.GetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swDefaultTemplateAssembly);
        var m = (ModelDoc2)SW.App.NewDocument(tpl, 0, 0, 0);
        SW.Created.Add(m.GetTitle());
        Add(m, P(BASE, "CB-151_Pie_Mastil_Camaras.SLDPRT"), Mast());
        Add(m, P(BASE, "CB-152_Mastil_Camaras_20x20x1.5.SLDPRT"), Mast());
        Add(m, P(BASE, "CB-153_Cabezal_Mastil.SLDPRT"), Mast());
        Add(m, P(BASE, "CB-154_Barra_Estereo.SLDPRT"), Bar());
        foreach (double side in new[] { 1.0, -1.0 })
        {
            Add(m, P(BASE, "CB-155_Soporte_C270.SLDPRT"), Cam(side));
            Add(m, P(EL, "CB-515_Camara_Logitech_C270.SLDPRT"), Cam(side));
        }
        foreach (double side in new[] { 1.0, -1.0 })
        {
            var lens = Xf.Tr(2, 0, 0).Then(Cam(side)).T;
            var ax = Cam(side); var o = ax.Apply(V(0, 0, 0)); var f = ax.Apply(V(1, 0, 0));
            Console.WriteLine((side > 0 ? "izq" : "der") + " lente (marco arbol): " + string.Join(", ", lens.Select(v => v.ToString("0.0"))) +
                "  eje: " + string.Join(", ", new[] { f[0] - o[0], f[1] - o[1], f[2] - o[2] }.Select(v => v.ToString("0.0000"))));
        }
        m.Extension.CustomPropertyManager[""].Add3("Descripcion", (int)swCustomInfoType_e.swCustomInfoText,
            "Modulo de camaras estereo (2 x C270) en un mastil en la esquina trasera del tablero. Marco: origen en el eje J1 sobre el tablero, x hacia el arbol.", 2);
        int e = 0, w = 0;
        m.ForceRebuild3(false);
        bool ok = m.Extension.SaveAs3(dst, 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, null, ref e, ref w);
        Console.WriteLine((ok ? "saved " : "SAVE FAIL ") + dst);
        SW.App.CloseDoc(m.GetTitle());
    }
    // agrega CB-150 a CB-100 (eslabón 0) en la posición que corresponde dentro de CB-000
    static void Base()
    {
        // CB-100 está en la identidad dentro de CB-000 (verificado): el marco de CB-100 es el mundo
        var m = SW.Open(P(BASE, "CB-100_Base_J0.SLDASM"));
        int er = 0; SW.App.ActivateDoc3(m.GetTitle(), false, 0, ref er);
        ((AssemblyDoc)m).ResolveAllLightWeightComponents(false);
        if (((object[])((AssemblyDoc)m).GetComponents(true)).Cast<Component2>().Any(c => c.Name2.StartsWith("CB-150"))) throw new Exception("CB-100 ya tiene CB-150");
        Add(m, P(BASE, "CB-150_Modulo_Camaras.SLDASM"), TreeToWorld());
        int e = 0, w = 0;
        m.ForceRebuild3(false);
        bool ok = m.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w);
        Console.WriteLine((ok ? "saved " : "SAVE FAIL ") + m.GetPathName() + " e=" + e + " w=" + w);
        SW.App.CloseDoc(m.GetTitle());
    }
}
