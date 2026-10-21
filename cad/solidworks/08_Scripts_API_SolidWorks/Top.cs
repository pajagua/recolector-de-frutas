using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Top.exe <root> : builds CB-000 general assembly with kinematic mates J1 (revolute), J2 (revolute), J3 (prismatic, limited)
class Top
{
    static string R;
    static ModelDoc2 M; static AssemblyDoc A;
    static string P(string rel) { return Path.Combine(R, rel); }

    static Component2 Add(string path, bool fix)
    {
        int e = 0, w = 0;
        SW.App.DocumentVisible(false, (int)swDocumentTypes_e.swDocASSEMBLY);
        SW.App.OpenDoc6(path, (int)swDocumentTypes_e.swDocASSEMBLY, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref e, ref w);
        SW.App.DocumentVisible(true, (int)swDocumentTypes_e.swDocASSEMBLY);
        var c = (Component2)A.AddComponent5(path, 0, "", false, "", 0, 0, 0);
        c.Transform2 = new Xf().ToMath();
        if (fix) { M.ClearSelection2(true); c.Select4(false, null, false); A.FixComponent(); }
        M.ClearSelection2(true);
        return c;
    }

    static Component2 Child(Component2 parent, string fileStart)
    {
        foreach (Component2 c in (object[])parent.GetChildren())
            if (Path.GetFileName(c.GetPathName()).StartsWith(fileStart, StringComparison.OrdinalIgnoreCase)) return c;
        foreach (Component2 c in (object[])parent.GetChildren())
        {
            var ch = c.GetChildren() as object[];
            if (ch != null && ch.Length > 0) { var r = Child(c, fileStart); if (r != null) return r; }
        }
        return null;
    }

    // find a face of a component's part (part coordinates) and return the assembly-context entity
    static Entity FindFace(Component2 comp, Func<Face2, Surface, bool> pred, string what)
    {
        var part = (PartDoc)comp.GetModelDoc2();
        foreach (Body2 b in (object[])part.GetBodies2((int)swBodyType_e.swSolidBody, true))
            foreach (Face2 f in (object[])b.GetFaces())
            {
                var s = (Surface)f.GetSurface();
                if (pred(f, s))
                {
                    var ent = (Entity)comp.GetCorrespondingEntity(f);
                    if (ent == null) throw new Exception("no corresponding entity for " + what);
                    Console.WriteLine("  face ok: " + what);
                    return ent;
                }
            }
        throw new Exception("face not found: " + what);
    }
    static bool Near(double a, double b, double tol = 0.05) { return Math.Abs(a - b) < tol; }
    static Func<Face2, Surface, bool> Cyl(double radius, int axis, double c1, double c2)
    {
        // axis 0=X (c1=y,c2=z), 1=Y (c1=x,c2=z), 2=Z (c1=x,c2=y) ; values in mm, part coordinates
        return (f, s) =>
        {
            if (!s.IsCylinder()) return false;
            double[] p = (double[])s.CylinderParams;
            if (!Near(p[6] * 1000, radius, 0.02)) return false;
            double[] ax = { p[3], p[4], p[5] };
            if (Math.Abs(Math.Abs(ax[axis]) - 1) > 1e-6) return false;
            double[] o = { p[0] * 1000, p[1] * 1000, p[2] * 1000 };
            int i1 = axis == 0 ? 1 : 0, i2 = axis == 2 ? 1 : 2;
            return Near(o[i1], c1) && Near(o[i2], c2);
        };
    }
    static Func<Face2, Surface, bool> Pln(int axis, double sign, double at, double areaMin, double areaMax)
    {
        return (f, s) =>
        {
            if (!s.IsPlane()) return false;
            double[] n = (double[])f.Normal;
            if (Math.Abs(n[axis] - sign) > 1e-6) return false;
            double[] bx = (double[])f.GetBox();
            if (!Near(bx[axis] * 1000, at, 0.02) || !Near(bx[axis + 3] * 1000, at, 0.02)) return false;
            double a = f.GetArea() * 1e6;
            return a >= areaMin && a <= areaMax;
        };
    }

    static Feature TopPlane(Component2 c)
    {
        Feature f = (Feature)c.FirstFeature();
        while (f != null)
        {
            if (f.GetTypeName2() == "RefPlane")
            {
                var rp = (RefPlane)f.GetSpecificFeature2();
                double[] t = (double[])((MathTransform)rp.Transform).ArrayData;
                if (Math.Abs(Math.Abs(t[8]) - 1) < 1e-6) return f;   // plane normal to Z (J1 axis, Z-up)
            }
            f = (Feature)f.GetNextFeature();
        }
        throw new Exception("no top plane in " + c.Name2);
    }

    static Feature Mate(string name, int type, int align, object e1, object e2, double dist = 0, double dmax = 0, double dmin = 0)
    {
        M.ClearSelection2(true);
        var sd = (SelectData)((SelectionMgr)M.SelectionManager).CreateSelectData();
        sd.Mark = 1;
        bool s1 = e1 is Entity ? ((Entity)e1).Select4(true, sd) : ((Feature)e1).Select2(true, 1);
        bool s2 = e2 is Entity ? ((Entity)e2).Select4(true, sd) : ((Feature)e2).Select2(true, 1);
        int err;
        var mt = (Mate2)A.AddMate5(type, align, false, dist / 1000, dmax / 1000, dmin / 1000, 0, 0, 0, 0, 0, false, false, 0, out err);
        Console.WriteLine("mate " + name + ": sel " + s1 + "/" + s2 + " err=" + err + (mt == null ? " FAILED" : ""));
        M.ClearSelection2(true);
        if (mt == null) return null;
        // rename the mate feature (last feature added in the mate group)
        Feature mg = null; Feature f = (Feature)M.FirstFeature();
        while (f != null) { if (f.GetTypeName2() == "MateGroup") mg = f; f = (Feature)f.GetNextFeature(); }
        Feature last = null; Feature sf = (Feature)mg.GetFirstSubFeature();
        while (sf != null) { last = sf; sf = (Feature)sf.GetNextSubFeature(); }
        if (last != null) last.Name = name;
        return last;
    }

    static void Main(string[] args)
    {
        R = args[0];
        SW.Connect();
        SW.CloseMine();
        string tpl = SW.App.GetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swDefaultTemplateAssembly);
        M = (ModelDoc2)SW.App.NewDocument(tpl, 0, 0, 0);
        A = (AssemblyDoc)M;
        SW.Created.Add(M.GetTitle());
        var c100 = Add(P(@"01_Base_J0\CB-100_Base_J0.SLDASM"), true);
        var c200 = Add(P(@"02_Cintura_J1\CB-200_Cintura_J1.SLDASM"), false);
        var c300 = Add(P(@"03_Hombro_E2_J2\CB-300_Hombro_E2_J2.SLDASM"), false);
        var c400 = Add(P(@"04_Prismatico_E3_J3\CB-400_Prismatico_E3_J3.SLDASM"), false);
        A.ResolveAllLightWeightComponents(false);

        int CONC = (int)swMateType_e.swMateCONCENTRIC, COIN = (int)swMateType_e.swMateCOINCIDENT, PAR = (int)swMateType_e.swMatePARALLEL,
            DIST = (int)swMateType_e.swMateDISTANCE, CL = (int)swMateAlign_e.swMateAlignCLOSEST;

        // ---- J1: cintura (revoluta, eje vertical) ----
        Mate("J1_plano_base", COIN, CL, TopPlane(c200), TopPlane(c100));
        var plato = Child(c200, "CB-201");
        var rod608 = Child(c100, "HW_Rodamiento_608ZZ");
        var fPlato = FindFace(plato, Cyl(5.5, 1, 0, 0), "1M1B agujero central D11");
        var f608 = FindFace(rod608, Cyl(4.0, 0, 0, 0), "608ZZ agujero");
        Mate("J1_eje_cintura", CONC, CL, fPlato, f608);

        // ---- J2: hombro (revoluta, eje del 5840) ----
        var mot = Child(c200, "CB-502");
        var eje = Child(c300, "CB-506");
        var torre = Child(c200, "CB-210");
        var e2 = Child(c300, "CB-301");
        Mate("J2_eje_hombro", CONC, CL, FindFace(mot, Cyl(9.3, 2, 0, 0), "buje D18.6 caja 5840"), FindFace(eje, Cyl(4.0, 2, -80, 0), "eje salida D8"));
        Mate("J2_cara_hombro", COIN, CL,
            FindFace(torre, Pln(1, -1, 173.2, 500, 1200), "aro D56 torre (y=173.2)"),
            FindFace(e2, Pln(2, -1, -35, 1300, 1500), "cara E2 z=-35"));

        // ---- J3: prismatico (E3 dentro del canal de E2) ----
        var e3 = Child(c400, "CB-401");
        var tsf = Child(c300, "CB-302");
        Mate("J3_eje_prismatico", CONC, CL, FindFace(e3, Cyl(10.2, 2, -120.245, 238.221), "E3 agujero D20.4"), FindFace(tsf, Cyl(5.0, 2, 0, 0), "TSF D10"));
        Mate("J3_antigiro", PAR, CL, FindFace(e3, Pln(0, 1, -97.545, 6000, 7000), "E3 cara lateral"), FindFace(e2, Pln(2, -1, 23, 8000, 9000), "E2 canal z=23"));
        Mate("J3_limite_carrera", DIST, CL, FindFace(e3, Pln(2, 1, -405.516, 5000, 6500), "E3 cara interior placa"),
            FindFace(e2, Pln(0, 1, -34, 2000, 2500), "E2 fondo canal x=-34"), 186.5, 216.0, 162.5);

        M.EditRebuild3();
        var cpm = M.Extension.CustomPropertyManager[""];
        cpm.Add3("Descripcion", (int)swCustomInfoType_e.swCustomInfoText, "CitriBot - ensamblaje general (base BCN3D Moveo + torre 2M1D mod + brazo Rx-rcp-003), sin herramienta", 2);
        M.ShowNamedView2("*Isometric", -1);
        M.ViewZoomtofit2();
        int e = 0, w = 0;
        string outp = P("CB-000_CitriBot_Ensamblaje_General.SLDASM");
        bool ok = M.Extension.SaveAs3(outp, 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, null, ref e, ref w);
        Console.WriteLine((ok ? "saved " : "SAVE FAIL ") + outp + " e=" + e + " w=" + w);
    }
}
