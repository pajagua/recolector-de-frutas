using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
// Agujeros156.exe <root> : agrega a la base del soporte estereo (CB-156) los 4 agujeros D3.2 del adaptador CB-154 y exporta el STL
class Agujeros156
{
    static void Main(string[] a)
    {
        string R = a[0];
        SW.Connect();
        var m = SW.Open(Path.Combine(R, "01_Base_J0", "CB-156_Soporte_Estereo_C270_Base.SLDPRT"));
        PB.Use(m);
        if (((PartDoc)m).FeatureByName("Agujeros_4xM3_adaptador") == null)
        {
            // misma posición que los agujeros del adaptador: x_h 50 y 65, y_h -6 y +15 (placa del centro, 2 mm)
            PB.Sketch('F'); foreach (double x in new[] { 50.0, 65.0 }) foreach (double y in new[] { -6.0, 15.0 }) PB.Circle(x, y, 3.2);
            PB.Extrude(-1, 5, true, "Agujeros_4xM3_adaptador");
            PB.Prop("Notas", "Remodelada con las cotas del STL del usuario (StereoVision_03) + 4 agujeros D3.2 para el adaptador CB-154 (4 x M3x12 + tuerca). " +
                "Para imprimir: cad/imprimir/CB-156_Soporte_Estereo_C270_Base.stl (exportado de este modelo).");
            int e = 0, w = 0; m.ForceRebuild3(false);
            Console.WriteLine("guardado " + m.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w));
        }
        PB.Report("CB-156");
        SW.App.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swSTLQuality, (int)swSTLQuality_e.swSTLQuality_Fine);
        SW.App.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swExportStlUnits, (int)swLengthUnit_e.swMM);
        SW.App.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swSTLBinaryFormat, true);
        string dst = Path.GetFullPath(Path.Combine(R, "..", "imprimir", "CB-156_Soporte_Estereo_C270_Base.stl"));
        int e2 = 0, w2 = 0;
        bool ok = m.Extension.SaveAs3(dst, 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent | (int)swSaveAsOptions_e.swSaveAsOptions_Copy, null, null, ref e2, ref w2);
        Console.WriteLine((ok ? "stl " : "STL FAIL ") + dst + " e=" + e2);
        SW.App.CloseDoc(m.GetTitle());
    }
}
