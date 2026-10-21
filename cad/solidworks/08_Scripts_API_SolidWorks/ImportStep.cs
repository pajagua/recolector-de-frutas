using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// ImportStep.exe <in.step> <out.SLDPRT>  : imports a STEP (part or single-part assembly) and saves the solid as a part
class ImportStep
{
    static void Main(string[] args)
    {
        SW.Connect();
        SW.CloseMine();
        int err = 0;
        var imp = (ImportStepData)SW.App.GetImportFileData(args[0]);
        var m = (ModelDoc2)SW.App.LoadFile4(args[0], "r", imp, ref err);
        if (m != null) SW.Created.Add(m.GetTitle());
        Console.WriteLine("import err=" + err);
        if (m == null) return;
        int e = 0, w = 0;
        bool ok;
        if (m.GetType() == (int)swDocumentTypes_e.swDocASSEMBLY)
        {
            // save the whole imported assembly as ONE multibody part (all components as bodies)
            SW.App.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swSaveAssemblyAsPartOptions, 2);
            ok = m.Extension.SaveAs3(args[1], 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent | (int)swSaveAsOptions_e.swSaveAsOptions_Copy, null, null, ref e, ref w);
        }
        else
            ok = m.Extension.SaveAs3(args[1], 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent | (int)swSaveAsOptions_e.swSaveAsOptions_Copy, null, null, ref e, ref w);
        Console.WriteLine("save " + ok + " err=" + e + " warn=" + w + " -> " + args[1]);
        SW.CloseMine();
    }
}
