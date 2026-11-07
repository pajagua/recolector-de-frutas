using System;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// CajaVentana.exe <root> [info|aplicar] (v7.4, 2026-10-30)
// Ventana en la pared de la caja de torsión CB-603, frente a la ranura del porta-servo CB-602, para sacar el cable del
// MG996R de torsión por el costado y lejos de la correa y las poleas (que van arriba de la tapa).
class CajaVentana
{
    // coordenadas propias de CB-603: ranura del porta-servo x 750.11..756.71 (centro 753.41); pared y 325..329; piso z 0..4
    const double XC = 753.41, ANCHO = 10, Z0 = 9.5, Z1 = 18.5, Y_IN = 325, Y_OUT = 329;

    static void Main(string[] a)
    {
        string R = a[0];
        SW.Connect(); SW.CloseMine();
        // ¿qué hay afuera de la ventana? caras de los otros componentes que cortan la línea que sale por el centro de la ventana
        var m = SW.Open(System.IO.Path.Combine(R, "09_Herramienta", "CB-600_Herramienta.SLDASM"));
        var asm = (AssemblyDoc)m; asm.ResolveAllLightWeightComponents(false);
        var comps = ((object[])asm.GetComponents(true)).Cast<Component2>().ToList();
        var x3 = Xf.FromComp(comps.First(c => c.Name2.StartsWith("CB-603")));
        var p0 = x3.Apply(new[] { XC, Y_OUT, (Z0 + Z1) / 2 }); var p1 = x3.Apply(new[] { XC, Y_OUT + 1, (Z0 + Z1) / 2 });
        var dir = new[] { p1[0] - p0[0], p1[1] - p0[1], p1[2] - p0[2] };
        Console.WriteLine(string.Format("ventana (mundo): centro {0:0.0}, {1:0.0}, {2:0.0}  hacia {3:0.00}, {4:0.00}, {5:0.00}", p0[0], p0[1], p0[2], dir[0], dir[1], dir[2]));
        foreach (var c in comps)
        {
            if (c.Name2.StartsWith("CB-603")) continue;
            var b = (double[])c.GetBox(false, false);
            // muestreo a lo largo del rayo hasta 80 mm
            for (double t = 0; t <= 80; t += 0.5)
            {
                double x = p0[0] + dir[0] * t, y = p0[1] + dir[1] * t, z = p0[2] + dir[2] * t;
                bool dentro = x * 1e-3 >= b[0] && x * 1e-3 <= b[3] && y * 1e-3 >= b[1] && y * 1e-3 <= b[4] && z * 1e-3 >= b[2] && z * 1e-3 <= b[5];
                if (dentro) { Console.WriteLine(string.Format("  caja de {0} a {1:0.0} mm de la pared", c.Name2, t)); break; }
            }
        }
        SW.App.CloseDoc(m.GetTitle());
        if (a.Length < 2 || a[1] != "aplicar") return;

        var p = SW.Open(System.IO.Path.Combine(R, "09_Herramienta", "CB-603_Caja_Torsion.SLDPRT")); PB.Use(p);
        if (((PartDoc)p).FeatureByName("CB_v74_Ventana_cable_servo") != null) { Console.WriteLine("CB-603 ya modificado"); SW.App.CloseDoc(p.GetTitle()); return; }
        PB.Report("CB-603 antes");
        PB.Sketch('T'); PB.Rect(XC - ANCHO / 2, Z0, XC + ANCHO / 2, Z1); PB.Extrude(Y_IN - 0.5, Y_OUT - Y_IN + 1, true, "CB_v74_Ventana_cable_servo");
        PB.Report("CB-603 despues");
        PB.Prop("Notas", "v7.4: ventana de 10 x 9 en la pared, frente a la ranura del porta-servo CB-602, para sacar el cable del servo de torsion por el costado " +
            "(lejos de la correa). Pasa el conector del servo.");
        int e = 0, w = 0; p.ForceRebuild3(false);
        Console.WriteLine("CB-603 guardado " + p.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w));
        SW.App.CloseDoc(p.GetTitle());
    }
}
