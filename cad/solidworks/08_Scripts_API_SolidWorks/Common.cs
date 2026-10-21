using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

public static class SW
{
    public static SldWorks App;

    public static SldWorks Connect()
    {
        try { App = (SldWorks)Marshal.GetActiveObject("SldWorks.Application"); }
        catch
        {
            var t = Type.GetTypeFromProgID("SldWorks.Application");
            App = (SldWorks)Activator.CreateInstance(t);
        }
        App.Visible = true;
        App.UserControl = false;
        return App;
    }

    // Close only documents that belong to this job (never the user's other documents)
    public static readonly string[] Mine = {
        @"C:\Users\tikip\Desktop\CitriBot_Ensamblaje", @"C:\Users\tikip\Desktop\BCN3D-Moveo-master",
        @"C:\Users\tikip\Desktop\FIUNA\2026_2\R1\Proyecto\3D\Rx-rcp-003", @"C:\Users\tikip\Desktop\" + "\u00a0", @"C:\Users\tikip\AppData\Local\Temp\swx" };
    public static System.Collections.Generic.HashSet<string> Created = new System.Collections.Generic.HashSet<string>();
    // Close only documents this job opened. NEVER close a document with unsaved changes and never close everything at once
    // (the user may have the assembly open in the same SolidWorks session).
    public static void CloseMine()
    {
        object[] docs = (object[])App.GetDocuments();
        if (docs == null) return;
        var titles = new System.Collections.Generic.List<string>();
        foreach (object o in docs)
        {
            try
            {
                var d = (ModelDoc2)o;
                string p = d.GetPathName(); string t = d.GetTitle();
                if (d.GetSaveFlag() && !Created.Contains(t)) continue;           // dirty and not created here: leave it
                if (p.EndsWith("CB-000_CitriBot_Ensamblaje_General.SLDASM", StringComparison.OrdinalIgnoreCase) && !Created.Contains(t)) continue;
                string tl = t.ToLower();
                bool blankMine = p == "" && (Created.Contains(t) || tl.Contains(".stp") || tl.Contains(".step") || tl.Contains(".stl") || tl.Contains(".igs"));
                bool mine = blankMine || (p != "" && Mine.Any(m => p.StartsWith(m, StringComparison.OrdinalIgnoreCase)));
                if (mine && d.Visible) mine = Created.Contains(t) || blankMine || OpenedHere.Contains(p.ToLower());
                if (mine) titles.Add(t);
            }
            catch { }
        }
        for (int pass = 0; pass < 2; pass++)
            foreach (var t in titles.OrderBy(t => t.ToUpper().Contains(".SLDASM") ? 0 : 1)) { try { App.CloseDoc(t); } catch { } }
    }
    public static System.Collections.Generic.HashSet<string> OpenedHere = new System.Collections.Generic.HashSet<string>();

    public static ModelDoc2 Open(string path, bool silent = true)
    {
        int type = path.ToUpper().EndsWith(".SLDASM") ? (int)swDocumentTypes_e.swDocASSEMBLY :
                   path.ToUpper().EndsWith(".SLDDRW") ? (int)swDocumentTypes_e.swDocDRAWING : (int)swDocumentTypes_e.swDocPART;
        int err = 0, warn = 0;
        int opt = (int)swOpenDocOptions_e.swOpenDocOptions_Silent;
        ModelDoc2 m = null;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try { m = (ModelDoc2)App.OpenDoc6(path, type, opt, "", ref err, ref warn); break; }
            catch (System.Runtime.InteropServices.COMException)
            {
                // SolidWorks still busy after closing a large assembly: wait and reconnect
                System.Threading.Thread.Sleep(8000);
                App = null; Connect();
            }
        }
        if (m != null) OpenedHere.Add(path.ToLower());
        if (m == null) Console.WriteLine("OPEN FAIL " + path + " err=" + err + " warn=" + warn);
        else if (err != 0 || warn != 0) Console.WriteLine("open " + Path.GetFileName(path) + " err=" + err + " warn=" + warn);
        return m;
    }

    public static string F(double v) { return (v * 1000).ToString("0.###"); } // m -> mm
    public static string Fa(double v) { return v.ToString("0.####"); }

    public static string Xf(MathTransform t)
    {
        if (t == null) return "null";
        double[] a = (double[])t.ArrayData;
        // a[0..8] rotation (row-major), a[9..11] translation, a[12] scale
        return string.Format("R=[{0} {1} {2}; {3} {4} {5}; {6} {7} {8}] T=({9},{10},{11})",
            Fa(a[0]), Fa(a[1]), Fa(a[2]), Fa(a[3]), Fa(a[4]), Fa(a[5]), Fa(a[6]), Fa(a[7]), Fa(a[8]), F(a[9]), F(a[10]), F(a[11]));
    }

    public static string Box(double[] b)
    {
        if (b == null) return "nobox";
        return string.Format("[{0},{1},{2}]..[{3},{4},{5}] size=({6} x {7} x {8})",
            F(b[0]), F(b[1]), F(b[2]), F(b[3]), F(b[4]), F(b[5]), F(b[3] - b[0]), F(b[4] - b[1]), F(b[5] - b[2]));
    }
}
