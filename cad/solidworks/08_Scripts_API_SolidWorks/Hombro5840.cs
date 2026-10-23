using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Hombro5840.exe <root> [tornillo|motor|eje|torre|asm200|mate ...]   (v6, 2026-10-23)
// Ajusta el motor del hombro al plano del vendedor ("Product size", 5840 sinfin):
//   caja 40 x 58 x 27, eje a 20 mm del borde superior (38 del lado motor), 4 resaltes D7.6 de 2 mm con M4 prof. 6,
//   buje D18.5 x 5 desde la caja, eje D8 (plano a 7) 20 mm desde la caja, buje trasero D18.5 x 4.45, motor D36 x 62
//   corrido 2 mm hacia un costado. Los resaltes de 2 mm apoyan contra la pared de la torre.
// CB-502 y CB-506 se rehacen dentro del mismo archivo (se borran sus operaciones) para no romper las referencias.
class Hombro5840
{
    static string R;
    static string P(string sub, string file) { return Path.Combine(R, sub, file); }
    static double[] V(params double[] v) { return v; }
    const double XA = 190.171, ZC = -86.723;   // eje J2 en coordenadas de la torre CB-210
    const double YPARED = 193.2;               // cara interior de la pared de la torre (apoyo del motor)
    const double HX = 13.75, HY = 29.0;        // agujeros M4 del lado motor: +-13.75 a 29 mm del eje

    static void Main(string[] args)
    {
        R = args[0];
        SW.Connect();
        var which = args.Skip(1).ToList();
        Func<string, bool> want = k => which.Count == 0 || which.Contains(k);
        if (want("tornillo")) Tornillo();
        if (want("motor")) Motor();
        if (want("eje")) Eje();
        if (want("torre")) Torre();
        if (want("asm200")) Asm200();
        if (want("mate")) MateJ2();
    }

    static void Save(ModelDoc2 m)
    {
        int e = 0, w = 0;
        m.ForceRebuild3(false);
        bool ok = m.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w);
        Console.WriteLine((ok ? "saved " : "SAVE FAIL ") + m.GetPathName() + " e=" + e + " w=" + w);
        SW.App.QuitDoc(m.GetTitle());
    }

    // borra todas las operaciones de sólido y sus croquis (el documento conserva su identidad)
    static void Vaciar(ModelDoc2 m)
    {
        var feats = new List<Feature>();
        for (Feature f = (Feature)m.FirstFeature(); f != null; f = (Feature)f.GetNextFeature()) feats.Add(f);
        string[] tipos = { "ICE", "Extrusion", "Cut", "ProfileFeature", "Fillet", "Chamfer", "Revolution", "RevCut" };
        for (int i = feats.Count - 1; i >= 0; i--)
        {
            try
            {
                if (!tipos.Contains(feats[i].GetTypeName2())) continue;
                m.ClearSelection2(true);
                if (feats[i].Select2(false, 0)) m.Extension.DeleteSelection2((int)swDeleteSelectionOptions_e.swDelete_Absorbed);
            }
            catch { }
        }
        m.ClearSelection2(true);
        m.ForceRebuild3(false);
        Console.WriteLine("  cuerpos tras vaciar: " + PB.BodyCount());
    }

    static void Tornillo()
    {
        PB.NewPart();
        // apoyo de la cabeza en el origen, cabeza hacia +Y, cana hacia -Y (como HW_M6x10)
        PB.Sketch('T'); PB.Circle(0, 0, 7); PB.Extrude(0, 4, false, "Cabeza");
        PB.Sketch('T'); PB.Circle(0, 0, 4); PB.Extrude(0, -10, false, "Cana");
        PB.Sketch('T'); PB.Circle(0, 0, 3); PB.Extrude(4, -2, true, "Hexagono");
        PB.PartColor(0.16, 0.16, 0.18);
        PB.Prop("Descripcion", "Tornillo Allen M4x10 DIN 912 (motor 5840 a la torre: 5.5 mm de pared + 4.5 mm de rosca en el resalte del motor)");
        PB.Report("M4x10");
        PB.Save(P("05_Comerciales\\Tornilleria", "HW_M4x10_Allen_DIN912.SLDPRT"));
        SW.App.QuitDoc(PB.M.GetTitle());
    }

    // Marco del motor: origen = eje de salida en el plano de apoyo (cara de los resaltes de 2 mm), +Z = sentido del eje,
    // motor hacia -Y, X horizontal. Mismo marco que el modelo anterior (CB-200 no cambia de transformacion).
    static void Motor()
    {
        var m = SW.Open(P("05_Comerciales", "CB-502_Motor_Sinfin_5840-36ZY.SLDPRT"));
        PB.Use(m);
        Vaciar(m);
        PB.Use(m);
        PB.Sketch('F'); PB.RRect(-20, -38, 20, 20, 2.5); var caja = PB.Extrude(-2, -27, false, "Caja_reductora_40x58x27");
        // resaltes de apoyo D7.6 x 2 (4 con M4 + 2 lisos)
        double[][] m4 = { V(-HX, 11.05), V(HX, 11.05), V(-HX, -HY), V(HX, -HY) };
        PB.Sketch('F'); foreach (var h in m4) PB.Circle(h[0], h[1], 7.6); PB.Circle(-6.15, -19.5, 7.6); PB.Circle(6.45, -19.5, 7.6);
        PB.Extrude(-2, 2, false, "Resaltes_apoyo_D7.6x2");
        PB.Sketch('F'); foreach (var h in m4) PB.Circle(h[0], h[1], 3.3); PB.Extrude(0, -6, true, "Agujeros_4xM4_prof6");
        PB.Sketch('F'); PB.Circle(0, 0, 18.5); PB.Extrude(-2, 5, false, "Buje_frontal_D18.5x5");
        PB.Sketch('F'); PB.Circle(0, 0, 18.5); PB.Extrude(-29, -4.45, false, "Buje_trasero_D18.5x4.45");
        // motor D36 x 62, eje del motor corrido 2 mm en -X y tangente al plano de la caja (z = -2 - 18)
        PB.Sketch('T'); PB.Circle(-2, -20, 36); var mo = PB.Extrude(-38, -62, false, "Motor_D36x62");
        PB.Sketch('T'); PB.Circle(-2, -20, 13); PB.Extrude(-100, -3, false, "Buje_motor_D13");
        PB.Sketch('T'); PB.Rect(-12.5, -21.5, -9.5, -18.5); PB.Rect(5.5, -21.5, 8.5, -18.5); var tm = PB.Extrude(-100, -5, false, "Terminales");
        PB.PartColor(0.78, 0.78, 0.8);
        PB.Color(mo, 0.12, 0.12, 0.12); PB.Color(tm, 0.8, 0.6, 0.2);
        PB.Prop("Descripcion", "Motorreductor sinfin-corona 5840 12 V autoblocante - hombro J2 (direct drive)");
        PB.Prop("Notas", "Segun el plano del vendedor (Product size): caja 40x58x27, eje D8 con plano a 7 (L20 desde la caja, plano 13), " +
            "buje D18.5 x 5, 4 resaltes D7.6 x 2 con M4 prof. 6 (pares a +11.05 y -29 del eje, separacion 27.5; el inferior izquierdo es colisa), " +
            "buje trasero D18.5 x 4.45, motor D36 x 62 corrido 2 mm. Origen en el plano de apoyo de los resaltes. Eje de salida modelado aparte (CB-506).");
        PB.Report("5840");
        Save(m);
    }

    // Eje de salida en coordenadas de E2 (eje Z en x=-80, y=0). Pared de la torre en z_E2 = 138.2 - y_torre:
    // buje del motor hasta z = -52.0, punta del eje en z = -37.0 (20 mm desde la caja, que queda a 2 mm de la pared).
    static void Eje()
    {
        var m = SW.Open(P("05_Comerciales", "CB-506_Eje_Salida_5840_D8.SLDPRT"));
        PB.Use(m);
        Vaciar(m);
        PB.Use(m);
        PB.Sketch('F'); PB.Circle(-80, 0, 8.0); PB.Extrude(-52, 15, false, "Eje_D8_L15");
        PB.Sketch('F'); PB.Rect(-80 + 3.0, -6, -80 + 6, 6); PB.Extrude(-50, 14, true, "Plano_D_a7_L13");
        PB.PartColor(0.6, 0.6, 0.62);
        PB.Prop("Notas", "Coordenadas de E2. D8 con plano a 7 mm (13 mm desde la punta), 15 mm libres desde el buje D18.5 del motor (z -52 .. -37).");
        PB.Report("Eje 5840");
        Save(m);
    }

    static void Torre()
    {
        var m = SW.Open(P("02_Cintura_J1", "CB-210_Torre_Cintura_2M1D_mod.SLDPRT"));
        PB.Use(m);
        if (((PartDoc)m).FeatureByName("CB_v6_Agujeros_M4_motor5840") != null) { Console.WriteLine("torre ya tiene M4"); SW.App.QuitDoc(m.GetTitle()); return; }
        PB.Report("torre antes");
        // se rellena el paso D6.4 (y 187.7 .. 193.2) y se agujerea D4.5; el avellanado D11 x 6.5 queda para la cabeza Allen M4 (D7 x 4)
        PB.Sketch('T'); PB.Circle(XA - HY, ZC - 13.85, 6.6); PB.Circle(XA - HY, ZC + 13.85, 6.6);
        PB.Extrude(187.7, YPARED - 187.7, false, "CB_v6_Relleno_pasos_M6");
        PB.Sketch('T'); PB.Circle(XA - HY, ZC - HX, 4.5); PB.Circle(XA - HY, ZC + HX, 4.5);
        PB.Extrude(181.0, 12.5, true, "CB_v6_Agujeros_M4_motor5840");
        PB.Report("torre despues");
        PB.Prop("Modificaciones_CB", "+2 agujeros D4.5 para M4 (par del lado motor del 5840, a 29 mm bajo el eje J2 y +-13.75) con avellanado D11 x 6.5 " +
            "(cabeza Allen M4 hundida 2.5 mm); +2 agujeros M4 (D4.4) para el soporte del iman. El par superior del 5840 cae dentro del alojamiento D44 y no se usa.");
        Save(m);
    }

    static void Asm200()
    {
        var m = SW.Open(P("02_Cintura_J1", "CB-200_Cintura_J1.SLDASM"));
        var a = (AssemblyDoc)m;
        a.ResolveAllLightWeightComponents(false);
        var comps = ((object[])a.GetComponents(true)).Cast<Component2>().ToArray();
        var torre = Xf.FromComp(comps.First(c => c.Name2.StartsWith("CB-210")));
        foreach (var c in comps.Where(c => c.Name2.StartsWith("HW_M6x10")))
        {
            string n = c.Name2;
            m.ClearSelection2(true); c.Select4(false, null, false);
            Console.WriteLine("  borrado " + n + " " + m.Extension.DeleteSelection2(0));
        }
        var down = Xf.Rows(V(1, 0, 0), V(0, -1, 0), V(0, 0, -1), V(0, 0, 0));   // cabeza hacia la pared exterior, cana hacia el motor
        string path = P("05_Comerciales\\Tornilleria", "HW_M4x10_Allen_DIN912.SLDPRT");
        foreach (double s in new[] { -HX, HX })
        {
            int e = 0, w = 0;
            SW.App.DocumentVisible(false, (int)swDocumentTypes_e.swDocPART);
            SW.App.OpenDoc6(path, (int)swDocumentTypes_e.swDocPART, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref e, ref w);
            SW.App.DocumentVisible(true, (int)swDocumentTypes_e.swDocPART);
            var c = (Component2)a.AddComponent5(path, 0, "", false, "", 0, 0, 0);
            c.Transform2 = down.Then(Xf.Tr(XA - HY, 187.7, ZC + s)).Then(torre).ToMath();
            m.ClearSelection2(true); c.Select4(false, null, false); a.FixComponent(); m.ClearSelection2(true);
        }
        Save(m);
    }

    // J2_eje_hombro: buje D18.5 del motor (CB-502) concentrico con el eje D8 (CB-506)
    static Component2 Find(Component2 parent, string start)
    {
        foreach (Component2 c in (object[])parent.GetChildren())
            if (Path.GetFileName(c.GetPathName()).StartsWith(start, StringComparison.OrdinalIgnoreCase)) return c;
        return null;
    }
    static Entity Cyl(Component2 comp, double radius, double c1, double c2, string what, double zMaxMin = -1e9)
    {
        var part = (PartDoc)comp.GetModelDoc2();
        foreach (Body2 b in (object[])part.GetBodies2((int)swBodyType_e.swSolidBody, true))
            foreach (Face2 f in (object[])b.GetFaces())
            {
                var s = (Surface)f.GetSurface();
                if (!s.IsCylinder()) continue;
                double[] p = (double[])s.CylinderParams;
                if (Math.Abs(p[6] * 1000 - radius) > 0.02 || Math.Abs(Math.Abs(p[5]) - 1) > 1e-6) continue;
                if (Math.Abs(p[0] * 1000 - c1) > 0.05 || Math.Abs(p[1] * 1000 - c2) > 0.05) continue;
                if (((double[])f.GetBox())[5] * 1000 < zMaxMin) continue;   // buje delantero (z > 0), no el trasero
                Console.WriteLine("  cara ok: " + what);
                return (Entity)comp.GetCorrespondingEntity(f);
            }
        throw new Exception("cara no encontrada: " + what);
    }
    static void MateJ2()
    {
        var m = SW.Open(Path.Combine(R, "CB-000_CitriBot_Ensamblaje_General.SLDASM"));
        var a = (AssemblyDoc)m;
        a.ResolveAllLightWeightComponents(false);
        Feature mg = null;
        for (Feature f = (Feature)m.FirstFeature(); f != null; f = (Feature)f.GetNextFeature()) if (f.GetTypeName2() == "MateGroup") mg = f;
        for (Feature sf = (Feature)mg.GetFirstSubFeature(); sf != null; sf = (Feature)sf.GetNextSubFeature())
            if (sf.Name == "J2_eje_hombro") { m.ClearSelection2(true); sf.Select2(false, 0); Console.WriteLine("  relacion borrada " + m.Extension.DeleteSelection2(0)); break; }
        var cs = ((object[])a.GetComponents(true)).Cast<Component2>().ToArray();
        var mot = Find(cs.First(c => c.Name2.StartsWith("CB-200")), "CB-502");
        var eje = Find(cs.First(c => c.Name2.StartsWith("CB-300")), "CB-506");
        m.ClearSelection2(true);
        var sd = (SelectData)((SelectionMgr)m.SelectionManager).CreateSelectData(); sd.Mark = 1;
        Cyl(mot, 9.25, 0, 0, "buje D18.5 del 5840", 1.0).Select4(true, sd);
        Cyl(eje, 4.0, -80, 0, "eje D8").Select4(true, sd);
        int err;
        var mt = a.AddMate5((int)swMateType_e.swMateCONCENTRIC, (int)swMateAlign_e.swMateAlignCLOSEST, false, 0, 0, 0, 0, 0, 0, 0, 0, false, false, 0, out err);
        Console.WriteLine("relacion J2_eje_hombro err=" + err + (mt == null ? " FALLO" : ""));
        m.ClearSelection2(true);
        Feature last = null;
        for (Feature sf = (Feature)mg.GetFirstSubFeature(); sf != null; sf = (Feature)sf.GetNextSubFeature()) last = sf;
        if (last != null) last.Name = "J2_eje_hombro";
        m.EditRebuild3();
        for (Feature sf = (Feature)mg.GetFirstSubFeature(); sf != null; sf = (Feature)sf.GetNextSubFeature()) { bool w; Console.WriteLine("  " + sf.Name + " err=" + sf.GetErrorCode2(out w)); }
        Save(m);
    }
}
