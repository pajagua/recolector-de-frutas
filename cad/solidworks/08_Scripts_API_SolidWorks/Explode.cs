using System;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
// Explode.exe <CB-150> <png_prefix> : vista explotada del cabezal (sin guardar)
class Explode
{
    static void Main(string[] a)
    {
        SW.Connect();
        var m = SW.Open(a[0]); var asm = (AssemblyDoc)m; asm.ResolveAllLightWeightComponents(false);
        int er = 0; SW.App.ActivateDoc3(m.GetTitle(), false, 0, ref er);
        var comps = ((object[])asm.GetComponents(true)).Cast<Component2>().ToList();
        var ad = comps.First(c => c.Name2.StartsWith("CB-154"));
        var xa = Xf.FromComp(ad); var o = xa.Apply(new double[] { 0, 0, 0 }); var zb = xa.Apply(new double[] { 0, 0, 1 });
        double[] up = { zb[0] - o[0], zb[1] - o[1], zb[2] - o[2] };
        Func<double, Xf> along = d => Xf.Tr(d * up[0], d * up[1], d * up[2]);
        foreach (var c in comps)
        {
            string n = c.Name2; double d = n.StartsWith("CB-156") ? 45 : n.StartsWith("CB-157") ? 75 : n.StartsWith("CB-515") ? 105 : 0;
            if (n.StartsWith("CB-151") || n.StartsWith("CB-152")) c.Visible = 0;
            if (d > 0) { m.ClearSelection2(true); c.Select4(false, null, false); asm.UnfixComponent(); m.ClearSelection2(true); c.Transform2 = Xf.FromComp(c).Then(along(d)).ToMath(); }
        }
        m.EditRebuild3();
        foreach (var v in new[] { "trimetric", "front" })
        {
            m.ShowNamedView2(v == "trimetric" ? "*Trimetric" : "*Front", -1); m.ViewZoomtofit2(); m.GraphicsRedraw2();
            int e = 0, w = 0; string p = a[1] + "_" + v + ".png";
            Console.WriteLine((m.Extension.SaveAs3(p, 0, 1, null, null, ref e, ref w) ? "saved " : "FAIL ") + p);
        }
        SW.App.CloseDoc(m.GetTitle());
    }
}
