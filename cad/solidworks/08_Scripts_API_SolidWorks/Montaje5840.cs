using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Montaje5840.exe <root solidworks> [torre|motor|tornillos|avellanado|asiento|buje|prueba ...]   (v7.13, 2026-11-12)
// Pasa el montaje del motor del hombro (5840) a las medidas del motor comprado, tomadas por el usuario
// (Desktop\CaraCajaReductora_5840.step, hecho en Fusion): caja 40 x 58, eje a 20 del borde superior,
// 4 resaltes D7 x 2 con M4: pares a +10.5 y -28.5 del eje, separados 27.0 (el inferior izquierdo es colisa a 45°),
// 2 resaltes lisos D7 en (±5.5, -15.5), buje frontal D18 x 4.
// Antes (plano del vendedor, Hombro5840.cs): pares a +11.05 y -29.0, separados 27.5, resaltes D7.6, buje D18.5 x 5.
//   torre     : CB-210, los 2 agujeros D4.5 del par de abajo pasan a (-28.5, ±13.5) del eje J2
//   motor     : CB-502, resaltes y agujeros M4 en las posiciones medidas (el buje no se toca: lleva la relación de J2)
//   tornillos : CB-200, los 2 × M4x10 sobre los agujeros nuevos
//   avellanado: CB-210, avellanados D11.5 centrados en los agujeros nuevos y pasantes por el anillo D56 (para que entre la cabeza del M4)
//   asiento   : CB-210, asiento D18.3 x 2 para el buje del motor en la cara donde apoya (centra el motor en el eje J2)
//   buje      : CB-502, buje frontal D18.5 -> D18 (medido), cambiando el radio del croquis (la cara conserva su ID: relación J2_eje_hombro)
//   prueba    : CB-211, pieza de prueba = trozo de la torre alrededor del motor (pared con los agujeros, avellanados, asiento y anillo), parada como la torre
class Montaje5840
{
    static string R;
    static string P(string sub, string file) { return Path.Combine(R, sub, file); }
    static double[] V(params double[] v) { return v; }
    const double XA = 190.171, ZC = -86.723;   // eje J2 en coordenadas de la torre CB-210 (eje a lo largo de y)
    const double YPARED = 193.2;               // cara interior de la pared (apoyo de los resaltes del motor)
    const double HX = 13.5, HY = 28.5;         // par de abajo (lado motor): ±13.5 a 28.5 mm del eje
    const double HXS = 13.5, HYS = 10.5;       // par de arriba (cae en el alojamiento D44 de E2: no se usa)

    static void Main(string[] a)
    {
        R = a[0];
        SW.Connect();
        var w = a.Skip(1).ToList();
        Func<string, bool> want = k => w.Count == 0 || w.Contains(k);
        if (want("torre")) Torre();
        if (want("motor")) Motor();
        if (want("tornillos")) Tornillos();
        if (want("avellanado")) Avellanado();
        if (want("asiento")) Asiento();
        if (want("buje")) Buje();
        if (want("prueba")) Prueba();
    }

    static ModelDoc2 Abrir(string path)
    {
        var m = SW.Open(path); int er = 0;
        SW.App.ActivateDoc3(m.GetTitle(), false, 0, ref er);
        return m;
    }
    static void Guardar(ModelDoc2 m)
    {
        int e = 0, w = 0; m.ForceRebuild3(false);
        bool ok = m.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w);
        Console.WriteLine((ok ? "guardado " : "NO GUARDADO ") + Path.GetFileName(m.GetPathName()) + " e=" + e + " w=" + w);
    }
    static void Borrar(ModelDoc2 m, string feat)
    {
        m.ClearSelection2(true);
        bool s = m.Extension.SelectByID2(feat, "BODYFEATURE", 0, 0, 0, false, 0, null, 0);
        if (s) m.Extension.DeleteSelection2((int)swDeleteSelectionOptions_e.swDelete_Absorbed);
        Console.WriteLine("  borrado " + feat + " " + s);
    }

    static void Torre()
    {
        var m = Abrir(P("02_Cintura_J1", "CB-210_Torre_Cintura_2M1D_mod.SLDPRT")); PB.Use(m);
        PB.Report("torre antes");
        Borrar(m, "CB_v6_Agujeros_M4_motor5840");
        Borrar(m, "CB_v713_Agujeros_M4_motor5840_medido");
        // mismo corte que en la v6 (y 181 .. 193.5), en las posiciones medidas
        PB.Sketch('T'); PB.Circle(XA - HY, ZC - HX, 4.5); PB.Circle(XA - HY, ZC + HX, 4.5);
        PB.Extrude(181.0, 12.5, true, "CB_v713_Agujeros_M4_motor5840_medido");
        PB.Report("torre despues");
        PB.Prop("Modificaciones_CB", "+2 agujeros D4.5 para M4 (par del lado motor del 5840, a 28.5 mm bajo el eje J2 y +-13.5, medidos en el motor comprado, v7.13; " +
            "hasta la v7.12 a 29 y +-13.75 segun el plano del vendedor) con avellanado D11 x 6.5 (cabeza Allen M4 hundida 2.5 mm); " +
            "+2 agujeros M4 (D4.4) para el soporte del iman. El par superior del 5840 cae dentro del alojamiento D44 y no se usa.");
        Guardar(m);
    }

    static void Motor()
    {
        var m = Abrir(P("05_Comerciales", "CB-502_Motor_Sinfin_5840-36ZY.SLDPRT")); PB.Use(m);
        PB.Report("5840 antes");
        foreach (var f in new[] { "Agujeros_4xM4_prof6", "Resaltes_apoyo_D7.6x2", "Resaltes_apoyo_D7x2", "Colisa_resalte_45", "Colisa_resalte_45_punta" }) Borrar(m, f);
        PB.Use(m);
        double[][] m4 = { V(-HXS, HYS), V(HXS, HYS), V(-HX, -HY), V(HX, -HY) };
        PB.Sketch('F'); foreach (var h in m4) PB.Circle(h[0], h[1], 7); PB.Circle(-5.5, -15.5, 7); PB.Circle(5.5, -15.5, 7);
        PB.Extrude(-2, 2, false, "Resaltes_apoyo_D7x2");
        // colisa del resalte inferior izquierdo: de (-13.5, -28.5) a (-9.26, -24.26), 7 de ancho (contornos separados: no se pueden superponer en un croquis)
        double c = 3.5 / Math.Sqrt(2);
        PB.Sketch('F'); PB.Poly(-HX - c, -HY + c, -HX + c, -HY - c, -9.26 + c, -24.26 - c, -9.26 - c, -24.26 + c);
        PB.Extrude(-2, 2, false, "Colisa_resalte_45");
        PB.Sketch('F'); PB.Circle(-9.26, -24.26, 7); PB.Extrude(-2, 2, false, "Colisa_resalte_45_punta");
        PB.Sketch('F'); foreach (var h in m4) PB.Circle(h[0], h[1], 3.3); PB.Extrude(0, -6, true, "Agujeros_4xM4_prof6");
        PB.Report("5840 despues");
        PB.Prop("Notas", "Caja 40x58x27, eje D8 con plano a 7 (L20 desde la caja, plano 13), buje D18.5 x 5 (medido: D18 x 4; no se cambia porque lleva la relacion de J2), " +
            "4 resaltes D7 x 2 con M4 prof. 6: pares a +10.5 y -28.5 del eje, separacion 27.0, el inferior izquierdo con colisa a 45 grados " +
            "(medidas del motor comprado, v7.13; antes +11.05 / -29 / 27.5 del plano del vendedor), 2 resaltes lisos D7 en (+-5.5, -15.5), " +
            "buje trasero D18.5 x 4.45, motor D36 x 62 corrido 2 mm. Origen en el plano de apoyo de los resaltes. Eje de salida modelado aparte (CB-506).");
        Guardar(m);
    }

    static void Tornillos()
    {
        var m = Abrir(P("02_Cintura_J1", "CB-200_Cintura_J1.SLDASM")); var a = (AssemblyDoc)m;
        a.ResolveAllLightWeightComponents(false);
        var comps = ((object[])a.GetComponents(true)).Cast<Component2>().ToArray();
        var torre = Xf.FromComp(comps.First(c => c.Name2.StartsWith("CB-210")));
        var down = Xf.Rows(V(1, 0, 0), V(0, -1, 0), V(0, 0, -1), V(0, 0, 0));   // cabeza hacia afuera, caña hacia el motor (como Hombro5840.cs)
        foreach (var c in comps.Where(c => c.Name2.StartsWith("HW_M4x10")))
        {
            var t = torre.Inv().Apply(Xf.FromComp(c).T);
            double s = t[2] < ZC ? -HX : HX;
            var nx = down.Then(Xf.Tr(XA - HY, 187.7, ZC + s)).Then(torre);
            m.ClearSelection2(true); c.Select4(false, null, false); a.UnfixComponent();
            c.Transform2 = nx.ToMath();
            m.ClearSelection2(true); c.Select4(false, null, false); a.FixComponent(); m.ClearSelection2(true);
            var t2 = torre.Inv().Apply(Xf.FromComp(c).T);
            Console.WriteLine("  " + c.Name2 + ": (" + t[0].ToString("0.00") + ", " + t[2].ToString("0.00") + ") -> (" + t2[0].ToString("0.00") + ", " + t2[2].ToString("0.00") + ")");
        }
        Guardar(m);
    }

    // Avellanados de los M4: hasta la v7.12 eran D11 x 6.5 centrados en los pasos M6 originales (-29, ±13.85) y empezaban en la cara de
    // afuera de la pared (y 181.1). Delante de esa cara está el anillo D44/D56 donde gira E2 (y 173.2 .. 181.2): con los agujeros medidos
    // (-28.5, ±13.5, a 31.54 del eje) la cabeza D7 del M4x10 pasaba a 0.04 mm del anillo al meter el tornillo. Ahora el avellanado D11
    // va centrado en cada agujero (D11.5) y atraviesa el anillo (rebaje de 2.1 mm en su cara externa; el alojamiento D44 queda con 3.8 mm de pared).
    static void Avellanado()
    {
        var m = Abrir(P("02_Cintura_J1", "CB-210_Torre_Cintura_2M1D_mod.SLDPRT")); PB.Use(m);
        PB.Report("torre antes");
        Borrar(m, "CB_Avellanado_M6_D11x6.5");
        Borrar(m, "CB_v713_Avellanado_M4_D11");
        Borrar(m, "CB_v713_Avellanado_M4_D11.5");
        // D11.5 y no D11: tiene que tapar el D10 x 4 de la version M6 (CB_Avellanado_M6_D10x4, en el centro viejo, 0.61 mm corrido): 5 + 0.61 < 5.75
        PB.Sketch('T'); PB.Circle(XA - HY, ZC - HX, 11.5); PB.Circle(XA - HY, ZC + HX, 11.5);
        PB.Extrude(173.0, 187.7 - 173.0, true, "CB_v713_Avellanado_M4_D11.5");
        PB.Report("torre despues");
        PB.Prop("Modificaciones_CB", "+2 agujeros D4.5 para M4 (par del lado motor del 5840, a 28.5 mm bajo el eje J2 y +-13.5, medidos en el motor comprado, v7.13; " +
            "hasta la v7.12 a 29 y +-13.75 segun el plano del vendedor) con avellanado D11.5 centrado (cabeza Allen M4 hundida 2.5 mm), que atraviesa el anillo D56 de la cara de afuera " +
            "para que entre la cabeza (v7.13); +2 agujeros M4 (D4.4) para el soporte del iman. El par superior del 5840 cae dentro del alojamiento D44 y no se usa.");
        Guardar(m);
    }

    // Asiento del buje: hasta acá el buje D18 del motor quedaba suelto en el agujero D32 (7 mm de juego) y al motor lo ubicaban solo los 2 M4
    // en agujeros D4.5: contra el par de J2 lo sostenía el rozamiento de dos tornillos a 27 mm entre sí (giro alrededor del punto medio).
    // Con el asiento el buje fija el centro: el motor solo puede girar alrededor del eje J2 y los M4 lo frenan a 31.5 mm del eje (2.3 veces más agarre).
    // El buje real sobresale 4 mm de la caja y los resaltes 2: entra 2 mm, de y 191.2 a 193.2. D18.3 porque los agujeros impresos salen chicos,
    // más los horizontales (la torre se imprime parada). Del lado de E2 el agujero sigue D32: ahí entra el cubo de E2.
    static void Asiento()
    {
        var m = Abrir(P("02_Cintura_J1", "CB-210_Torre_Cintura_2M1D_mod.SLDPRT")); PB.Use(m);
        PB.Report("torre antes");
        Borrar(m, "CB_v713_Asiento_buje_D18.3");
        PB.Use(m);
        // anillo D32.5 / D18.3: el D32.5 se mete 0.25 mm en la pared para que se una al agujero D32
        PB.Sketch('T'); PB.Circle(XA, ZC, 32.5); PB.Circle(XA, ZC, 18.3);
        PB.Extrude(YPARED - 2.0, 2.0, false, "CB_v713_Asiento_buje_D18.3");
        PB.Report("torre despues");
        PB.Prop("Modificaciones_CB", "+2 agujeros D4.5 para M4 (par del lado motor del 5840, a 28.5 mm bajo el eje J2 y +-13.5, medidos en el motor comprado, v7.13; " +
            "hasta la v7.12 a 29 y +-13.75 segun el plano del vendedor) con avellanado D11.5 centrado (cabeza Allen M4 hundida 2.5 mm), que atraviesa el anillo D56 de la cara de afuera " +
            "para que entre la cabeza (v7.13); asiento D18.3 x 2 para el buje D18 del 5840 en la cara donde apoya el motor (lo centra en el eje J2, v7.13); " +
            "+2 agujeros M4 (D4.4) para el soporte del iman. El par superior del 5840 cae dentro del alojamiento D44 y no se usa.");
        Guardar(m);
    }

    // Buje frontal del modelo: D18.5 (plano del vendedor) -> D18 (medido). Se cambia el radio del círculo del croquis en lugar de rehacer la operación,
    // así la cara cilíndrica conserva su ID y no se rompe la relación J2_eje_hombro de CB-000. El largo queda en 5 (el real es 4): en el modelo el buje
    // pasa el asiento D18.3 de la torre y entra 1 mm más en el D32, sin tocar nada.
    static void Buje()
    {
        var m = Abrir(P("05_Comerciales", "CB-502_Motor_Sinfin_5840-36ZY.SLDPRT"));
        var pd = (PartDoc)m;
        var f = (Feature)pd.FeatureByName("Buje_frontal_D18.5x5") ?? (Feature)pd.FeatureByName("Buje_frontal_D18x5");
        if (f == null) { Console.WriteLine("NO se encontro el buje frontal"); return; }
        var skf = (Feature)f.GetFirstSubFeature();
        var sk = (Sketch)skf.GetSpecificFeature2();
        m.ClearSelection2(true);
        skf.Select2(false, 0);
        m.EditSketch();
        foreach (object o in (object[])sk.GetSketchSegments())
        {
            var arc = o as SketchArc;
            if (arc == null || arc.IsCircle() == 0) continue;
            double d0 = arc.GetRadius() * 2000;
            bool ok = arc.SetRadius(0.009);
            Console.WriteLine("  circulo del buje: D" + d0.ToString("0.00") + " -> D" + (arc.GetRadius() * 2000).ToString("0.00") + " (" + ok + ")");
        }
        m.ClearSelection2(true);
        m.SketchManager.InsertSketch(true);
        f.Name = "Buje_frontal_D18x5";
        m.ForceRebuild3(false);
        foreach (Body2 b in (object[])pd.GetBodies2((int)swBodyType_e.swSolidBody, true))
            foreach (Face2 fc in (object[])b.GetFaces())
            {
                var su = (Surface)fc.GetSurface(); if (!su.IsCylinder()) continue;
                var p = (double[])su.CylinderParams; double d = p[6] * 2000; if (d < 17 || d > 20) continue;
                var bx = (double[])fc.GetBox();
                Console.WriteLine("  cara D" + d.ToString("0.00") + " eje en (" + (p[0] * 1000).ToString("0.00") + ", " + (p[1] * 1000).ToString("0.00") + ") z " +
                    (bx[2] * 1000).ToString("0.0") + " .. " + (bx[5] * 1000).ToString("0.0"));
            }
        m.Extension.CustomPropertyManager[""].Add3("Notas", (int)swCustomInfoType_e.swCustomInfoText,
            "Caja 40x58x27, eje D8 con plano a 7 (L20 desde la caja, plano 13), buje D18 x 5 (medido: D18 x 4; en el modelo queda en 5: pasa el asiento D18.3 de la torre " +
            "y entra 1 mm en el D32), 4 resaltes D7 x 2 con M4 prof. 6: pares a +10.5 y -28.5 del eje, separacion 27.0, el inferior izquierdo con colisa a 45 grados " +
            "(medidas del motor comprado, v7.13; antes +11.05 / -29 / 27.5 y buje D18.5 del plano del vendedor), 2 resaltes lisos D7 en (+-5.5, -15.5), " +
            "buje trasero D18.5 x 4.45, motor D36 x 62 corrido 2 mm. Origen en el plano de apoyo de los resaltes. Eje de salida modelado aparte (CB-506).",
            (int)swCustomPropertyAddOption_e.swCustomPropertyReplaceValue);
        Guardar(m);
        SW.App.CloseDoc(m.GetTitle());
    }

    // Pieza de prueba: la torre cortada por una caja alrededor del motor. Marco de la pieza = como se imprime la torre (parada sobre su base):
    // Z = hacia arriba de la torre (x), Y = a lo largo del eje J2 (0 en la cara donde apoya el motor, negativo hacia E2), X = -z de la torre.
    // Origen en el eje J2 sobre la cara de apoyo del motor. La propiedad "Marco" guarda la transformación pieza -> torre (VerMontaje5840.cs).
    static void Prueba()
    {
        var t = Abrir(P("02_Cintura_J1", "CB-210_Torre_Cintura_2M1D_mod.SLDPRT"));
        var torreBody = (Body2)((object[])((PartDoc)t).GetBodies2((int)swBodyType_e.swSolidBody, true))[0];
        var mod = (Modeler)SW.App.GetModeler();
        // caja (coordenadas de la torre): x de XA-43.5 (los pasos M6 originales de la abrazadera empiezan en XA-44.2 y no entran) a XA+30
        // (anillo D56 entero; la caja del motor va de -38 a +20), z = ZC ± 31, y de 173.0 (delante del anillo) a 223 (detrás de la pared no hay material)
        double x0 = XA - 43.5, x1 = XA + 30, z0 = ZC - 31, z1 = ZC + 31, y0 = 173.0, y1 = 223;
        // el centro que pide CreateBodyFromBox3 es el de la cara de abajo (la altura va a lo largo del eje)
        var box = (Body2)mod.CreateBodyFromBox3(new double[] { (x0 + x1) / 2000, y0 / 1000, (z0 + z1) / 2000, 0, 1, 0, (x1 - x0) / 1000, (z1 - z0) / 1000, (y1 - y0) / 1000 });
        int err;
        Func<Body2, string> caja = bb => string.Join(" ", ((double[])bb.GetBodyBox()).Select(v => (v * 1000).ToString("0.0")));
        Console.WriteLine("caja de corte " + caja(box) + " | torre " + caja(torreBody));
        var res = (object[])((Body2)torreBody.Copy()).Operations2((int)swBodyOperationType_e.SWBODYINTERSECT, box, out err);
        Console.WriteLine("interseccion err=" + err + " cuerpos=" + (res == null ? 0 : res.Length));
        if (res == null || res.Length == 0) return;
        // marco de impresión (filas = ejes de la pieza en coordenadas de la torre)
        var marco = Xf.Rows(V(0, 0, -1), V(0, 1, 0), V(1, 0, 0), V(XA, YPARED, ZC));
        var inv = marco.Inv();
        PB.NewPart();
        foreach (Body2 b in res)
        {
            b.ApplyTransform(inv.ToMath());
            var f = (Feature)((PartDoc)PB.M).CreateFeatureFromBody3(b, false, (int)swCreateFeatureBodyOpts_e.swCreateFeatureBodyCheck);
            Console.WriteLine("  cuerpo " + (f != null ? f.Name : "FALLO"));
        }
        PB.PartColor(0.85, 0.55, 0.15);
        PB.Prop("Descripcion", "Pieza de prueba del montaje del motor del hombro (5840) en la torre CB-210");
        PB.Prop("Material", "PETG");
        PB.Prop("Notas", "v7.13: trozo de CB-210 alrededor del 5840: pared de 12 mm con el agujero D32 del buje y el asiento D18.3 x 2 del lado del motor, " +
            "2 agujeros D4.5 de los M4 a 28.5 bajo el eje J2 y +-13.5, avellanados D11.5 centrados y el anillo D44/D56 de la cara de afuera " +
            "(por donde tiene que pasar la cabeza del tornillo). " +
            "Origen en el eje J2 sobre la cara donde apoya el motor; Z hacia arriba de la torre, Y a lo largo del eje J2 (negativo hacia E2). " +
            "Imprimir parada sobre el borde de abajo (Z min), como la torre, para que los agujeros salgan horizontales igual que en la torre. " +
            "Probar: buje D18 calzado en el asiento D18.3 (sin juego y sin forzar), resaltes apoyados en la pared, 2 x M4x10 que entran derechos " +
            "con el buje calzado y cuya cabeza pasa por el anillo.");
        PB.Prop("Marco", string.Join(" ", new[] { 0, 0, -1, 0, 1, 0, 1, 0, 0, XA, YPARED, ZC }.Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture))));
        PB.Report("CB-211");
        PB.Save(P("12_Pruebas", "CB-211_Prueba_Montaje_5840.SLDPRT"));
        SW.App.CloseDoc(t.GetTitle());
    }
}
