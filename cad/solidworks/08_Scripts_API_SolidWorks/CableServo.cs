using System;
using System.IO;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// CableServo.exe <root> [info|aplicar] (v7.3, 2026-10-29)
// El MG996R de la torsión (CB-611) saca el cable por la cara del extremo, 7-10 mm sobre su base, y esa pared del porta-servo
// CB-602 está cerrada: el pasacables se clava 5.7 mm en la pared y el servo no entra. Se abre una ranura en esa pared
// (de arriba hasta debajo del pasacables, todo el espesor), se corren 0.6 mm hacia afuera los dos agujeros de las orejas de
// ese lado para que no queden pegados a la ranura, y se abre una ventana en la pared de la caja CB-603 para que salga el cable.
class CableServo
{
    static string R;
    static string P(string f) { return Path.Combine(R, "09_Herramienta", f); }
    // porta-servo, coordenadas propias: bolsillo x 109.4..130.0, y -273.3..-232.5; pared del cable y -232.5..-224.8; alto 0..30.7
    const double XC = 119.7, RAN_W = 6.6, RAN_Z0 = 5.5, PARED_IN = -232.5, PARED_OUT = -224.8, TOP = 30.7;
    const double AG_Y = -228.15, AG_X1 = 114.7, AG_X2 = 124.7, AG_D = 2.6, AG_Z0 = 18.7, CORRE = 0.6;
    const double VEN_Z1 = 14.0;   // la ventana de la caja va desde el fondo de la ranura hasta acá (coordenadas del porta-servo)

    static void Main(string[] a)
    {
        R = a[0];
        SW.Connect(); SW.CloseMine();
        // transformaciones de CB-602 y CB-603 en CB-600
        var m = SW.Open(P("CB-600_Herramienta.SLDASM"));
        var asm = (AssemblyDoc)m; asm.ResolveAllLightWeightComponents(false);
        var comps = ((object[])asm.GetComponents(true)).Cast<Component2>().ToList();
        var x602 = Xf.FromComp(comps.First(c => c.Name2.StartsWith("CB-602")));
        var x603 = Xf.FromComp(comps.First(c => c.Name2.StartsWith("CB-603")));
        SW.App.CloseDoc(m.GetTitle());
        // ventana en coordenadas de CB-603: esquinas de la ranura (a la altura del cable) llevadas al mundo y de ahí a CB-603
        var a2b = x602.Then(x603.Inv());
        double x0 = XC - RAN_W / 2, x1 = XC + RAN_W / 2;
        var pts = new[] { new[] { x0, PARED_OUT, RAN_Z0 }, new[] { x1, PARED_OUT, RAN_Z0 }, new[] { x0, PARED_OUT, VEN_Z1 }, new[] { x1, PARED_OUT, VEN_Z1 } }.Select(p => a2b.Apply(p)).ToArray();
        foreach (var p in pts) Console.WriteLine("ventana en CB-603: " + string.Join(", ", p.Select(v => v.ToString("0.00"))));
        double vx0 = pts.Min(p => p[0]), vx1 = pts.Max(p => p[0]), vz0 = pts.Min(p => p[2]), vz1 = pts.Max(p => p[2]), vy = pts.Average(p => p[1]);
        Console.WriteLine(string.Format("CB-603: ventana x {0:0.00}..{1:0.00}, z {2:0.00}..{3:0.00}, cara de afuera del porta-servo en y = {4:0.00}", vx0, vx1, vz0, vz1, vy));
        if (a.Length < 2 || a[1] != "aplicar") return;

        // --- CB-602: ranura y agujeros corridos ---
        var p2 = SW.Open(P("CB-602_Porta_Servo.SLDPRT")); PB.Use(p2);
        if (((PartDoc)p2).FeatureByName("CB_v73_Ranura_cable") != null) { Console.WriteLine("CB-602 ya modificado"); SW.App.CloseDoc(p2.GetTitle()); return; }
        PB.Report("CB-602 antes");
        PB.Sketch('F'); PB.Circle(AG_X1, AG_Y, AG_D + 0.1); PB.Circle(AG_X2, AG_Y, AG_D + 0.1); PB.Extrude(AG_Z0 - 0.1, TOP - AG_Z0 + 0.1, false, "CB_v73_Relleno_agujeros_orejas");
        PB.Sketch('T'); PB.Rect(x0, RAN_Z0, x1, TOP + 1); PB.Extrude(PARED_IN - 0.5, PARED_OUT - PARED_IN + 1, true, "CB_v73_Ranura_cable");
        PB.Sketch('F'); PB.Circle(AG_X1 - CORRE, AG_Y, AG_D); PB.Circle(AG_X2 + CORRE, AG_Y, AG_D); PB.Extrude(AG_Z0, TOP - AG_Z0 + 0.5, true, "CB_v73_Agujeros_orejas_corridos");
        PB.Report("CB-602 despues");
        PB.Prop("Notas", "v7.3: ranura de 6.6 en la pared del lado del cable (de arriba hasta 5.5 mm del fondo, todo el espesor) para que pase el pasacables del MG996R; " +
            "los dos agujeros de las orejas de ese lado se corrieron 0.6 mm hacia afuera (siguen entrando en las ranuras de las orejas). El tornillo de abajo de ese lado agarra 5.5 mm.");
        int e = 0, w = 0; p2.ForceRebuild3(false);
        Console.WriteLine("CB-602 guardado " + p2.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w));
        SW.App.CloseDoc(p2.GetTitle());
        // CB-603 no se toca: su pared ya tiene una muesca de 12 x 7 arriba (x 747.6..759.6, z 28.2..35.2 en sus coordenadas)
        // justo frente a la ranura; el cable sube por la ranura y sale por esa muesca, debajo de la tapa.
    }
}
