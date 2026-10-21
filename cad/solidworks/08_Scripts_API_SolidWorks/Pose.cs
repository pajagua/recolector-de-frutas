using System;
using System.IO;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Pose.exe <CB-000> <j1deg> <j2deg> <imgprefix> : moves the arm (without saving) and checks mates + interference
class Pose
{
    static Xf Rot(double[] p, double[] u, double deg)
    {
        double t = deg * Math.PI / 180, c = Math.Cos(t), s = Math.Sin(t), n = Math.Sqrt(u[0] * u[0] + u[1] * u[1] + u[2] * u[2]);
        double x = u[0] / n, y = u[1] / n, z = u[2] / n;
        // column-vector rotation matrix M; row-vector convention uses R = M^T
        double[,] M = { { c + x * x * (1 - c), x * y * (1 - c) - z * s, x * z * (1 - c) + y * s },
                        { y * x * (1 - c) + z * s, c + y * y * (1 - c), y * z * (1 - c) - x * s },
                        { z * x * (1 - c) - y * s, z * y * (1 - c) + x * s, c + z * z * (1 - c) } };
        var r = new Xf();
        for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) r.R[i * 3 + j] = M[j, i];
        return Xf.Tr(-p[0], -p[1], -p[2]).Then(r).Then(Xf.Tr(p[0], p[1], p[2]));
    }
    static void Main(string[] args)
    {
        SW.Connect(); SW.CloseMine();
        var m = SW.Open(args[0]); var a = (AssemblyDoc)m;
        a.ResolveAllLightWeightComponents(false);
        double j1 = double.Parse(args[1]), j2 = double.Parse(args[2]);
        var comps = ((object[])a.GetComponents(true)).Cast<Component2>().ToDictionary(c => c.Name2.Substring(0, 6));
        // J2 axis at home (global Z-up): tower point (190.171, *, -86.723) along tower Y; tower pose = Torre * ZUp (recomputed from the component)
        var torreComp = ((object[])comps["CB-200"].GetChildren()).Cast<Component2>().First(c => c.Name2.Split((char)47).Last().StartsWith("CB-210"));
        var tw = Xf.FromComp(torreComp);
        double[] p2 = tw.Apply(new double[] { 190.171, 0, -86.723 });
        double[] p2b = tw.Apply(new double[] { 190.171, 1, -86.723 });
        double[] u2 = { p2b[0] - p2[0], p2b[1] - p2[1], p2b[2] - p2[2] };
        Console.WriteLine("J2 axis point " + string.Join(",", p2.Select(v => v.ToString("0.#"))) + " dir " + string.Join(",", u2.Select(v => v.ToString("0.###"))));
        var r1 = Rot(new double[] { 0, 0, 0 }, new double[] { 0, 0, 1 }, j1);
        var r2 = Rot(p2, u2, j2);
        comps["CB-200"].Transform2 = r1.ToMath();
        comps["CB-300"].Transform2 = r2.Then(r1).ToMath();
        comps["CB-400"].Transform2 = r2.Then(r1).ToMath();
        m.EditRebuild3();
        foreach (var k in new[] { "CB-200", "CB-300", "CB-400" }) Console.WriteLine(k + " " + SW.Xf(comps[k].Transform2));
        Feature f = (Feature)m.FirstFeature();
        while (f != null)
        {
            if (f.GetTypeName2() == "MateGroup")
                for (Feature sf = (Feature)f.GetFirstSubFeature(); sf != null; sf = (Feature)sf.GetNextSubFeature())
                { bool w; Console.WriteLine("mate " + sf.Name + " err=" + sf.GetErrorCode2(out w)); }
            f = (Feature)f.GetNextFeature();
        }
        var idm = a.InterferenceDetectionManager;
        idm.TreatCoincidenceAsInterference = false; idm.TreatSubAssembliesAsComponents = true; idm.IncludeMultibodyPartInterferences = false;
        object[] ints = (object[])idm.GetInterferences();
        Console.WriteLine("interferencias entre eslabones: " + (ints == null ? 0 : ints.Length));
        if (ints != null) foreach (IInterference it in ints)
                Console.WriteLine(string.Format("  {0,8:0.0} mm3 {1}", it.Volume * 1e9, string.Join(" <-> ", ((object[])it.Components).Cast<Component2>().Select(c => c.Name2))));
        idm.Done();
        foreach (var v in new[] { "iso", "front" })
        {
            m.ShowNamedView2(v == "iso" ? "*Isometric" : "*Front", -1); m.ViewZoomtofit2(); m.GraphicsRedraw2();
            int e = 0, w2 = 0; m.Extension.SaveAs3(args[3] + "_" + v + ".png", 0, 1, null, null, ref e, ref w2);
        }
        SW.App.CloseDoc(m.GetTitle());
    }
}
