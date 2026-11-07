using System;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Local.exe <asm> <ref> <filtro,...> : caja de cada componente (cuerpos en su espacio de pieza, 8 esquinas) en el sistema de la pieza ref
class Local
{
    static void Main(string[] a)
    {
        SW.Connect();
        bool ya = SW.App.GetOpenDocumentByName(a[0]) != null;
        var m = SW.Open(a[0]); var asm = (AssemblyDoc)m; asm.ResolveAllLightWeightComponents(false);
        var cs = ((object[])asm.GetComponents(false)).Cast<Component2>().ToList();
        var rc = a[1] == "-" ? null : cs.First(c => c.Name2.Split('/').Last().StartsWith(a[1])); var inv = rc == null ? new Xf() : Xf.FromComp(rc).Inv();
        if (rc != null) Console.WriteLine("ref " + rc.Name2 + " " + Xf.FromComp(rc));
        var f = a[2].Split(',');
        foreach (var c in cs)
        {
            if (!f.Any(x => c.Name2.Split('/').Last().StartsWith(x))) continue;
            var x0 = Xf.FromComp(c).Then(inv); object bi; var bodies = (object[])c.GetBodies3((int)swBodyType_e.swSolidBody, out bi);
            if (bodies == null) continue;
            double[] lo = { 1e9, 1e9, 1e9 }, hi = { -1e9, -1e9, -1e9 };
            foreach (Body2 b in bodies)
            {
                var bb = (double[])b.GetBodyBox();
                for (int i = 0; i < 8; i++)
                {
                    var p = x0.Apply(new[] { ((i & 1) == 0 ? bb[0] : bb[3]) * 1000, ((i & 2) == 0 ? bb[1] : bb[4]) * 1000, ((i & 4) == 0 ? bb[2] : bb[5]) * 1000 });
                    for (int k = 0; k < 3; k++) { lo[k] = Math.Min(lo[k], p[k]); hi[k] = Math.Max(hi[k], p[k]); }
                }
            }
            var o = x0.Apply(new[] { 0.0, 0, 0 });
            Console.WriteLine(string.Format("{0,-45} x {1:0.0}..{2:0.0}  y {3:0.0}..{4:0.0}  z {5:0.0}..{6:0.0}  origen ({7:0.0},{8:0.0},{9:0.0}) R {10}",
                c.Name2.Length > 45 ? c.Name2.Substring(c.Name2.Length - 45) : c.Name2, lo[0], hi[0], lo[1], hi[1], lo[2], hi[2], o[0], o[1], o[2],
                string.Join(" ", x0.R.Select(v => v.ToString("0.00")))));
        }
        if (!ya) SW.App.CloseDoc(m.GetTitle());
    }
}
