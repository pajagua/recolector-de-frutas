using System;
using System.IO;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// ExportStl.exe <root solidworks> <carpeta\pieza> ... : exporta cada pieza a cad\imprimir\<carpeta>\<pieza>.stl (mm, calidad fina;
// 09_Herramienta va a 00_Herramienta para que quede primera)
// e informa la caja y si entra en la Ender 3 V2 (220 x 220 x 250) en alguna orientación a 90°.
class ExportStl
{
    static void Main(string[] a)
    {
        string R = a[0];
        SW.Connect(); SW.CloseMine();
        SW.App.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swSTLQuality, (int)swSTLQuality_e.swSTLQuality_Fine);
        SW.App.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swExportStlUnits, (int)swLengthUnit_e.swMM);
        var cama = new[] { 220.0, 220.0, 250.0 };
        foreach (var rel in a.Skip(1))
        {
            string src = Path.Combine(R, rel + ".SLDPRT");
            string sub = Path.GetDirectoryName(rel); if (sub == "09_Herramienta") sub = "00_Herramienta";   // la herramienta va primero en cad/imprimir
            string dir = Path.GetFullPath(Path.Combine(R, "..", "imprimir", sub));
            Directory.CreateDirectory(dir);
            string dst = Path.Combine(dir, Path.GetFileName(rel) + ".stl");
            var m = SW.Open(src); if (m == null) { Console.WriteLine("NO ABRE " + src); continue; }
            PB.Use(m);
            var b = PB.BodyBox();
            var dims = new[] { (b[3] - b[0]) * 1000, (b[4] - b[1]) * 1000, (b[5] - b[2]) * 1000 };
            var s = dims.OrderBy(v => v).ToArray();
            bool entra = s[0] <= cama[0] && s[1] <= cama[1] && s[2] <= cama[2];
            int e = 0, w = 0;
            string tmp = dst + ".tmp.stl";
            bool ok = m.Extension.SaveAs3(tmp, 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent | (int)swSaveAsOptions_e.swSaveAsOptions_Copy, null, null, ref e, ref w);
            ok = ok && File.Exists(tmp);   // SolidWorks a veces devuelve true sin escribir el archivo
            if (ok) { if (File.Exists(dst)) File.Delete(dst); File.Move(tmp, dst); }   // nombre en minúsculas (.stl)
            Console.WriteLine(string.Format("{0} {1}  {2:0} x {3:0} x {4:0}  cuerpos={5}  {6}", ok ? "stl" : "FALLO", rel, dims[0], dims[1], dims[2], PB.BodyCount(), entra ? "entra" : "NO ENTRA en la Ender 3"));
            SW.App.CloseDoc(m.GetTitle());
        }
    }
}
