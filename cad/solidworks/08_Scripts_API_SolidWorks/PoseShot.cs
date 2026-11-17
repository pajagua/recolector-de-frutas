using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// IntfCuerpos.exe <CB-000> <j1|actual> <elev|actual> <q3|actual> [brazo] [min_mm3]   (v7.11, no guarda)
// Interferencias de CB-000 sin ventana (el detector de SolidWorks no anda con el documento oculto): pone el brazo en la pose
// pedida (J1: 0 = al árbol, CCW +; elev: ángulo de E2 sobre la horizontal; q3 en mm, 0 = retraído) como SweepJ1.cs y calcula la
// intersección de cuerpos de cada par de componentes cuyas cajas se tocan. Con "brazo", solo los pares brazo-base y entre
// subconjuntos distintos del brazo (como SweepJ1.cs). Omite los pares dentro de la herramienta (CB-600: modelos de servo
// superpuestos). Deja CB-000 abierto sin ventana para la próxima pose; "cerrar" como j1 lo cierra sin guardar.
class PoseShot
{
    static Xf Rot(double[] p, double[] u, double deg)
    {
        double t = deg * Math.PI / 180, c = Math.Cos(t), s = Math.Sin(t), n = Math.Sqrt(u[0] * u[0] + u[1] * u[1] + u[2] * u[2]);
        double x = u[0] / n, y = u[1] / n, z = u[2] / n;
        double[,] M = { { c + x * x * (1 - c), x * y * (1 - c) - z * s, x * z * (1 - c) + y * s },
                        { y * x * (1 - c) + z * s, c + y * y * (1 - c), y * z * (1 - c) - x * s },
                        { z * x * (1 - c) - y * s, z * y * (1 - c) + x * s, c + z * z * (1 - c) } };
        var r = new Xf();
        for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) r.R[i * 3 + j] = M[j, i];
        return Xf.Tr(-p[0], -p[1], -p[2]).Then(r).Then(Xf.Tr(p[0], p[1], p[2]));
    }
    static Component2 Find(Component2 parent, string start)
    {
        var ch = parent.GetChildren() as object[]; if (ch == null) return null;
        foreach (Component2 c in ch)
        {
            if (Path.GetFileName(c.GetPathName()).StartsWith(start, StringComparison.OrdinalIgnoreCase)) return c;
            var r = Find(c, start); if (r != null) return r;
        }
        return null;
    }
    static double[] Dir(Xf x, double[] v) { var a = x.Apply(new double[] { 0, 0, 0 }); var b = x.Apply(v); return new[] { b[0] - a[0], b[1] - a[1], b[2] - a[2] }; }
    static double Elev(double[] d) { return Math.Asin(d[2] / Math.Sqrt(d[0] * d[0] + d[1] * d[1] + d[2] * d[2])) * 180 / Math.PI; }
    static string Ruta(Component2 c) { var l = new List<string>(); for (var p = c; p != null; p = p.GetParent()) l.Insert(0, p.Name2.Split('/').Last()); return string.Join("/", l); }

    static void Main(string[] a)
    {
        SW.Connect();
        var doc = (ModelDoc2)SW.App.GetOpenDocumentByName(a[0]);
        if (a[1] == "cerrar") { if (doc != null && !doc.Visible) { SW.App.QuitDoc(doc.GetTitle()); Console.WriteLine("cerrado sin guardar"); } return; }
        if (doc == null)
        {
            SW.App.DocumentVisible(true, (int)swDocumentTypes_e.swDocASSEMBLY);
            try { doc = SW.Open(a[0]); } finally { SW.App.DocumentVisible(true, (int)swDocumentTypes_e.swDocASSEMBLY); }
        }
        else { Console.WriteLine("ya está abierto: no se mueve"); return; }
        var m = doc; var asm = (AssemblyDoc)m; asm.ResolveAllLightWeightComponents(false);
        var top = ((object[])asm.GetComponents(true)).Cast<Component2>().ToDictionary(c => c.Name2.Substring(0, 6));
        var c200 = top["CB-200"]; var c300 = top["CB-300"]; var c400 = top["CB-400"]; Component2 c410 = top.ContainsKey("CB-410") ? top["CB-410"] : null;   // variante B: tramo intermedio (q3/2)
        var X200 = Xf.FromComp(c200); var X300 = Xf.FromComp(c300); var X400 = Xf.FromComp(c400); var X410 = c410 != null ? Xf.FromComp(c410) : null;
        var tree = new[] { Math.Cos(-112.5 * Math.PI / 180), Math.Sin(-112.5 * Math.PI / 180) };
        var e2w = Xf.FromComp(Find(c300, "CB-301"));
        double[] ax = Dir(e2w, new double[] { 1, 0, 0 }); double axh = Math.Sqrt(ax[0] * ax[0] + ax[1] * ax[1]);
        double j1now = Math.Atan2(tree[0] * ax[1] / axh - tree[1] * ax[0] / axh, tree[0] * ax[0] / axh + tree[1] * ax[1] / axh) * 180 / Math.PI;
        double el0 = Elev(ax);
        var brida = Find(c400, "CB-402") ?? Find(c400, "CB-432");   // A: CB-402, B: CB-432B (origen en x = q3 en las dos)
        double q30 = e2w.Inv().Apply(Xf.FromComp(brida).T)[0];   // q3 = x del origen de la brida en E2 (la cara de J3_limite_carrera está 15 mm delante del origen: distancia = q3 + 2)
        var tw = Xf.FromComp(Find(c200, "CB-210"));
        double[] p2 = tw.Apply(new double[] { 190.171, 0, -86.723 }), p2b = tw.Apply(new double[] { 190.171, 1, -86.723 });
        double[] u2 = { p2b[0] - p2[0], p2b[1] - p2[1], p2b[2] - p2[2] };
        double sgn = Elev(Dir(Rot(p2, u2, 1), ax)) > el0 ? 1 : -1;
        for (Feature f = (Feature)m.FirstFeature(); f != null; f = (Feature)f.GetNextFeature())
            if (f.GetTypeName2() == "MateGroup")
                for (Feature sf = (Feature)f.GetFirstSubFeature(); sf != null; sf = (Feature)sf.GetNextSubFeature())
                    if (sf.Name == "J3_limite_carrera" || sf.Name == "J3B_intermedio_limite")
                        sf.SetSuppression2((int)swFeatureSuppressionAction_e.swSuppressFeature, (int)swInConfigurationOpts_e.swThisConfiguration, null);
        double j1 = a[1] == "actual" ? j1now : double.Parse(a[1]), el = a[2] == "actual" ? el0 : double.Parse(a[2]);
        double q3 = a[3] == "actual" ? q30 : double.Parse(a[3]);
        var r1 = Rot(new double[] { 0, 0, 0 }, new double[] { 0, 0, 1 }, j1 - j1now);
        var r2 = Rot(p2, u2, sgn * (el - el0));
        c200.Transform2 = X200.Then(r1).ToMath();
        c300.Transform2 = X300.Then(r2).Then(r1).ToMath();
        c400.Transform2 = X400.Then(Xf.Tr((q3 - q30) * ax[0], (q3 - q30) * ax[1], (q3 - q30) * ax[2])).Then(r2).Then(r1).ToMath();
        if (c410 != null) { double qm0 = e2w.Inv().Apply(Xf.FromComp(Find(c410, "CB-411")).T)[0]; double dm = q3 / 2 - qm0; c410.Transform2 = X410.Then(Xf.Tr(dm * ax[0], dm * ax[1], dm * ax[2])).Then(r2).Then(r1).ToMath(); }
        m.EditRebuild3();
        var e2 = Xf.FromComp(Find(c300, "CB-301")); var d = Dir(e2, new double[] { 1, 0, 0 }); double dh = Math.Sqrt(d[0] * d[0] + d[1] * d[1]);
        Console.WriteLine(string.Format("pose: J1 {0:0.0}°  elev {1:0.0}°  q3 {2:0.0}",
            Math.Atan2(tree[0] * d[1] / dh - tree[1] * d[0] / dh, tree[0] * d[0] / dh + tree[1] * d[1] / dh) * 180 / Math.PI, Elev(d),
            e2.Inv().Apply(Xf.FromComp(brida).T)[0]));

        // PoseShot.exe <asm> <j1> <elev> <q3> <salida_sin_ext> <vista[:ocultar1,ocultar2]> ... : capturas sin guardar
        m.Visible = true; int er = 0; SW.App.ActivateDoc3(m.GetTitle(), false, 0, ref er);
        var todas = ((object[])asm.GetComponents(false)).Cast<Component2>().ToList();
        foreach (var v in a.Skip(5))
        {
            var partes = v.Split(':'); string vista = partes[0];
            var ocultos = new List<Component2>();
            if (partes.Length > 1 && partes[1].StartsWith("solo="))   // deja visibles solo las hojas cuyo archivo empieza con esos prefijos
            {
                var pres = partes[1].Substring(5).Split(',');
                ocultos.AddRange(todas.Where(c => (c.GetChildren() as object[] ?? new object[0]).Length == 0 && (Ruta(c).StartsWith("CB-100") || !pres.Any(pre => Path.GetFileName(c.GetPathName()).StartsWith(pre, StringComparison.OrdinalIgnoreCase) || c.Name2.Split('/').Last() == pre))));
            }
            else if (partes.Length > 1) foreach (var pre in partes[1].Split(',').Where(x => x.Length > 0)) ocultos.AddRange(todas.Where(c => Path.GetFileName(c.GetPathName()).StartsWith(pre, StringComparison.OrdinalIgnoreCase)));
            foreach (var c in ocultos) c.Visible = 0;
            string vn = vista.StartsWith("zoom") ? "*Trimetric" : vista == "trimetric" ? "*Trimetric" : "*" + char.ToUpper(vista[0]) + vista.Substring(1);
            m.ShowNamedView2(vn, -1); m.ViewZoomtofit2();
            if (vista.StartsWith("zoom"))   // zoom=<prefijo del componente a encuadrar>
            {
                string pre = vista.Substring(5); m.ClearSelection2(true);
                foreach (var c in todas.Where(c => Path.GetFileName(c.GetPathName()).StartsWith(pre, StringComparison.OrdinalIgnoreCase))) c.Select4(true, null, false);
                m.ViewZoomToSelection(); m.ClearSelection2(true); m.ViewZoomout(); m.ViewZoomout();
            }
            m.GraphicsRedraw2(); System.Threading.Thread.Sleep(800);
            int e = 0, w = 0; string outp = a[4] + "_" + (vista.StartsWith("zoom") ? "detalle" : vista) + ".png";
            if (partes.Length > 2) outp = a[4] + "_" + partes[2] + ".png";
            Console.WriteLine((m.Extension.SaveAs3(outp, 0, 1, null, null, ref e, ref w) ? "imagen " : "FALLO ") + outp);
            foreach (var c in ocultos) c.Visible = 1;
        }
        SW.App.QuitDoc(m.GetTitle());
    }
}
