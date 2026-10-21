using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Colorize.exe <root> : realistic colours for the purchased components (printed parts untouched)
class Colorize
{
    static double[] Mat(double r, double g, double b, bool metal = false)
    {
        return metal ? new[] { r, g, b, 0.6, 0.8, 0.9, 0.7, 0, 0 } : new[] { r, g, b, 0.5, 0.9, 0.3, 0.3, 0, 0 };
    }
    // palette
    static readonly double[] Steel = Mat(0.78, 0.79, 0.81, true), Alu = Mat(0.84, 0.85, 0.88, true), Zinc = Mat(0.80, 0.80, 0.84, true),
        BlackOxide = Mat(0.16, 0.16, 0.18, true), Nickel = Mat(0.74, 0.75, 0.78, true), Black = Mat(0.07, 0.07, 0.08),
        Rubber = Mat(0.06, 0.06, 0.06), PcbGreen = Mat(0.05, 0.38, 0.18), PcbBlue = Mat(0.08, 0.22, 0.62), White = Mat(0.95, 0.95, 0.95),
        TermGreen = Mat(0.15, 0.62, 0.25), DarkGrey = Mat(0.18, 0.18, 0.2);

    static string R;
    static int ops;

    static void ClearFaceAndFeatureColors(ModelDoc2 m)
    {
        var p = (PartDoc)m;
        foreach (Body2 b in (object[])p.GetBodies2((int)swBodyType_e.swAllBodies, false))
            foreach (Face2 f in (object[])b.GetFaces())
                if (f.MaterialPropertyValues != null) f.RemoveMaterialProperty2((int)swInConfigurationOpts_e.swAllConfiguration, null);
        Feature ft = (Feature)m.FirstFeature();
        while (ft != null)
        {
            try { ft.RemoveMaterialProperty2((int)swInConfigurationOpts_e.swAllConfiguration, null); } catch { }
            ft = (Feature)ft.GetNextFeature();
        }
    }

    static void Body(Body2 b, double[] c) { b.MaterialPropertyValues2 = c; ops++; }
    static void Face(Face2 f, double[] c) { f.MaterialPropertyValues = c; ops++; }

    static void Do(string rel, Action<ModelDoc2, Body2[]> act, bool clear = true)
    {
        var m = SW.Open(Path.Combine(R, rel));
        if (m == null) return;
        var bodies = ((object[])((PartDoc)m).GetBodies2((int)swBodyType_e.swAllBodies, false)).Cast<Body2>().ToArray();
        ops = 0;
        if (clear) ClearFaceAndFeatureColors(m);
        act(m, bodies);
        m.GraphicsRedraw2();
        int e = 0, w = 0;
        bool ok = m.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w);
        Console.WriteLine((ok ? "ok   " : "FAIL ") + rel + " (" + ops + " cambios)");
        SW.App.CloseDoc(m.GetTitle());
    }
    static void All(Body2[] bs, double[] c) { foreach (var b in bs) Body(b, c); }
    static double Vol(Body2 b) { var mp = (double[])b.GetMassProperties(1.0); return mp == null ? 0 : mp[3]; }

    static void Main(string[] args)
    {
        R = args[0];
        SW.Connect();

        // ---- hardware: socket screws black oxide, nuts/washers zinc, M8 hex bolt zinc; selfoil bushings keep bronze
        foreach (var s in new[] { "HW_M3x6_Allen_DIN912", "HW_M3x8", "HW_M3x10", "HW_M4x30", "HW_M4x40", "HW_M4x45", "HW_M5x20", "HW_M5x30", "HW_M6x10_Allen_DIN912" })
            Do(@"05_Comerciales\Tornilleria\" + s + ".SLDPRT", (m, bs) => { m.MaterialPropertyValues = BlackOxide; All(bs, BlackOxide); });
        foreach (var s in new[] { "HW_M8x65", "HW_Tuerca_M4", "HW_Tuerca_M4_Autoblocante", "HW_Tuerca_M5_Autoblocante", "HW_Tuerca_M8_Autoblocante", "HW_Arandela_M4", "HW_Arandela_M5" })
            Do(@"05_Comerciales\Tornilleria\" + s + ".SLDPRT", (m, bs) => { m.MaterialPropertyValues = Zinc; All(bs, Zinc); });

        // ---- bearings, pulley, magnet, shaft
        Do(@"05_Comerciales\HW_Rodamiento_625ZZ_5x16x5.SLDPRT", (m, bs) => { m.MaterialPropertyValues = Steel; All(bs, Steel); });
        Do(@"05_Comerciales\HW_Polea_T5_10T_eje6.SLDPRT", (m, bs) => { m.MaterialPropertyValues = Alu; All(bs, Alu); });
        Do(@"05_Comerciales\CB-504_Iman_Diametral_D6x2.5.SLDPRT", (m, bs) => { m.MaterialPropertyValues = Nickel; All(bs, Nickel); });
        Do(@"05_Comerciales\CB-506_Eje_Salida_5840_D8.SLDPRT", (m, bs) => { m.MaterialPropertyValues = Steel; All(bs, Steel); });
        Do(@"09_Herramienta\CB-615_Rodamiento_5972K217.SLDPRT", (m, bs) => { m.MaterialPropertyValues = Steel; All(bs, Steel); });
        Do(@"09_Herramienta\CB-616_Rodamiento_5972K91.SLDPRT", (m, bs) => { m.MaterialPropertyValues = Steel; All(bs, Steel); });

        // ---- servo MG996R: black housing and ears; aluminium horn
        Do(@"09_Herramienta\CB-612_MG996R_Cuerpo.SLDPRT", (m, bs) => { m.MaterialPropertyValues = Black; All(bs, Black); });
        Do(@"09_Herramienta\CB-613_MG996R_Componente1.SLDPRT", (m, bs) => { m.MaterialPropertyValues = DarkGrey; All(bs, DarkGrey); });
        Do(@"09_Herramienta\CB-614_Horn_Servo_D20.SLDPRT", (m, bs) => { m.MaterialPropertyValues = Alu; All(bs, Alu); });

        // ---- vacuum cup: rubber cup (largest body) + metal fitting
        Do(@"09_Herramienta\CB-617_Ventosa_5427A638.SLDPRT", (m, bs) =>
        {
            m.MaterialPropertyValues = Zinc;
            var big = bs.OrderByDescending(b => Vol(b)).First();
            foreach (var b in bs) Body(b, b == big ? Rubber : Zinc);
            // the cup lip is the widest part: colour faces above the fitting as rubber too if the cup and fitting share a body
            foreach (var b in bs.Where(x => x != big))
                Console.WriteLine("   ventosa cuerpo " + b.Name + " " + SW.Box((double[])b.GetBodyBox()));
            Console.WriteLine("   ventosa cuerpo grande " + big.Name + " " + SW.Box((double[])big.GetBodyBox()));
        });

        // ---- AS5600: blue PCB, white silkscreen, keep chip/pins
        Do(@"05_Comerciales\Electronica\CB-503_Modulo_AS5600.SLDPRT", (m, bs) =>
        {
            foreach (var b in bs)
            {
                if (b.Name.Contains("PCB Geometry")) Body(b, PcbBlue);
                else if (b.Name.Contains("txt")) Body(b, White);
            }
        }, false);

        // ---- Sabertooth 2x60: green PCB plate, aluminium heatsink, black capacitors, green terminal blocks
        Do(@"05_Comerciales\Electronica\CB-510_Sabertooth_2x60.SLDPRT", (m, bs) =>
        {
            m.MaterialPropertyValues = Alu;
            var main = bs.OrderByDescending(b => Vol(b)).First();
            Body(main, Alu);
            foreach (Face2 f in (object[])main.GetFaces())
            {
                double[] bx = (double[])f.GetBox();
                var s = (Surface)f.GetSurface();
                if (bx[2] * 1000 >= -0.05) Face(f, PcbGreen);                                     // PCB plate (z 0..5)
                else if (s.IsCylinder())
                {
                    double[] cp = (double[])s.CylinderParams; double r = cp[6] * 1000;
                    if (Math.Abs(Math.Abs(cp[5]) - 1) < 1e-6 && r > 3.5 && r < 7.5) Face(f, Black);   // capacitors
                }
            }
            foreach (var b in bs.Where(x => x != main))
                Body(b, b.Name.StartsWith("Chamfer") ? TermGreen : Steel);
        });

        // ---- 37D: silver gearbox and can, black encoder cap (already feature-coloured) -> make metals shinier
        Do(@"05_Comerciales\CB-501_Motor_37D_90-1_Encoder64CPR.SLDPRT", (m, bs) => { m.MaterialPropertyValues = Alu; }, false);
        Do(@"05_Comerciales\CB-502_Motor_Sinfin_5840-36ZY.SLDPRT", (m, bs) => { m.MaterialPropertyValues = Alu; }, false);
    }
}
