using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// SweepJ1.exe <CB-000x> <q3_mm> <elev> <j1_min> <j1_max> <paso> : gira J1 (0 = apuntando al arbol) con J2 y J3 fijos; lista choques
// Como Alcance.cs, pero parte de la pose guardada (cualquier J1, J2 y J3) y mueve tambien el tramo intermedio
// CB-410B (a q3/2) si existe. Apunta J1 al borde del tablero mas cercano, barre la elevacion de J2 con J3 = q3
// y lista los choques del brazo (CB-300/400/410) contra base y torre, y entre piezas del brazo. No guarda nada.
class SweepJ1
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
        var ch = parent.GetChildren() as object[];
        if (ch == null) return null;
        foreach (Component2 c in ch)
        {
            if (Path.GetFileName(c.GetPathName()).StartsWith(start, StringComparison.OrdinalIgnoreCase)) return c;
            var r = Find(c, start); if (r != null) return r;
        }
        return null;
    }
    static double[] Dir(Xf x, double[] v) { var a = x.Apply(new double[] { 0, 0, 0 }); var b = x.Apply(v); return new[] { b[0] - a[0], b[1] - a[1], b[2] - a[2] }; }
    static double Elev(double[] d) { return Math.Asin(d[2] / Math.Sqrt(d[0] * d[0] + d[1] * d[1] + d[2] * d[2])) * 180 / Math.PI; }
    static Xf Along(double[] ax, double d) { return Xf.Tr(d * ax[0], d * ax[1], d * ax[2]); }

    static void Main(string[] args)
    {
        SW.Connect();
        double q3 = double.Parse(args[1]);
        double elevFix = double.Parse(args[2]), jmin = double.Parse(args[3]), jmax = double.Parse(args[4]), paso = double.Parse(args[5]);
        var m = SW.Open(args[0]); var a = (AssemblyDoc)m;
        a.ResolveAllLightWeightComponents(false);
        var comps = ((object[])a.GetComponents(true)).Cast<Component2>().ToDictionary(c => c.Name2.Substring(0, 6));
        var c200 = comps["CB-200"]; var c300 = comps["CB-300"]; var c400 = comps["CB-400"];
        Component2 c410 = comps.ContainsKey("CB-410") ? comps["CB-410"] : null;
        var X200 = Xf.FromComp(c200); var X300 = Xf.FromComp(c300); var X400 = Xf.FromComp(c400); var X410 = c410 != null ? Xf.FromComp(c410) : null;

        // borde del tablero mas cercano al eje J1 (normal hacia afuera, en el plano XY del mundo)
        var board = Find(comps["CB-100"], "CB-111");
        var xb = Xf.FromComp(board);
        var bbody = (Body2)((object[])((PartDoc)board.GetModelDoc2()).GetBodies2((int)swBodyType_e.swSolidBody, true))[0];
        double[] pb = ((double[])bbody.GetBodyBox()).Select(v => v * 1000).ToArray();
        var dims = Enumerable.Range(0, 3).Select(i => pb[i + 3] - pb[i]).ToArray();
        int thin = Array.IndexOf(dims, dims.Min()); int i1 = (thin + 1) % 3, i2 = (thin + 2) % 3;
        var corners = new List<double[]>();
        foreach (var uv in new[] { new[] { 0, 0 }, new[] { 1, 0 }, new[] { 1, 1 }, new[] { 0, 1 } })
        {
            var q = new double[3]; q[thin] = pb[thin + 3]; q[i1] = uv[0] == 0 ? pb[i1] : pb[i1 + 3]; q[i2] = uv[1] == 0 ? pb[i2] : pb[i2 + 3];
            corners.Add(xb.Apply(q));
        }
        double best = 1e9; double[] nEdge = null;
        for (int k = 0; k < 4; k++)
        {
            var p = corners[k]; var q = corners[(k + 1) % 4];
            double ex = q[0] - p[0], ey = q[1] - p[1], L = Math.Sqrt(ex * ex + ey * ey);
            double d = Math.Abs(p[0] * ey - p[1] * ex) / L, t = -(p[0] * ex + p[1] * ey) / (L * L);
            if (d < best) { best = d; nEdge = new[] { (p[0] + t * ex) / d, (p[1] + t * ey) / d }; }
        }

        // pose actual (mundo)
        var e2w = Xf.FromComp(Find(c300, "CB-301"));
        double[] ax = Dir(e2w, new double[] { 1, 0, 0 });
        double elev0 = Elev(ax);
        var tw = Xf.FromComp(Find(c200, "CB-210"));
        double[] p2 = tw.Apply(new double[] { 190.171, 0, -86.723 }), p2b = tw.Apply(new double[] { 190.171, 1, -86.723 });
        double[] u2 = { p2b[0] - p2[0], p2b[1] - p2[1], p2b[2] - p2[2] };
        double sgn = Elev(Dir(Rot(p2, u2, 1), ax)) > elev0 ? 1 : -1;
        double axh = Math.Sqrt(ax[0] * ax[0] + ax[1] * ax[1]);
        double dj1 = Math.Atan2(ax[0] / axh * nEdge[1] - ax[1] / axh * nEdge[0], ax[0] / axh * nEdge[0] + ax[1] / axh * nEdge[1]) * 180 / Math.PI;

        var brida = Find(c400, "CB-402") ?? Find(c400, "CB-432");
        double q30 = e2w.Inv().Apply(Xf.FromComp(brida).T)[0];
        double qm0 = 0;
        if (c410 != null) qm0 = e2w.Inv().Apply(Xf.FromComp(Find(c410, "CB-411")).T)[0];
        Console.WriteLine("borde cercano a " + best.ToString("0.0") + " mm; pose guardada: elev " + elev0.ToString("0.0") + ", q3 " + q30.ToString("0.0") +
            (c410 != null ? ", intermedio " + qm0.ToString("0.0") : "") + "; giro J1 " + dj1.ToString("0.0"));

        for (Feature f = (Feature)m.FirstFeature(); f != null; f = (Feature)f.GetNextFeature())
            if (f.GetTypeName2() == "MateGroup")
                for (Feature sf = (Feature)f.GetFirstSubFeature(); sf != null; sf = (Feature)sf.GetNextSubFeature())
                    if (sf.Name == "J3_limite_carrera" || sf.Name == "J3B_intermedio_limite")
                        sf.SetSuppression2((int)swFeatureSuppressionAction_e.swSuppressFeature, (int)swInConfigurationOpts_e.swThisConfiguration, null);
        var idm = a.InterferenceDetectionManager;
        double elev = elevFix;
        for (double j1 = jmin; j1 <= jmax + 1e-6; j1 += paso)
        {
            var r1 = Rot(new double[] { 0, 0, 0 }, new double[] { 0, 0, 1 }, dj1 + j1);
            var r2 = Rot(p2, u2, sgn * (elev - elev0));
            c200.Transform2 = X200.Then(r1).ToMath();
            c300.Transform2 = X300.Then(r2).Then(r1).ToMath();
            c400.Transform2 = X400.Then(Along(ax, q3 - q30)).Then(r2).Then(r1).ToMath();
            if (c410 != null) c410.Transform2 = X410.Then(Along(ax, q3 / 2 - qm0)).Then(r2).Then(r1).ToMath();
            m.EditRebuild3();
            var xe2 = Xf.FromComp(Find(c300, "CB-301"));
            double real = Elev(Dir(xe2, new double[] { 1, 0, 0 }));
            double q3real = xe2.Inv().Apply(Xf.FromComp(brida).T)[0];
            string qm = c410 != null ? " intermedio " + xe2.Inv().Apply(Xf.FromComp(Find(c410, "CB-411")).T)[0].ToString("0.0") : "";
            var hits = new List<string>();
            try
            {
                idm.TreatCoincidenceAsInterference = false; idm.TreatSubAssembliesAsComponents = false; idm.IncludeMultibodyPartInterferences = false;
                object[] ints = (object[])idm.GetInterferences();
                if (ints != null) foreach (IInterference it in ints)
                    {
                        var names = ((object[])it.Components).Cast<Component2>().Select(c => c.Name2).ToArray();
                        Func<string, bool> any = s => names.Any(n => n.StartsWith(s));
                        bool arm = any("CB-300") || any("CB-400") || any("CB-410");
                        bool rest = any("CB-100") || any("CB-200");
                        bool shaft = names.Any(n => n.Contains("CB-506")) && names.Any(n => n.Contains("CB-502"));
                        int nArm = new[] { "CB-300", "CB-400", "CB-410" }.Count(p => any(p));
                        bool tool = names.All(n => n.StartsWith("CB-400") && n.Contains("/CB-6"));
                        bool screws = names.Any(n => n.Contains("/HW_M"));
                        bool inside410 = nArm == 1 && any("CB-410");
                        if ((arm && rest && !shaft) || nArm >= 2 || inside410)
                            if (!tool && !screws)
                                hits.Add((it.Volume * 1e9).ToString("0") + " mm3 " + string.Join(" <-> ", names.Select(n => n.Split('/').Last())));
                    }
                idm.Done();
            }
            catch (Exception ex) { hits.Add("ERROR " + ex.Message); }
            Console.WriteLine(string.Format("J1 {4,6:0}  J3 {0,5:0.0}{1}  elev {2,5:0.0}  {3}", q3real, qm, real, hits.Count == 0 ? "libre" : "CHOQUE " + string.Join(" | ", hits), j1));
        }
        SW.App.CloseDoc(m.GetTitle());
    }
}
