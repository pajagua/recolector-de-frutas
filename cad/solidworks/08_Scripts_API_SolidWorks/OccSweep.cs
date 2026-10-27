using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// OccSweep.exe <CB-000> <dir_conos> <q3> <elev...> : agrega (sin guardar) un cono de vision por camara, desde 12 mm
// delante de la lente hasta el plano del centro de la esfera, tangente a la esfera de 300 mm, y lista que lo tapa en cada pose
// (J1 apuntando al arbol, J3 = q3, J2 = cada elev).
class OccSweep
{
    static readonly double[] S = { 394.8, 0, 270 };                 // centro de la esfera (marco arbol)
    static readonly double[][] LENS = { new[] { -217.3, 261.5, 670.0 }, new[] { -254.3, 147.4, 670.0 } };   // izq, der (Vision.exe)
    const double RS = 150, START = 12;
    static double[] Sub(double[] a, double[] b) { return new[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] }; }
    static double Norm(double[] a) { return Math.Sqrt(a[0] * a[0] + a[1] * a[1] + a[2] * a[2]); }
    static double[] Unit(double[] a) { double n = Norm(a); return new[] { a[0] / n, a[1] / n, a[2] / n }; }
    static double[] Cross(double[] a, double[] b) { return new[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] }; }

    static string Cone(string dir, int i)
    {
        double d = Norm(Sub(S, LENS[i])), alfa = Math.Asin(RS / d);
        PB.NewPart();
        // cono escalonado: 30 cilindros, cada uno con el radio de su extremo lejano (un poco más grande que el cono real)
        int N = 30; double L = (d - START) / N;
        for (int k = 0; k < N; k++)
        {
            double x0 = START + k * L, r = (x0 + L) * Math.Tan(alfa);
            PB.Sketch('R'); PB.Circle(0, 0, 2 * r); PB.Extrude(x0, L, false, "Tramo_" + k);
        }
        PB.Report("cono " + i + " d=" + d.ToString("0") + " alfa=" + (alfa * 180 / Math.PI).ToString("0.0"));
        string p = Path.Combine(dir, "CONO_vision_" + (i == 0 ? "izq" : "der") + ".SLDPRT");
        PB.Save(p); SW.App.CloseDoc(PB.M.GetTitle());
        return p;
    }
    static Xf ConePlace(int i)
    {
        var u = Unit(Sub(S, LENS[i])); var v = Unit(Cross(new double[] { 0, 0, 1 }, u)); var w = Cross(u, v);
        var tree = Xf.Rows(u, v, w, LENS[i]);
        double c = Math.Cos(-112.5 * Math.PI / 180), s = Math.Sin(-112.5 * Math.PI / 180);
        return tree.Then(Xf.Rows(new[] { c, s, 0 }, new[] { -s, c, 0 }, new double[] { 0, 0, 1 }, new double[] { 0, 0, 0 }));
    }
    static Xf Rot(double[] p, double[] u, double deg)
    {
        double t = deg * Math.PI / 180, c = Math.Cos(t), s = Math.Sin(t), n = Norm(u);
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
        foreach (Component2 c in ch) { if (Path.GetFileName(c.GetPathName()).StartsWith(start, StringComparison.OrdinalIgnoreCase)) return c; var r = Find(c, start); if (r != null) return r; }
        return null;
    }
    static double[] Dir(Xf x, double[] v) { var a = x.Apply(new double[] { 0, 0, 0 }); var b = x.Apply(v); return new[] { b[0] - a[0], b[1] - a[1], b[2] - a[2] }; }
    static double Elev(double[] d) { return Math.Asin(d[2] / Norm(d)) * 180 / Math.PI; }

    static void Main(string[] args)
    {
        SW.Connect();
        string dir = args[1]; double q3 = double.Parse(args[2]);
        var elevs = args.Skip(3).Select(double.Parse).ToArray();
        var cones = new[] { Cone(dir, 0), Cone(dir, 1) };
        var m = SW.Open(args[0]); var a = (AssemblyDoc)m; a.ResolveAllLightWeightComponents(false);
        int er = 0; SW.App.ActivateDoc3(m.GetTitle(), false, 0, ref er);
        var top = ((object[])a.GetComponents(true)).Cast<Component2>().ToDictionary(c => c.Name2.Substring(0, 6));
        for (int i = 0; i < 2; i++)
        {
            int e = 0, w = 0;
            SW.App.DocumentVisible(false, (int)swDocumentTypes_e.swDocPART);
            SW.App.OpenDoc6(cones[i], (int)swDocumentTypes_e.swDocPART, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref e, ref w);
            SW.App.DocumentVisible(true, (int)swDocumentTypes_e.swDocPART);
            var c = (Component2)a.AddComponent5(cones[i], 0, "", false, "", 0, 0, 0);
            c.Transform2 = ConePlace(i).ToMath();
            m.ClearSelection2(true); c.Select4(false, null, false); a.FixComponent(); m.ClearSelection2(true);
        }
        var c200 = top["CB-200"]; var c300 = top["CB-300"]; var c400 = top["CB-400"];
        var X200 = Xf.FromComp(c200); var X300 = Xf.FromComp(c300); var X400 = Xf.FromComp(c400);
        var e2w = Xf.FromComp(Find(c300, "CB-301")); double[] ax = Dir(e2w, new double[] { 1, 0, 0 }); double elev0 = Elev(ax);
        var tw = Xf.FromComp(Find(c200, "CB-210"));
        double[] p2 = tw.Apply(new double[] { 190.171, 0, -86.723 }), p2b = tw.Apply(new double[] { 190.171, 1, -86.723 });
        double[] u2 = { p2b[0] - p2[0], p2b[1] - p2[1], p2b[2] - p2[2] };
        double sgn = Elev(Dir(Rot(p2, u2, 1), ax)) > elev0 ? 1 : -1;
        // arbol en el mundo: x_arbol = (cos -112.5, sin -112.5)
        double tx = Math.Cos(-112.5 * Math.PI / 180), ty = Math.Sin(-112.5 * Math.PI / 180);
        double axh = Math.Sqrt(ax[0] * ax[0] + ax[1] * ax[1]);
        double dj1 = Math.Atan2(ax[0] / axh * ty - ax[1] / axh * tx, ax[0] / axh * tx + ax[1] / axh * ty) * 180 / Math.PI;
        var r1 = Rot(new double[] { 0, 0, 0 }, new double[] { 0, 0, 1 }, dj1);
        var brida = Find(c400, "CB-402");
        double q30 = e2w.Inv().Apply(Xf.FromComp(brida).T)[0];
        for (Feature f = (Feature)m.FirstFeature(); f != null; f = (Feature)f.GetNextFeature())
            if (f.GetTypeName2() == "MateGroup")
                for (Feature sf = (Feature)f.GetFirstSubFeature(); sf != null; sf = (Feature)sf.GetNextSubFeature())
                    if (sf.Name.Contains("limite")) sf.SetSuppression2((int)swFeatureSuppressionAction_e.swSuppressFeature, (int)swInConfigurationOpts_e.swThisConfiguration, null);
        var idm = a.InterferenceDetectionManager;
        foreach (double elev in elevs)
        {
            var r2 = Rot(p2, u2, sgn * (elev - elev0));
            c200.Transform2 = X200.Then(r1).ToMath();
            c300.Transform2 = X300.Then(r2).Then(r1).ToMath();
            c400.Transform2 = X400.Then(Xf.Tr((q3 - q30) * ax[0], (q3 - q30) * ax[1], (q3 - q30) * ax[2])).Then(r2).Then(r1).ToMath();
            m.EditRebuild3();
            double real = Elev(Dir(Xf.FromComp(Find(c300, "CB-301")), new double[] { 1, 0, 0 }));
            var hits = new List<string>();
            try
            {
                idm.TreatCoincidenceAsInterference = false; idm.TreatSubAssembliesAsComponents = false; idm.IncludeMultibodyPartInterferences = false;
                object[] ints = (object[])idm.GetInterferences();
                if (ints != null) foreach (IInterference it in ints)
                    {
                        var names = ((object[])it.Components).Cast<Component2>().Select(c => c.Name2.Split('/').Last()).ToArray();
                        var cone = names.FirstOrDefault(n => n.StartsWith("CONO"));
                        if (cone == null) continue;
                        var other = names.First(n => n != cone);
                        if (other.StartsWith("CONO") || other.StartsWith("CB-515") || other.StartsWith("CB-15")) continue;
                        hits.Add(cone.Replace("CONO_vision_", "").Split('-')[0] + ": " + other + " " + (it.Volume * 1e9).ToString("0") + " mm3");
                    }
                idm.Done();
            }
            catch (Exception ex) { hits.Add("ERROR " + ex.Message); }
            Console.WriteLine(string.Format("J3 {0,4:0}  elev {1,5:0.0}  {2}", q3, real, hits.Count == 0 ? "vista libre" : "TAPA " + string.Join(" | ", hits)));
        }
        SW.App.CloseDoc(m.GetTitle());
    }
}
