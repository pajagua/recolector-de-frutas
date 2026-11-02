using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Horn21.exe <root solidworks> (v7.7, 2026-10-31)
// Adapta la herramienta al horn de plástico medido del MG996R y al servo medido:
//   horn: disco Ø21 x 2.5, alto total 6, resalte Ø8.5 x 1 en la cara plana, 12 agujeros pasantes en Ø14.5.
//   servo: agujeros de las pestañas 9 x 49.
// CB-605 polea : asiento Ø21.4 de 4 mm (la cara plana queda donde estaba el disco metálico: la polea no cambia de altura),
//                alivio del resalte y 4 pilotos Ø2 para los tornillos del kit, desde abajo.
// CB-603 caja  : rellena el asiento viejo y agrega un suplemento de 2 mm (el cubo del horn de plástico es 2 mm más corto
//                que el del metálico), con el mismo asiento, alivio y pilotos. Se atornilla desde afuera.
// CB-601 horquilla: agujeros de las pestañas pasantes Ø3.4 en 9 x 49 y tuercas M3 atrapadas por dentro del brazo.
class Horn21
{
    const double SEAT_D = 21.4, ALIVIO_D = 9.7, ALIVIO_H = 1.4, PCD = 14.5, PILOTO = 2.0, CENTRO = 7;
    const double ANG0 = 26.6;   // entre los agujeros viejos del horn metálico (a 4.6° + k·45°)

    static void Main(string[] a)
    {
        string R = Path.Combine(a[0], "09_Herramienta");
        SW.Connect();
        // documentos sin guardar de una corrida anterior que falló: cerrarlos sin guardar
        foreach (var t in new[] { "CB-605_Polea_Servo_GT2.SLDPRT", "CB-603_Caja_Torsion.SLDPRT", "CB-601_Horquilla_U.SLDPRT" })
            if (SW.App.GetOpenDocumentByName(Path.Combine(R, t)) != null) SW.App.CloseDoc(t);
        SW.CloseMine();
        Polea(Path.Combine(R, "CB-605_Polea_Servo_GT2.SLDPRT"));
        Caja(Path.Combine(R, "CB-603_Caja_Torsion.SLDPRT"));
        Horquilla(Path.Combine(R, "CB-601_Horquilla_U.SLDPRT"));
    }

    static bool Abrir(string path, string marca)
    {
        var p = SW.Open(path); if (p == null) return false; PB.Use(p);
        if (((PartDoc)p).FeatureByName(marca) != null) { Console.WriteLine(Path.GetFileName(path) + " ya modificado"); SW.App.CloseDoc(p.GetTitle()); return false; }
        PB.Report(Path.GetFileName(path) + " antes");
        return true;
    }

    static void Guardar(string notas)
    {
        var p = PB.M; PB.Report("despues");
        PB.Prop("Notas_v75", notas);
        int e = 0, w = 0; p.ForceRebuild3(false);
        Console.WriteLine("guardado " + p.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w));
        SW.App.CloseDoc(p.GetTitle());
    }

    // 4 pilotos en el plano activo, centro (ca, cb)
    static void Pilotos(double ca, double cb)
    {
        for (int k = 0; k < 4; k++)
        {
            double t = (ANG0 + 90 * k) * Math.PI / 180;
            PB.Circle(ca + PCD / 2 * Math.Cos(t), cb + PCD / 2 * Math.Sin(t), PILOTO);
        }
    }

    static void Polea(string path)
    {
        if (!Abrir(path, "CB_v75_Asiento_horn21")) return;
        double cx = 0, cy = -22.25, top = 11.6, fondo = top - 4;   // cara del horn arriba (z = 11.6)
        PB.Sketch('F'); PB.Circle(cx, cy, SEAT_D); PB.Extrude(fondo, 5, true, "CB_v75_Asiento_horn21");
        PB.Sketch('F'); PB.Circle(cx, cy, ALIVIO_D); PB.Extrude(fondo - ALIVIO_H, ALIVIO_H + 0.1, true, "CB_v75_Alivio_resalte");
        PB.Sketch('F'); Pilotos(cx, cy); PB.Extrude(-1, top + 2, true, "CB_v75_Pilotos_horn");
        Guardar("v7.7: asiento para el horn de plastico del MG996R (disco 21 x 2.5, resalte 8.5 x 1, agujeros en 14.5). " +
            "4 tornillos del kit desde abajo por el horn. Los agujeros de 3 del horn metalico quedan sin uso.");
    }

    static void Caja(string path)
    {
        if (!Abrir(path, "CB_v75_Suplemento_horn")) return;
        double cy = 296.1, cz = 20.7, pared = 782.898, viejo = pared - 1.5, sup = 2.0;
        double cara = pared + sup - 1.5;   // fondo del asiento nuevo = cara plana del horn (2 mm más afuera que el disco metálico)
        // 'R' = normal X, coordenadas (z, y)
        PB.Sketch('R'); PB.Circle(cz, cy, 25); PB.Extrude(viejo, pared + sup - viejo, false, "CB_v75_Suplemento_horn");
        PB.Sketch('R'); PB.Circle(cz, cy, SEAT_D); PB.Extrude(cara, 3, true, "CB_v75_Asiento_horn21");
        PB.Sketch('R'); PB.Circle(cz, cy, ALIVIO_D); PB.Extrude(cara - ALIVIO_H, ALIVIO_H + 0.1, true, "CB_v75_Alivio_resalte");
        PB.Sketch('R'); PB.Circle(cz, cy, CENTRO); PB.Extrude(pared - 12, 20, true, "CB_v75_Paso_central");
        PB.Sketch('R'); Pilotos(cz, cy); PB.Extrude(pared - 12, 20, true, "CB_v75_Pilotos_horn");
        Guardar("v7.7: suplemento de 2 mm y asiento para el horn de plastico del MG996R (disco 21 x 2.5, resalte 8.5 x 1). " +
            "4 tornillos del kit desde afuera, por el horn; las puntas salen adentro de la caja.");
    }

    static void Horquilla(string path)
    {
        if (!Abrir(path, "CB_v75_Pestanas_9x49")) return;
        double cy = 40, cz = 57.6, xin = -19, xout = -12, af = 5.7, prof = 2.6;
        double r = af / Math.Sqrt(3);   // radio del hexágono (a los vértices)
        PB.Sketch('R');
        foreach (double sy in new[] { -1.0, 1.0 }) foreach (double sz in new[] { -1.0, 1.0 })
            PB.Circle(cz + sz * 24.5, cy + sy * 4.5, 3.4);
        PB.Extrude(xin - 1, xout - xin + 2, true, "CB_v75_Pestanas_9x49");
        // Tuercas: caras planas hacia ±z (los vértices hacia ±y), por la cara interior del brazo
        PB.Sketch('R');
        foreach (double sy in new[] { -1.0, 1.0 }) foreach (double sz in new[] { -1.0, 1.0 })
        {
            double za = cz + sz * 24.5, ya = cy + sy * 4.5;
            var pts = new double[12];
            for (int k = 0; k < 6; k++) { double t = (60 * k) * Math.PI / 180; pts[2 * k] = za + r * Math.Sin(t); pts[2 * k + 1] = ya + r * Math.Cos(t); }
            PB.Poly(pts);
        }
        PB.Extrude(xin - 1, prof + 1, true, "CB_v75_Tuercas_M3");
        Guardar("v7.7: agujeros de las pestanas del servo de muneca medidos (9 x 49), pasantes 3.4, con tuerca M3 atrapada " +
            "en la cara interior del brazo. Tornillo M3 x 12 Allen desde afuera, por la pestana.");
    }
}
