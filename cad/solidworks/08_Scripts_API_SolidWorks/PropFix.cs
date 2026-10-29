using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// PropFix.exe <root> : corrige la propiedad "Notas" de piezas v7 (fijaciones al ras en vez de M3 pasantes)
class PropFix
{
    static void Set(string root, string rel, string notas)
    {
        string p = Path.Combine(root, rel);
        var m = SW.Open(p);
        if (m == null) return;
        m.Extension.CustomPropertyManager[""].Add3("Notas", (int)swCustomInfoType_e.swCustomInfoText, notas, (int)swCustomPropertyAddOption_e.swCustomPropertyReplaceValue);
        int e = 0, w = 0;
        bool ok = m.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w);
        Console.WriteLine((ok ? "ok " : "FALLO ") + rel);
        SW.App.CloseDoc(m.GetTitle());
    }
    static void Main(string[] args)
    {
        string R = args[0];
        SW.Connect();
        const string A = "10_v7A_Prismatico_Correa\\", B = "11_v7B_Prismatico_Telescopico\\";
        Set(R, A + "CB-421A_Tubo_E3_Aluminio_30x30x2_L440.SLDPRT",
            "Coordenadas de E2, J3 retraido: x -425..15. Desliza en el canal 31 x 31 de E2. 2 agujeros D3.2 (eje y): pasadores de acero D3 x 30 al ras + epoxi para la brida CB-402 y el tapon CB-422A (una cabeza de tornillo rozaria el canal, que tiene 0.5 mm de juego).");
        Set(R, A + "CB-422A_Tapon_Trasero_Pinza_Correa.SLDPRT",
            "Coordenadas de E2, J3 retraido. Pegado con epoxi + pasador D3 al ras. Los dos extremos de la correa entran en la ranura de 1.8 mm con los dientes enfrentados y se aseguran con un precinto.");
        Set(R, B + "CB-412B_Tapon_Trasero_Intermedio_Polea.SLDPRT",
            "Coordenadas de E2, J3 retraido. Pegado en el tubo + pasador D3 x 30 al ras (el tubo desliza en el canal con 0.5 mm de juego). Eje de la polea: M3x25 + tuerca autoblocante.");
        Set(R, B + "CB-421B_Tubo_Interior_20x20x1.5_L263.SLDPRT",
            "Coordenadas de E2, J3 retraido: x -248..15. Agujeros laterales D3.2 (eje z) para pasadores D3 x 20 al ras: el tubo pasa por el buje delantero con 0.2 mm de juego, no admite cabezas de tornillo.");
        Set(R, B + "CB-422B_Buje_Trasero_Interior_Pinza_2a1.SLDPRT",
            "Coordenadas de E2, J3 retraido. Pasador D3 x 25 al ras + epoxi. La correa pasa por el canal de 9 mm y se fija al tubo con una placa dentada y 2 x M2x6 (no modelados).");
        Set(R, B + "CB-432B_Brida_E3_Herramienta_20x20.SLDPRT",
            "Igual que CB-402 (cara exterior a 105 mm del eje J2 con J3 retraido) pero con tapon de 16.8 para el tubo 20 x 20 x 1.5. Epoxi + pasador D3 x 20 al ras.");
        Set(R, B + "CB-413B_Soporte_Polea_Delantera.SLDPRT",
            "Coordenadas de E2, J3 retraido. 2 x M3x4 en la cara superior del tubo (roscados en la pared de 2 mm, al ras por dentro: por ahi pasa la correa) + epoxi. Eje M3x20 + tuerca autoblocante.");
        Set(R, B + "CB-415B_Pinza_Correa_Etapa1.SLDPRT",
            "Coordenadas de E2, J3 retraido. 2 x M3x5 en la cara superior del tubo (roscados, al ras por dentro) + epoxi. Extremos de la correa en la ranura de 1.8 mm + precinto.");
    }
}
