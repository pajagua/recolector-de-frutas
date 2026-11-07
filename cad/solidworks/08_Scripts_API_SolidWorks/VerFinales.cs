using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// VerFinales.exe <CB-000> <j1|actual> <q3> [todo] : (v7.11) mueve J1 (0 = al árbol, CCW +) y J3 como SweepJ1.cs y lista las
// interferencias de las piezas nuevas (finales de carrera, soportes, PCA9685, leva de la torre). Con "todo", todas.
// Abre CB-000 sin ventana y lo deja abierto (sin guardar) para la siguiente corrida; "cerrar" como j1 lo cierra sin guardar.
class VerFinales
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
    static string Ruta(Component2 c) { string r = c.Name2; var p = c.GetParent(); while (p != null) { r = p.Name2 + "/" + r; p = p.GetParent(); } return r; }

    static void Main(string[] a)
    {
        SW.Connect();
        var doc = (ModelDoc2)SW.App.GetOpenDocumentByName(a[0]);
        if (a[1] == "cerrar") { if (doc != null && !doc.Visible) { string t = doc.GetTitle(); SW.App.QuitDoc(t); Console.WriteLine("cerrado sin guardar"); } return; }
        if (doc == null)
        {
            SW.App.DocumentVisible(false, (int)swDocumentTypes_e.swDocASSEMBLY);
            try { doc = SW.Open(a[0]); } finally { SW.App.DocumentVisible(true, (int)swDocumentTypes_e.swDocASSEMBLY); }
        }
        else if (doc.Visible) { Console.WriteLine("CB-000 está abierto con ventana (del usuario): no se mueve"); return; }
        var m = doc; var asm = (AssemblyDoc)m; asm.ResolveAllLightWeightComponents(false);
        var comps = ((object[])asm.GetComponents(true)).Cast<Component2>().ToDictionary(c => c.Name2.Substring(0, 6));
        var c200 = comps["CB-200"]; var c300 = comps["CB-300"]; var c400 = comps["CB-400"];
        var X200 = Xf.FromComp(c200); var X300 = Xf.FromComp(c300); var X400 = Xf.FromComp(c400);
        var tree = new[] { Math.Cos(-112.5 * Math.PI / 180), Math.Sin(-112.5 * Math.PI / 180) };
        var e2w = Xf.FromComp(Find(c300, "CB-301"));
        double[] ax = Dir(e2w, new double[] { 1, 0, 0 });
        double axh = Math.Sqrt(ax[0] * ax[0] + ax[1] * ax[1]);
        double j1now = Math.Atan2(tree[0] * ax[1] / axh - tree[1] * ax[0] / axh, tree[0] * ax[0] / axh + tree[1] * ax[1] / axh) * 180 / Math.PI;
        var brida = Find(c400, "CB-402");
        double q30 = e2w.Inv().Apply(Xf.FromComp(brida).T)[0];   // q3 = x del origen de la brida en E2 (la cara de J3_limite_carrera está 15 mm delante del origen)
        for (Feature f = (Feature)m.FirstFeature(); f != null; f = (Feature)f.GetNextFeature())
            if (f.GetTypeName2() == "MateGroup")
                for (Feature sf = (Feature)f.GetFirstSubFeature(); sf != null; sf = (Feature)sf.GetNextSubFeature())
                    if (sf.Name == "J3_limite_carrera")
                        sf.SetSuppression2((int)swFeatureSuppressionAction_e.swSuppressFeature, (int)swInConfigurationOpts_e.swThisConfiguration, null);
        double j1 = a[1] == "actual" ? j1now : double.Parse(a[1]), q3 = double.Parse(a[2]);
        var r1 = Rot(new double[] { 0, 0, 0 }, new double[] { 0, 0, 1 }, j1 - j1now);
        c200.Transform2 = X200.Then(r1).ToMath();
        c300.Transform2 = X300.Then(r1).ToMath();
        c400.Transform2 = X400.Then(Xf.Tr((q3 - q30) * ax[0], (q3 - q30) * ax[1], (q3 - q30) * ax[2])).Then(r1).ToMath();
        m.EditRebuild3();
        var e2 = Xf.FromComp(Find(c300, "CB-301")); var d = Dir(e2, new double[] { 1, 0, 0 }); double dh = Math.Sqrt(d[0] * d[0] + d[1] * d[1]);
        double j1real = Math.Atan2(tree[0] * d[1] / dh - tree[1] * d[0] / dh, tree[0] * d[0] / dh + tree[1] * d[1] / dh) * 180 / Math.PI;
        double q3real = e2.Inv().Apply(Xf.FromComp(brida).T)[0];
        Console.WriteLine(string.Format("J1 {0:0.0}° (pedido {1:0.0}, guardado {2:0.0})  q3 {3:0.0}", j1real, j1, j1now, q3real));
        var nuevos = new[] { "CB-516", "CB-161", "CB-162", "CB-307", "CB-142", "CB-517", "HW_M2", "HW_Tirafondo", "HW_Tuerca_M2", "HW_Separador_M8", "HW_M8x65" };
        // El detector de interferencias no anda con el documento sin ventana: intersección de cuerpos (copias transformadas)
        // de cada pieza nueva (y la torre) contra los componentes cuya caja se superpone.
        var hojas = ((object[])asm.GetComponents(false)).Cast<Component2>().Where(c => !c.IsSuppressed() && (c.GetChildren() as object[] ?? new object[0]).Length == 0).ToList();
        Func<Component2, double[]> caja = c => (double[])c.GetBox(false, false);
        Func<double[], double[], bool> toca = (p, q) => p != null && q != null && p[0] <= q[3] && q[0] <= p[3] && p[1] <= q[4] && q[1] <= p[4] && p[2] <= q[5] && q[2] <= p[5];
        var cuerpos = new Dictionary<Component2, List<Body2>>();
        Func<Component2, List<Body2>> cu = c =>
        {
            List<Body2> l; if (cuerpos.TryGetValue(c, out l)) return l;
            l = new List<Body2>(); object bi; var bs = c.GetBodies3((int)swBodyType_e.swSolidBody, out bi) as object[];
            if (bs != null) foreach (Body2 b in bs) { var k = (Body2)b.Copy(); k.ApplyTransform((MathTransform)c.Transform2); l.Add(k); }
            cuerpos[c] = l; return l;
        };
        var objetivo = hojas.Where(c => nuevos.Any(k => c.Name2.Split('/').Last().StartsWith(k)) || c.Name2.Split('/').Last().StartsWith("CB-210")).ToList();
        var vistos = new HashSet<string>(); int n = 0;
        foreach (var c1 in objetivo)
            foreach (var c2 in hojas)
            {
                if (c1 == c2) continue;
                string key = string.Join("|", new[] { Ruta(c1), Ruta(c2) }.OrderBy(s => s)); if (!vistos.Add(key)) continue;
                bool torre1 = c1.Name2.Split('/').Last().StartsWith("CB-210");
                if (torre1 && !Ruta(c2).StartsWith("CB-100")) continue;          // la torre solo contra la base
                if (!toca(caja(c1), caja(c2))) continue;
                double vol = 0; int err; double[] lo = { 1e9, 1e9, 1e9 }, hi = { -1e9, -1e9, -1e9 };
                // caja de la intersección en el marco de E2 (piezas del brazo) o del tablero (base)
                bool brazo = Ruta(c1).StartsWith("CB-300") || Ruta(c1).StartsWith("CB-400") || Ruta(c2).StartsWith("CB-300") || Ruta(c2).StartsWith("CB-400");
                var marco = brazo ? e2.Inv() : Xf.Rows(new[] { tree[0], tree[1], 0 }, new[] { -tree[1], tree[0], 0 }, new double[] { 0, 0, 1 }, new double[] { 0, 0, 0 }).Inv();
                foreach (var b1 in cu(c1)) foreach (var b2 in cu(c2))
                    {
                        var r = ((Body2)b1.Copy()).Operations2((int)swBodyOperationType_e.SWBODYINTERSECT, (Body2)b2.Copy(), out err) as object[];
                        if (r != null) foreach (Body2 rb in r)
                            {
                                var mp = rb.GetMassProperties(1.0) as double[]; if (mp != null) vol += mp[3] * 1e9;
                                var bb = (double[])rb.GetBodyBox();
                                for (int i = 0; i < 8; i++)
                                {
                                    var p = marco.Apply(new[] { ((i & 1) == 0 ? bb[0] : bb[3]) * 1000, ((i & 2) == 0 ? bb[1] : bb[4]) * 1000, ((i & 4) == 0 ? bb[2] : bb[5]) * 1000 });
                                    for (int k = 0; k < 3; k++) { lo[k] = Math.Min(lo[k], p[k]); hi[k] = Math.Max(hi[k], p[k]); }
                                }
                            }
                    }
                if (vol < 0.01) continue;
                n++;
                Func<Component2, string> nom = c => Ruta(c).Split('/').Last();
                Console.WriteLine(string.Format("{0,8:0.0} mm3  {1} <-> {2}   [{3}] x {4:0.0}..{5:0.0} y {6:0.0}..{7:0.0} z {8:0.0}..{9:0.0}", vol, nom(c1), nom(c2),
                    brazo ? "E2" : "árbol", lo[0], hi[0], lo[1], hi[1], lo[2], hi[2]));
            }
        Console.WriteLine("interferencias: " + n + " (piezas revisadas: " + objetivo.Count + ")");
    }
}
