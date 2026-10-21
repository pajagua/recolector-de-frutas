using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Part builder helpers. All user-facing numbers in mm; converted to m for the API.
// Planes are addressed by the model axis they are normal to:  'F' = normal Z (sketch coords x,y),
// 'T' = normal Y (sketch coords x,z), 'R' = normal X (sketch coords z,y). Extrude start/depth are along +axis.
public static class PB
{
    public const double MM = 0.001;
    public static ModelDoc2 M;
    static Feature[] planes = new Feature[3];   // index = normal axis (0 X, 1 Y, 2 Z)

    public static ModelDoc2 NewPart()
    {
        string tpl = SW.App.GetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swDefaultTemplatePart);
        var nd = (ModelDoc2)SW.App.NewDocument(tpl, 0, 0, 0);
        SW.Created.Add(nd.GetTitle());
        return Use(nd);
    }

    public static ModelDoc2 Use(ModelDoc2 m)
    {
        M = m;
        planes = new Feature[3];
        Feature f = (Feature)m.FirstFeature();
        int n = 0;
        while (f != null && n < 3)
        {
            if (f.GetTypeName2() == "RefPlane")
            {
                n++;
                var rp = (RefPlane)f.GetSpecificFeature2();
                double[] t = (double[])((MathTransform)rp.Transform).ArrayData;
                double nx = Math.Abs(t[6]), ny = Math.Abs(t[7]), nz = Math.Abs(t[8]);
                int ax = nx > 0.9 ? 0 : ny > 0.9 ? 1 : 2;
                planes[ax] = f;
            }
            f = (Feature)f.GetNextFeature();
        }
        M.SketchManager.AddToDB = true;
        M.SketchManager.DisplayWhenAdded = false;
        return m;
    }

    static char curPl;
    static MathTransform m2s;
    static double nSign = 1;
    static double orient = 1;

    static double[] ModelPt(char pl, double a, double b)
    {
        if (pl == 'F') return new[] { a, b, 0.0 };
        if (pl == 'T') return new[] { a, 0.0, b };
        return new[] { 0.0, b, a };
    }
    public static double[] Map(double a, double b)
    {
        var p = ModelPt(curPl, a, b);
        var mu = (MathUtility)SW.App.GetMathUtility();
        var mp = (MathPoint)mu.CreatePoint(new double[] { p[0] * MM, p[1] * MM, p[2] * MM });
        var sp = (MathPoint)mp.MultiplyTransform(m2s);
        double[] d = (double[])sp.ArrayData;
        return new[] { d[0], d[1] };
    }

    public static void Sketch(char pl)
    {
        curPl = pl;
        int ax = pl == 'F' ? 2 : pl == 'T' ? 1 : 0;
        M.ClearSelection2(true);
        if (!planes[ax].Select2(false, 0)) throw new Exception("cannot select plane " + planes[ax].Name);
        M.SketchManager.InsertSketch(true);
        var sk = (SolidWorks.Interop.sldworks.Sketch)M.SketchManager.ActiveSketch;
        m2s = (MathTransform)sk.ModelToSketchTransform;
        var s2m = (MathTransform)m2s.Inverse();
        var mu = (MathUtility)SW.App.GetMathUtility();
        var nv = (MathVector)((MathVector)mu.CreateVector(new double[] { 0, 0, 1 })).MultiplyTransform(s2m);
        double[] n = (double[])nv.ArrayData;
        nSign = n[ax] >= 0 ? 1 : -1;
        var o = Map(0, 0); var ea = Map(1, 0); var eb = Map(0, 1);
        double det = (ea[0] - o[0]) * (eb[1] - o[1]) - (eb[0] - o[0]) * (ea[1] - o[1]);
        orient = det >= 0 ? 1 : -1;
    }

    public static void Circle(double a, double b, double d)
    {
        var p = Map(a, b);
        M.SketchManager.CreateCircleByRadius(p[0], p[1], 0, d / 2 * MM);
    }
    public static void Rect(double a1, double b1, double a2, double b2)
    {
        Line(a1, b1, a2, b1); Line(a2, b1, a2, b2); Line(a2, b2, a1, b2); Line(a1, b2, a1, b1);
    }
    public static void Poly(params double[] ab)
    {
        int n = ab.Length / 2;
        for (int i = 0; i < n; i++)
            Line(ab[2 * i], ab[2 * i + 1], ab[(2 * i + 2) % (2 * n)], ab[(2 * i + 3) % (2 * n)]);
    }
    public static void RRect(double a1, double b1, double a2, double b2, double r)
    {
        double x1 = Math.Min(a1, a2), x2 = Math.Max(a1, a2), y1 = Math.Min(b1, b2), y2 = Math.Max(b1, b2);
        Line(x1 + r, y1, x2 - r, y1); Line(x2, y1 + r, x2, y2 - r); Line(x2 - r, y2, x1 + r, y2); Line(x1, y2 - r, x1, y1 + r);
        Arc(x2 - r, y1 + r, x2 - r, y1, x2, y1 + r);
        Arc(x2 - r, y2 - r, x2, y2 - r, x2 - r, y2);
        Arc(x1 + r, y2 - r, x1 + r, y2, x1, y2 - r);
        Arc(x1 + r, y1 + r, x1, y1 + r, x1 + r, y1);
    }
    public static void Line(double a1, double b1, double a2, double b2)
    {
        var p = Map(a1, b1); var q = Map(a2, b2);
        M.SketchManager.CreateLine(p[0], p[1], 0, q[0], q[1], 0);
    }
    // arc counter-clockwise in (a,b) space from s to e around c
    public static void Arc(double ca, double cb, double sa, double sb, double ea, double eb)
    {
        var c = Map(ca, cb); var s = Map(sa, sb); var e = Map(ea, eb);
        M.SketchManager.CreateArc(c[0], c[1], 0, s[0], s[1], 0, e[0], e[1], 0, (short)(orient > 0 ? 1 : -1));
    }
    // D profile: circle diameter d with a flat at distance f from the center, flat normal along +a
    public static void DShape(double ca, double cb, double d, double f)
    {
        double r = d / 2; double h = Math.Sqrt(r * r - f * f);
        Line(ca + f, cb - h, ca + f, cb + h);
        Arc(ca, cb, ca + f, cb + h, ca + f, cb - h);
    }

    // Extrude the active sketch: start face at 'start' along +axis, extruding 'depth' along +axis (negative = -axis)
    public static Feature Extrude(double start, double depth, bool cut = false, string name = null)
    {
        start *= nSign; depth *= nSign;
        var fm = M.FeatureManager;
        bool flip = cut ? depth > 0 : depth < 0;
        double d = Math.Abs(depth) * MM;
        int t0 = Math.Abs(start) < 1e-9 ? (int)swStartConditions_e.swStartSketchPlane : (int)swStartConditions_e.swStartOffset;
        double so = Math.Abs(start) * MM;
        bool flipStart = start < 0;
        Feature f;
        if (!cut)
            f = fm.FeatureExtrusion3(true, false, flip, (int)swEndConditions_e.swEndCondBlind, 0, d, 0, false, false, false, false, 0, 0,
                false, false, false, false, true, true, true, t0, so, flipStart);
        else
            f = fm.FeatureCut4(true, false, flip, (int)swEndConditions_e.swEndCondBlind, 0, d, 0, false, false, false, false, 0, 0,
                false, false, false, false, false, true, true, true, true, false, t0, so, flipStart, false);
        if (f == null)
        {
            M.SketchManager.InsertSketch(true);
            throw new Exception("extrude failed (" + (cut ? "cut" : "boss") + " start=" + start + " depth=" + depth + ")");
        }
        if (name != null) f.Name = name;
        M.ClearSelection2(true);
        return f;
    }

    public static void Color(Feature f, double r, double g, double b, double spec = 0.4)
    {
        double[] v = { r, g, b, 1, 1, spec, 0.3, 0, 0 };
        f.SetMaterialPropertyValues2(v, (int)swInConfigurationOpts_e.swAllConfiguration, null);
    }
    public static void PartColor(double r, double g, double b)
    {
        M.MaterialPropertyValues = new double[] { r, g, b, 1, 1, 0.4, 0.3, 0, 0 };
    }

    public static void Prop(string name, string val)
    {
        var cpm = M.Extension.CustomPropertyManager[""];
        cpm.Add3(name, (int)swCustomInfoType_e.swCustomInfoText, val, (int)swCustomPropertyAddOption_e.swCustomPropertyReplaceValue);
    }

    public static void Save(string path)
    {
        int e = 0, w = 0;
        M.ForceRebuild3(false);
        M.ShowNamedView2("*Isometric", -1);
        M.ViewZoomtofit2();
        bool ok = M.Extension.SaveAs3(path, 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, null, ref e, ref w);
        Console.WriteLine((ok ? "saved " : "SAVE FAIL ") + path + (e != 0 ? " err=" + e : ""));
    }

    public static double[] BodyBox()
    {
        object[] bs = (object[])((PartDoc)M).GetBodies2((int)swBodyType_e.swSolidBody, true);
        double[] r = null;
        if (bs == null) return null;
        foreach (Body2 b in bs)
        {
            double[] x = (double[])b.GetBodyBox();
            if (r == null) r = x;
            else { for (int i = 0; i < 3; i++) { r[i] = Math.Min(r[i], x[i]); r[i + 3] = Math.Max(r[i + 3], x[i + 3]); } }
        }
        return r;
    }
    public static int BodyCount()
    {
        object[] bs = (object[])((PartDoc)M).GetBodies2((int)swBodyType_e.swSolidBody, true);
        return bs == null ? 0 : bs.Length;
    }
    public static double Volume()
    {
        var mp = (MassProperty)M.Extension.CreateMassProperty();
        return mp == null ? -1 : mp.Volume * 1e9;
    }
    public static void Report(string tag)
    {
        Console.WriteLine(tag + ": bodies=" + BodyCount() + " box " + SW.Box(BodyBox()) + " vol=" + Volume().ToString("0") + "mm3");
    }
}
