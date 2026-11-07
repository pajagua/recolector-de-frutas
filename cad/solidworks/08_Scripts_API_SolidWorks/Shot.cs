using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Shot.exe <doc> <outprefix> [views...]   views: iso front back top bottom left right dimetric trimetric
class Shot
{
    static void Main(string[] args)
    {
        SW.Connect();
        if (System.Environment.GetEnvironmentVariable("SW_KEEP") == null) SW.CloseMine();
        bool ya = SW.App.GetOpenDocumentByName(args[0]) != null;
        var m = SW.Open(args[0]);
        if (m == null) return;
        int er0 = 0; SW.App.ActivateDoc3(m.GetTitle(), false, 0, ref er0);
        if (m.GetType() == (int)swDocumentTypes_e.swDocASSEMBLY) ((AssemblyDoc)m).ResolveAllLightWeightComponents(false);
        string[] views = args.Length > 2 ? new string[args.Length - 2] : new[] { "iso" }; if (args.Length > 2) Array.Copy(args, 2, views, 0, args.Length - 2);
        foreach (var v in views)
        {
            string name = v == "iso" ? "*Isometric" : v == "dimetric" ? "*Dimetric" : v == "trimetric" ? "*Trimetric" :
                "*" + char.ToUpper(v[0]) + v.Substring(1);
            m.ShowNamedView2(name, -1);
            m.ViewZoomtofit2();
            m.GraphicsRedraw2();
            int e = 0, w = 0;
            string outp = args[1] + "_" + v + ".png";
            bool ok = m.Extension.SaveAs3(outp, 0, 1, null, null, ref e, ref w);
            Console.WriteLine((ok ? "saved " : "FAIL ") + outp);
        }
            if (!ya) SW.App.CloseDoc(m.GetTitle());
    }
}
