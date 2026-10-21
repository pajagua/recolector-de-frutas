using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Pack.exe <asm> <mapfile> [-list]
// mapfile lines: <original file name>|<new full path>
class Pack
{
    static void Main(string[] args)
    {
        SW.Connect();
        SW.CloseMine();
        var m = SW.Open(args[0]);
        if (m == null) return;
        var pgo = (PackAndGo)m.Extension.GetPackAndGo();
        pgo.IncludeDrawings = false;
        pgo.IncludeSimulationResults = false;
        pgo.IncludeToolboxComponents = true;
        pgo.IncludeSuppressed = true;
        object names;
        bool st = pgo.GetDocumentNames(out names);
        string[] docs = (string[])names;
        if (args.Length > 2 && args[2] == "-list")
        {
            foreach (var d in docs) Console.WriteLine(d);
            return;
        }
        var map = File.ReadAllLines(args[1]).Where(l => l.Contains("|")).Select(l => l.Split('|'))
            .ToDictionary(p => p[0].Trim().ToLowerInvariant(), p => p[1].Trim());
        var outNames = new string[docs.Length];
        bool missing = false;
        for (int i = 0; i < docs.Length; i++)
        {
            string fn = Path.GetFileName(docs[i]).ToLowerInvariant();
            if (!map.ContainsKey(fn)) { Console.WriteLine("NO MAP: " + docs[i]); missing = true; continue; }
            outNames[i] = map[fn];
            Directory.CreateDirectory(Path.GetDirectoryName(outNames[i]));
        }
        if (missing) return;
        pgo.SetSaveToName(true, Path.GetDirectoryName(outNames[0]));
        pgo.FlattenToSingleFolder = false;
        bool ok = pgo.SetDocumentSaveToNames(outNames);
        Console.WriteLine("SetDocumentSaveToNames " + ok);
        int[] res = (int[])m.Extension.SavePackAndGo(pgo);
        for (int i = 0; i < docs.Length; i++) Console.WriteLine((res != null && i < res.Length ? res[i].ToString() : "?") + "  " + Path.GetFileName(docs[i]) + " -> " + outNames[i]);
        SW.CloseMine();
    }
}
