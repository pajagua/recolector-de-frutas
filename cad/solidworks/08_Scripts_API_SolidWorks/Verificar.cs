using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Verificar.exe <root solidworks> <ensamblaje relativo> ... (v7.11, solo lectura: no guarda)
// Abre cada ensamblaje sin ventana, lo reconstruye y lista: errores y advertencias (What's Wrong) del ensamblaje y de cada
// documento referenciado, componentes sin documento, referencias fuera del repo o a archivos que no existen, y relaciones con
// error. Al final cierra sin guardar lo que abrió.
class Verificar
{
    static HashSet<string> vistos = new HashSet<string>();
    static int problemas = 0;
    static void Reportar(ModelDoc2 m)
    {
        if (m == null || !vistos.Add(m.GetPathName().ToLower())) return;
        int n = m.Extension.GetWhatsWrongCount();
        if (n <= 0) return;
        object feats, codes, warns; m.Extension.GetWhatsWrong(out feats, out codes, out warns);
        var fs = (object[])feats; var cs = (int[])codes; var ws = (bool[])warns;
        for (int i = 0; i < n; i++)
        {
            var f = fs[i] as Feature; problemas++;
            Console.WriteLine("  " + (ws[i] ? "ADVERTENCIA " : "ERROR       ") + Path.GetFileName(m.GetPathName()) + " :: " +
                (f != null ? f.Name + " (" + f.GetTypeName2() + ")" : "?") + "  " + (swFeatureError_e)cs[i]);
        }
    }
    static void Main(string[] a)
    {
        SW.Connect();
        string root = Path.GetFullPath(a[0]).TrimEnd('\\') + "\\";
        var abiertosAntes = new HashSet<string>(((object[])SW.App.GetDocuments() ?? new object[0]).Cast<ModelDoc2>().Select(d => d.GetPathName().ToLower()));
        foreach (var rel in a.Skip(1))
        {
            string p = Path.Combine(root, rel);
            Console.WriteLine("== " + rel);
            if (!File.Exists(p)) { Console.WriteLine("  NO EXISTE"); problemas++; continue; }
            bool ya = SW.App.GetOpenDocumentByName(p) != null;
            if (ya && ((ModelDoc2)SW.App.GetOpenDocumentByName(p)).Visible) { Console.WriteLine("  abierto con ventana: se salta"); continue; }
            SW.App.DocumentVisible(false, (int)swDocumentTypes_e.swDocASSEMBLY);
            ModelDoc2 m;
            try { m = SW.Open(p); } finally { SW.App.DocumentVisible(true, (int)swDocumentTypes_e.swDocASSEMBLY); }
            if (m == null) { problemas++; continue; }
            var asm = (AssemblyDoc)m; asm.ResolveAllLightWeightComponents(false);
            m.ForceRebuild3(false);
            var comps = ((object[])asm.GetComponents(false) ?? new object[0]).Cast<Component2>().ToList();
            int sup = 0, nodoc = 0, fuera = 0, falta = 0;
            foreach (var c in comps)
            {
                if (c.IsSuppressed()) { sup++; continue; }
                string cp = c.GetPathName();
                if (!File.Exists(cp)) { falta++; problemas++; Console.WriteLine("  ARCHIVO FALTANTE " + c.Name2 + " -> " + cp); }
                else if (!Path.GetFullPath(cp).StartsWith(root, StringComparison.OrdinalIgnoreCase)) { fuera++; problemas++; Console.WriteLine("  FUERA DEL REPO   " + c.Name2 + " -> " + cp); }
                var d = (ModelDoc2)c.GetModelDoc2();
                if (d == null) { nodoc++; problemas++; Console.WriteLine("  SIN DOCUMENTO    " + c.Name2); continue; }
                Reportar(d);
            }
            Reportar(m);
            // relaciones: errores y advertencias de cada relación del nivel superior
            int rel_ = 0, relMal = 0;
            for (Feature f = (Feature)m.FirstFeature(); f != null; f = (Feature)f.GetNextFeature())
                if (f.GetTypeName2() == "MateGroup")
                    for (Feature sf = (Feature)f.GetFirstSubFeature(); sf != null; sf = (Feature)sf.GetNextSubFeature())
                    {
                        rel_++; bool w; int e = sf.GetErrorCode2(out w);
                        if (e != 0) { relMal++; problemas++; Console.WriteLine("  RELACION " + (w ? "advertencia " : "error ") + sf.Name + " " + (swFeatureError_e)e); }
                    }
            Console.WriteLine(string.Format("  componentes {0} (suprimidos {1}), relaciones del nivel superior {2} ({3} con problemas)", comps.Count, sup, rel_, relMal));
        }
        Console.WriteLine("documentos revisados " + vistos.Count + ", problemas " + problemas);
        // cerrar sin guardar lo que se abrió acá (sin ventana): primero se juntan los títulos, después se cierra
        var cerrar = new List<string>();
        foreach (ModelDoc2 d in ((object[])SW.App.GetDocuments() ?? new object[0]).Cast<ModelDoc2>().ToList())
        {
            try { string pp = d.GetPathName().ToLower(); if (!abiertosAntes.Contains(pp) && !d.Visible && pp.EndsWith(".sldasm")) cerrar.Add(d.GetTitle()); } catch { }
        }
        foreach (var t in cerrar) { try { SW.App.QuitDoc(t); } catch { } }
    }
}
