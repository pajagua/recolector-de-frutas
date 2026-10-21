using System;
using System.IO;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Mods.exe <root> [which]  : adds the documented modifications to the copied parts
class Mods
{
    static string R;
    static ModelDoc2 Open(string rel) { var m = SW.Open(Path.Combine(R, rel)); PB.Use(m); return m; }
    static void SaveClose(ModelDoc2 m)
    {
        int e = 0, w = 0;
        m.ForceRebuild3(false);
        bool ok = m.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w);
        Console.WriteLine((ok ? "saved " : "SAVE FAIL ") + m.GetPathName() + " e=" + e + " w=" + w);
        SW.CloseMine();
    }
    static bool Has(ModelDoc2 m, string feat) { return ((PartDoc)m).FeatureByName(feat) != null; }

    static void Main(string[] args)
    {
        R = args[0];
        SW.Connect();
        SW.CloseMine();
        var which = args.Skip(1).ToList();
        Func<string, bool> want = k => which.Count == 0 || which.Contains(k);

        if (want("torre"))
        {
            var m = Open(@"02_Cintura_J1\CB-210_Torre_Cintura_2M1D_mod.SLDPRT");
            if (Has(m, "CB_Avellanado_M6_D10x4") && !Has(m, "CB_Avellanado_M6_D11x6.5"))
            {
                // deeper/wider counterbore (contains the old D10x4) so the M6 heads sit below the face; E2 skirt passes 1 mm away
                double xa0 = 190.171, zc0 = -86.723;
                PB.Sketch('T'); PB.Circle(xa0 - 29.0, zc0 - 13.85, 11.0); PB.Circle(xa0 - 29.0, zc0 + 13.85, 11.0);
                PB.Extrude(181.2, 6.5, true, "CB_Avellanado_M6_D11x6.5");
                PB.Report("torre cbore nuevo");
            }
            if (!Has(m, "CB_Agujeros_M6_motor5840"))
            {
                double xa = 190.171, zc = -86.723;
                PB.Report("torre antes");
                PB.Sketch('T'); PB.Circle(xa - 29.0, zc - 13.85, 6.4); PB.Circle(xa - 29.0, zc + 13.85, 6.4);
                PB.Extrude(181.2, 12.0, true, "CB_Agujeros_M6_motor5840");
                PB.Sketch('T'); PB.Circle(xa - 29.0, zc - 13.85, 11.0); PB.Circle(xa - 29.0, zc + 13.85, 11.0);
                PB.Extrude(181.2, 6.5, true, "CB_Avellanado_M6_D11x6.5");
                PB.Sketch('T'); PB.Circle(82, zc - 9, 4.4); PB.Circle(82, zc + 9, 4.4);
                PB.Extrude(181.2, 12.0, true, "CB_Agujeros_M4_soporte_AS5600");
                PB.Report("torre despues");
            }
            PB.Prop("Descripcion", "Torre/eslabon de cintura (2M1D modificado por el usuario a partir de E1 del Rx-rcp-003)");
            PB.Prop("Origen", "Importado de 2M1D.STEP (Fusion 360, cuerpo 'E1 v4')");
            PB.Prop("Modificaciones_CB", "+2 agujeros M6 (D6.4 + avellanado D11x6.5 para cabeza Allen) para el 5840 (par del lado motor, a 29 mm bajo el eje J2); " +
                "+2 agujeros M4 (D4.4) para el soporte del AS5600. El par superior del 5840 cae dentro del alojamiento D44 y no se usa.");
            PB.Prop("Material", "PETG impreso");
            SaveClose(m);
        }
        if (want("e2"))
        {
            var m = Open(@"03_Hombro_E2_J2\CB-301_Eslabon_E2_mod5840.SLDPRT");
            if (!Has(m, "CB_Recorte_cubo_4.5mm"))
            {
                PB.Report("E2 antes");
                PB.Sketch('F'); PB.Circle(-80, 0, 20); PB.Extrude(-56, 5.5, true, "CB_Recorte_cubo_4.5mm");
                PB.Report("E2 despues");
            }
            PB.Prop("Descripcion", "Eslabon E2 (brazo del hombro) del Rx-rcp-003, adaptado al 5840-36ZY");
            PB.Prop("Modificaciones_CB", "Cubo del lado de la torre recortado 4.5 mm (z -55 -> -50.5) para no chocar con el buje D18.6 del 5840. " +
                "El eje D8 se acopla con el buje CB-305 dentro del alojamiento D12.4.");
            PB.Prop("Material", "PETG impreso");
            SaveClose(m);
        }
        if (want("te2"))
        {
            var m = Open(@"03_Hombro_E2_J2\CB-303_Tapa_E2_con_Iman_TE2.SLDPRT");
            if (!Has(m, "CB_Alojamiento_iman_D6.2x2.6"))
            {
                PB.Report("TE2 antes");
                PB.Sketch('F'); PB.Circle(0, 0, 6.2); PB.Extrude(4.5, -2.6, true, "CB_Alojamiento_iman_D6.2x2.6");
                PB.Report("TE2 despues");
            }
            PB.Prop("Descripcion", "Tapa TE2 de E2 (Rx-rcp-003) con alojamiento para el iman diametral del AS5600");
            PB.Prop("Modificaciones_CB", "Bolsillo D6.2 x 2.6 en el centro de la cara exterior (eje J2) para iman D6x2.5, pegar con cianoacrilato.");
            SaveClose(m);
        }
        if (want("polea"))
        {
            var m = Open(@"05_Comerciales\HW_Polea_T5_10T_eje6.SLDPRT");
            if (!Has(m, "CB_Agujero_D6"))
            {
                PB.Report("polea antes");
                PB.Sketch('R'); PB.Circle(0, 0, 14); PB.Extrude(0, 6.5, false, "CB_Cubo_D14");
                PB.Sketch('R'); PB.Circle(0, 0, 6.0); PB.Extrude(-1, 28.3, true, "CB_Agujero_D6");
                PB.Report("polea despues");
            }
            PB.Prop("Descripcion", "Polea T5 10 dientes, ancho 17 mm, agujero 6 mm (para eje D6 del 37D) - BOM BCN3D #22 con otro agujero");
            SaveClose(m);
        }
        if (want("buje"))
        {
            var m = Open(@"03_Hombro_E2_J2\CB-305_Buje_D8_Hombro.SLDPRT");
            if (!Has(m, "Entrada_redonda_D8.1x1.5"))
            {
                PB.Report("buje antes");
                PB.Sketch('F'); PB.Circle(-80, 0, 8.1); PB.Extrude(-50.5, 1.5, true, "Entrada_redonda_D8.1x1.5");
                PB.Report("buje despues");
            }
            SaveClose(m);
        }
        if (want("e2v2"))
        {
            var m = Open(@"03_Hombro_E2_J2\CB-301_Eslabon_E2_mod5840.SLDPRT");
            if (!Has(m, "CB_Alojamiento_motor_D51"))
            {
                PB.Report("E2 v2 antes");
                // NEMA 14 (35.5 x 35.3) inside the motor bore: D50 -> D51
                PB.Sketch('R'); PB.Circle(0, 0, 51); PB.Extrude(-135.5, 95.5, true, "CB_Alojamiento_motor_D51");
                // boss of the NEMA 14 (D22 x 2.7) in the wall between bore and channel
                PB.Sketch('R'); PB.Circle(0, 0, 23); PB.Extrude(-40, 3, true, "CB_Rebaje_buje_NEMA14_D23x3");
                // 4 x M3 for the NEMA 14 face (25.7 square), counterbore from the channel side
                PB.Sketch('R'); foreach (double a in new[] { -12.85, 12.85 }) foreach (double b in new[] { -12.85, 12.85 }) PB.Circle(a, b, 3.4);
                PB.Extrude(-40.5, 7, true, "CB_Agujeros_4xM3_NEMA14");
                PB.Sketch('R'); foreach (double a in new[] { -12.85, 12.85 }) foreach (double b in new[] { -12.85, 12.85 }) PB.Circle(a, b, 6.0);
                PB.Extrude(-34, -3.2, true, "CB_Avellanado_M3_D6x3.2");
                // pilot holes for the 3 screws of the AS5600 cap (r = 24.5 around the J2 axis)
                PB.Sketch('F'); foreach (double a in new[] { 90.0, 210.0, 330.0 }) PB.Circle(-80 + 24.5 * Math.Cos(a * Math.PI / 180), 24.5 * Math.Sin(a * Math.PI / 180), 2.5);
                PB.Extrude(35, -8, true, "CB_Pilotos_3xM3_tapa_AS5600");
                PB.Report("E2 v2 despues");
            }
            PB.Prop("Modificaciones_CB", "Cubo del lado de la torre recortado 4.5 mm (5840); alojamiento del motor D50 -> D51 para NEMA 14; rebaje D23x3 para el buje del NEMA; " +
                "4 x M3 (25.7 x 25.7) avellanados D6x3.2 desde el canal; 3 pilotos D2.5x8 para la tapa-soporte del AS5600.");
            SaveClose(m);
        }
        if (want("e2v2b"))
        {
            var m = Open(@"03_Hombro_E2_J2\CB-301_Eslabon_E2_mod5840.SLDPRT");
            if (!Has(m, "CB_Rebaje_buje_NEMA14_extra0.6"))
            {
                PB.Report("E2 antes");
                PB.Sketch('R'); PB.Circle(0, 0, 23); PB.Extrude(-37, 0.6, true, "CB_Rebaje_buje_NEMA14_extra0.6");
                PB.Report("E2 despues");
            }
            SaveClose(m);
        }
        if (want("tablero2"))
        {
            var m = Open(@"01_Base_J0\CB-111_Tablero_Madera_550x550x16.SLDPRT");
            if (!Has(m, "CB_Agujeros_bandeja_electronica_4xD4.5"))
            {
                PB.Report("tablero antes");
                // tray holes (-98,+-30), (60,+-40) in the tray frame -> board coords (x+280.802, z-494.5)
                PB.Sketch('T'); PB.Circle(182.802, -524.5, 4.5); PB.Circle(182.802, -464.5, 4.5); PB.Circle(340.802, -534.5, 4.5); PB.Circle(340.802, -454.5, 4.5);
                PB.Extrude(-1, 18, true, "CB_Agujeros_bandeja_electronica_4xD4.5");
                PB.Report("tablero despues");
            }
            PB.Prop("Modificaciones_CB", "Agujero pasante D42 bajo el motor 37D; 4 agujeros D4.5 para la bandeja de electronica CB-141.");
            SaveClose(m);
        }
        if (want("tsf"))
        {
            var m = Open(@"03_Hombro_E2_J2\CB-302_Tornillo_SinFin_E3_TSF.SLDPRT");
            if (!Has(m, "CB_Agujero_eje_NEMA14_D5"))
            {
                PB.Report("TSF antes");
                PB.Sketch('F'); PB.Circle(0, 0, 5.0); PB.Extrude(28.5, -20.7, true, "CB_Agujero_eje_NEMA14_D5");
                PB.Report("TSF despues");
            }
            PB.Prop("Modificaciones_CB", "Agujero D5 x 20 desde el extremo del motor para el eje del NEMA 14 (antes D4.25 en D para el motor ME3). Fijar con prisionero M3 o epoxi.");
            SaveClose(m);
        }
        if (want("as5600"))
        {
            var m = Open(@"05_Comerciales\Electronica\CB-503_Modulo_AS5600.SLDPRT");
            if (!Has(m, "CB_Quitar_iman_del_modelo"))
            {
                PB.Report("AS5600 antes");
                m.ClearSelection2(true);
                int n = 0;
                foreach (Body2 b in (object[])((PartDoc)m).GetBodies2((int)swBodyType_e.swSolidBody, true))
                    if (b.Name.Contains("Magnet")) { b.Select2(true, null); n++; }
                var f = (Feature)m.FeatureManager.InsertDeleteBody2(false);
                Console.WriteLine("cuerpos de iman seleccionados " + n + " feature " + (f != null));
                if (f != null) f.Name = "CB_Quitar_iman_del_modelo";
                m.ClearSelection2(true);
                PB.Report("AS5600 despues");
            }
            PB.Prop("Descripcion", "Modulo encoder magnetico AS5600 (modelo STEP del usuario, carpeta Cambios)");
            PB.Prop("Modificaciones_CB", "Se quito el cuerpo del iman que traia el modelo: el iman va en el soporte fijo CB-232.");
            SaveClose(m);
        }
        if (want("tablero"))
        {
            var m = Open(@"01_Base_J0\CB-111_Tablero_Madera_550x550x16.SLDPRT");
            if (!Has(m, "CB_Agujero_motor_J1_D42"))
            {
                PB.Report("tablero antes");
                PB.Sketch('T'); PB.Circle(134.585 - 7.0, -355.235, 42); PB.Extrude(-1, 18, true, "CB_Agujero_motor_J1_D42");
                PB.Report("tablero despues");
            }
            PB.Prop("Descripcion", "Tablero de madera 550x550x16 del BCN3D Moveo");
            PB.Prop("Modificaciones_CB", "Agujero pasante D42 bajo el motor 37D (centro de la caja reductora, 7 mm hacia afuera del eje de la polea).");
            SaveClose(m);
        }
    }
}
