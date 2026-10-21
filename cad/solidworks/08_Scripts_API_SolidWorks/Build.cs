using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

class AB
{
    public AssemblyDoc A; public ModelDoc2 M;
    // the user's templates are Z-up; BCN3D / Rx-rcp data is Y-up -> rotate +90 deg about X (Y -> Z)
    public static readonly Xf ZUp = Xf.Rows(new double[] { 1, 0, 0 }, new double[] { 0, 0, 1 }, new double[] { 0, -1, 0 }, new double[] { 0, 0, 0 });
    public Xf World = new Xf();
    public static AB New()
    {
        string tpl = SW.App.GetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swDefaultTemplateAssembly);
        var ab = new AB();
        ab.M = (ModelDoc2)SW.App.NewDocument(tpl, 0, 0, 0);
        ab.A = (AssemblyDoc)ab.M;
        SW.Created.Add(ab.M.GetTitle());
        return ab;
    }
    public Component2 Add(string path, Xf x, bool fix = true)
    {
        int type = path.ToUpper().EndsWith(".SLDASM") ? (int)swDocumentTypes_e.swDocASSEMBLY : (int)swDocumentTypes_e.swDocPART;
        int e = 0, w = 0;
        SW.App.DocumentVisible(false, type);
        var d = SW.App.OpenDoc6(path, type, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref e, ref w);
        SW.App.DocumentVisible(true, type);
        if (d == null) throw new Exception("cannot open " + path + " e=" + e);
        var c = (Component2)A.AddComponent5(path, (int)swAddComponentConfigOptions_e.swAddComponentConfigOptions_CurrentSelectedConfig, "", false, "", 0, 0, 0);
        if (c == null) throw new Exception("AddComponent failed " + path);
        c.Transform2 = x.Then(World).ToMath();
        if (fix)
        {
            M.ClearSelection2(true);
            c.Select4(false, null, false);
            A.FixComponent();
            M.ClearSelection2(true);
        }
        return c;
    }
    public void Save(string path)
    {
        M.EditRebuild3();
        M.ShowNamedView2("*Isometric", -1);
        M.ViewZoomtofit2();
        int e = 0, w = 0;
        bool ok = M.Extension.SaveAs3(path, 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, null, ref e, ref w);
        Console.WriteLine((ok ? "saved " : "SAVE FAIL ") + path + " e=" + e + " w=" + w);
    }
}

class Build
{
    static string R;
    static string P(string rel) { return Path.Combine(R, rel); }
    static Dictionary<string, Xf> mv = new Dictionary<string, Xf>();
    static Xf G(string moveoName) { return mv[moveoName].Then(Xf.Tr(0.007, 48.856, -242.5)); }   // Moveo -> global (J1 axis at origin, board top y=0)

    static double[] V(params double[] v) { return v; }

    // ---- poses (global, home) ----
    static Xf Mod, Torre, Motor5840, E2, MovE1toTorre;

    static void Main(string[] args)
    {
        R = args[0];
        string only = args.Length > 1 ? args[1] : "all";
        SW.Connect();
        SW.CloseMine();

        // 1) read BCN3D Moveo top-level transforms
        var mo = SW.Open(@"C:\Users\tikip\Desktop\BCN3D-Moveo-master\CAD files\BCN3D Moveo assembly.SLDASM");
        foreach (Component2 c in (object[])((AssemblyDoc)mo).GetComponents(true)) mv[c.Name2] = Xf.FromComp(c);
        SW.CloseMine();
        Console.WriteLine("Moveo comps: " + mv.Count);

        // 2) poses
        // J1 motor module frame = 1M3 frame shifted to the shaft (1M3 coords (-0.298,-34.058,75.3))
        var m1m3 = G("1M3-1");
        Mod = Xf.Tr(-0.298, -34.058, 75.3).Then(m1m3);
        Mod.T[1] = 0.0;   // sit exactly on the board top
        Console.WriteLine("Modulo J1: " + Mod);
        // tower (CB-210) : base bottom center (-73.786,221.2,-86.723) on 1M1B top center, X -> +Y, -Y_2 -> middle hole direction of a triple
        var c1m1b = G("1M1B-1").T;           // 1M1B center (global)
        var h2 = G("M4x45-2").T;              // middle hole of a triple
        double dx = h2[0] - c1m1b[0], dz = h2[2] - c1m1b[2], dn = Math.Sqrt(dx * dx + dz * dz);
        dx /= dn; dz /= dn;
        var rot = Xf.Rows(V(0, 1, 0), V(-dx, 0, -dz), V(-dz, 0, dx), V(0, 0, 0)); // Z = X x Y
        var p0 = rot.Apply(V(-73.786, 221.2, -86.723));
        double ytop = G("M4x45-2").T[1] - 22.0;   // 2M1D flange is 22 thick under the M4x45 heads
        Torre = rot.Then(Xf.Tr(c1m1b[0] - p0[0], ytop - p0[1], c1m1b[2] - p0[2]));
        Console.WriteLine("Torre: " + Torre + "  (1M1B top y=" + ytop.ToString("0.###") + ", hole radius " + dn.ToString("0.###") + ")");
        // shoulder motor in tower coords
        Motor5840 = Xf.Rows(V(0, 0, -1), V(1, 0, 0), V(0, -1, 0), V(190.171, 193.2, -86.723)).Then(Torre);
        // E1 (Rx-rcp) -> tower coords, then E2
        MovE1toTorre = Xf.Rows(V(1, 0, 0), V(0, 0, 1), V(0, -1, 0), V(124.171, 221.2, -86.723));
        var e2inE1 = Xf.Rows(V(0.6171003291873941, -0.78688447927050276, 0), V(0.78688447927050253, 0.61710032918739421, 0), V(0, 0, 1),
                             V(115.36802633499194, -62.950758341640977, 83.000000000000057));
        E2 = e2inE1.Then(MovE1toTorre).Then(Torre);
        Console.WriteLine("E2: " + E2);

        Directory.CreateDirectory(P("00_Ensamblaje_General"));
        if (only == "all" || only == "110") Build110();
        if (only == "all" || only == "130") Build130();
        if (only == "all" || only == "140") Build140();
        if (only == "all" || only == "100") Build100();
        if (only == "all" || only == "200") Build200();
        if (only == "all" || only == "300") Build300();
        if (only == "all" || only == "400") Build400();
        SW.CloseMine();
    }

    static readonly string HW = @"05_Comerciales\Tornilleria\";

    static void Build130()
    {
        var ab = AB.New();
        ab.World = AB.ZUp;
        var I = new Xf();
        ab.Add(P(@"01_Base_J0\CB-131_Soporte_Motor_J1_37D.SLDPRT"), I);
        ab.Add(P(@"05_Comerciales\CB-501_Motor_37D_90-1_Encoder64CPR.SLDPRT"), Xf.Tr(0, 55, 0));
        ab.Add(P(@"05_Comerciales\HW_Polea_T5_10T_eje6.SLDPRT"), Xf.Rows(V(0, 1, 0), V(-1, 0, 0), V(0, 0, 1), V(0, 61.5, 0)));
        foreach (double a in new[] { 30, 90, 150, 210, 270, 330 })
        {
            double t = a * Math.PI / 180;
            ab.Add(P(HW + "HW_M3x8.SLDPRT"), Xf.Rows(V(1, 0, 0), V(0, -1, 0), V(0, 0, -1), V(7 + 15.5 * Math.Cos(t), 62.6, 15.5 * Math.Sin(t))));
        }
        ab.Save(P(@"01_Base_J0\CB-130_Modulo_Motor_J1.SLDASM"));
        SW.CloseMine();
    }

    // board + 4 legs of the BCN3D base (the BCN3D electronics box is replaced by CB-140)
    static void Build110()
    {
        var ab = AB.New();
        ab.World = AB.ZUp;
        var gb = G("Base assembly-1");
        ab.Add(P(@"01_Base_J0\CB-111_Tablero_Madera_550x550x16.SLDPRT"), Xf.Tr(-275.602, 1.335, 550).Then(gb));
        ab.Add(P(@"01_Base_J0\CB-112_Pata_Base.SLDPRT"), Xf.Rows(V(0, 0, -1), V(0, 1, 0), V(1, 0, 0), V(-215.602, -62.665, 565)).Then(gb));
        ab.Add(P(@"01_Base_J0\CB-112_Pata_Base.SLDPRT"), Xf.Tr(-290.602, -62.665, 60).Then(gb));
        ab.Add(P(@"01_Base_J0\CB-112_Pata_Base.SLDPRT"), Xf.Rows(V(0, 0, 1), V(0, 1, 0), V(-1, 0, 0), V(214.398, -62.665, -15)).Then(gb));
        ab.Add(P(@"01_Base_J0\CB-112_Pata_Base.SLDPRT"), Xf.Rows(V(-1, 0, 0), V(0, 1, 0), V(0, 0, -1), V(289.398, -62.665, 490)).Then(gb));
        ab.Save(P(@"01_Base_J0\CB-110_Base_Tablero_Patas.SLDASM"));
        SW.CloseMine();
    }

    // electronics tray pose: tray frame (origin bottom centre, Y up) where the BCN3D box was (Base assembly coords x=5.2, z=55.5)
    static Xf Pose140() { return Xf.Tr(5.2, 17.335, 55.5).Then(G("Base assembly-1")); }

    // electronics module, in the tray frame (Y-up content, file Z-up)
    static void Build140()
    {
        var ab = AB.New();
        ab.World = AB.ZUp;
        string E = @"05_Comerciales\Electronica\";
        ab.Add(P(@"01_Base_J0\CB-141_Bandeja_Electronica.SLDPRT"), new Xf());
        var mega = Xf.Tr(-42.06, 10, -2);
        ab.Add(P(E + "CB-507_Arduino_Mega_2560.SLDPRT"), mega);
        // RAMPS 1.4 (STEP model): long pin rows along its Z at x=-28.2/+20.3, 36-pin double row at z=-96 -> Mega rows / end header;
        // PCB bottom resting on the male-header plastic (2.5 mm) over the Mega female headers (top y=8.31)
        var ramps = Xf.Rows(V(0, 0, 1), V(0, 1, 0), V(-1, 0, 0), V(-48.67, 10.85, 2.0)).Then(mega);
        ab.Add(P(E + "CB-508_RAMPS_1.4.SLDPRT"), ramps);
        // A4988 in the driver socket whose rows are at x=-18.06/-5.29, z=-50.5..-30.0 (socket top y=10.51)
        ab.Add(P(E + "CB-509_Driver_A4988.SLDPRT"), Xf.Rows(V(1, 0, 0), V(0, 0, -1), V(0, 1, 0), V(-11.67, 13.05, -40.25)).Then(ramps));
        ab.Add(P(E + "CB-510_Sabertooth_2x60.SLDPRT"), Xf.Rows(V(1, 0, 0), V(0, 0, 1), V(0, -1, 0), V(18.4, 14, 1.9)));
        var down = Xf.Rows(V(1, 0, 0), V(0, -1, 0), V(0, 0, -1), V(0, 0, 0));   // BCN3D M3 screws: head top at origin, shank +Y
        double[][] mh = { new[] { -37.16, -25.16 }, new[] { -38.31, 25.13 }, new[] { 40.99, -25.16 }, new[] { 47.52, 25.13 }, new[] { 15.68, 19.85 } };
        foreach (var h in mh) ab.Add(P(HW + "HW_M3x8.SLDPRT"), down.Then(Xf.Tr(h[0] - 42.06, 16.41, h[1] - 2)));
        double[][] sh = { new[] { 23.0, -41.3 }, new[] { 93.1, -41.3 }, new[] { 23.0, 41.3 }, new[] { 93.1, 41.3 } };
        foreach (var h in sh) ab.Add(P(HW + "HW_M3x10.SLDPRT"), down.Then(Xf.Tr(h[0], 17.6, h[1])));
        // tray to the wooden board: M4x30 (head bearing at origin, shank -Y) + washer + locknut under the board (board bottom at y=-16)
        double[][] th = { new[] { -98.0, -30.0 }, new[] { -98.0, 30.0 }, new[] { 60.0, -40.0 }, new[] { 60.0, 40.0 } };
        foreach (var h in th)
        {
            ab.Add(P(HW + "HW_M4x30.SLDPRT"), Xf.Tr(h[0], 4, h[1]));
            ab.Add(P(HW + "HW_Arandela_M4.SLDPRT"), down.Then(Xf.Tr(h[0], -16.0, h[1])));
            ab.Add(P(HW + "HW_Tuerca_M4_Autoblocante.SLDPRT"), down.Then(Xf.Tr(h[0], -16.45, h[1])));
        }
        ab.Save(P(@"01_Base_J0\CB-140_Modulo_Electronica.SLDASM"));
        SW.CloseMine();
    }

    static void Build100()
    {
        var ab = AB.New();
        ab.World = AB.ZUp;
        ab.Add(P(@"01_Base_J0\CB-110_Base_Tablero_Patas.SLDASM"), AB.ZUp.Inv());          // content already global + Z-up
        ab.Add(P(@"01_Base_J0\CB-140_Modulo_Electronica.SLDASM"), AB.ZUp.Inv().Then(Pose140()));
        ab.Add(P(@"01_Base_J0\CB-120_LazySusan_Fija_J1.SLDASM"), G("Rotary Plate-1"));
        ab.Add(P(@"01_Base_J0\CB-130_Modulo_Motor_J1.SLDASM"), AB.ZUp.Inv().Then(Mod));   // module file is Z-up too
        var map = new Dictionary<string, string> {
            {"M4x40", "HW_M4x40"}, {"M4x30", "HW_M4x30"}, {"Volandera M4", "HW_Arandela_M4"}, {"M4 Locknut", "HW_Tuerca_M4_Autoblocante"},
            {"M5x30", "HW_M5x30"}, {"Volandera M5", "HW_Arandela_M5"}, {"M5 Lock", "HW_Tuerca_M5_Autoblocante"} };
        var skip = new HashSet<string> { "M4x30-7", "M4x30-8", "Volandera M4-7", "Volandera M4-3", "M4 Locknut-15", "M4 Locknut-12" };
        int n = 0;
        foreach (var kv in mv.OrderBy(k => k.Key))
        {
            string baseName = kv.Key.Substring(0, kv.Key.LastIndexOf('-'));
            if (!map.ContainsKey(baseName) || skip.Contains(kv.Key)) continue;
            var g = G(kv.Key);
            if (g.T[1] > 65) continue;          // only base-region hardware (link 1 hardware: M4 locknuts at y=76)
            ab.Add(P(HW + map[baseName] + ".SLDPRT"), g); n++;
        }
        Console.WriteLine("hardware base: " + n);
        ab.Save(P(@"01_Base_J0\CB-100_Base_J0.SLDASM"));
        SW.CloseMine();
    }

    static void Build200()
    {
        var ab = AB.New();
        ab.World = AB.ZUp;
        ab.Add(P(@"02_Cintura_J1\CB-201_Plato_Giratorio_Polea_J1_1M1B.SLDPRT"), G("1M1B-1"));
        ab.Add(P(@"02_Cintura_J1\CB-210_Torre_Cintura_2M1D_mod.SLDPRT"), Torre);
        ab.Add(P(@"02_Cintura_J1\CB-232_Soporte_Iman_J2.SLDPRT"), Torre);
        ab.Add(P(@"05_Comerciales\CB-502_Motor_Sinfin_5840-36ZY.SLDPRT"), Motor5840);
        foreach (var kv in mv.OrderBy(k => k.Key))
        {
            string b = kv.Key.Substring(0, kv.Key.LastIndexOf('-'));
            var g = G(kv.Key);
            if (b == "M4x45") ab.Add(P(HW + "HW_M4x45.SLDPRT"), g);
            else if (b == "M4 Locknut" && g.T[1] > 60) ab.Add(P(HW + "HW_Tuerca_M4_Autoblocante.SLDPRT"), g);
            else if (b == "M8x65") ab.Add(P(HW + "HW_M8x65.SLDPRT"), g);
            else if (b == "M8 Autoblocant") ab.Add(P(HW + "HW_Tuerca_M8_Autoblocante.SLDPRT"), g);
            else if (b == "Spacer M8x20") ab.Add(P(HW + "HW_Separador_M8x20.SLDPRT"), g);
        }
        double zc = -86.723;
        var down = Xf.Rows(V(1, 0, 0), V(0, -1, 0), V(0, 0, -1), V(0, 0, 0));   // screw shank along +Y of the tower
        foreach (double s in new[] { -13.85, 13.85 })
            ab.Add(P(HW + "HW_M6x10_Allen_DIN912.SLDPRT"), down.Then(Xf.Tr(161.171, 187.7, zc + s)).Then(Torre));
        foreach (double s in new[] { -9.0, 9.0 })
        {
            ab.Add(P(HW + "HW_M4x30.SLDPRT"), down.Then(Xf.Tr(82, 175.2, zc + s)).Then(Torre));
            ab.Add(P(HW + "HW_Tuerca_M4.SLDPRT"), Xf.Tr(82, 193.2, zc + s).Then(Torre));
        }
        // diametric magnet in the fixed holder, on the J2 axis, face toward E2 (y 91.7..94.2 in tower coords)
        ab.Add(P(@"05_Comerciales\CB-504_Iman_Diametral_D6x2.5.SLDPRT"), Xf.Rows(V(1, 0, 0), V(0, 0, -1), V(0, 1, 0), V(190.171, 91.7, zc)).Then(Torre));
        ab.Save(P(@"02_Cintura_J1\CB-200_Cintura_J1.SLDASM"));
        SW.CloseMine();
    }

    static void Build300()
    {
        var ab = AB.New();
        ab.World = AB.ZUp;
        ab.Add(P(@"03_Hombro_E2_J2\CB-301_Eslabon_E2_mod5840.SLDPRT"), E2);
        ab.Add(P(@"03_Hombro_E2_J2\CB-305_Buje_D8_Hombro.SLDPRT"), E2);
        ab.Add(P(@"05_Comerciales\CB-506_Eje_Salida_5840_D8.SLDPRT"), E2);
        // NEMA 14 (BCN3D model) in the E2 motor bore, face against the wall at x=-40, shaft toward the TSF screw
        ab.Add(P(@"05_Comerciales\CB-505_Motor_NEMA14_Prismatico.SLDPRT"), Xf.Rows(V(0, 1, 0), V(1, 0, 0), V(0, 0, -1), V(-75, 0, 0)).Then(E2));
        foreach (double a in new[] { -12.85, 12.85 }) foreach (double b in new[] { -12.85, 12.85 })
            ab.Add(P(HW + "HW_M3x6_Allen_DIN912.SLDPRT"), Xf.Rows(V(0, 1, 0), V(1, 0, 0), V(0, 0, -1), V(-37.2, a, b)).Then(E2));
        // TSF-E3 screw moved +4 mm (clears the NEMA boss), bored D5 for the NEMA shaft
        ab.Add(P(@"03_Hombro_E2_J2\CB-302_Tornillo_SinFin_E3_TSF.SLDPRT"), Xf.Rows(V(0, 1, 0), V(0, 0, -1), V(-1, 0, 0), V(-8.5, 0, 0)).Then(E2));
        // E2 cap = AS5600 holder (E2 coordinates) + AS5600 module + screws
        ab.Add(P(@"03_Hombro_E2_J2\CB-303_Tapa_E2_Soporte_AS5600.SLDPRT"), E2);
        ab.Add(P(@"05_Comerciales\Electronica\CB-503_Modulo_AS5600.SLDPRT"), Xf.Rows(V(1, 0, 0), V(0, 0, 1), V(0, -1, 0), V(-91.196, -11.653, 39.42)).Then(E2));
        foreach (double x in new[] { -87.8, -71.64 }) foreach (double y in new[] { -8.25, 7.98 })
            ab.Add(P(HW + "HW_M3x6_Allen_DIN912.SLDPRT"), Xf.Rows(V(1, 0, 0), V(0, 0, 1), V(0, -1, 0), V(x, y, 41)).Then(E2));
        foreach (double ang in new[] { 90.0, 210.0, 330.0 })
            ab.Add(P(HW + "HW_M3x10.SLDPRT"), Xf.Rows(V(1, 0, 0), V(0, 0, -1), V(0, 1, 0),
                V(-80 + 24.5 * Math.Cos(ang * Math.PI / 180), 24.5 * Math.Sin(ang * Math.PI / 180), 40.6)).Then(E2));
        ab.Save(P(@"03_Hombro_E2_J2\CB-300_Hombro_E2_J2.SLDASM"));
        SW.CloseMine();
    }

    // home of the prismatic joint: bar inner end at x=-10 (E2 coords), middle of the TSF-E3 screw range (-34..15)
    public const double E3Shift = -63.5;
    static void Build400()
    {
        var ab = AB.New();
        ab.World = AB.ZUp;
        // E3 v2 rotated 90 deg about the E2 channel axis with respect to the Rx-rcp pose (so 53.4 x 45.4 bar matches the 54 x 46 channel)
        ab.Add(P(@"04_Prismatico_E3_J3\CB-401_Eslabon_E3_v2.SLDPRT"),
            Xf.Rows(V(0, 0, -1), V(0, -1, 0), V(-1, 0, 0), V(-189.51631710902279, 238.22093322800185, -120.24528724151845)).Then(Xf.Tr(E3Shift, 0, 0)).Then(E2));
        ab.Save(P(@"04_Prismatico_E3_J3\CB-400_Prismatico_E3_J3.SLDASM"));
        SW.CloseMine();
    }
}
