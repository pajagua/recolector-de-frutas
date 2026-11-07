using System;
using System.Collections.Generic;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Slice.exe <asm> <ref> <filtro,...> <eje x|y|z> <valor> <u0> <u1> <v0> <v1> <paso> : corte por el plano eje = valor (coordenadas de la pieza ref),
// mapa ASCII (u = primer eje restante, v = segundo, filas de v1 a v0); cada componente con su letra (leyenda abajo)
class Slice
{
    static void Main(string[] a)
    {
        SW.Connect();
        bool ya = SW.App.GetOpenDocumentByName(a[0]) != null;
        var m = SW.Open(a[0]); var asm = (AssemblyDoc)m; asm.ResolveAllLightWeightComponents(false);
        var cs = ((object[])asm.GetComponents(false)).Cast<Component2>().ToList();
        var rc = a[1] == "-" ? null : cs.First(c => c.Name2.Split('/').Last().StartsWith(a[1])); var inv = rc == null ? new Xf() : Xf.FromComp(rc).Inv();
        var f = a[2].Split(','); int ax = "xyz".IndexOf(a[3]); double val = double.Parse(a[4]);
        int iu = ax == 0 ? 1 : 0, iv = ax == 2 ? 1 : 2;
        double u0 = double.Parse(a[5]), u1 = double.Parse(a[6]), v0 = double.Parse(a[7]), v1 = double.Parse(a[8]), st = double.Parse(a[9]);
        int nu = (int)Math.Round((u1 - u0) / st) + 1, nv = (int)Math.Round((v1 - v0) / st) + 1;
        var grid = new char[nv, nu]; for (int i = 0; i < nv; i++) for (int j = 0; j < nu; j++) grid[i, j] = '.';
        string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"; int li = 0; var legend = new List<string>();
        foreach (var c in cs)
        {
            if (!f.Any(x => c.Name2.Split('/').Last().StartsWith(x))) continue;
            if (c.IsSuppressed()) continue;
            object bi; var bodies = (object[])c.GetBodies3((int)swBodyType_e.swSolidBody, out bi); if (bodies == null) continue;
            var x0 = Xf.FromComp(c).Then(inv); char ch = letters[li++ % letters.Length]; bool any = false;
            foreach (Body2 b in bodies)
            {
                var segs = new List<double[]>();
                foreach (Face2 fc in (object[])b.GetFaces())
                {
                    var t = fc.GetTessTriangles(true) as float[]; if (t == null) continue;
                    for (int i = 0; i + 8 < t.Length; i += 9)
                    {
                        var p = new double[3][];
                        for (int k = 0; k < 3; k++) p[k] = x0.Apply(new double[] { t[i + 3 * k] * 1000, t[i + 3 * k + 1] * 1000, t[i + 3 * k + 2] * 1000 });
                        var pts = new List<double[]>();
                        for (int k = 0; k < 3; k++)
                        {
                            var A = p[k]; var B = p[(k + 1) % 3]; double da = A[ax] - val, db = B[ax] - val;
                            if ((da < 0) != (db < 0)) { double s = da / (da - db); pts.Add(new[] { A[iu] + s * (B[iu] - A[iu]), A[iv] + s * (B[iv] - A[iv]) }); }
                        }
                        if (pts.Count == 2) segs.Add(new[] { pts[0][0], pts[0][1], pts[1][0], pts[1][1] });
                    }
                }
                if (segs.Count == 0) continue;
                for (int i = 0; i < nv; i++)
                {
                    double v = v0 + i * st + 1e-4;
                    var xs = new List<double>();
                    foreach (var s in segs) if ((s[1] < v) != (s[3] < v)) xs.Add(s[0] + (v - s[1]) / (s[3] - s[1]) * (s[2] - s[0]));
                    xs.Sort();
                    for (int k = 0; k + 1 < xs.Count; k += 2)
                        for (int j = 0; j < nu; j++) { double u = u0 + j * st; if (u >= xs[k] && u <= xs[k + 1]) { grid[i, j] = grid[i, j] == '.' ? ch : '#'; any = true; } }
                }
            }
            legend.Add(ch + " = " + c.Name2.Split('/').Last() + (any ? "" : " (fuera del corte)"));
        }
        Console.WriteLine("corte " + a[3] + " = " + val + "   u = " + "xyz"[iu] + " de " + u0 + " a " + u1 + "   v = " + "xyz"[iv] + " (filas, de arriba " + v1 + " a abajo " + v0 + "), paso " + st);
        for (int i = nv - 1; i >= 0; i--) { var sb = new System.Text.StringBuilder(); for (int j = 0; j < nu; j++) sb.Append(grid[i, j]); Console.WriteLine(string.Format("{0,7:0.0} ", v0 + i * st) + sb); }
        foreach (var l in legend) Console.WriteLine(l);
        if (!ya) SW.App.CloseDoc(m.GetTitle());
    }
}
