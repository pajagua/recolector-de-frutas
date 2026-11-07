using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Uniones.exe <root> [rodillos|plato] (v7.10, 2026-11-06)
// rodillos: en la placa fija de J1 (CB-121) faltaban los agujeros de los ejes M5 de los 4 rodillos diagonales de la lazy susan
//           (los tornillos atravesaban 8.8 mm de plástico macizo). Se agregan agujeros Ø5.3 radiales sobre cada eje, de r = 44 a 70.
// plato:    el rebaje de la cabeza del M8 x 65 en el plato 1M1B (CB-201) era Ø13.5 y la cabeza hexagonal mide 15 entre puntas:
//           se agranda a Ø16 x 5.5.
class Uniones
{
    static string R;
    static string P(string rel) { return Path.Combine(R, rel); }
    static void Main(string[] a)
    {
        R = a[0]; SW.Connect();
        foreach (var k in a.Skip(1)) { if (k == "rodillos") Rodillos(); if (k == "plato") Plato(); }
    }
    static ModelDoc2 Abrir(string rel, out bool ya) { ya = SW.App.GetOpenDocumentByName(P(rel)) != null; var m = SW.Open(P(rel)); int er = 0; SW.App.ActivateDoc3(m.GetTitle(), false, 0, ref er); return m; }
    static void Guardar(ModelDoc2 m, bool ya)
    {
        int e = 0, w = 0; m.ForceRebuild3(false);
        Console.WriteLine("guardado " + m.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w) + " " + m.GetTitle());
        if (!ya) SW.App.CloseDoc(m.GetTitle());
    }
    static Feature Plano(ModelDoc2 m, int eje)   // plano de referencia de la pieza cuya normal es el eje 0=X, 1=Y, 2=Z
    {
        for (var f = (Feature)m.FirstFeature(); f != null; f = (Feature)f.GetNextFeature())
            if (f.GetTypeName2() == "RefPlane")
            {
                var t = (double[])((MathTransform)((RefPlane)f.GetSpecificFeature2()).Transform).ArrayData;
                if (Math.Abs(t[6 + eje]) > 0.9) return f;
            }
        return null;
    }

    static void Rodillos()
    {
        // ejes de los rodillos en coordenadas de CB-121, sacados del subconjunto CB-120
        bool yaA; var asmDoc = Abrir("01_Base_J0\\CB-120_LazySusan_Fija_J1.SLDASM", out yaA); var asm = (AssemblyDoc)asmDoc;
        asm.ResolveAllLightWeightComponents(false);
        var cs = ((object[])asm.GetComponents(false)).Cast<Component2>().ToList();
        var placa = cs.First(c => c.Name2.Split('/').Last().StartsWith("CB-121")); var inv = Xf.FromComp(placa).Inv();
        var ejes = new List<double[]>();
        foreach (var c in cs.Where(c => c.Name2.Split('/').Last().StartsWith("HW_Rodamiento_625ZZ")))
        {
            var b = (double[])c.GetBox(false, false);
            var p = inv.Apply(new[] { (b[0] + b[3]) * 500, (b[1] + b[4]) * 500, (b[2] + b[5]) * 500 });
            double r = Math.Sqrt(p[0] * p[0] + p[2] * p[2]);
            bool diag = Math.Abs(Math.Abs(p[0]) - Math.Abs(p[2])) < 5;
            Console.WriteLine(string.Format("rodillo en CB-121: ({0:0.0}, {1:0.0}, {2:0.0}) r {3:0.0} {4}", p[0], p[1], p[2], r, diag ? "diagonal" : ""));
            if (diag) ejes.Add(p);
        }
        if (!yaA) SW.App.CloseDoc(asmDoc.GetTitle());

        bool ya; var m = Abrir("01_Base_J0\\CB-121_Placa_Fija_J1_1M2A.SLDPRT", out ya); PB.Use(m);
        if (((PartDoc)m).FeatureByName("CB_v710_Ejes_rodillos_diagonales_1") != null) { Console.WriteLine("CB-121 ya tiene los agujeros"); if (!ya) SW.App.CloseDoc(m.GetTitle()); return; }
        PB.Report("CB-121 antes");
        // eje de la placa (Y) = intersección de los planos con normal X y normal Z
        m.ClearSelection2(true); Plano(m, 0).Select2(false, 0); Plano(m, 2).Select2(true, 0);
        m.InsertAxis2(true); var ejeY = (Feature)m.Extension.GetLastFeatureAdded(); ejeY.Name = "CB_v710_Eje_placa";
        int n = 0;
        foreach (var ang in new[] { 45.0, 135.0 })
        {
            m.ClearSelection2(true); Plano(m, 0).Select2(false, 0); ejeY.Select2(true, 1);
            var pl = (Feature)m.FeatureManager.InsertRefPlane((int)swRefPlaneReferenceConstraints_e.swRefPlaneReferenceConstraint_Angle, ang * Math.PI / 180,
                                                             (int)swRefPlaneReferenceConstraints_e.swRefPlaneReferenceConstraint_Coincident, 0, 0, 0);
            if (pl == null) throw new Exception("no se pudo crear el plano a " + ang);
            pl.Name = "CB_v710_Plano_rodillos_" + ang.ToString("0");
            var t = (double[])((MathTransform)((RefPlane)pl.GetSpecificFeature2()).Transform).ArrayData;
            var nrm = new[] { t[6], t[7], t[8] };
            foreach (var e in ejes)
            {
                double r = Math.Sqrt(e[0] * e[0] + e[2] * e[2]); var d = new[] { e[0] / r, 0, e[2] / r };
                double s = d[0] * nrm[0] + d[2] * nrm[2];
                if (Math.Abs(Math.Abs(s) - 1) > 0.01) continue;     // este eje no es normal a este plano
                m.ClearSelection2(true); pl.Select2(false, 0); m.SketchManager.InsertSketch(true);
                var sk = (Sketch)m.SketchManager.ActiveSketch; var m2s = (MathTransform)sk.ModelToSketchTransform;
                var mu = (MathUtility)SW.App.GetMathUtility();
                var q = (double[])((MathPoint)((MathPoint)mu.CreatePoint(new[] { 0.0, e[1] / 1000, 0.0 })).MultiplyTransform(m2s)).ArrayData;
                m.SketchManager.CreateCircleByRadius(q[0], q[1], 0, 5.3 / 2000);
                // de r = 44 a r = 70 en el sentido del eje (s = +1 sentido de la normal del plano)
                var f = s > 0 ? PB.Extrude(44, 26, true) : PB.Extrude(-70, 26, true);
                f.Name = "CB_v710_Ejes_rodillos_diagonales_" + (++n);
                Console.WriteLine(string.Format("  agujero Ø5.3 en el eje ({0:0.0}, {1:0.0}) altura {2:0.0}", d[0], d[2], e[1]));
            }
            pl.Select2(false, 0); m.BlankRefGeom(); m.ClearSelection2(true);
        }
        ejeY.Select2(false, 0); m.BlankRefGeom(); m.ClearSelection2(true);
        PB.Report("CB-121 despues");
        Guardar(m, ya);
    }

    static void Plato()
    {
        bool yaA; var asmDoc = Abrir("02_Cintura_J1\\CB-200_Cintura_J1.SLDASM", out yaA); var asm = (AssemblyDoc)asmDoc; asm.ResolveAllLightWeightComponents(false);
        var cs = ((object[])asm.GetComponents(false)).Cast<Component2>().ToList();
        var plato = cs.First(c => c.Name2.Split('/').Last().StartsWith("CB-201")); var inv = Xf.FromComp(plato).Inv();
        var tor = cs.First(c => c.Name2.Split('/').Last().StartsWith("HW_M8x65")); var bt = (double[])tor.GetBox(false, false);
        var pb = (double[])plato.GetBox(false, false);
        // cara del plato del lado de la cabeza (arriba) y eje del tornillo, en coordenadas de CB-201
        double cx = (bt[0] + bt[3]) * 500, cy = (bt[1] + bt[4]) * 500, ztop = pb[5] * 1000;
        var a0 = inv.Apply(new[] { cx, cy, ztop }); var a1 = inv.Apply(new[] { cx, cy, ztop - 5.5 });
        Console.WriteLine(string.Format("plato: cara superior en z {0:0.0}; en CB-201 ({1:0.0},{2:0.0},{3:0.0}) -> ({4:0.0},{5:0.0},{6:0.0})", ztop, a0[0], a0[1], a0[2], a1[0], a1[1], a1[2]));
        if (!yaA) SW.App.CloseDoc(asmDoc.GetTitle());
        bool ya; var m = Abrir("02_Cintura_J1\\CB-201_Plato_Giratorio_Polea_J1_1M1B.SLDPRT", out ya); PB.Use(m);
        if (((PartDoc)m).FeatureByName("CB_v710_Rebaje_cabeza_M8_D16") != null) { Console.WriteLine("CB-201 ya modificado"); if (!ya) SW.App.CloseDoc(m.GetTitle()); return; }
        PB.Report("CB-201 antes");
        // el eje del plato es Y en la pieza: boceto en el plano normal a Y
        // PB no encontró el plano normal a Y en esta pieza importada: se lo damos
        var fld = typeof(PB).GetField("planes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var arr = (Feature[])fld.GetValue(null); for (int i = 0; i < 3; i++) if (arr[i] == null) arr[i] = Plano(m, i);
        PB.Sketch('T'); PB.Circle(a0[0], a0[2], 16);
        double y0 = Math.Min(a0[1], a1[1]), y1 = Math.Max(a0[1], a1[1]);
        var fe = PB.Extrude(y0 - (a0[1] < a1[1] ? 0.5 : 0), (y1 - y0) + 0.5, true, "CB_v710_Rebaje_cabeza_M8_D16");
        PB.Report("CB-201 despues");
        Guardar(m, ya);
    }
}
