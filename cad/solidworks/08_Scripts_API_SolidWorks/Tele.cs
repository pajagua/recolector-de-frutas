using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using SolidWorks.Interop.swmotionstudy;   // compilar con -r:SolidWorks.Interop.swmotionstudy.dll

// Tele.exe <root solidworks> [correa2|sub|asm|frames <carpeta> ...]   (v7.8, 2026-11-02)
// Prismático telescópico (v7B) solo, para verlo moverse:
//   correa2 : la correa del multiplicador CB-442B pasa a ser un lazo cerrado (sin el hueco del anclaje), así sirve en cualquier posición
//   sub     : CB-442B pasa de CB-300B (fija a E2) a CB-410B (se mueve con el tramo intermedio); nuevo CB-420B = tubo interior + buje + brida
//   asm     : CB-450B_Prismatico_Telescopico_Solo: E2 (transparente) + NEMA + poleas + correa de la etapa 1 + CB-410B + CB-420B,
//             movido por la variable global q3 (ecuaciones): el intermedio avanza q3/2 y el interior q3, como el multiplicador 2:1.
//             (Se probó la relación de acoplamiento lineal 2:1 de SolidWorks, pero por API no se pudo verificar que arrastre bien.)
//             Desde "estudio" ya no tiene q3: lo mueve el estudio de movimiento.
//   estudio : cambia las cotas de q3 por límites de carrera y agrega el estudio de movimiento "Movimiento_telescopico":
//             dos motores lineales respecto de E2, 340 mm el interior y 170 mm el intermedio en 5 s (velocidad constante,
//             así se mantiene el 2:1), reproducción de ida y vuelta. Se ve con Play en la pestaña del estudio.
//   frames  : imágenes del recorrido (q3 = 0 … 340 … 0) en vista lateral y trimétrica, sin guardar el ensamblaje
// Todo en coordenadas de E2 (ver V7.cs): eje del prismático = X, y hacia arriba del brazo, eje J2 = Z en x = -80.
class Tele
{
    static string R;
    const string DB = "11_v7B_Prismatico_Telescopico", COM = "05_Comerciales";
    static string P(string sub, string f) { return Path.Combine(R, sub, f); }
    static double[] V(params double[] v) { return v; }

    // ---- medidas de la v7B (V7.cs) ----
    const double Stroke = 340, XFront = 13, XFlIn = 15, XFlOut = 25;
    const double B_XMotor = -40, B_YFace = 50, B_XIdler = -252, B_YbPulley = 25.5;
    const double B_YPul = 17.3, B_XAnchor0 = -16, B_XAnchor1 = -8;
    const double B_XWallIn = -279, B_XTapRear = -276;   // cara interior de la pared trasera de E2 y cara trasera del tapón del intermedio

    static ModelDoc2 M; static AssemblyDoc A;
    static int GirosZ = int.Parse(System.Environment.GetEnvironmentVariable("GIROS_Z") ?? "6");   // 6 x 15° = 90°

    static void Main(string[] a)
    {
        R = a[0];
        SW.Connect();
        var w = a.Skip(1).ToList();
        if (w.Contains("correa2")) Correa2();
        if (w.Contains("sub")) Sub();
        if (w.Contains("asm")) Asm();
        if (w.Contains("estudio")) Estudio();
        int k = w.IndexOf("frames"); if (k >= 0) Frames(w[k + 1], w.Skip(k + 2).ToArray());
    }

    // ---------------- correa del multiplicador: lazo cerrado ----------------
    static void Correa2()
    {
        var m = SW.Open(P(DB, "CB-442B_Correa_GT2_6mm_Multiplicador.SLDPRT")); PB.Use(m);
        if (((PartDoc)m).FeatureByName("Tramo_superior_anclaje") == null)
        {
            double r1 = 4.95, r2 = 6.35;
            PB.Sketch('F'); PB.Rect(B_XAnchor0 - 0.01, B_YPul + r1, B_XAnchor1 + 0.01, B_YPul + r2); PB.Extrude(-3, 6, false, "Tramo_superior_anclaje");
            PB.Prop("Notas", "Coordenadas de E2, J3 retraido. Dibujada como lazo cerrado alrededor de las dos poleas 16T del tramo intermedio: " +
                "se mueve con el intermedio (CB-410B). En la realidad es abierta: los dos extremos van al anclaje de E2 (x -16 … -8 con J3 retraido) y el tramo inferior a la pinza del tubo interior.");
            PB.Report("CB-442B");
            Guardar(m);
        }
        SW.App.CloseDoc(m.GetTitle());
    }

    // ---------------- subensamblajes ----------------
    static void Sub()
    {
        // CB-410B: agrega la correa 2:1
        var m = SW.Open(P(DB, "CB-410B_Tramo_Intermedio_v7B.SLDASM")); M = m; A = (AssemblyDoc)m;
        Activar(m); A.ResolveAllLightWeightComponents(false);
        if (!Comps(m).Any(c => c.Name2.StartsWith("CB-442B")))
        {
            Add(P(DB, "CB-442B_Correa_GT2_6mm_Multiplicador.SLDPRT"), new Xf(), true);
            Desc("Tramo intermedio del telescopio J3 (v7B, home = retraido): tubo 30x30x2 L275 + tapon con polea 16T + soporte con polea 16T + buje delantero + pinza de la etapa 1 + correa del multiplicador 2:1 (se mueve con el intermedio). Coordenadas de E2.");
            Guardar(m);
        }
        SW.App.CloseDoc(m.GetTitle());

        // CB-300B: saca la correa 2:1 (ahora va en CB-410B)
        m = SW.Open(P(DB, "CB-300B_Hombro_E2_v7B.SLDASM")); M = m; A = (AssemblyDoc)m;
        Activar(m); A.ResolveAllLightWeightComponents(false);
        var c442 = Comps(m).FirstOrDefault(c => c.Name2.StartsWith("CB-442B"));
        if (c442 != null)
        {
            m.ClearSelection2(true); c442.Select4(false, null, false);
            Console.WriteLine("CB-442B fuera de CB-300B: " + m.Extension.DeleteSelection2(0));
            Desc("Eslabon 2 (hombro) v7B: E2 con manga corta + placa CB-306 y NEMA 17 arriba del cubo + poleas y correa GT2 de la etapa 1 + tapa-soporte AS5600 (la correa del multiplicador va en CB-410B)");
            Guardar(m);
        }
        SW.App.CloseDoc(m.GetTitle());

        // CB-420B: tramo interior sin la herramienta
        string dst = P(DB, "CB-420B_Tramo_Interior_v7B.SLDASM");
        if (!File.Exists(dst))
        {
            NuevoAsm();
            Add(P(DB, "CB-421B_Tubo_Interior_20x20x1.5_L263.SLDPRT"), new Xf(), true);
            Add(P(DB, "CB-422B_Buje_Trasero_Interior_Pinza_2a1.SLDPRT"), new Xf(), true);
            Add(P(DB, "CB-432B_Brida_E3_Herramienta_20x20.SLDPRT"), new Xf(), true);
            Desc("Tramo interior del telescopio J3 (v7B, home = retraido): tubo 20x20x1.5 L263 + buje trasero con pinza 2:1 + brida de la herramienta. Coordenadas de E2. Sin la herramienta.");
            GuardarComo(dst);
        }
    }

    // ---------------- ensamblaje solo del prismático ----------------
    static void Asm()
    {
        string dst = P(DB, "CB-450B_Prismatico_Telescopico_Solo.SLDASM");
        if (File.Exists(dst)) throw new Exception("ya existe " + dst);
        NuevoAsm();
        var e2 = Add(P(DB, "CB-301B_Eslabon_E2_v7B_Telescopico.SLDPRT"), new Xf(), true);
        Add(P("03_Hombro_E2_J2", "CB-306_Placa_Soporte_NEMA17_Prismatico.SLDPRT"), Xf.Tr(B_XMotor, B_YFace - 5, 0), true);
        Add(P(COM, "CB-505_Motor_NEMA17_Prismatico.SLDPRT"), Xf.Rows(V(0, -1, 0), V(1, 0, 0), V(0, 0, 1), V(B_XMotor, B_YFace - 491.5, 0)), true);
        Add(P(COM, "HW_Polea_GT2_20T_D5.SLDPRT"), Xf.Tr(B_XMotor, B_YbPulley, 0), true);
        Add(P(COM, "HW_Polea_Loca_GT2_20T_D5.SLDPRT"), Xf.Tr(B_XIdler, B_YbPulley, 0), true);
        Add(P(DB, "CB-441B_Correa_GT2_6mm_Etapa1.SLDPRT"), new Xf(), true);
        var c410 = Add(P(DB, "CB-410B_Tramo_Intermedio_v7B.SLDASM"), new Xf(), false);
        var c420 = Add(P(DB, "CB-420B_Tramo_Interior_v7B.SLDASM"), new Xf(), false);
        M.EditRebuild3();

        // E2 transparente para ver los tubos y las correas de adentro
        e2.SetMaterialPropertyValues2(new double[] { 0.92, 0.92, 0.94, 1, 1, 0.3, 0.3, 0.7, 0 }, (int)swInConfigurationOpts_e.swAllConfiguration, null);

        var mid = Child(c410, "CB-411B"); var tap = Child(c410, "CB-412B"); var br = Child(c420, "CB-432B");
        var front = FindPlaneFace(e2, 0, 1, XFront, 2000, 9000, "frente de E2 x=13");
        var pared = FindPlaneFace(e2, 0, 1, B_XWallIn, 300, 6000, "pared trasera de E2 por dentro x=-279");
        Mate("Intermedio_plano_medio", (int)swMateType_e.swMateCOINCIDENT, RefPlane(mid, 2), RefPlane(e2, 2));
        Mate("Intermedio_plano_eje", (int)swMateType_e.swMateCOINCIDENT, RefPlane(mid, 1), RefPlane(e2, 1));
        var fq1 = Mate("Intermedio_q3", (int)swMateType_e.swMateDISTANCE, FindPlaneFace(tap, 0, -1, B_XTapRear, 300, 2000, "tapon trasero x=-276"), pared, B_XTapRear - B_XWallIn);
        Mate("Interior_plano_medio", (int)swMateType_e.swMateCOINCIDENT, RefPlane(br, 2), RefPlane(e2, 2));
        Mate("Interior_plano_eje", (int)swMateType_e.swMateCOINCIDENT, RefPlane(br, 1), RefPlane(e2, 1));
        var fq2 = Mate("Interior_q3", (int)swMateType_e.swMateDISTANCE, FindPlaneFace(br, 0, -1, XFlIn, 4000, 6500, "brida cara interior x=15"), front, XFlIn - XFront);

        // q3 (variable global): el intermedio avanza q3/2 y el interior q3, como hace el multiplicador 2:1
        var eq = M.GetEquationMgr();
        string d1 = "\"" + ((DisplayDimension)fq1.GetFirstDisplayDimension()).GetDimension2(0).Name + "@" + fq1.Name + "\"";
        string d2 = "\"" + ((DisplayDimension)fq2.GetFirstDisplayDimension()).GetDimension2(0).Name + "@" + fq2.Name + "\"";
        Console.WriteLine("ecuaciones: " + eq.Add2(-1, "\"q3\" = 0", true) + " " +
            eq.Add2(-1, d1 + " = " + (B_XTapRear - B_XWallIn) + " + \"q3\" / 2", true) + " " +
            eq.Add2(-1, d2 + " = " + (XFlIn - XFront) + " + \"q3\"", true));
        M.EditRebuild3();
        foreach (var mt in Mates()) { bool ww; Console.WriteLine("  " + mt.Name + " err=" + mt.GetErrorCode2(out ww)); }
        foreach (var q in new[] { 200.0, 340.0, 0.0 })
        {
            Q(q);
            Console.WriteLine("  q3 = " + q + ": intermedio x = " + Xf.FromComp(c410).T[0].ToString("0.00") + ", interior x = " + Xf.FromComp(c420).T[0].ToString("0.00"));
        }
        Desc("Prismatico J3 telescopico (v7B) solo, para ver el mecanismo: E2 (transparente) + NEMA 17 + correa de la etapa 1 + tramo intermedio CB-410B " +
             "(con las poleas y la correa del multiplicador) + tramo interior CB-420B. Mover con la variable global q3 (Herramientas > Ecuaciones, 0 a 340 mm): " +
             "el intermedio avanza q3/2 y el interior q3.");
        M.ShowNamedView2("*Trimetric", -1); M.ViewZoomtofit2();
        GuardarComo(dst);
    }

    // ---------------- estudio de movimiento ----------------
    const double Duracion = 5;
    static void Estudio()
    {
        var m = SW.Open(P(DB, "CB-450B_Prismatico_Telescopico_Solo.SLDASM")); M = m; A = (AssemblyDoc)m;
        Activar(m); A.ResolveAllLightWeightComponents(false);
        var cs = Comps(m);
        var e2 = cs.First(c => c.Name2.StartsWith("CB-301B"));
        var c410 = cs.First(c => c.Name2.StartsWith("CB-410B")); var c420 = cs.First(c => c.Name2.StartsWith("CB-420B"));

        // 1) fuera las ecuaciones de q3 y las cotas fijas; en su lugar, límites de carrera (los tramos quedan libres en X)
        var eq = M.GetEquationMgr();
        for (int i = eq.GetCount() - 1; i >= 0; i--) { Console.WriteLine("  ecuacion borrada: " + eq.Equation[i]); eq.Delete(i); }
        foreach (var mt in Mates().Where(x => x.Name == "Intermedio_q3" || x.Name == "Interior_q3").ToList())
        { M.ClearSelection2(true); mt.Select2(false, 0); Console.WriteLine("  relacion borrada " + mt.Name + " " + M.Extension.DeleteSelection2(0)); }
        M.EditRebuild3();
        var tap = Child(c410, "CB-412B"); var br = Child(c420, "CB-432B"); var mid = Child(c410, "CB-411B");
        if (!Mates().Any(x => x.Name.StartsWith("Intermedio_carrera")))
        {
            var front = FindPlaneFace(e2, 0, 1, XFront, 2000, 9000, "frente de E2 x=13");
            var pared = FindPlaneFace(e2, 0, 1, B_XWallIn, 300, 6000, "pared trasera de E2 por dentro x=-279");
            double g = B_XTapRear - B_XWallIn;
            Mate("Intermedio_carrera_0-170", (int)swMateType_e.swMateDISTANCE, FindPlaneFace(tap, 0, -1, B_XTapRear, 300, 2000, "tapon trasero x=-276"), pared, g, g + Stroke / 2, g);
            Mate("Interior_carrera_0-340", (int)swMateType_e.swMateDISTANCE, FindPlaneFace(br, 0, -1, XFlIn, 4000, 6500, "brida cara interior x=15"), front, XFlIn - XFront, XFlIn - XFront + Stroke, XFlIn - XFront);
            M.EditRebuild3();
        }
        foreach (var mt in Mates()) { bool ww; Console.WriteLine("  " + mt.Name + " err=" + mt.GetErrorCode2(out ww)); }

        // 2) estudio de movimiento (animación) con dos motores lineales
        var msm = (MotionStudyManager)m.Extension.GetMotionStudyManager();
        var nombres = (object[])msm.GetMotionStudyNames();
        Console.WriteLine("  estudios: " + (nombres == null ? "-" : string.Join(", ", nombres.Select(o => o.ToString()))));
        foreach (bool reves in new[] { false })
        {
            MotionStudy ms = nombres != null && nombres.Length > 0 ? msm.GetMotionStudy(nombres[0].ToString()) : msm.CreateMotionStudy();
            ms.Activate();
            ms.StudyType = (int)swMotionStudyType_e.swMotionStudyTypeAssembly;
            ms.SetDuration(Duracion);
            foreach (var t in new[] { Tuple.Create(br, XFlOut, 4000.0, 6800.0, Stroke, "interior"), Tuple.Create(mid, XFront, 150.0, 300.0, Stroke / 2, "intermedio") })
            {
                var d = (SimulationMotorFeatureData)ms.CreateDefinition((int)swFeatureNameID_e.swFmAEMLinearMotor);
                var cara = FindPlaneFace(t.Item1, 0, 1, t.Item2, t.Item3, t.Item4, "cara del motor " + t.Item6);
                d.Location = cara; d.DirectionReference = cara; d.ReverseDirection = reves;
                d.RelativeComponent = e2;
                d.DistanceMotor(t.Item5 / 1000, 0, Duracion);
                Console.WriteLine("  motor " + t.Item6 + ": " + (ms.CreateFeature(d) == null ? "FALLO" : "ok"));
            }
            Console.WriteLine("  calcular " + ms.Calculate());
            ms.Stop();   // Calculate deja la animación corriendo
            double xi = 0, xm = 0, xmax = 0; bool ok21 = true;
            foreach (double tt in new[] { 1.0, 2.5, 4.0, 4.9 })
            {
                ms.SetTime(tt); System.Threading.Thread.Sleep(2500); M.GraphicsRedraw2(); System.Threading.Thread.Sleep(500);
                xi = Xf.FromComp(c420).T[0]; xm = Xf.FromComp(c410).T[0];
                Console.WriteLine("  t = " + tt + " s: interior x = " + xi.ToString("0.0") + ", intermedio x = " + xm.ToString("0.0"));
                ok21 &= Math.Abs(xi - 2 * xm) < 0.02 * Stroke && xi > -1 && xi < Stroke + 1; xmax = Math.Max(xmax, xi);
            }
            ms.SetTime(0);
            if (ok21)
            {
                ms.Name = "Movimiento_telescopico";
                var fs = (object[])ms.GetMotionFeatures();
                if (fs != null) foreach (Feature f in fs) Console.WriteLine("  en el estudio: " + f.Name + " (" + f.GetTypeName2() + ")");
                ms.PlayMode = (int)swAnimationPlayMode_e.swAnimationPlayModeReciprocate;
                Desc("Prismatico J3 telescopico (v7B) solo, para ver el mecanismo: E2 (transparente) + NEMA 17 + correa de la etapa 1 + tramo intermedio CB-410B " +
                     "(con las poleas y la correa del multiplicador) + tramo interior CB-420B. Estudio de movimiento Movimiento_telescopico (Play): " +
                     "en 5 s el interior avanza 340 mm y el intermedio 170 mm, ida y vuelta.");
                int e = 0, w = 0; M.ForceRebuild3(false);
                Console.WriteLine((M.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w) ? "guardado " : "NO GUARDADO ") + M.GetTitle());
                ms.Play();
                return;
            }
            Console.WriteLine("  no da la relacion esperada");
        }
        Console.WriteLine("NO SE PUDO: el ensamblaje queda sin guardar");
    }

    // ---------------- imágenes del movimiento ----------------
    static void Frames(string dir, string[] vistas)
    {
        Directory.CreateDirectory(dir);
        var m = SW.Open(P(DB, "CB-450B_Prismatico_Telescopico_Solo.SLDASM")); M = m; A = (AssemblyDoc)m;
        Activar(m); A.ResolveAllLightWeightComponents(false);
        var cs = Comps(m);
        var c410 = cs.First(c => c.Name2.StartsWith("CB-410B")); var c420 = cs.First(c => c.Name2.StartsWith("CB-420B"));
        var qs = new List<double>();
        double paso = double.Parse(System.Environment.GetEnvironmentVariable("PASO_Q") ?? "20");
        for (double q = 0; q <= Stroke + 0.1; q += paso) qs.Add(q);
        // fondo blanco liso solo durante la captura (se restaura al final)
        int apar0 = SW.App.GetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swColorsBackgroundAppearance);
        int fondo0 = SW.App.GetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swSystemColorsViewportBackground);
        SW.App.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swColorsBackgroundAppearance, (int)swColorsBackgroundAppearance_e.swColorsBackgroundAppearance_Plain);
        SW.App.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swSystemColorsViewportBackground, 0xFFFFFF);
        try {
        if (vistas.Length == 0) vistas = new[] { "Lado", "Trimetric" };
        foreach (var vista in vistas)
        {
            Q(Stroke);
            if (vista == "Lado") { m.ShowNamedView2("*Top", -1); for (int r = 0; r < GirosZ; r++) m.ViewRotateplusz(); }   // de costado, con el brazo horizontal
            else m.ShowNamedView2("*" + vista, -1);
            m.ViewZoomtofit2();
            int i = 0;
            foreach (var q in qs)
            {
                Q(q);
                m.GraphicsRedraw2();
                int e = 0, w = 0;
                string f = Path.Combine(dir, vista.ToLower() + "_" + (i++).ToString("00") + ".png");
                bool ok = m.Extension.SaveAs3(f, 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent | (int)swSaveAsOptions_e.swSaveAsOptions_Copy, null, null, ref e, ref w);
                Console.WriteLine((ok ? "  " : "  FALLO ") + Path.GetFileName(f) + "  q3 = " + q);
                if (vista == vistas[0] && (q == 0 || q == 160 || q == Stroke)) Choques(q);
            }
        }
        } finally {
            SW.App.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swColorsBackgroundAppearance, apar0);
            SW.App.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swSystemColorsViewportBackground, fondo0);
        }
        Q(0);
        SW.App.CloseDoc(m.GetTitle());   // sin guardar
    }
    // cambia la variable global q3 y reconstruye
    static void Q(double q)
    {
        var eq = M.GetEquationMgr(); bool hay = false;
        for (int i = 0; i < eq.GetCount(); i++)
            if (eq.Equation[i].Replace(" ", "").StartsWith("\"q3\"=")) { eq.Equation[i] = "\"q3\" = " + q; hay = true; }
        if (hay) { eq.EvaluateAll(); M.EditRebuild3(); return; }
        // desde que tiene el estudio de movimiento: el motor de distancia sigue una curva en S,
        // q3 / 340 = 3u² - 2u³ con u = t / 5 s (medido: t = 1 s -> 35.4 mm, t = 2.5 s -> 170, t = 4 s -> 304.6)
        var msm = (MotionStudyManager)M.Extension.GetMotionStudyManager();
        var ms = msm.GetMotionStudy("Movimiento_telescopico");
        if (!ms.IsActive) ms.Activate();
        double lo = 0, hi = 1, x = q / Stroke;
        for (int i = 0; i < 40; i++) { double u = (lo + hi) / 2; if (3 * u * u - 2 * u * u * u < x) lo = u; else hi = u; }
        ms.SetTime(Math.Min((lo + hi) / 2 * Duracion, Duracion - 0.01)); System.Threading.Thread.Sleep(1500); M.GraphicsRedraw2();
    }
    static void Choques(double q)
    {
        var idm = A.InterferenceDetectionManager;
        idm.TreatCoincidenceAsInterference = false; idm.TreatSubAssembliesAsComponents = false; idm.IncludeMultibodyPartInterferences = false;
        var ints = (object[])idm.GetInterferences();
        Console.WriteLine("    choques con q3 = " + q + ": " + (ints == null ? 0 : ints.Length));
        if (ints != null) foreach (IInterference it in ints)
            Console.WriteLine("      " + (it.Volume * 1e9).ToString("0.0") + " mm3  " + string.Join(" <-> ", ((object[])it.Components).Cast<Component2>().Select(c => c.Name2.Split('/').Last())));
        idm.Done();
    }

    // ---------------- ayudas ----------------
    static void Activar(ModelDoc2 m) { int er = 0; SW.App.ActivateDoc3(m.GetTitle(), false, 0, ref er); }
    static Component2[] Comps(ModelDoc2 m) { return ((object[])((AssemblyDoc)m).GetComponents(true)).Cast<Component2>().ToArray(); }
    static void NuevoAsm()
    {
        string tpl = SW.App.GetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swDefaultTemplateAssembly);
        M = (ModelDoc2)SW.App.NewDocument(tpl, 0, 0, 0); A = (AssemblyDoc)M;
        SW.Created.Add(M.GetTitle());
    }
    static Component2 Add(string path, Xf x, bool fix)
    {
        int e = 0, w = 0;
        int type = path.ToUpper().EndsWith(".SLDASM") ? (int)swDocumentTypes_e.swDocASSEMBLY : (int)swDocumentTypes_e.swDocPART;
        SW.App.DocumentVisible(false, type);
        SW.App.OpenDoc6(path, type, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref e, ref w);
        SW.App.DocumentVisible(true, type);
        var c = (Component2)A.AddComponent5(path, (int)swAddComponentConfigOptions_e.swAddComponentConfigOptions_CurrentSelectedConfig, "", false, "", 0, 0, 0);
        if (c == null) throw new Exception("AddComponent fallo " + path);
        c.Transform2 = x.ToMath();
        if (fix) { M.ClearSelection2(true); c.Select4(false, null, false); A.FixComponent(); M.ClearSelection2(true); }
        return c;
    }
    static void Desc(string d) { M.Extension.CustomPropertyManager[""].Add3("Descripcion", (int)swCustomInfoType_e.swCustomInfoText, d, 2); }
    static void Guardar(ModelDoc2 m)
    {
        int e = 0, w = 0; m.ForceRebuild3(false);
        Console.WriteLine((m.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w) ? "guardado " : "NO GUARDADO ") + Path.GetFileName(m.GetPathName()));
    }
    static void GuardarComo(string dst)
    {
        int e = 0, w = 0; M.ForceRebuild3(false);
        bool ok = M.Extension.SaveAs3(dst, 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, null, ref e, ref w);
        Console.WriteLine((ok ? "guardado " : "NO GUARDADO ") + dst + " e=" + e);
        SW.App.CloseDoc(M.GetTitle());
    }
    static Component2 Child(Component2 parent, string fileStart)
    {
        foreach (Component2 c in (object[])parent.GetChildren())
            if (Path.GetFileName(c.GetPathName()).StartsWith(fileStart, StringComparison.OrdinalIgnoreCase)) return c;
        throw new Exception("no esta " + fileStart + " en " + parent.Name2);
    }
    static bool Near(double a, double b, double tol = 0.05) { return Math.Abs(a - b) < tol; }
    static Entity FindPlaneFace(Component2 comp, int axis, double sign, double at, double areaMin, double areaMax, string what)
    {
        var part = (PartDoc)comp.GetModelDoc2();
        var x = Xf.FromComp(comp);   // las piezas están en coordenadas de E2; el componente puede estar corrido
        foreach (Body2 b in (object[])part.GetBodies2((int)swBodyType_e.swSolidBody, true))
            foreach (Face2 f in (object[])b.GetFaces())
            {
                var s = (Surface)f.GetSurface();
                if (!s.IsPlane()) continue;
                double[] n = (double[])f.Normal;
                if (Math.Abs(n[axis] - sign) > 1e-6) continue;
                double[] bx = (double[])f.GetBox();
                if (!Near(bx[axis] * 1000, at) || !Near(bx[axis + 3] * 1000, at)) continue;
                double ar = f.GetArea() * 1e6;
                if (ar < areaMin || ar > areaMax) { Console.WriteLine("  (cara descartada " + what + ": " + ar.ToString("0") + " mm2)"); continue; }
                Console.WriteLine("  cara ok: " + what + " (" + ar.ToString("0") + " mm2)");
                return (Entity)comp.GetCorrespondingEntity(f);
            }
        throw new Exception("cara no encontrada: " + what);
    }
    static Feature RefPlane(Component2 comp, int axis)
    {
        for (Feature f = (Feature)comp.FirstFeature(); f != null; f = (Feature)f.GetNextFeature())
            if (f.GetTypeName2() == "RefPlane")
            {
                var rp = (RefPlane)f.GetSpecificFeature2();
                double[] t = (double[])((MathTransform)rp.Transform).ArrayData;
                if (Math.Abs(Math.Abs(t[6 + axis]) - 1) < 1e-6) return f;
            }
        throw new Exception("sin plano normal a " + axis + " en " + comp.Name2);
    }
    static Feature Mate(string name, int type, object e1, object e2, double dist = 0, double dmax = 0, double dmin = 0)
    {
        M.ClearSelection2(true);
        var sd = (SelectData)((SelectionMgr)M.SelectionManager).CreateSelectData();
        sd.Mark = 1;
        bool s1 = e1 is Entity ? ((Entity)e1).Select4(true, sd) : ((Feature)e1).Select2(true, 1);
        bool s2 = e2 is Entity ? ((Entity)e2).Select4(true, sd) : ((Feature)e2).Select2(true, 1);
        int err;
        var mt = (Mate2)A.AddMate5(type, (int)swMateAlign_e.swMateAlignCLOSEST, false, dist / 1000, dmax / 1000, dmin / 1000, 0, 0, 0, 0, 0, false, false, 0, out err);
        if (mt == null && type == (int)swMateType_e.swMateDISTANCE && dmax == 0)
        {
            Console.WriteLine("  reintento con limites = distancia (err=" + err + ")");
            ((Entity)e1).Select4(true, sd); ((Entity)e2).Select4(true, sd);
            mt = (Mate2)A.AddMate5(type, (int)swMateAlign_e.swMateAlignCLOSEST, false, dist / 1000, dist / 1000, dist / 1000, 0, 0, 0, 0, 0, false, false, 0, out err);
        }
        Console.WriteLine("relacion " + name + ": sel " + s1 + "/" + s2 + " err=" + err + (mt == null ? " FALLO" : ""));
        M.ClearSelection2(true);
        if (mt == null) return null;
        var last = Mates().LastOrDefault();
        if (last != null) last.Name = name;
        return last;
    }
    static List<Feature> Mates()
    {
        var r = new List<Feature>();
        for (Feature f = (Feature)M.FirstFeature(); f != null; f = (Feature)f.GetNextFeature())
            if (f.GetTypeName2() == "MateGroup")
                for (Feature sf = (Feature)f.GetFirstSubFeature(); sf != null; sf = (Feature)sf.GetNextSubFeature()) r.Add(sf);
        return r;
    }
}
