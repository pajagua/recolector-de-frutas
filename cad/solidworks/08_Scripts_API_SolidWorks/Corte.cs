using System;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Corte.exe <doc> <outprefix> <x_plano_mm> <x1 y1 z1 x2 y2 z2 | fit> [vistas...] : vista en corte por el plano x = x_plano (paralelo al plano derecho),
// imagen PNG por vista y cierra sin guardar.
class Corte
{
    static void Main(string[] a)
    {
        SW.Connect();
        var m = SW.Open(a[0]); if (m == null) return;
        if (m.GetType() == (int)swDocumentTypes_e.swDocASSEMBLY) ((AssemblyDoc)m).ResolveAllLightWeightComponents(false);
        int er = 0; SW.App.ActivateDoc3(m.GetTitle(), false, 0, ref er);
        double xp = double.Parse(a[2]);
        bool fit = a[3] == "fit"; int k = fit ? 4 : 9;
        double[] box = fit ? null : Enumerable.Range(0, 6).Select(i => double.Parse(a[3 + i]) / 1000).ToArray();
        Feature right = null;
        for (var f = (Feature)m.FirstFeature(); f != null; f = (Feature)f.GetNextFeature())
            if (f.GetTypeName2() == "RefPlane")
            {
                var t = (double[])((MathTransform)((RefPlane)f.GetSpecificFeature2()).Transform).ArrayData;
                if (Math.Abs(t[6]) > 0.9 || Math.Abs(t[0]) > 0.9 && Math.Abs(t[8]) < 0.1 && Math.Abs(t[6]) > 0.9) { right = f; break; }
            }
        if (right == null) throw new Exception("sin plano derecho");
        Feature pl = null;
        foreach (bool flip in new[] { false, true })
        {
            m.ClearSelection2(true); right.Select2(false, 0);
            int c = (int)swRefPlaneReferenceConstraints_e.swRefPlaneReferenceConstraint_Distance | (flip ? (int)swRefPlaneReferenceConstraints_e.swRefPlaneReferenceConstraint_OptionFlip : 0);
            pl = (Feature)m.FeatureManager.InsertRefPlane(c, Math.Abs(xp) / 1000, 0, 0, 0, 0);
            if (pl == null) continue;
            var t = (double[])((MathTransform)((RefPlane)pl.GetSpecificFeature2()).Transform).ArrayData;
            Console.WriteLine("plano flip=" + flip + " T = " + string.Join(" ", t.Take(12).Select(v => v.ToString("0.###"))));
            if (Math.Abs(t[9] * 1000 - xp) < 0.5 || Math.Abs(xp) < 1e-6) break;
            m.ClearSelection2(true); pl.Select2(false, 0); m.EditDelete(); pl = null;
        }
        if (pl == null) throw new Exception("no se pudo crear el plano");
        var mvm = m.ModelViewManager;
        var svd = (SectionViewData)mvm.CreateSectionViewData();
        svd.FirstPlane = pl; svd.FirstReverseDirection = a.Contains("rev");
        Console.WriteLine("corte " + mvm.CreateSectionView(svd));
        pl.Select2(false, 0); m.BlankRefGeom(); m.ClearSelection2(true);
        foreach (var v in a.Skip(k).Where(s => s != "rev"))
        {
            string name = v == "iso" ? "*Isometric" : v == "trimetric" ? "*Trimetric" : "*" + char.ToUpper(v[0]) + v.Substring(1);
            m.ShowNamedView2(name, -1);
            if (fit) m.ViewZoomtofit2(); else m.ViewZoomTo2(box[0], box[1], box[2], box[3], box[4], box[5]);
            m.GraphicsRedraw2();
            int e = 0, w = 0; string outp = a[1] + "_" + v + ".png";
            Console.WriteLine((m.Extension.SaveAs3(outp, 0, 1, null, null, ref e, ref w) ? "saved " : "FAIL ") + outp);
        }
        SW.App.CloseDoc(m.GetTitle());
    }
}
