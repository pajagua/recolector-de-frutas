using System;
using System.IO;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
// MechShot.exe <asm> <outprefix> <q3> <ocultar: 0 = nada, 1 = base, torre y E2> [vistas...] : captura sin guardar
class MechShot
{
    static Component2 Find(Component2 parent, string start)
    {
        var ch = parent.GetChildren() as object[]; if (ch == null) return null;
        foreach (Component2 c in ch) { if (Path.GetFileName(c.GetPathName()).StartsWith(start, StringComparison.OrdinalIgnoreCase)) return c; var r = Find(c, start); if (r != null) return r; }
        return null;
    }
    static void Main(string[] args)
    {
        SW.Connect();
        double q3 = double.Parse(args[2]); bool hide = args[3] == "1";
        var m = SW.Open(args[0]); var a = (AssemblyDoc)m; a.ResolveAllLightWeightComponents(false);
        var top = ((object[])a.GetComponents(true)).Cast<Component2>().ToDictionary(c => c.Name2.Substring(0, 6));
        for (Feature f = (Feature)m.FirstFeature(); f != null; f = (Feature)f.GetNextFeature())
            if (f.GetTypeName2() == "MateGroup")
                for (Feature sf = (Feature)f.GetFirstSubFeature(); sf != null; sf = (Feature)sf.GetNextSubFeature())
                    if (sf.Name.Contains("limite")) sf.SetSuppression2((int)swFeatureSuppressionAction_e.swSuppressFeature, (int)swInConfigurationOpts_e.swThisConfiguration, null);
        var e2 = Find(top["CB-300"], "CB-301");
        var xe = Xf.FromComp(e2); var o = xe.Apply(new double[] { 0, 0, 0 }); var p = xe.Apply(new double[] { 1, 0, 0 });
        double[] ax = { p[0] - o[0], p[1] - o[1], p[2] - o[2] };
        Func<double, Xf> along = d => Xf.Tr(d * ax[0], d * ax[1], d * ax[2]);
        top["CB-400"].Transform2 = Xf.FromComp(top["CB-400"]).Then(along(q3)).ToMath();
        if (top.ContainsKey("CB-410")) top["CB-410"].Transform2 = Xf.FromComp(top["CB-410"]).Then(along(q3 / 2)).ToMath();
        m.EditRebuild3();
        if (hide)
        {
            top["CB-100"].Visible = 0; top["CB-200"].Visible = 0; e2.Visible = 0;
        }
        foreach (var v in args.Skip(4))
        {
            m.ShowNamedView2(v == "trimetric" ? "*Trimetric" : "*" + char.ToUpper(v[0]) + v.Substring(1), -1);
            m.ViewZoomtofit2(); m.GraphicsRedraw2();
            int e = 0, w = 0; string outp = args[1] + "_" + v + ".png";
            Console.WriteLine((m.Extension.SaveAs3(outp, 0, 1, null, null, ref e, ref w) ? "saved " : "FAIL ") + outp);
        }
        SW.App.CloseDoc(m.GetTitle());   // sin guardar
    }
}
