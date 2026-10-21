using System;
using System.IO;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Pose.exe <CB-000> <j1deg> <j2deg> <imgprefix> : moves the arm (without saving) and checks mates + interference
class Sweep
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
        // E2 arm direction at home (E2 part +X) for elevation reporting
        var e2c = ((object[])comps["CB-300"].GetChildren()).Cast<Component2>().First(c => c.Name2.Split((char)47).Last().StartsWith("CB-301"));
        var e2x = Xf.FromComp(e2c);
        double[] q0 = e2x.Apply(new double[] { 0, 0, 0 }), q1 = e2x.Apply(new double[] { 1, 0, 0 });
        double[] armDir = { q1[0] - q0[0], q1[1] - q0[1], q1[2] - q0[2] };
        var idm = a.InterferenceDetectionManager;
        for (double ang = -180; ang < 180; ang += 15)
        {
            var r2 = Rot(p2, u2, ang);
            comps["CB-300"].Transform2 = r2.ToMath();
            comps["CB-400"].Transform2 = r2.ToMath();
            m.EditRebuild3();
            double[] d = { 0, 0, 0 };
            for (int j = 0; j < 3; j++) for (int k = 0; k < 3; k++) d[j] += armDir[k] * r2.R[k * 3 + j];
            double elev = Math.Asin(d[2]) * 180 / Math.PI;
            idm.TreatCoincidenceAsInterference = false; idm.TreatSubAssembliesAsComponents = true; idm.IncludeMultibodyPartInterferences = false;
            object[] ints = (object[])idm.GetInterferences();
            double vol = 0; var who = new System.Collections.Generic.HashSet<string>();
            if (ints != null) foreach (IInterference it in ints)
            {
                var names = ((object[])it.Components).Cast<Component2>().Select(c => c.Name2).ToArray();
                bool arm = names.Any(n => n.StartsWith("CB-300") || n.StartsWith("CB-400"));
                bool rest = names.Any(n => n.StartsWith("CB-100") || n.StartsWith("CB-200"));
                if (arm && rest && !(names.Any(n => n.Contains("CB-506")) && names.Any(n => n.Contains("CB-502"))))
                { vol += it.Volume * 1e9; foreach (var n in names) who.Add(n.Split((char)47).Last()); }
            }
            idm.Done();
            Console.WriteLine(string.Format("J2 {0,5:0} deg  elev {1,6:0.0} deg  choque {2,9:0.0} mm3  {3}", ang, elev, vol, string.Join(", ", who)));
        }
        SW.App.CloseDoc(m.GetTitle());
    }
}
