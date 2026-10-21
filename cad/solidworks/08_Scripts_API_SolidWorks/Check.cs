using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Check.exe <asm> [-int] [-img prefix]
class Check
{
    static void Main(string[] args)
    {
        SW.Connect();
        SW.CloseMine();
        var m = SW.Open(args[0]);
        var a = (AssemblyDoc)m;
        a.ResolveAllLightWeightComponents(false);
        m.ForceRebuild3(false);
        foreach (Component2 c in (object[])a.GetComponents(true))
            Console.WriteLine("comp " + c.Name2 + " " + SW.Xf(c.Transform2) + " fixed=" + c.IsFixed());
        Feature f = (Feature)m.FirstFeature();
        while (f != null)
        {
            if (f.GetTypeName2() == "MateGroup")
            {
                Feature sf = (Feature)f.GetFirstSubFeature();
                while (sf != null)
                {
                    bool warn;
                    int ec = sf.GetErrorCode2(out warn);
                    Console.WriteLine("mate " + sf.Name + " err=" + ec + " warn=" + warn);
                    sf = (Feature)sf.GetNextSubFeature();
                }
            }
            f = (Feature)f.GetNextFeature();
        }
        if (args.Contains("-int"))
        {
            var idm = a.InterferenceDetectionManager;
            idm.TreatCoincidenceAsInterference = false;
            idm.TreatSubAssembliesAsComponents = false;
            idm.IncludeMultibodyPartInterferences = false;
            idm.MakeInterferingPartsTransparent = false;
            idm.CreateFastenersFolder = true;
            idm.IgnoreHiddenBodies = true;
            object[] ints = (object[])idm.GetInterferences();
            Console.WriteLine("interferences: " + (ints == null ? 0 : ints.Length));
            if (ints != null)
                foreach (IInterference it in ints)
                {
                    var comps = ((object[])it.Components).Cast<Component2>().Select(c => c.Name2.Split('/').Last());
                    Console.WriteLine(string.Format("  {0,10:0.0} mm3  {1}", it.Volume * 1e9, string.Join("  <->  ", comps)));
                }
            idm.Done();
        }
        int ii = Array.IndexOf(args, "-img");
        if (ii >= 0)
        {
            string pre = args[ii + 1];
            foreach (var v in new[] { "iso", "front", "right", "top", "trimetric" })
            {
                string name = v == "iso" ? "*Isometric" : v == "trimetric" ? "*Trimetric" : "*" + char.ToUpper(v[0]) + v.Substring(1);
                m.ShowNamedView2(name, -1); m.ViewZoomtofit2(); m.GraphicsRedraw2();
                int e = 0, w = 0;
                m.Extension.SaveAs3(pre + "_" + v + ".png", 0, 1, null, null, ref e, ref w);
            }
        }
            SW.App.CloseDoc(m.GetTitle());
    }
}
