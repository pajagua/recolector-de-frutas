using System;
using System.IO;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Telescopico.exe <root solidworks> [sr|j3b ...]   (v7.12, 2026-11-07)
// La variante B (prismático telescópico 2:1) pasa a ser el diseño oficial. Completa lo que tenía solo la A:
//   sr  : copia los sistemas de referencia DH SR2_E2_eje_J3 (CB-301 -> CB-301B) y SR3_E3_eje_J4 (CB-401 -> CB-421B). Las piezas
//         de E2 y E3 de las dos variantes están modeladas en las coordenadas de E2 (E3 con el origen en 0 en home), así que el
//         sistema se copia igual de pieza a pieza; se verifica que la transformación quede idéntica.
//   j3b : final de carrera de J3 en la pared trasera de E2-B. En home la cara trasera del tapón del intermedio (CB-412B) queda
//         en x -276 y la pared trasera va de -284 a -279: no entra un switch en el canal. Se agrega a CB-301B un alojamiento de
//         8 mm detrás de la pared (x -292 … -284) con una ventana pasante; el switch entra desde atrás, la palanca asoma 2 mm en
//         el canal y opera en x -276 (base del switch en -289.1). Con el tope (la brida contra el frente de E2, q3 = -2, el
//         intermedio en -1) quedan 2.6 mm hasta el cuerpo. 2 x M2x12 de arriba hacia abajo (pasantes arriba, piloto abajo).
class Telescopico
{
    static string R;
    static string P(string rel) { return Path.Combine(R, rel); }
    static double[] V(params double[] v) { return v; }
    const string EL = "05_Comerciales\\Electronica\\", TO = "05_Comerciales\\Tornilleria\\", B = "11_v7B_Prismatico_Telescopico\\";
    const string E2B = B + "CB-301B_Eslabon_E2_v7B_Telescopico.SLDPRT";

    // switch de J3 (marco de E2): palanca hacia +x, largo en z (-9.9 … 9.9), espesor en y (-12 … -5.6), base en x -289.1
    static Xf SwJ3B() { return Xf.Rows(V(0, 0, 1), V(1, 0, 0), V(0, 1, 0), V(-289.1, -8.8, -9.9)); }
    static readonly double[][] SW_AG = { V(5.15, 2.9), V(14.65, 2.9) };
    static double[] EnSw(Xf s, double[] xy) { return s.Apply(V(xy[0], xy[1], 0)); }

    static void Main(string[] a)
    {
        R = a[0]; SW.Connect();
        var w = a.Skip(1).ToList();
        if (w.Contains("sr")) { CopiarSR("03_Hombro_E2_J2\\CB-301_Eslabon_E2_mod5840.SLDPRT", E2B, "SR2_E2_eje_J3");
                                CopiarSR("04_Prismatico_E3_J3\\CB-401_Tubo_E3_Aluminio_30x30x2.SLDPRT", B + "CB-421B_Tubo_Interior_20x20x1.5_L263.SLDPRT", "SR3_E3_eje_J4"); }
        if (w.Contains("j3b")) { AlojamientoJ3B(); SwitchJ3B(); }
    }

    static ModelDoc2 Abrir(string rel, out bool ya, bool activar)
    {
        string p = P(rel); ya = SW.App.GetOpenDocumentByName(p) != null;
        bool asm = rel.ToUpper().EndsWith(".SLDASM");
        int t = asm ? (int)swDocumentTypes_e.swDocASSEMBLY : (int)swDocumentTypes_e.swDocPART;
        if (!activar && !ya) SW.App.DocumentVisible(false, t);
        ModelDoc2 m;
        try { m = SW.Open(p); } finally { if (!activar && !ya) SW.App.DocumentVisible(true, t); }
        if (activar) { int er = 0; SW.App.ActivateDoc3(m.GetTitle(), false, 0, ref er); }
        if (asm) ((AssemblyDoc)m).ResolveAllLightWeightComponents(false);
        return m;
    }
    static void Guardar(ModelDoc2 m, bool ya)
    {
        int e = 0, w = 0; m.ForceRebuild3(false);
        Console.WriteLine("guardado " + m.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w) + " " + m.GetTitle() + " e=" + e + " w=" + w);
        if (!ya) SW.App.CloseDoc(m.GetTitle());
    }

    static double[] Arr(ModelDoc2 m, string nombre)
    {
        var t = m.Extension.GetCoordinateSystemTransformByName(nombre) as MathTransform;
        return t == null ? null : (double[])t.ArrayData;
    }
    static void CopiarSR(string origen, string destino, string nombre)
    {
        bool yo; var o = Abrir(origen, out yo, false);
        var t = Arr(o, nombre);
        if (!yo) SW.App.QuitDoc(o.GetTitle());
        if (t == null) { Console.WriteLine("no está " + nombre + " en " + origen); return; }
        bool yd; var d = Abrir(destino, out yd, true);
        if (((PartDoc)d).FeatureByName(nombre) != null) { Console.WriteLine(nombre + " ya está en " + Path.GetFileName(destino)); if (!yd) SW.App.CloseDoc(d.GetTitle()); return; }
        // dos lecturas posibles de la transformación (sistema -> modelo o al revés) y dos convenciones de ángulos: se prueba
        // hasta que el sistema creado tenga exactamente la misma transformación que el original
        var rt = new Xf(); for (int i = 0; i < 9; i++) rt.R[i] = t[i]; rt.T = new[] { t[9] * 1000, t[10] * 1000, t[11] * 1000 };
        Feature hecho = null;
        foreach (var x in new[] { rt, rt.Inv() })
        {
            var org = x.T; double[] ex = { x.R[0], x.R[1], x.R[2] }, ey = { x.R[3], x.R[4], x.R[5] }, ez = { x.R[6], x.R[7], x.R[8] };
            double[,] Rm = { { ex[0], ey[0], ez[0] }, { ex[1], ey[1], ez[1] }, { ex[2], ey[2], ez[2] } };
            foreach (var c in new[] { "XYZ", "ZYX" })
            {
                double ax, ay, az;
                if (c == "ZYX") { ay = Math.Asin(-Rm[2, 0]); ax = Math.Atan2(Rm[2, 1], Rm[2, 2]); az = Math.Atan2(Rm[1, 0], Rm[0, 0]); }
                else if (Math.Abs(Rm[0, 2]) > 0.99999) { double sg = Math.Sign(Rm[0, 2]); ay = sg * Math.PI / 2; az = 0; ax = Math.Atan2(sg * Rm[1, 0], Rm[1, 1]); }
                else { ay = Math.Asin(Rm[0, 2]); ax = Math.Atan2(-Rm[1, 2], Rm[2, 2]); az = Math.Atan2(-Rm[0, 1], Rm[0, 0]); }
                d.ClearSelection2(true);
                var f = (Feature)d.FeatureManager.CreateCoordinateSystemUsingNumericalValues(true, org[0] / 1000, org[1] / 1000, org[2] / 1000, true, ax, ay, az);
                if (f == null) continue;
                f.Name = nombre;
                var t2 = Arr(d, nombre);
                double err = 0; for (int i = 0; i < 12; i++) err = Math.Max(err, Math.Abs(t2[i] - t[i]));
                if (err < 1e-7) { hecho = f; Console.WriteLine(string.Format("{0} copiado a {1} (error {2:0.0e0})", nombre, Path.GetFileName(destino), err)); break; }
                d.ClearSelection2(true); f.Select2(false, 0); d.EditDelete();
            }
            if (hecho != null) break;
        }
        if (hecho == null) { Console.WriteLine("NO se pudo copiar " + nombre); if (!yd) SW.App.QuitDoc(d.GetTitle()); return; }
        Guardar(d, yd);
    }

    static void AlojamientoJ3B()
    {
        bool ya; var m = Abrir(E2B, out ya, true); PB.Use(m);
        if (((PartDoc)m).FeatureByName("CB_v712_Alojamiento_final_J3") != null) { Console.WriteLine("CB-301B ya tiene el alojamiento"); if (!ya) SW.App.CloseDoc(m.GetTitle()); return; }
        PB.Report("CB-301B antes");
        // plano normal a x: (a = z, b = y)
        PB.Sketch('R'); PB.Rect(-13, -15.3, 13, -2.8); PB.Extrude(-292, 8, false, "CB_v712_Alojamiento_final_J3");
        PB.Sketch('R'); PB.Rect(-10.2, -12.3, 10.2, -5.3); PB.Extrude(-293, 14.6, true, "CB_v712_Ventana_final_J3");
        var s = SwJ3B();
        // plano normal a y: (a = x, b = z)
        PB.Sketch('T'); foreach (var h in SW_AG) { var p = EnSw(s, h); PB.Circle(p[0], p[2], 2.4); } PB.Extrude(-5.4, 3.0, true, "CB_v712_Pasantes_M2");
        PB.Sketch('T'); foreach (var h in SW_AG) { var p = EnSw(s, h); PB.Circle(p[0], p[2], 1.6); } PB.Extrude(-15.6, 3.4, true, "CB_v712_Pilotos_M2");
        PB.Report("CB-301B despues");
        Guardar(m, ya);
    }

    static void SwitchJ3B()
    {
        bool ya; var m = Abrir(B + "CB-300B_Hombro_E2_v7B.SLDASM", out ya, false); var asm = (AssemblyDoc)m;
        var cs = ((object[])asm.GetComponents(true)).Cast<Component2>().ToList();
        if (cs.Any(c => c.Name2.StartsWith("CB-516"))) { Console.WriteLine("CB-300B ya tiene el switch"); if (!ya) SW.App.CloseDoc(m.GetTitle()); return; }
        var x = Xf.FromComp(cs.First(c => c.Name2.StartsWith("CB-301B")));
        var s = SwJ3B();
        Add(m, P(EL + "CB-516_Microswitch_Palanca_19.8x6.4.SLDPRT"), s.Then(x));
        foreach (var h in SW_AG) { var q = EnSw(s, h); Add(m, P(TO + "HW_M2x12.SLDPRT"), Torn(V(q[0], -2.8, q[2]), V(0, 1, 0)).Then(x)); }
        Guardar(m, ya);
    }
    static Component2 Add(ModelDoc2 m, string path, Xf x)
    {
        var a = (AssemblyDoc)m;
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
    // tornillo: eje Z de la pieza hacia la cabeza (dir), cara de apoyo de la cabeza en p
    static Xf Torn(double[] p, double[] dir)
    {
        double[] z = dir, xx = Math.Abs(z[2]) > 0.9 ? V(1, 0, 0) : V(0, 0, 1);
        double[] y = { z[1] * xx[2] - z[2] * xx[1], z[2] * xx[0] - z[0] * xx[2], z[0] * xx[1] - z[1] * xx[0] };
        xx = new[] { y[1] * z[2] - y[2] * z[1], y[2] * z[0] - y[0] * z[2], y[0] * z[1] - y[1] * z[0] };
        return Xf.Rows(xx, y, z, p);
    }
}
