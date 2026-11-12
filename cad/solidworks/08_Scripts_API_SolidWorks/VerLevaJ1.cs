using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// VerLevaJ1.exe <root solidworks> [desde hasta paso]   (v7.13, solo lectura: no guarda nada)
// Final de carrera de J1: cuánto baja la leva de la torre CB-210 la palanca del switch de J1 (CB-516 en CB-160) para cada J1
// (0 = brazo hacia el árbol, CCW +, como VerFinales.cs). Abre CB-000 sin ventana (si ya estaba abierto no lo cierra).
// La palanca se separa del cuerpo del switch (Y > 10.65 en el marco del switch) y se gira alrededor de su bisagra (X 18.6, Y 10.3)
// hasta que deja de chocar con la leva: ese giro es lo que la leva la baja. El switch opera cuando la punta de arriba de la palanca
// (15.0 sobre la base, libre) llega a 13.1 (medida del usuario: 10.2 sobre los agujeros, que están a 2.9).
// También dice por dónde llega la leva: del lado de la bisagra (bien: la palanca sube hacia la leva y hace de rampa) o de la punta.
class VerLevaJ1
{
    static Modeler mod;
    static Xf Rot(double[] p, double[] u, double deg)
    {
        double t = deg * Math.PI / 180, c = Math.Cos(t), s = Math.Sin(t), n = Math.Sqrt(u[0] * u[0] + u[1] * u[1] + u[2] * u[2]);
        double x = u[0] / n, y = u[1] / n, z = u[2] / n;
        double[,] M = { { c + x * x * (1 - c), x * y * (1 - c) - z * s, x * z * (1 - c) + y * s },
                        { y * x * (1 - c) + z * s, c + y * y * (1 - c), y * z * (1 - c) - x * s },
                        { z * x * (1 - c) - y * s, z * y * (1 - c) + x * s, c + z * z * (1 - c) } };
        var r = new Xf();
        for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) r.R[i * 3 + j] = M[j, i];
        return Xf.Tr(-p[0], -p[1], -p[2]).Then(r).Then(Xf.Tr(p[0], p[1], p[2]));
    }
    static Component2 Find(Component2 parent, string start)
    {
        var ch = parent.GetChildren() as object[]; if (ch == null) return null;
        foreach (Component2 c in ch)
        {
            if (Path.GetFileName(c.GetPathName()).StartsWith(start, StringComparison.OrdinalIgnoreCase)) return c;
            var r = Find(c, start); if (r != null) return r;
        }
        return null;
    }
    static double Vol(object[] r)
    {
        double v = 0; if (r == null) return 0;
        foreach (Body2 b in r) { var mp = b.GetMassProperties(1.0) as double[]; if (mp != null) v += mp[3] * 1e9; }
        return v;
    }
    static object[] Op(Body2 a, Body2 b, swBodyOperationType_e t) { int e; return ((Body2)a.Copy()).Operations2((int)t, (Body2)b.Copy(), out e) as object[]; }
    static double Inter(Body2 a, Body2 b) { return Vol(Op(a, b, swBodyOperationType_e.SWBODYINTERSECT)); }
    static Body2 Cil(double x, double y, double z, double d, double h)
    { return (Body2)mod.CreateBodyFromCyl(new double[] { x / 1000, y / 1000, z / 1000, 0, 0, 1, d / 2000, h / 1000 }); }
    static double P(string s) { return double.Parse(s, CultureInfo.InvariantCulture); }

    static void Main(string[] a)
    {
        string root = a[0];
        double desde = a.Length > 1 ? P(a[1]) : -55, hasta = a.Length > 2 ? P(a[2]) : -110, paso = a.Length > 3 ? P(a[3]) : 0.5;
        SW.Connect(); mod = (Modeler)SW.App.GetModeler();
        string pa = Path.Combine(root, "CB-000_CitriBot_Ensamblaje_General.SLDASM");
        var abierto = (ModelDoc2)SW.App.GetOpenDocumentByName(pa);
        ModelDoc2 m = abierto;
        if (m == null)
        {
            SW.App.DocumentVisible(false, (int)swDocumentTypes_e.swDocASSEMBLY);
            try { m = SW.Open(pa); } finally { SW.App.DocumentVisible(true, (int)swDocumentTypes_e.swDocASSEMBLY); }
        }
        var asm = (AssemblyDoc)m; asm.ResolveAllLightWeightComponents(false);
        var top = ((object[])asm.GetComponents(true)).Cast<Component2>().ToList();
        var c200 = top.First(c => c.Name2.StartsWith("CB-200")); var c300 = top.First(c => c.Name2.StartsWith("CB-300"));
        var cT = Find(c200, "CB-210"); var cE2 = Find(c300, "CB-301");
        var hojas = ((object[])asm.GetComponents(false)).Cast<Component2>().ToList();
        var cS = hojas.Where(c => Path.GetFileName(c.GetPathName() ?? "").StartsWith("CB-516")).FirstOrDefault(c =>
        {
            var t = Xf.FromComp(c).Apply(new double[] { 9.9, 10, 0 }); return Math.Abs(Math.Sqrt(t[0] * t[0] + t[1] * t[1]) - 92) < 6 && t[2] > 50;
        });
        if (cS == null) { Console.WriteLine("NO se encontro el switch de J1"); return; }
        // J1 guardado (como VerFinales.cs: dirección del eje x de E2 respecto del árbol, que en CB-000 está a -112.5°)
        var tree = new[] { Math.Cos(-112.5 * Math.PI / 180), Math.Sin(-112.5 * Math.PI / 180) };
        var e2w = Xf.FromComp(cE2); var o = e2w.Apply(new double[3]); var x1 = e2w.Apply(new double[] { 1, 0, 0 });
        double ax0 = x1[0] - o[0], ax1 = x1[1] - o[1], axh = Math.Sqrt(ax0 * ax0 + ax1 * ax1);
        double j1now = Math.Atan2(tree[0] * ax1 / axh - tree[1] * ax0 / axh, tree[0] * ax0 / axh + tree[1] * ax1 / axh) * 180 / Math.PI;
        // leva: la torre recortada a un anillo r 70 … 100, z 88 … 98 alrededor del eje J1 (origen de CB-000, vertical)
        var torre = (Body2)((object[])((PartDoc)cT.GetModelDoc2()).GetBodies2((int)swBodyType_e.swSolidBody, true))[0];
        torre = (Body2)torre.Copy(); torre.ApplyTransform((MathTransform)cT.Transform2);
        var anillo = (Body2)Op(Cil(0, 0, 88, 200, 10), Cil(0, 0, 87, 140, 12), swBodyOperationType_e.SWBODYCUT)[0];
        var leva = (Body2)Op(torre, anillo, swBodyOperationType_e.SWBODYINTERSECT)[0];
        // palanca, en el marco del switch: X 2 … 18.45, Y 10.65 … 15.6, Z ±2.5
        var sp = (Body2)((object[])((PartDoc)cS.GetModelDoc2()).GetBodies2((int)swBodyType_e.swSolidBody, true))[0];
        var caja = (Body2)mod.CreateBodyFromBox3(new double[] { 0.010225, 0.01065, 0, 0, 1, 0, 0.01645, 0.005, 0.00495 });
        var palanca = (Body2)Op(sp, caja, swBodyOperationType_e.SWBODYINTERSECT)[0];
        var xs = Xf.FromComp(cS);
        double[] bis = { 18.6, 10.3, 0 };
        double R0 = Math.Sqrt(16.1 * 16.1 + 4.7 * 4.7), ang0 = Math.Atan2(4.7, -16.1) * 180 / Math.PI;
        double dOP = (180 - Math.Asin((13.1 - 10.3) / R0) * 180 / Math.PI) - ang0;
        var wb = xs.Apply(bis); var wp = xs.Apply(new double[] { 2.5, 14.7, 0 });
        Func<double[], double> angDe = q => Math.Atan2(q[1], q[0]) * 180 / Math.PI;
        Console.WriteLine(string.Format("J1 guardado {0:0.0}°; palanca {1:0.0} mm3; bisagra a {2:0.0}° y punta a {3:0.0}° (mundo, r {4:0.0}); opera con {5:0.00}° de giro",
            j1now, Vol(new object[] { palanca }), angDe(wb), angDe(wp), Math.Sqrt(wp[0] * wp[0] + wp[1] * wp[1]), dOP));
        // al bajar J1 la leva gira en sentido horario (ángulos que bajan): llega primero al extremo de la palanca con ángulo mayor
        double aB = angDe(wb), aP = angDe(wp); double dif = (aB - aP + 540) % 360 - 180;
        Console.WriteLine("al ir hacia el home (J1 bajando) la leva llega primero a " + (dif > 0 ? "la BISAGRA (bien)" : "la PUNTA (mal: empuja la palanca a lo largo)"));
        Func<double, double, double> choque = (j, d) =>
        {
            var b = (Body2)palanca.Copy();
            b.ApplyTransform(Rot(bis, new double[] { 0, 0, 1 }, d).Then(xs).Then(Rot(new double[3], new double[] { 0, 0, 1 }, -(j - j1now))).ToMath());
            return Inter(b, leva);
        };
        Func<double, double> giro = j =>
        {
            if (choque(j, 0) < 1e-4) return 0;
            if (choque(j, 20) > 1e-4) return 99;
            double lo = 0, hi = 20;
            for (int i = 0; i < 12; i++) { double mid = (lo + hi) / 2; if (choque(j, mid) > 1e-4) lo = mid; else hi = mid; }
            return hi;
        };
        foreach (var j in new[] { 100.0, 50, 0, -40 }) Console.WriteLine(string.Format("J1 {0,7:0.0}°: giro {1:0.00}°", j, giro(j)));
        double? toca = null, opera = null, ultimaOp = null, ultimoToque = null; double maxGiro = 0;
        for (double j = desde; j >= hasta - 1e-9; j -= paso)
        {
            double d = giro(j);
            string est = d >= 99 ? "TRABA (mas de 20°)" : d <= 0 ? "libre" : d >= dOP ? "OPERA" : "toca";
            double hp = 10.3 + R0 * Math.Sin((ang0 + Math.Min(d, 20)) * Math.PI / 180);
            Console.WriteLine(string.Format("J1 {0,7:0.0}°: giro {1,5:0.00}°  punta a {2,5:0.00} sobre la base  {3}", j, d, hp, est));
            if (d > 0 && toca == null) toca = j;
            if (d >= dOP && opera == null) opera = j;
            if (d >= dOP) ultimaOp = j;
            if (d > 0) ultimoToque = j;
            maxGiro = Math.Max(maxGiro, d);
        }
        Console.WriteLine(string.Format("RESUMEN: toca desde {0}, opera desde {1} hasta {2}, toca hasta {3}; giro maximo {4:0.00}° (opera con {5:0.00}°)",
            toca == null ? "-" : toca.Value.ToString("0.0"), opera == null ? "-" : opera.Value.ToString("0.0"), ultimaOp == null ? "-" : ultimaOp.Value.ToString("0.0"),
            ultimoToque == null ? "-" : ultimoToque.Value.ToString("0.0"), maxGiro, dOP));
        if (abierto == null) SW.App.CloseDoc(m.GetTitle());
    }
}
