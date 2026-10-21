using System;
using System.Linq;
using SolidWorks.Interop.sldworks;

// Rigid transform in SolidWorks row-vector convention: p' = p * R + T   (mm)
public class Xf
{
    public double[] R = { 1, 0, 0, 0, 1, 0, 0, 0, 1 };
    public double[] T = { 0, 0, 0 };

    public static Xf Rows(double[] r0, double[] r1, double[] r2, double[] t)
    {
        var x = new Xf();
        x.R = new[] { r0[0], r0[1], r0[2], r1[0], r1[1], r1[2], r2[0], r2[1], r2[2] };
        x.T = (double[])t.Clone();
        return x;
    }
    public static Xf Tr(double x, double y, double z) { var a = new Xf(); a.T = new[] { x, y, z }; return a; }
    public static Xf FromComp(Component2 c)
    {
        double[] a = (double[])((MathTransform)c.Transform2).ArrayData;
        var x = new Xf();
        x.R = a.Take(9).ToArray();
        x.T = new[] { a[9] * 1000, a[10] * 1000, a[11] * 1000 };
        return x;
    }
    // apply this, then b
    public Xf Then(Xf b)
    {
        var o = new Xf();
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
            {
                double s = 0;
                for (int k = 0; k < 3; k++) s += R[i * 3 + k] * b.R[k * 3 + j];
                o.R[i * 3 + j] = s;
            }
        for (int j = 0; j < 3; j++)
        {
            double s = b.T[j];
            for (int k = 0; k < 3; k++) s += T[k] * b.R[k * 3 + j];
            o.T[j] = s;
        }
        return o;
    }
    public double[] Apply(double[] p)
    {
        var o = new double[3];
        for (int j = 0; j < 3; j++) { o[j] = T[j]; for (int k = 0; k < 3; k++) o[j] += p[k] * R[k * 3 + j]; }
        return o;
    }
    public Xf Inv()
    {
        var o = new Xf();
        for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) o.R[i * 3 + j] = R[j * 3 + i];
        for (int j = 0; j < 3; j++) { double s = 0; for (int k = 0; k < 3; k++) s -= T[k] * o.R[k * 3 + j]; o.T[j] = s; }
        return o;
    }
    public MathTransform ToMath()
    {
        var mu = (MathUtility)SW.App.GetMathUtility();
        double[] a = new double[16];
        for (int i = 0; i < 9; i++) a[i] = R[i];
        a[9] = T[0] / 1000; a[10] = T[1] / 1000; a[11] = T[2] / 1000; a[12] = 1;
        return (MathTransform)mu.CreateTransform(a);
    }
    public override string ToString()
    {
        Func<double, string> f = v => v.ToString("0.####");
        return string.Format("R=[{0} {1} {2}; {3} {4} {5}; {6} {7} {8}] T=({9},{10},{11})",
            f(R[0]), f(R[1]), f(R[2]), f(R[3]), f(R[4]), f(R[5]), f(R[6]), f(R[7]), f(R[8]), T[0].ToString("0.###"), T[1].ToString("0.###"), T[2].ToString("0.###"));
    }
}
