using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
// Separador.exe <root> (v7.11): el separador M8 x 20 del eje central de J1 era Ø12 y el agujero del plato 1M1B es Ø11.
// Pasa a Ø10: tubo de aluminio 10 x 8 (pared 1) cortado a 20 mm; apoya en el anillo interior de los 608ZZ.
// Se cambia la cota D1@Croquis1 (12 -> 10) y no se corta: así la cara exterior sigue siendo la misma y no se rompen las
// relaciones concéntricas que la usan (en CB-120 se rompía con un corte). Borra el corte CB_v711_Diametro_10 si existe.
class Separador
{
    static void Main(string[] a)
    {
        SW.Connect();
        string p = Path.Combine(a[0], "05_Comerciales", "Tornilleria", "HW_Separador_M8x20.SLDPRT");
        bool ya = SW.App.GetOpenDocumentByName(p) != null; var m = SW.Open(p); int er = 0; SW.App.ActivateDoc3(m.GetTitle(), false, 0, ref er); PB.Use(m);
        PB.Report("antes");
        var corte = ((PartDoc)m).FeatureByName("CB_v711_Diametro_10") as Feature;
        if (corte != null)
        {
            m.ClearSelection2(true); corte.Select2(false, 0);
            bool ok = m.Extension.DeleteSelection2((int)swDeleteSelectionOptions_e.swDelete_Absorbed);
            Console.WriteLine("corte borrado " + ok);
        }
        var d = (Dimension)m.Parameter("D1@Croquis1");
        Console.WriteLine("D1@Croquis1 = " + (d.SystemValue * 1000).ToString("0.###") + " -> 10");
        d.SetSystemValue3(0.010, (int)swSetValueInConfiguration_e.swSetValue_InAllConfigurations, null);
        m.ForceRebuild3(false);
        PB.Prop("Descripcion", "Separador M8 x 20: tubo de aluminio 10 x 8 (pared 1) cortado a 20 mm (el agujero del plato 1M1B es Ø11)");
        PB.Report("despues");
        int e = 0, w = 0; Console.WriteLine("guardado " + m.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w));
        if (!ya) SW.App.CloseDoc(m.GetTitle());
    }
}
