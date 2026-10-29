using System;
using System.IO;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Ventosa3.exe <root> [limpiar|eje|ventosa|llave|asm|stl ...]   (v7.2c, 2026-10-28)
// La torsión gira la VENTOSA, no la rosca: la parte de arriba de la ventosa JXD (inserto de 7 mm, Ø19 con dos planos a 17)
// entra en un alojamiento doble D en la punta de CB-606. Encima hay un fondo de 3 mm: el racor de 6 mm x G1/4 baja por dentro
// del eje, apoya el anillo en el fondo y se enrosca en la ventosa, que queda apretada contra el fondo (eso la retiene).
// El racor no tiene traba de giro; para apretarlo se usa la llave de montaje CB-620 (tubo con hexágono 14.4 en la punta).
class Ventosa3
{
    static string R;
    const string HER = "09_Herramienta";
    static string P(string f) { return Path.Combine(R, HER, f); }
    static double[] V(params double[] v) { return v; }
    // eje de CB-606: (CX, CY), punta en z = ZN, el interior hacia -z
    const double CX = -22.25, CY = 22.25, ZN = 67;
    // ventosa (medidas del usuario): goma 15 de alto (labio Ø40, cuello Ø25) + inserto de 7 (Ø19, planos a 17, rosca hembra G1/4)
    const double INS_D = 19.0, INS_F = 17.0, INS_H = 7.0, GOMA_H = 15.0;
    // racor (estimado de la foto): anillo Ø16.5 x 1.8, cuello con la junta 3, rosca 4.5, hexágono 14
    const double ANILLO_D = 16.5, ANILLO_H = 1.8, CUELLO = 3.0, HEX = 14.0, HEX_H = 7.0;
    // punta del eje: alojamiento doble D 0.2 más chico de hondo que el inserto (la ventosa aprieta contra el fondo, no contra la goma)
    const double ALOJ_D = INS_D + 0.4, ALOJ_F = INS_F + 0.4, ALOJ_H = INS_H - 0.2, FONDO = CUELLO, PASO = 14.2, ASIENTO_D = 17.2, BORE = 20.5;
    static double zAloj = ZN - ALOJ_H, zFondo = zAloj - FONDO, zAsiento = zFondo - ANILLO_H;   // 60.2, 57.2, 55.4
    const string F_VENTOSA = "CB-618_Ventosa_JXD_40.SLDPRT", F_RACOR = "CB-619_Racor_6mm_G1-4.SLDPRT", F_LLAVE = "CB-620_Llave_Montaje_Racor.SLDPRT";

    static void Main(string[] a)
    {
        R = a[0];
        SW.Connect(); SW.CloseMine();
        foreach (var k in a.Skip(1))
        {
            if (k == "limpiar") Limpiar();
            if (k == "eje") Eje();
            if (k == "ventosa") Ventosa();
            if (k == "llave") Llave();
            if (k == "asm") Asm();
            if (k == "stl") { Stl("CB-606_Eje_Hueco_Polea.SLDPRT"); Stl(F_LLAVE); }
        }
    }
    static void Hex(double cx, double cy, double af)
    {
        double r = af / Math.Sqrt(3); var p = new double[12];
        for (int i = 0; i < 6; i++) { p[2 * i] = cx + r * Math.Cos(Math.PI / 3 * i + Math.PI / 6); p[2 * i + 1] = cy + r * Math.Sin(Math.PI / 3 * i + Math.PI / 6); }
        PB.Poly(p);
    }
    // círculo de diámetro d con dos planos paralelos (normales a Y) separados f
    static void DobleD(double cx, double cy, double d, double f)
    {
        double r = d / 2, h = f / 2, x = Math.Sqrt(r * r - h * h);
        PB.Line(cx - x, cy + h, cx + x, cy + h);
        PB.Line(cx + x, cy - h, cx - x, cy - h);
        PB.Arc(cx, cy, cx + x, cy - h, cx + x, cy + h);
        PB.Arc(cx, cy, cx - x, cy + h, cx - x, cy - h);
    }
    static void Save(ModelDoc2 m)
    {
        int e = 0, w = 0; m.ForceRebuild3(false);
        Console.WriteLine("guardado " + m.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w) + " " + m.GetTitle());
        SW.App.CloseDoc(m.GetTitle());
    }

    static void Limpiar()
    {
        var m = SW.Open(P("CB-600_Herramienta.SLDASM"));
        int er = 0; SW.App.ActivateDoc3(m.GetTitle(), false, 0, ref er);
        var a = (AssemblyDoc)m; a.ResolveAllLightWeightComponents(false);
        foreach (var c in ((object[])a.GetComponents(true)).Cast<Component2>().Where(c => c.Name2.StartsWith("CB-618") || c.Name2.StartsWith("CB-619") || c.Name2.StartsWith("CB-620")).ToList())
        { m.ClearSelection2(true); c.Select4(false, null, false); Console.WriteLine("  borrado " + c.Name2 + ": " + m.Extension.DeleteSelection2(0)); }
        m.ClearSelection2(true); Save(m);
    }

    // sobre CB-606 original: se rellena el hexágono abierto de la McMaster y se hace, desde la punta hacia adentro,
    // alojamiento doble D de la ventosa, fondo de 3 mm con paso para la rosca, asiento del anillo del racor y paso de Ø20.5
    static void Eje()
    {
        var m = SW.Open(P("CB-606_Eje_Hueco_Polea.SLDPRT"));
        PB.Use(m);
        if (((PartDoc)m).FeatureByName("CB_v72_Relleno_hex") != null) { Console.WriteLine("CB-606 ya modificado"); SW.App.CloseDoc(m.GetTitle()); return; }
        PB.Report("CB-606 antes");
        PB.Sketch('F'); PB.Circle(CX, CY, 20.8); PB.Extrude(51.0, ZN - 51.0, false, "CB_v72_Relleno_hex");
        PB.Sketch('F'); PB.Circle(CX, CY, BORE); PB.Extrude(50.9, zAsiento - 50.9, true, "CB_v72_Paso_D20.5");
        PB.Sketch('F'); PB.Circle(CX, CY, ASIENTO_D); PB.Extrude(zAsiento - 0.01, ANILLO_H + 0.01, true, "CB_v72_Asiento_anillo_racor");
        PB.Sketch('F'); PB.Circle(CX, CY, PASO); PB.Extrude(zFondo - 0.1, FONDO + 0.2, true, "CB_v72_Paso_rosca_D14.2");
        PB.Sketch('F'); DobleD(CX, CY, ALOJ_D, ALOJ_F); PB.Extrude(zAloj, ALOJ_H + 0.1, true, "CB_v72_Alojamiento_ventosa_D19.4_planos17.4");
        PB.Report("CB-606 despues");
        Console.WriteLine(string.Format("paso D20.5 hasta z {0:0.0}, asiento {0:0.0}..{1:0.0}, fondo {1:0.0}..{2:0.0}, doble D {2:0.0}..{3:0.0}", zAsiento, zFondo, zAloj, ZN));
        PB.Prop("Notas", "Punta v7.2: la parte de arriba de la ventosa JXD (D19 con planos a 17, 7 de alto) entra en el alojamiento doble D de 6.8 y el eje la gira. " +
            "Encima, fondo de 3 mm: el racor 6 mm x G1/4 baja por dentro del eje, apoya el anillo en el asiento D17.2 y se enrosca en la ventosa (llave de montaje CB-620), " +
            "que queda apretada contra el fondo. Imprimir con la polea abajo (la punta arriba), PETG al 100 %.");
        int e = 0, w = 0; m.ForceRebuild3(false);
        Console.WriteLine("guardado " + m.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w));
        SW.App.CloseDoc(m.GetTitle());
    }

    static void Revolucion(string nombre, params double[] rz)
    {
        PB.Sketch('T');
        double zmin = double.MaxValue, zmax = double.MinValue;
        for (int i = 1; i < rz.Length; i += 2) { zmin = Math.Min(zmin, rz[i]); zmax = Math.Max(zmax, rz[i]); }
        var c1 = PB.Map(0, zmin - 1); var c2 = PB.Map(0, zmax + 1);
        PB.M.SketchManager.CreateCenterLine(c1[0], c1[1], 0, c2[0], c2[1], 0);
        PB.Poly(rz);
        var f = PB.M.FeatureManager.FeatureRevolve2(true, true, false, false, false, false, 0, 0, 2 * Math.PI, 0, false, false, 0, 0, 0, 0, 0, true, true, true);
        if (f == null) { PB.M.SketchManager.InsertSketch(true); throw new Exception("revolucion fallo " + nombre); }
        f.Name = nombre; PB.M.ClearSelection2(true);
    }
    // ventosa (local: cara de arriba del inserto en z = 0, goma desde z = -7, labio en z = -22)
    static void Ventosa()
    {
        double c = 3 * Math.Cos(Math.PI / 4), zg = -INS_H, zl = -INS_H - GOMA_H;
        PB.NewPart();
        Revolucion("Goma_D40x15", 0, zg, 12.5, zg, 12.5, zg - 5, 20 - c, zl + c, 20, zl, 0, zl);
        PB.Sketch('F'); DobleD(0, 0, INS_D, INS_F); var ins = PB.Extrude(zg - 0.01, INS_H + 0.01, false, "Inserto_D19_planos17");
        PB.Sketch('F'); PB.Circle(0, 0, 13.3); PB.Extrude(zg, INS_H + 0.5, true, "Rosca_G1-4_hembra_x7");
        PB.Sketch('F'); PB.Circle(0, 0, 4); PB.Extrude(zl - 0.5, GOMA_H + 1, true, "Paso_de_aire_D4");
        PB.PartColor(0.12, 0.25, 0.75); PB.Color(ins, 0.8, 0.8, 0.82);
        PB.Prop("Descripcion", "Ventosa JXD D40 (medidas del usuario): goma con labio D40 y cuello D25 (15 de alto) + inserto de 7 (D19 con dos planos a 17, rosca hembra G1/4)");
        PB.Prop("Notas", "Goma: revolucion del boceto del usuario (labio a 45 grados de 3 mm, cono hasta D25 a 10 mm). Angulo y labio aproximados. Rosca no modelada (agujero D13.3).");
        PB.Report("Ventosa"); PB.Save(P(F_VENTOSA)); SW.App.CloseDoc(PB.M.GetTitle());
    }
    // llave de montaje: tubo D19.8 (pasa por el paso de D20.5) con hexágono de 14.4 en la punta y mango hexagonal de 24 arriba
    static void Llave()
    {
        double L = zAsiento + 8;   // desde el anillo del racor hasta 8 mm afuera del extremo de la polea
        PB.NewPart();
        PB.Sketch('F'); PB.Circle(0, 0, 19.8); PB.Extrude(0, L, false, "Tubo_D19.8");
        PB.Sketch('F'); Hex(0, 0, 24); PB.Extrude(L, 12, false, "Mango_hex_24");
        PB.Sketch('F'); Hex(0, 0, HEX + 0.4); PB.Extrude(-0.1, HEX_H + 2.1, true, "Hexagono_racor_14.4");
        PB.Sketch('F'); PB.Circle(0, 0, 13.6); PB.Extrude(HEX_H + 1.9, L + 13, true, "Paso_cuerpo_y_collar");
        PB.PartColor(0.95, 0.55, 0.1);
        PB.Prop("Descripcion", "CB-620 Llave de montaje del racor: se mete por el lado de la polea y aprieta el racor contra la ventosa (no queda en el robot)");
        PB.Prop("Material", "PETG, 4 perimetros, 50 %");
        PB.Prop("Notas", "Tubo D19.8 x " + L.ToString("0") + " con hexagono de 14.4 x 9 en la punta; mango hexagonal de 24 (se puede usar llave fija). Imprimir de pie, el mango arriba.");
        PB.Report("Llave"); PB.Save(P(F_LLAVE)); SW.App.CloseDoc(PB.M.GetTitle());
    }

    static void Asm()
    {
        var m = SW.Open(P("CB-600_Herramienta.SLDASM"));
        int er = 0; SW.App.ActivateDoc3(m.GetTitle(), false, 0, ref er);
        var a = (AssemblyDoc)m; a.ResolveAllLightWeightComponents(false);
        var xe = Xf.FromComp(((object[])a.GetComponents(true)).Cast<Component2>().First(c => c.Name2.StartsWith("CB-606")));
        var flip = Xf.Rows(V(1, 0, 0), V(0, -1, 0), V(0, 0, -1), V(0, 0, 0));
        Func<double, Xf> enPunta = zc => flip.Then(Xf.Tr(CX, CY, zc)).Then(xe);
        Add(m, P(F_RACOR), enPunta(zFondo));      // anillo sobre el fondo, rosca hacia afuera
        Add(m, P(F_VENTOSA), enPunta(zAloj));     // inserto apretado contra el fondo
        var lab = enPunta(zAloj).Apply(V(0, 0, -INS_H - GOMA_H));
        Console.WriteLine("labio (CB-600): " + string.Join(", ", lab.Select(v => v.ToString("0.0"))) + "  (" + (zAloj + INS_H + GOMA_H - ZN).ToString("0.0") + " mm afuera de la punta)");
        Save(m);
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
        Console.WriteLine("  agregado " + c.Name2);
        return c;
    }
    static void Stl(string file)
    {
        var m = SW.Open(P(file));
        SW.App.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swSTLQuality, (int)swSTLQuality_e.swSTLQuality_Fine);
        SW.App.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swExportStlUnits, (int)swLengthUnit_e.swMM);
        int e = 0, w = 0;
        string dst = Path.GetFullPath(Path.Combine(R, "..", "imprimir", Path.ChangeExtension(file, ".stl")));
        Console.WriteLine((m.Extension.SaveAs3(dst, 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent | (int)swSaveAsOptions_e.swSaveAsOptions_Copy, null, null, ref e, ref w) ? "stl " : "STL FAIL ") + dst);
        SW.App.CloseDoc(m.GetTitle());
    }
}
