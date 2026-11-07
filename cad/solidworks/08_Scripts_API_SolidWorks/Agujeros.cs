using System;
using System.Collections.Generic;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Agujeros.exe <asm> <pieza> <rmin> <rmax> <tornillo> : agujeros cilíndricos (radio entre rmin y rmax, eje vertical del ensamblaje)
// de la pieza y distancia de cada tornillo (filtro) al agujero más cercano, en planta (coordenadas del ensamblaje)
class Agujeros
{
    static void Main(string[] a)
    {
        SW.Connect();
        bool ya = SW.App.GetOpenDocumentByName(a[0]) != null;
        var m = SW.Open(a[0]); var asm = (AssemblyDoc)m; asm.ResolveAllLightWeightComponents(false);
        double rmin = double.Parse(a[2]), rmax = double.Parse(a[3]);
        var cs = ((object[])asm.GetComponents(false)).Cast<Component2>().ToList();
        var pz = cs.First(c => c.Name2.Split('/').Last().StartsWith(a[1])); var xp = Xf.FromComp(pz);
        var hol = new List<double[]>(); object bi;
        foreach (Body2 b in (object[])pz.GetBodies3((int)swBodyType_e.swSolidBody, out bi))
            foreach (Face2 f in (object[])b.GetFaces())
            {
                if (!((Surface)f.GetSurface()).IsCylinder()) continue;
                var t = f.GetTessTriangles(true) as float[]; if (t == null) continue;
                var p = new List<double[]>(); for (int i = 0; i + 2 < t.Length; i += 3) p.Add(xp.Apply(new double[] { t[i] * 1000, t[i + 1] * 1000, t[i + 2] * 1000 }));
                double cx = p.Average(q => q[0]), cy = p.Average(q => q[1]);
                double r = p.Average(q => Math.Sqrt((q[0] - cx) * (q[0] - cx) + (q[1] - cy) * (q[1] - cy)));
                double zr = p.Max(q => q[2]) - p.Min(q => q[2]);
                if (r >= rmin && r <= rmax && zr > 0.5) hol.Add(new[] { cx, cy, r, p.Min(q => q[2]), p.Max(q => q[2]) });
            }
        Console.WriteLine(a[1] + ": " + hol.Count + " agujeros");
        foreach (var h in hol) Console.WriteLine(string.Format("  agujero ({0:0.00}, {1:0.00}) Ø{2:0.0}  z {3:0.0}..{4:0.0}", h[0], h[1], 2 * h[2], h[3], h[4]));
        foreach (var c in cs.Where(c => c.Name2.Split('/').Last().StartsWith(a[4])))
        {
            var bb = (double[])c.GetBox(false, false); double cx = (bb[0] + bb[3]) * 500, cy = (bb[1] + bb[4]) * 500;
            var h = hol.OrderBy(q => Math.Pow(q[0] - cx, 2) + Math.Pow(q[1] - cy, 2)).FirstOrDefault();
            double d = h == null ? -1 : Math.Sqrt(Math.Pow(h[0] - cx, 2) + Math.Pow(h[1] - cy, 2));
            Console.WriteLine(string.Format("  {0,-28} centro ({1:0.00}, {2:0.00})  al agujero más cercano: {3:0.00} mm", c.Name2.Split('/').Last(), cx, cy, d));
        }
        if (!ya) SW.App.CloseDoc(m.GetTitle());
    }
}
