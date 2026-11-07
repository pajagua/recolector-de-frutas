using System;
using System.IO;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// ExportStl.exe <root solidworks> <carpeta\pieza> ... : exporta cada pieza a cad\imprimir\<carpeta>\<pieza>.stl (mm, calidad fina;
// 09_Herramienta va a 00_Herramienta para que quede primera)
// e informa la caja y si entra en la Ender 3 V2 (220 x 220 x 250) en alguna orientación a 90°.
// v7.11: activa la pieza antes de exportar (con otra pieza activa, SolidWorks exportó el plato CB-201 en el STL de CB-121) y
// compara el volumen del STL con el de la pieza: si difieren más de 1 % no reemplaza el STL. Con la variable STL_OUT=<carpeta>
// escribe ahí en lugar de cad\imprimir (para comparar sin tocar el repo).
class ExportStl
{
    static double VolumenStl(string path)
    {
        var d = File.ReadAllBytes(path);
        double v = 0;
        if (d.Length > 84 && !(System.Text.Encoding.ASCII.GetString(d, 0, 5).ToLower() == "solid" && System.Text.Encoding.ASCII.GetString(d, 0, Math.Min(400, d.Length)).Contains("facet")))
        {
            int n = BitConverter.ToInt32(d, 80);
            for (int i = 0; i < n; i++)
            {
                int o = 84 + i * 50 + 12; var p = new double[9];
                for (int k = 0; k < 9; k++) p[k] = BitConverter.ToSingle(d, o + 4 * k);
                v += (p[0] * (p[4] * p[8] - p[5] * p[7]) - p[1] * (p[3] * p[8] - p[5] * p[6]) + p[2] * (p[3] * p[7] - p[4] * p[6])) / 6;
            }
        }
        else
        {
            var vs = File.ReadAllLines(path).Select(l => l.Trim()).Where(l => l.StartsWith("vertex"))
                .Select(l => l.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Skip(1).Select(double.Parse).ToArray()).ToList();
            for (int i = 0; i + 2 < vs.Count; i += 3)
            {
                var a = vs[i]; var b = vs[i + 1]; var c = vs[i + 2];
                v += (a[0] * (b[1] * c[2] - b[2] * c[1]) - a[1] * (b[0] * c[2] - b[2] * c[0]) + a[2] * (b[0] * c[1] - b[1] * c[0])) / 6;
            }
        }
        return Math.Abs(v);
    }

    static void Main(string[] a)
    {
        string R = a[0];
        SW.Connect();
        SW.App.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swSTLQuality, (int)swSTLQuality_e.swSTLQuality_Fine);
        SW.App.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swExportStlUnits, (int)swLengthUnit_e.swMM);
        var cama = new[] { 220.0, 220.0, 250.0 };
        string salida = System.Environment.GetEnvironmentVariable("STL_OUT");
        foreach (var rel in a.Skip(1))
        {
            string src = Path.Combine(R, rel + ".SLDPRT");
            string sub = Path.GetDirectoryName(rel); if (sub == "09_Herramienta") sub = "00_Herramienta";   // la herramienta va primero en cad/imprimir
            string dir = salida ?? Path.GetFullPath(Path.Combine(R, "..", "imprimir", sub));
            Directory.CreateDirectory(dir);
            string dst = Path.Combine(dir, Path.GetFileName(rel) + ".stl");
            bool ya = SW.App.GetOpenDocumentByName(src) != null;
            var m = SW.Open(src); if (m == null) { Console.WriteLine("NO ABRE " + src); continue; }
            int er = 0; SW.App.ActivateDoc3(m.GetTitle(), false, 0, ref er);
            m.ForceRebuild3(false);
            PB.Use(m);
            var b = PB.BodyBox();
            var dims = new[] { (b[3] - b[0]) * 1000, (b[4] - b[1]) * 1000, (b[5] - b[2]) * 1000 };
            var s = dims.OrderBy(v => v).ToArray();
            bool entra = s[0] <= cama[0] && s[1] <= cama[1] && s[2] <= cama[2];
            int e = 0, w = 0;
            string tmp = dst + ".tmp.stl";
            bool ok = m.Extension.SaveAs3(tmp, 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent | (int)swSaveAsOptions_e.swSaveAsOptions_Copy, null, null, ref e, ref w);
            ok = ok && File.Exists(tmp);   // SolidWorks a veces devuelve true sin escribir el archivo
            string vol = "";
            if (ok)
            {
                double vp = PB.Volume(), vs = VolumenStl(tmp), dif = (vs - vp) / vp * 100;
                vol = string.Format("  vol pieza {0:0} / stl {1:0} ({2:+0.00;-0.00} %)", vp, vs, dif);
                if (Math.Abs(dif) > 1) { ok = false; vol += "  VOLUMEN NO COINCIDE: no se reemplaza"; File.Delete(tmp); }
            }
            if (ok) { if (File.Exists(dst)) File.Delete(dst); File.Move(tmp, dst); }   // nombre en minúsculas (.stl)
            Console.WriteLine(string.Format("{0} {1}  {2:0} x {3:0} x {4:0}  cuerpos={5}  {6}{7}", ok ? "stl" : "FALLO", rel, dims[0], dims[1], dims[2], PB.BodyCount(), entra ? "entra" : "NO ENTRA en la Ender 3", vol));
            if (!ya) SW.App.CloseDoc(m.GetTitle());
        }
    }
}
