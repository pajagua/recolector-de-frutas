using System;
using System.IO;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

class Dump
{
    static void Walk(Component2 c, int depth, bool recurse)
    {
        string ind = new string(' ', depth * 2);
        string p = c.GetPathName();
        double[] box = null;
        try { box = (double[])c.GetBox(false, false); } catch { }
        Console.WriteLine(ind + "- " + c.Name2 + " | " + p + " | sup=" + c.GetSuppression() + " fixed=" + c.IsFixed() + " cfg=" + c.ReferencedConfiguration);
        Console.WriteLine(ind + "    " + SW.Xf(c.Transform2));
        Console.WriteLine(ind + "    box " + SW.Box(box));
        if (!recurse) return;
        object[] ch = (object[])c.GetChildren();
        if (ch != null) foreach (Component2 k in ch) Walk(k, depth + 1, recurse);
    }

    static void Main(string[] args)
    {
        SW.Connect();
        SW.CloseMine();
        bool recurse = args.Length > 1 && args[1] == "-r";
        var m = SW.Open(args[0]);
        if (m == null) return;
        Console.WriteLine("DOC " + m.GetPathName());
        if (m.GetType() == (int)swDocumentTypes_e.swDocASSEMBLY)
        {
            var a = (AssemblyDoc)m;
            a.ResolveAllLightWeightComponents(false);
            object[] comps = (object[])a.GetComponents(true);
            Console.WriteLine("Top-level components: " + comps.Length);
            foreach (Component2 c in comps) Walk(c, 0, recurse);
            // mates
            Console.WriteLine("MATES:");
            Feature f = (Feature)m.FirstFeature();
            while (f != null)
            {
                if (f.GetTypeName2() == "MateGroup")
                {
                    Feature sf = (Feature)f.GetFirstSubFeature();
                    while (sf != null)
                    {
                        object spec = sf.GetSpecificFeature2();
                        Mate2 mt = spec as Mate2;
                        if (mt != null)
                        {
                            string s = "  " + sf.Name + " type=" + (swMateType_e)mt.Type + " align=" + mt.Alignment + " sup=" + sf.IsSuppressed();
                            for (int i = 0; i < mt.GetMateEntityCount(); i++)
                            {
                                MateEntity2 me = mt.MateEntity(i);
                                var rc = me.ReferenceComponent;
                                double[] pr = (double[])me.EntityParams;
                                s += "\n      [" + (rc != null ? rc.Name2 : "?") + " " + (swSelectType_e)me.ReferenceType2 + " p=" + string.Join(",", pr.Select(x => (x).ToString("0.####"))) + "]";
                            }
                            Console.WriteLine(s);
                        }
                        else Console.WriteLine("  " + sf.Name + " (" + sf.GetTypeName2() + ")");
                        sf = (Feature)sf.GetNextSubFeature();
                    }
                }
                f = (Feature)f.GetNextFeature();
            }
        }
        else
        {
            var pd = (PartDoc)m;
            object[] bodies = (object[])pd.GetBodies2((int)swBodyType_e.swSolidBody, true);
            Console.WriteLine("bodies: " + (bodies == null ? 0 : bodies.Length));
            if (bodies != null) foreach (Body2 b in bodies) Console.WriteLine("  " + b.Name + " box " + SW.Box((double[])b.GetBodyBox()));
            Feature f = (Feature)m.FirstFeature();
            while (f != null) { Console.WriteLine("  feat " + f.Name + " (" + f.GetTypeName2() + ")"); f = (Feature)f.GetNextFeature(); }
        }
        if (args.Length > 2 && args[2] == "-close") SW.App.CloseDoc(m.GetTitle());
    }
}
