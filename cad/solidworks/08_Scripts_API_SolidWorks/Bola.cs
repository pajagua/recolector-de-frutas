using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Bola.exe <root> : pieza de pruebas CB-158 con la bola integrada para la rótula del trípode (reemplaza al perno original M6).
// Placa que se atornilla a la base del soporte estéreo (CB-156) con los 4 x M3x12 del adaptador; abajo, cuello D4.8 x 7 y bola D11.8.
// El cuello lleva de alma un M3x25 Allen: cabeza en un rebaje de la punta de la bola, tuerca en un hexágono arriba de la placa.
// Coordenadas: origen bajo el centro de las cámaras (x_h = 57.5, y_h = 4.5 del soporte), z = 0 en la cara de apoyo de la base.
class Bola
{
    static string R;
    const string DIR = "12_Pruebas", BASE = "01_Base_J0", EL = "05_Comerciales\\Electronica";
    static string P(string sub, string file) { return Path.Combine(R, sub, file); }
    static double[] V(params double[] v) { return v; }
    const double T = 12, BOLA = 11.8, CUELLO = 4.8, LCUELLO = 7;

    static void Main(string[] a)
    {
        R = a[0];
        SW.Connect(); SW.CloseMine();
        Pieza();
        Ensamblaje();
    }
    static void Hex(double cx, double cy, double af)
    {
        double r = af / Math.Sqrt(3); var p = new double[12];
        for (int i = 0; i < 6; i++) { p[2 * i] = cx + r * Math.Cos(Math.PI / 3 * i + Math.PI / 6); p[2 * i + 1] = cy + r * Math.Sin(Math.PI / 3 * i + Math.PI / 6); }
        PB.Poly(p);
    }
    static void Pieza()
    {
        double rb = BOLA / 2, zc = -T - LCUELLO - Math.Sqrt(rb * rb - CUELLO * CUELLO / 4), tip = zc - rb;
        PB.NewPart();
        PB.Sketch('F'); PB.RRect(-27.5, -15, 27.5, 20.8, 3); PB.Extrude(-T, T, false, "Placa_55x36x12");
        PB.Sketch('F'); PB.Circle(0, 0, CUELLO); PB.Extrude(-T - LCUELLO - 1, LCUELLO + 1, false, "Cuello_D4.8x7");
        // bola: revolución de media circunferencia alrededor del eje z (plano XZ)
        PB.Sketch('T');
        var c1 = PB.Map(0, zc - rb - 1); var c2 = PB.Map(0, zc + rb + 1);
        PB.M.SketchManager.CreateCenterLine(c1[0], c1[1], 0, c2[0], c2[1], 0);
        PB.Arc(0, zc, 0, zc - rb, 0, zc + rb); PB.Line(0, zc + rb, 0, zc - rb);
        var f = PB.M.FeatureManager.FeatureRevolve2(true, true, false, false, false, false, 0, 0, 2 * Math.PI, 0, false, false, 0, 0, 0, 0, 0, true, true, true);
        if (f == null) { PB.M.SketchManager.InsertSketch(true); throw new Exception("revolucion fallo"); }
        f.Name = "Bola_D11.8"; PB.M.ClearSelection2(true);
        // alma M3: paso D3.2 desde la punta de la bola hasta arriba, rebaje D6.2 x 4 para la cabeza, hexágono de la tuerca arriba (5 mm)
        PB.Sketch('F'); PB.Circle(0, 0, 3.2); PB.Extrude(tip - 1, -tip + 1, true, "Paso_alma_M3");
        PB.Sketch('F'); PB.Circle(0, 0, 6.2); PB.Extrude(tip - 1, 5, true, "Rebaje_cabeza_M3");
        PB.Sketch('F'); Hex(0, 0, 5.9); PB.Extrude(-5, 6, true, "Tuerca_alma_M3");
        // 4 x M3 a la base del soporte (x_h 50/65, y_h -6/15 -> -7.5/7.5, -10.5/10.5), tuercas desde abajo
        PB.Sketch('F'); foreach (double x in new[] { -7.5, 7.5 }) foreach (double y in new[] { -10.5, 10.5 }) PB.Circle(x, y, 3.4);
        PB.Extrude(-T - 1, T + 2, true, "Agujeros_4xM3");
        PB.Sketch('F'); foreach (double x in new[] { -7.5, 7.5 }) foreach (double y in new[] { -10.5, 10.5 }) Hex(x, y, 5.9);
        PB.Extrude(-T - 1, 9, true, "Alojamientos_tuercas_M3");
        PB.PartColor(0.2, 0.55, 0.85);
        PB.Prop("Descripcion", "Pieza de pruebas: soporte estereo CB-156 sobre la rotula del tripode, con bola D11.8 integrada (en lugar del perno M6 original)");
        PB.Prop("Material", "PETG, 4 perimetros, 50 % de relleno");
        PB.Prop("Notas", "Solo para pruebas de vision antes de montar el brazo. Alma: M3x25 Allen desde la punta de la bola + tuerca M3 en el hexagono de arriba " +
            "(antes de atornillar la base). Base: 4 x M3x12 desde arriba + tuerca en los hexagonos de abajo. Imprimir con la cara de arriba en la cama y soportes bajo la bola.");
        PB.Report("Pieza bola"); Console.WriteLine("centro de la bola z = " + zc.ToString("0.00") + ", punta z = " + tip.ToString("0.00"));
        PB.Save(P(DIR, "CB-158_Adaptador_Rotula_Bola12.SLDPRT"));
        SW.App.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swSTLQuality, (int)swSTLQuality_e.swSTLQuality_Fine);
        SW.App.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swExportStlUnits, (int)swLengthUnit_e.swMM);
        int e = 0, w = 0;
        string stl = Path.GetFullPath(Path.Combine(R, "..", "imprimir", "CB-158_Adaptador_Rotula_Bola12.stl"));
        Console.WriteLine((PB.M.Extension.SaveAs3(stl, 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent | (int)swSaveAsOptions_e.swSaveAsOptions_Copy, null, null, ref e, ref w) ? "stl " : "STL FAIL ") + stl);
        SW.App.CloseDoc(PB.M.GetTitle());
    }
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
    static void Ensamblaje()
    {
        string dst = P(DIR, "CB-159_Camaras_Prueba_Tripode.SLDASM");
        if (File.Exists(dst)) File.Delete(dst);
        string tpl = SW.App.GetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swDefaultTemplateAssembly);
        var m = (ModelDoc2)SW.App.NewDocument(tpl, 0, 0, 0);
        SW.Created.Add(m.GetTitle());
        Add(m, P(DIR, "CB-158_Adaptador_Rotula_Bola12.SLDPRT"), Xf.Tr(57.5, 4.5, 0));
        Add(m, P(BASE, "CB-156_Soporte_Estereo_C270_Base.SLDPRT"), new Xf());
        foreach (double x0 in new[] { 0.0, 71.0 }) Add(m, P(BASE, "CB-157_Soporte_Estereo_C270_Tapa.SLDPRT"), Xf.Tr(x0, 0, 25));
        foreach (double xz in new[] { 22.0, 93.0 }) Add(m, P(EL, "CB-515_Camara_Logitech_C270.SLDPRT"), Xf.Rows(V(0, -1, 0), V(1, 0, 0), V(0, 0, 1), V(xz, -6.5, 50)));
        m.Extension.CustomPropertyManager[""].Add3("Descripcion", (int)swCustomInfoType_e.swCustomInfoText,
            "Prueba de vision sin el brazo: soporte estereo con 2 C270 sobre la pieza CB-158 con bola para la rotula del tripode", 2);
        int e = 0, w = 0; m.ForceRebuild3(false);
        Console.WriteLine((m.Extension.SaveAs3(dst, 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, null, ref e, ref w) ? "saved " : "SAVE FAIL ") + dst);
        SW.App.CloseDoc(m.GetTitle());
    }
}
