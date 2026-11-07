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
class IntfCuerpos
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
            SW.App.DocumentVisible(false, (int)swDocumentTypes_e.swDocASSEMBLY);
            try { doc = SW.Open(a[0]); } finally { SW.App.DocumentVisible(true, (int)swDocumentTypes_e.swDocASSEMBLY); }
        }
        else if (doc.Visible) { Console.WriteLine("CB-000 está abierto con ventana: no se mueve"); return; }
        var m = doc; var asm = (AssemblyDoc)m; asm.ResolveAllLightWeightComponents(false);
        var top = ((object[])asm.GetComponents(true)).Cast<Component2>().ToDictionary(c => c.Name2.Substring(0, 6));
        var c200 = top["CB-200"]; var c300 = top["CB-300"]; var c400 = top["CB-400"];
        var X200 = Xf.FromComp(c200); var X300 = Xf.FromComp(c300); var X400 = Xf.FromComp(c400);
        var tree = new[] { Math.Cos(-112.5 * Math.PI / 180), Math.Sin(-112.5 * Math.PI / 180) };
        var e2w = Xf.FromComp(Find(c300, "CB-301"));
        double[] ax = Dir(e2w, new double[] { 1, 0, 0 }); double axh = Math.Sqrt(ax[0] * ax[0] + ax[1] * ax[1]);
        double j1now = Math.Atan2(tree[0] * ax[1] / axh - tree[1] * ax[0] / axh, tree[0] * ax[0] / axh + tree[1] * ax[1] / axh) * 180 / Math.PI;
        double el0 = Elev(ax);
        var brida = Find(c400, "CB-402");
        double q30 = e2w.Inv().Apply(Xf.FromComp(brida).T)[0];   // q3 = x del origen de la brida en E2 (la cara de J3_limite_carrera está 15 mm delante del origen: distancia = q3 + 2)
        var tw = Xf.FromComp(Find(c200, "CB-210"));
        double[] p2 = tw.Apply(new double[] { 190.171, 0, -86.723 }), p2b = tw.Apply(new double[] { 190.171, 1, -86.723 });
        double[] u2 = { p2b[0] - p2[0], p2b[1] - p2[1], p2b[2] - p2[2] };
        double sgn = Elev(Dir(Rot(p2, u2, 1), ax)) > el0 ? 1 : -1;
        for (Feature f = (Feature)m.FirstFeature(); f != null; f = (Feature)f.GetNextFeature())
            if (f.GetTypeName2() == "MateGroup")
                for (Feature sf = (Feature)f.GetFirstSubFeature(); sf != null; sf = (Feature)sf.GetNextSubFeature())
                    if (sf.Name == "J3_limite_carrera")
                        sf.SetSuppression2((int)swFeatureSuppressionAction_e.swSuppressFeature, (int)swInConfigurationOpts_e.swThisConfiguration, null);
        double j1 = a[1] == "actual" ? j1now : double.Parse(a[1]), el = a[2] == "actual" ? el0 : double.Parse(a[2]);
        double q3 = a[3] == "actual" ? q30 : double.Parse(a[3]);
        var r1 = Rot(new double[] { 0, 0, 0 }, new double[] { 0, 0, 1 }, j1 - j1now);
        var r2 = Rot(p2, u2, sgn * (el - el0));
        c200.Transform2 = X200.Then(r1).ToMath();
        c300.Transform2 = X300.Then(r2).Then(r1).ToMath();
        c400.Transform2 = X400.Then(Xf.Tr((q3 - q30) * ax[0], (q3 - q30) * ax[1], (q3 - q30) * ax[2])).Then(r2).Then(r1).ToMath();
        m.EditRebuild3();
        var e2 = Xf.FromComp(Find(c300, "CB-301")); var d = Dir(e2, new double[] { 1, 0, 0 }); double dh = Math.Sqrt(d[0] * d[0] + d[1] * d[1]);
        Console.WriteLine(string.Format("pose: J1 {0:0.0}°  elev {1:0.0}°  q3 {2:0.0}",
            Math.Atan2(tree[0] * d[1] / dh - tree[1] * d[0] / dh, tree[0] * d[0] / dh + tree[1] * d[1] / dh) * 180 / Math.PI, Elev(d),
            e2.Inv().Apply(Xf.FromComp(brida).T)[0]));

        bool soloBrazo = a.Contains("brazo");
        double min = a.Length > 5 && a.Last() != "brazo" ? double.Parse(a.Last()) : 0.05;
        var hojas = ((object[])asm.GetComponents(false)).Cast<Component2>()
            .Where(c => !c.IsSuppressed() && (c.GetChildren() as object[] ?? new object[0]).Length == 0).ToList();
        var ruta = hojas.ToDictionary(c => c, Ruta);
        Func<Component2, string> sub = c => ruta[c].Substring(0, 6);
        Func<Component2, bool> herr = c => ruta[c].Contains("CB-600");
        var cajas = hojas.ToDictionary(c => c, c => (double[])c.GetBox(false, false));
        Func<double[], double[], bool> toca = (p, q) => p != null && q != null && p[0] <= q[3] && q[0] <= p[3] && p[1] <= q[4] && q[1] <= p[4] && p[2] <= q[5] && q[2] <= p[5];
        var cuerpos = new Dictionary<Component2, List<Body2>>();
        Func<Component2, List<Body2>> cu = c =>
        {
            List<Body2> l; if (cuerpos.TryGetValue(c, out l)) return l;
            l = new List<Body2>(); object bi; var bs = c.GetBodies3((int)swBodyType_e.swSolidBody, out bi) as object[];
            if (bs != null) foreach (Body2 b in bs) { var k = (Body2)b.Copy(); k.ApplyTransform((MathTransform)c.Transform2); l.Add(k); }
            cuerpos[c] = l; return l;
        };
        int pares = 0, n = 0; var t0 = DateTime.Now;
        for (int i = 0; i < hojas.Count; i++)
            for (int j = i + 1; j < hojas.Count; j++)
            {
                var c1 = hojas[i]; var c2 = hojas[j];
                if (herr(c1) && herr(c2)) continue;
                string s1 = sub(c1), s2 = sub(c2);
                if (soloBrazo)
                {
                    bool b1 = s1 != "CB-100", b2 = s2 != "CB-100";
                    if (!(b1 ^ b2) && !(b1 && b2 && s1 != s2)) continue;
                }
                if (!toca(cajas[c1], cajas[c2])) continue;
                pares++;
                double vol = 0; int err;
                foreach (var x1 in cu(c1))
                {
                    var bb1 = (double[])x1.GetBodyBox();
                    foreach (var x2 in cu(c2))
                    {
                        if (!toca(bb1, (double[])x2.GetBodyBox())) continue;
                        var r = ((Body2)x1.Copy()).Operations2((int)swBodyOperationType_e.SWBODYINTERSECT, (Body2)x2.Copy(), out err) as object[];
                        if (r != null) foreach (Body2 rb in r) { var mp = rb.GetMassProperties(1.0) as double[]; if (mp != null) vol += mp[3] * 1e9; }
                    }
                }
                if (vol < min) continue;
                n++;
                Console.WriteLine(string.Format("{0,10:0.0} mm3  {1}  <->  {2}", vol, ruta[c1], ruta[c2]));
            }
        Console.WriteLine(string.Format("interferencias {0} (pares revisados {1}, hojas {2}, {3:0} s)", n, pares, hojas.Count, (DateTime.Now - t0).TotalSeconds));
    }
}
