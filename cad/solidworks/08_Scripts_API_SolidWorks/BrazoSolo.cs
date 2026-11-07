using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// BrazoSolo.exe <root> [copiar|limpiar|relaciones|sr0] (v7.6, 2026-10-30)
// Ensamblaje solo del brazo, sin la base de madera, la electrónica, las cámaras ni los motores, en 13_Brazo_Solo:
//   CB-001 (copia de CB-000) con copias de los subconjuntos: CB-100S (solo la placa fija de J1, CB-120), CB-200S (sin el 5840),
//   CB-300S (sin el NEMA 17), CB-400S (con CB-600S) y CB-600S (sin los dos MG996R). Los originales no se tocan.
// Las copias de archivo conservan el ID interno, así las relaciones de CB-000 siguen valiendo; la de J2 que tomaba el
// buje del 5840 se rehace contra el imán del AS5600 (CB-504), que está sobre el eje J2.
class BrazoSolo
{
    static string R;
    const string D = "13_Brazo_Solo";
    static string P(string rel) { return Path.Combine(R, rel); }
    static readonly string[,] COPIAS = {
        { "CB-000_CitriBot_Ensamblaje_General.SLDASM", D + "\\CB-001_Brazo_Solo.SLDASM" },
        { "01_Base_J0\\CB-100_Base_J0.SLDASM", D + "\\CB-100S_Base_Brazo.SLDASM" },
        { "02_Cintura_J1\\CB-200_Cintura_J1.SLDASM", D + "\\CB-200S_Cintura_J1_Sin_Motor.SLDASM" },
        { "03_Hombro_E2_J2\\CB-300_Hombro_E2_J2.SLDASM", D + "\\CB-300S_Hombro_E2_J2_Sin_Motor.SLDASM" },
        { "04_Prismatico_E3_J3\\CB-400_Prismatico_E3_J3.SLDASM", D + "\\CB-400S_Prismatico_E3_J3.SLDASM" },
        { "09_Herramienta\\CB-600_Herramienta.SLDASM", D + "\\CB-600S_Herramienta_Sin_Servos.SLDASM" } };

    static void Main(string[] a)
    {
        R = a[0];
        SW.Connect();
        foreach (var k in a.Skip(1))
        {
            if (k == "copiar") Copiar();
            if (k == "superior") Superior();
            if (k == "limpiar") Limpiar();
            if (k == "relaciones") Relaciones();
            if (k == "sr0") Sr0();
            if (k == "telescopico") { Telescopico(); Relaciones(); }
        }
    }

    // v7.12: CB-000 tiene el prismático telescópico. CB-001 se vuelve a copiar de CB-000 y usa copias nuevas de CB-300B (sin el
    // NEMA 17) y CB-400B (con CB-600S). CB-100S, CB-200S y CB-600S no cambian y se reusan; CB-410B (intermedio) va el original.
    // Las copias de la variante A (CB-300S, CB-400S) dejan de usarse.
    static readonly string[,] COPIAS_B = {
        { "CB-000_CitriBot_Ensamblaje_General.SLDASM", D + "\\CB-001_Brazo_Solo.SLDASM" },
        { "11_v7B_Prismatico_Telescopico\\CB-300B_Hombro_E2_v7B.SLDASM", D + "\\CB-300BS_Hombro_E2_v7B_Sin_Motor.SLDASM" },
        { "11_v7B_Prismatico_Telescopico\\CB-400B_Prismatico_E3_v7B.SLDASM", D + "\\CB-400BS_Prismatico_E3_v7B.SLDASM" } };
    static void Telescopico()
    {
        for (int i = 0; i < COPIAS_B.GetLength(0); i++)
        {
            if (SW.App.GetOpenDocumentByName(P(COPIAS_B[i, 1])) != null) throw new Exception("abierto: " + COPIAS_B[i, 1]);
            File.Copy(P(COPIAS_B[i, 0]), P(COPIAS_B[i, 1]), true); File.SetAttributes(P(COPIAS_B[i, 1]), FileAttributes.Normal);
            Console.WriteLine("copiado " + COPIAS_B[i, 1]);
        }
        Ref(COPIAS_B[0, 1], COPIAS[1, 0], COPIAS[1, 1]);   // base -> CB-100S
        Ref(COPIAS_B[0, 1], COPIAS[2, 0], COPIAS[2, 1]);   // cintura -> CB-200S
        Ref(COPIAS_B[0, 1], COPIAS_B[1, 0], COPIAS_B[1, 1]);
        Ref(COPIAS_B[0, 1], COPIAS_B[2, 0], COPIAS_B[2, 1]);
        Ref(COPIAS_B[2, 1], COPIAS[5, 0], COPIAS[5, 1]);   // herramienta -> CB-600S
        // hombro: sin el NEMA 17 (abierto sin ventana)
        SW.App.DocumentVisible(false, (int)swDocumentTypes_e.swDocASSEMBLY);
        ModelDoc2 m; try { m = SW.Open(P(COPIAS_B[1, 1])); } finally { SW.App.DocumentVisible(true, (int)swDocumentTypes_e.swDocASSEMBLY); }
        var asm = (AssemblyDoc)m; asm.ResolveAllLightWeightComponents(false);
        var cs = ((object[])asm.GetComponents(true)).Cast<Component2>().ToList();
        var nema = cs.First(c => c.Name2.StartsWith("CB-505")); var gn = Caja(nema);
        Borrar(m, cs.Where(c => c == nema || (c.Name2.StartsWith("HW_M3x6") && Cerca(Caja(c), gn, 4))));
        Guardar(m);
    }

    static void Copiar()
    {
        Directory.CreateDirectory(P(D));
        for (int i = 0; i < COPIAS.GetLength(0); i++)
        {
            if (SW.App.GetOpenDocumentByName(P(COPIAS[i, 1])) != null) throw new Exception("abierto: " + COPIAS[i, 1]);
            File.Copy(P(COPIAS[i, 0]), P(COPIAS[i, 1]), true); File.SetAttributes(P(COPIAS[i, 1]), FileAttributes.Normal);
            Console.WriteLine("copiado " + COPIAS[i, 1]);
        }
        for (int i = 1; i <= 4; i++) Ref(COPIAS[0, 1], COPIAS[i, 0], COPIAS[i, 1]);
        Ref(COPIAS[4, 1], COPIAS[5, 0], COPIAS[5, 1]);
    }
    // solo el ensamblaje superior: copia CB-000 otra vez y lo apunta a los subconjuntos S (que ya están limpios)
    static void Superior()
    {
        File.Copy(P(COPIAS[0, 0]), P(COPIAS[0, 1]), true); File.SetAttributes(P(COPIAS[0, 1]), FileAttributes.Normal);
        Console.WriteLine("copiado " + COPIAS[0, 1]);
        for (int i = 1; i <= 4; i++) Ref(COPIAS[0, 1], COPIAS[i, 0], COPIAS[i, 1]);
    }
    static void Ref(string asm, string oldRef, string newRef)
    {
        bool ok = SW.App.ReplaceReferencedDocument(P(asm), P(oldRef), P(newRef));
        Console.WriteLine((ok ? "ref ok   " : "REF FALLO ") + Path.GetFileName(asm) + ": " + Path.GetFileName(oldRef) + " -> " + Path.GetFileName(newRef));
        if (!ok) throw new Exception("fallo ReplaceReferencedDocument");
    }

    static ModelDoc2 Abrir(string rel)
    {
        var m = SW.Open(P(rel)); int er = 0; SW.App.ActivateDoc3(m.GetTitle(), false, 0, ref er);
        ((AssemblyDoc)m).ResolveAllLightWeightComponents(false); return m;
    }
    static void Guardar(ModelDoc2 m)
    {
        int e = 0, w = 0; m.ForceRebuild3(false);
        Console.WriteLine("guardado " + m.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w) + " " + m.GetTitle() + (e != 0 ? " e=" + e : ""));
        SW.App.CloseDoc(m.GetTitle());
    }
    static void Borrar(ModelDoc2 m, IEnumerable<Component2> cs)
    {
        foreach (var c in cs.ToList())
        {
            m.ClearSelection2(true); c.Select4(false, null, false);
            Console.WriteLine("  borrado " + c.Name2 + ": " + m.Extension.DeleteSelection2((int)swDeleteSelectionOptions_e.swDelete_Absorbed));
        }
        m.ClearSelection2(true);
    }
    static double[] Caja(Component2 c) { var b = (double[])c.GetBox(false, false); return b.Select(v => v * 1000).ToArray(); }
    static bool Cerca(double[] b, double[] g, double tol)
    {
        double cx = (b[0] + b[3]) / 2, cy = (b[1] + b[4]) / 2, cz = (b[2] + b[5]) / 2;
        return cx > g[0] - tol && cx < g[3] + tol && cy > g[1] - tol && cy < g[4] + tol && cz > g[2] - tol && cz < g[5] + tol;
    }

    static void Limpiar()
    {
        // base: solo la placa fija de J1 (CB-120) y la tornillería que está sobre ella
        var m = Abrir(COPIAS[1, 1]); var asm = (AssemblyDoc)m;
        var cs = ((object[])asm.GetComponents(true)).Cast<Component2>().ToList();
        var g = Caja(cs.First(c => c.Name2.StartsWith("CB-120")));
        Borrar(m, cs.Where(c => !c.Name2.StartsWith("CB-120") && (c.Name2.StartsWith("CB-") || !Cerca(Caja(c), g, 3))));
        Guardar(m);
        // cintura: sin el motor 5840 ni sus 2 tornillos
        m = Abrir(COPIAS[2, 1]); asm = (AssemblyDoc)m; cs = ((object[])asm.GetComponents(true)).Cast<Component2>().ToList();
        Borrar(m, cs.Where(c => c.Name2.StartsWith("CB-502") || c.Name2.StartsWith("HW_M4x10_Allen")));
        Guardar(m);
        // hombro: sin el NEMA 17 ni sus 4 tornillos (los M3x6 que están junto al motor)
        m = Abrir(COPIAS[3, 1]); asm = (AssemblyDoc)m; cs = ((object[])asm.GetComponents(true)).Cast<Component2>().ToList();
        var nema = cs.First(c => c.Name2.StartsWith("CB-505")); var gn = Caja(nema);
        Borrar(m, cs.Where(c => c == nema || (c.Name2.StartsWith("HW_M3x6") && Cerca(Caja(c), gn, 4))));
        Guardar(m);
        // herramienta: sin los dos servos
        m = Abrir(COPIAS[5, 1]); asm = (AssemblyDoc)m; cs = ((object[])asm.GetComponents(true)).Cast<Component2>().ToList();
        Borrar(m, cs.Where(c => c.Name2.StartsWith("CB-610") || c.Name2.StartsWith("CB-611")));
        Guardar(m);
    }

    // J2: la concéntrica que usaba el buje del 5840 se rehace entre el eje de salida (CB-506, en E2) y el imán del AS5600 (CB-504, en la cintura)
    static void Relaciones()
    {
        var m = Abrir(COPIAS[0, 1]); var asm = (AssemblyDoc)m;
        // pose guardada (la de CB-000): se guarda antes de tocar las relaciones y se vuelve a poner al final
        var pose = ((object[])asm.GetComponents(true)).Cast<Component2>().ToDictionary(c => c.Name2, c => (MathTransform)c.Transform2);
        Feature malo = null;
        for (var f = (Feature)m.FirstFeature(); f != null; f = (Feature)f.GetNextFeature())
            if (f.GetTypeName2() == "MateGroup")
                for (var s = (Feature)f.GetFirstSubFeature(); s != null; s = (Feature)s.GetNextSubFeature())
                    if (s.Name == "J2_eje_hombro") malo = s;
        if (malo != null) { m.ClearSelection2(true); malo.Select2(false, 0); Console.WriteLine("borrada J2_eje_hombro: " + m.Extension.DeleteSelection2(0)); }
        var cs = ((object[])asm.GetComponents(false)).Cast<Component2>().ToList();
        var eje = cs.First(c => c.Name2.Split('/').Last().StartsWith("CB-506")); var iman = cs.First(c => c.Name2.Split('/').Last().StartsWith("CB-504"));
        var e2 = Xf.FromComp(cs.First(c => c.Name2.Split('/').Last().StartsWith("CB-301")));
        var pJ2 = e2.Apply(new[] { -80.0, 0, 0 }); var q2 = e2.Apply(new[] { -80.0, 0, 1 }); var uJ2 = new[] { q2[0] - pJ2[0], q2[1] - pJ2[1], q2[2] - pJ2[2] };
        var f1 = Cilindro(eje, 4.0, pJ2, uJ2); var f2 = Cilindro(iman, 3.0, pJ2, uJ2);
        m.ClearSelection2(true);
        bool s1 = ((Entity)f1).Select4(true, null), s2 = ((Entity)f2).Select4(true, null);
        int err;
        var mate = (Feature)asm.AddMate5((int)swMateType_e.swMateCONCENTRIC, (int)swMateAlign_e.swMateAlignCLOSEST, false, 0, 0, 0, 0, 0, 0, 0, 0, false, false, 0, out err);
        if (mate == null) { Console.WriteLine("no se pudo crear la concéntrica de J2, err " + err + " sel " + s1 + s2); }
        else { mate.Name = "J2_eje_hombro_iman"; Console.WriteLine("J2_eje_hombro_iman creada (err " + err + ")"); }
        m.ClearSelection2(true);
        foreach (Component2 c in (object[])asm.GetComponents(true)) if (!c.IsFixed() && pose.ContainsKey(c.Name2)) c.Transform2 = pose[c.Name2];
        m.EditRebuild3();
        for (var f = (Feature)m.FirstFeature(); f != null; f = (Feature)f.GetNextFeature())
            if (f.GetTypeName2() == "MateGroup")
                for (var s = (Feature)f.GetFirstSubFeature(); s != null; s = (Feature)s.GetNextSubFeature())
                { bool w; Console.WriteLine("  " + s.Name + " err " + s.GetErrorCode2(out w) + (w ? " (aviso)" : "")); }
        m.Extension.CustomPropertyManager[""].Add3("Descripcion", (int)swCustomInfoType_e.swCustomInfoText,
            "Brazo CitriBot solo (sin base de madera, electronica, camaras ni motores): copia de CB-000 con los subconjuntos S de 13_Brazo_Solo", 2);
        Guardar(m);
    }
    // cara cilíndrica de radio r (mm) del componente que está sobre el eje (p, u) del ensamblaje: menor dispersión de distancias al eje
    static Face2 Cilindro(Component2 c, double r, double[] p, double[] u)
    {
        object bi; Face2 mejor = null; double mejorStd = 1e9; var xc = Xf.FromComp(c);
        foreach (Body2 b in (object[])c.GetBodies3((int)swBodyType_e.swSolidBody, out bi))
            foreach (Face2 f in (object[])b.GetFaces())
            {
                var s = (Surface)f.GetSurface(); if (!s.IsCylinder()) continue;
                var t = f.GetTessTriangles(true) as float[]; if (t == null || t.Length < 9) continue;
                foreach (bool transformar in new[] { false, true })
                {
                    var ds = new List<double>();
                    for (int i = 0; i + 2 < t.Length; i += 3)
                    {
                        var q = new double[] { t[i] * 1000, t[i + 1] * 1000, t[i + 2] * 1000 };
                        if (transformar) q = xc.Apply(q);
                        var w = new[] { q[0] - p[0], q[1] - p[1], q[2] - p[2] }; double k = w[0] * u[0] + w[1] * u[1] + w[2] * u[2];
                        ds.Add(Math.Sqrt(Math.Max(0, w[0] * w[0] + w[1] * w[1] + w[2] * w[2] - k * k)));
                    }
                    double mean = ds.Average(), std = Math.Sqrt(ds.Select(d => (d - mean) * (d - mean)).Average());
                    if (Math.Abs(mean - r) < 0.6 && std < mejorStd) { mejor = f; mejorStd = std; }
                }
            }
        if (mejor == null) throw new Exception("sin cara cilíndrica de radio " + r + " sobre el eje en " + c.Name2);
        Console.WriteLine(string.Format("{0}: cara cilíndrica sobre el eje J2 (dispersión {1:0.000} mm, área {2:0} mm2)", c.Name2.Split('/').Last(), mejorStd, mejor.GetArea() * 1e6));
        return mejor;
    }

    // SR0 pasa del tablero (CB-111, que no está en el brazo solo) a la placa fija de J1 (CB-121)
    static void Sr0()
    {
        var m = Abrir(COPIAS[0, 1]); var asm = (AssemblyDoc)m;
        var c121 = ((object[])asm.GetComponents(false)).Cast<Component2>().First(c => c.Name2.Split('/').Last().StartsWith("CB-121"));
        var x = Xf.FromComp(c121); var inv = x.Inv(); string path = c121.GetPathName();
        SW.App.CloseDoc(m.GetTitle());
        var o = inv.Apply(new[] { 0.0, 0, 0 }); var o1 = inv.Apply(new[] { 1.0, 0, 0 }); var o3 = inv.Apply(new[] { 0.0, 0, 1 });
        var ex = new[] { o1[0] - o[0], o1[1] - o[1], o1[2] - o[2] }; var ez = new[] { o3[0] - o[0], o3[1] - o[1], o3[2] - o[2] };
        var ey = new[] { ez[1] * ex[2] - ez[2] * ex[1], ez[2] * ex[0] - ez[0] * ex[2], ez[0] * ex[1] - ez[1] * ex[0] };
        double[,] Rm = { { ex[0], ey[0], ez[0] }, { ex[1], ey[1], ez[1] }, { ex[2], ey[2], ez[2] } };
        double ax, ay, az;
        if (Math.Abs(Rm[0, 2]) > 0.99999) { double sg = Math.Sign(Rm[0, 2]); ay = sg * Math.PI / 2; az = 0; ax = Math.Atan2(sg * Rm[1, 0], Rm[1, 1]); }
        else { ay = Math.Asin(Rm[0, 2]); ax = Math.Atan2(-Rm[1, 2], Rm[2, 2]); az = Math.Atan2(-Rm[0, 1], Rm[0, 0]); }
        foreach (var it in new[] { Tuple.Create(path, true), Tuple.Create(P("01_Base_J0\\CB-111_Tablero_Madera_550x550x16.SLDPRT"), false) })
        {
            bool yaAbierto = SW.App.GetOpenDocumentByName(it.Item1) != null;
            var p = SW.Open(it.Item1);
            var old = (Feature)((PartDoc)p).FeatureByName("SR0_base_eje_J1");
            if (old != null) { p.ClearSelection2(true); old.Select2(false, 0); p.EditDelete(); Console.WriteLine("SR0 borrado de " + Path.GetFileName(it.Item1)); }
            if (it.Item2)
            {
                var f = (Feature)p.FeatureManager.CreateCoordinateSystemUsingNumericalValues(true, o[0] / 1000, o[1] / 1000, o[2] / 1000, true, ax, ay, az);
                f.Name = "SR0_base_eje_J1";
                var t = (double[])((MathTransform)p.Extension.GetCoordinateSystemTransformByName("SR0_base_eje_J1")).ArrayData;
                double e1 = 0; for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) e1 += Math.Abs(t[i * 3 + j] - Rm[j, i]);
                double eo = Math.Abs(t[9] * 1000 - o[0]) + Math.Abs(t[10] * 1000 - o[1]) + Math.Abs(t[11] * 1000 - o[2]);
                Console.WriteLine(string.Format("SR0 creado en {0}: error ejes {1:0.000}, origen {2:0.000} mm", Path.GetFileName(it.Item1), e1, eo));
            }
            int e = 0, w = 0; p.ForceRebuild3(false);
            Console.WriteLine("  guardado " + p.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w));
            if (!yaAbierto) SW.App.CloseDoc(p.GetTitle());
        }
    }
}
