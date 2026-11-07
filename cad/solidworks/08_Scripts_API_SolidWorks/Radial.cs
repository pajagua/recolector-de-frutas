using System;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
// Radial.exe <asm> <ref> <filtro,...> : radio mínimo/máximo (respecto del eje z de ref) y z de cada componente, por teselado
class Radial
{
    static void Main(string[] a)
    {
        SW.Connect();
        bool ya = SW.App.GetOpenDocumentByName(a[0]) != null;
        var m = SW.Open(a[0]); var asm = (AssemblyDoc)m; asm.ResolveAllLightWeightComponents(false);
        var cs = ((object[])asm.GetComponents(false)).Cast<Component2>().ToList();
        var rc = a[1] == "-" ? null : cs.First(c => c.Name2.Split('/').Last().StartsWith(a[1])); var inv = rc == null ? new Xf() : Xf.FromComp(rc).Inv();
        var f = a[2].Split(',');
        foreach (var c in cs)
        {
            if (!f.Any(x => c.Name2.Split('/').Last().StartsWith(x))) continue;
            object bi; var bodies = (object[])c.GetBodies3((int)swBodyType_e.swSolidBody, out bi); if (bodies == null) continue;
            var x0 = Xf.FromComp(c).Then(inv); double r0 = 1e9, r1 = 0, z0 = 1e9, z1 = -1e9, a0 = 0;
            foreach (Body2 b in bodies) foreach (Face2 fc in (object[])b.GetFaces())
                {
                    var t = fc.GetTessTriangles(true) as float[]; if (t == null) continue;
                    for (int i = 0; i + 2 < t.Length; i += 3)
                    {
                        var p = x0.Apply(new double[] { t[i] * 1000, t[i + 1] * 1000, t[i + 2] * 1000 });
                        double r = Math.Sqrt(p[0] * p[0] + p[1] * p[1]); if (r < r0) r0 = r; if (r > r1) { r1 = r; a0 = Math.Atan2(p[1], p[0]) * 180 / Math.PI; }
                        z0 = Math.Min(z0, p[2]); z1 = Math.Max(z1, p[2]);
                    }
                }
            Console.WriteLine(string.Format("{0,-45} r {1:0.0}..{2:0.0} (rmax a {5:0}°)  z {3:0.0}..{4:0.0}", c.Name2.Length > 45 ? c.Name2.Substring(c.Name2.Length - 45) : c.Name2, r0, r1, z0, z1, a0));
        }
        if (!ya) SW.App.CloseDoc(m.GetTitle());
    }
}
