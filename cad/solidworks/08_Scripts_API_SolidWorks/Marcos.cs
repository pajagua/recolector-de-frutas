using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Marcos.exe <root> [info|aplicar] (v7.5, 2026-10-30)
// Sistemas de referencia de Denavit-Hartenberg del brazo, calculados con la geometría del CAD en la pose guardada de CB-000,
// y creados como "Sistemas de coordenadas" dentro de la pieza de cada eslabón (así se mueven con él):
//   SR0 base (z0 = eje J1) en CB-111, SR1 (z1 = eje J2) en CB-210, SR2 (z2 = eje J3) en CB-301, SR3 (z3 = eje J4) en CB-401,
//   SR4 (z4 = eje J5) en CB-603, SR5 = punta de la ventosa (TCP) en CB-606.
class Marcos
{
    static double[] Sub(double[] a, double[] b) { return new[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] }; }
    static double[] Add(double[] a, double[] b) { return new[] { a[0] + b[0], a[1] + b[1], a[2] + b[2] }; }
    static double[] Mul(double[] a, double k) { return new[] { a[0] * k, a[1] * k, a[2] * k }; }
    static double Dot(double[] a, double[] b) { return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]; }
    static double[] Cross(double[] a, double[] b) { return new[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] }; }
    static double[] Unit(double[] a) { double n = Math.Sqrt(Dot(a, a)); return Mul(a, 1 / n); }
    static string F(double[] v) { return string.Format("({0:0.00}, {1:0.00}, {2:0.00})", v[0], v[1], v[2]); }
    static double[] Dir(Xf x, double[] d) { return Sub(x.Apply(d), x.Apply(new[] { 0.0, 0, 0 })); }

    class Marco { public string Nombre, Pieza; public double[] O, X, Y, Z; }

    static void Main(string[] a)
    {
        string R = a[0];
        SW.Connect(); SW.CloseMine();
        var m = SW.Open(Path.Combine(R, "CB-000_CitriBot_Ensamblaje_General.SLDASM"));
        var asm = (AssemblyDoc)m; asm.ResolveAllLightWeightComponents(false);
        var comps = ((object[])asm.GetComponents(false)).Cast<Component2>().ToList();
        Func<string, Component2> C = n => comps.First(c => c.Name2.Split('/').Last().StartsWith(n));
        var piezas = new[] { "CB-111", "CB-210", "CB-301", "CB-401", "CB-603", "CB-606", "CB-616", "CB-614_Horn_Servo_D20-1" };
        var X = new Dictionary<string, Xf>(); var path = new Dictionary<string, string>();
        foreach (var p in piezas) { var c = C(p); X[p] = Xf.FromComp(c); path[p] = c.GetPathName(); Console.WriteLine(p + " -> " + c.Name2); }
        SW.App.CloseDoc(m.GetTitle());

        // ejes de las articulaciones en coordenadas de CB-000
        var o0 = new[] { 0.0, 0, 0 }; var zJ1 = new[] { 0.0, 0, 1 };
        var pJ2 = X["CB-301"].Apply(new[] { -80.0, 0, 0 }); var zJ2 = Unit(Dir(X["CB-301"], new[] { 0.0, 0, 1 }));   // E2: eje J2 = Z en (-80, 0)
        var pJ3 = pJ2; var zJ3 = Unit(Dir(X["CB-301"], new[] { 1.0, 0, 0 }));                                       // eje del prismático = X de E2
        var c616 = Centro(X["CB-616"], path["CB-616"]); var c614 = Centro(X["CB-614_Horn_Servo_D20-1"], path["CB-614_Horn_Servo_D20-1"]);
        var pJ4 = c616; var zJ4 = Unit(Sub(c614, c616));                                                            // cabeceo: rodamiento 608 -> horn de muñeca
        var pJ5 = X["CB-606"].Apply(new[] { -22.25, 22.25, 0.0 }); var zJ5 = Unit(Dir(X["CB-606"], new[] { 0.0, 0, 1 }));   // torsión, hacia la ventosa
        var tcp = X["CB-606"].Apply(new[] { -22.25, 22.25, 84.2 });                                                 // labio de la ventosa
        Console.WriteLine("J1 " + F(o0) + " " + F(zJ1)); Console.WriteLine("J2 " + F(pJ2) + " " + F(zJ2)); Console.WriteLine("J3 " + F(pJ3) + " " + F(zJ3));
        Console.WriteLine("J4 " + F(pJ4) + " " + F(zJ4)); Console.WriteLine("J5 " + F(pJ5) + " " + F(zJ5)); Console.WriteLine("TCP " + F(tcp));

        // marcos DH (convención estándar): o_i sobre z_i en la normal común con z_(i-1)
        var ejes = new[] { Tuple.Create(o0, zJ1), Tuple.Create(pJ2, zJ2), Tuple.Create(pJ3, zJ3), Tuple.Create(pJ4, zJ4), Tuple.Create(pJ5, zJ5) };
        var M = new List<Marco>();
        M.Add(new Marco { Nombre = "SR0_base_eje_J1", Pieza = "CB-111", O = o0, X = new[] { 1.0, 0, 0 }, Z = zJ1 });
        for (int i = 1; i < 5; i++)
        {
            var p = ejes[i - 1].Item1; var u = ejes[i - 1].Item2; var q = ejes[i].Item1; var v = ejes[i].Item2;
            double b = Dot(u, v), d = Dot(u, Sub(p, q)), e = Dot(v, Sub(p, q)), den = 1 - b * b;
            double s = (b * e - d) / den, t = (e - b * d) / den;    // P = p + s u, Q = q + t v
            var P = Add(p, Mul(u, s)); var Q = Add(q, Mul(v, t)); var n = Sub(Q, P); double dist = Math.Sqrt(Dot(n, n));
            var xi = dist > 0.05 ? Unit(n) : Unit(Cross(u, v));
            Console.WriteLine(string.Format("normal común z{0}-z{1}: {2:0.00} mm", i - 1, i, dist));
            M.Add(new Marco { Nombre = new[] { "", "SR1_cintura_eje_J2", "SR2_E2_eje_J3", "SR3_E3_eje_J4", "SR4_caja_eje_J5" }[i],
                Pieza = new[] { "", "CB-210", "CB-301", "CB-401", "CB-603" }[i], O = Q, X = xi, Z = v });
        }
        M.Add(new Marco { Nombre = "SR5_ventosa_TCP", Pieza = "CB-606", O = Add(M[4].O, Mul(zJ5, Dot(Sub(tcp, M[4].O), zJ5))), X = M[4].X, Z = zJ5 });
        foreach (var k in M) k.Y = Cross(k.Z, k.X);

        // parámetros DH en la pose del CAD
        var tipo = new[] { "", "R", "R", "P", "R", "R" };
        Console.WriteLine("\ni  tipo  theta(°)   d(mm)    a(mm)   alfa(°)");
        for (int i = 1; i < M.Count; i++)
        {
            var A = M[i - 1]; var B = M[i]; var dp = Sub(B.O, A.O);
            double d = Dot(dp, A.Z), aa = Dot(dp, B.X);
            double th = Math.Atan2(Dot(Cross(A.X, B.X), A.Z), Dot(A.X, B.X)) * 180 / Math.PI;
            double al = Math.Atan2(Dot(Cross(A.Z, B.Z), B.X), Dot(A.Z, B.Z)) * 180 / Math.PI;
            double resto = Math.Sqrt(Math.Max(0, Dot(dp, dp) - d * d - aa * aa));
            Console.WriteLine(string.Format("{0}  {1}   {2,8:0.00} {3,8:0.00} {4,8:0.00} {5,8:0.00}   (fuera de DH: {6:0.00} mm, x{0}·z{7} = {8:0.000})",
                i, tipo[i], th, d, aa, al, resto, i - 1, Dot(B.X, A.Z)));
        }
        foreach (var k in M) Console.WriteLine(string.Format("{0,-20} {1}  O {2}  X {3}  Z {4}", k.Nombre, k.Pieza, F(k.O), F(k.X), F(k.Z)));
        if (a.Length < 2 || a[1] != "aplicar") return;

        foreach (var k in M) Crear(path[k.Pieza], X[k.Pieza], k);
    }
    static double[] Centro(Xf x, string pieza)
    {
        var p = SW.Open(pieza); PB.Use(p); var b = PB.BodyBox(); SW.App.CloseDoc(p.GetTitle());
        return x.Apply(new[] { (b[0] + b[3]) * 500, (b[1] + b[4]) * 500, (b[2] + b[5]) * 500 });
    }

    // crea el sistema de coordenadas en la pieza con valores numéricos y verifica sus ejes
    static void Crear(string pieza, Xf xp, Marco k)
    {
        var inv = xp.Inv();
        var o = inv.Apply(k.O); var ex = Unit(Dir(inv, k.X)); var ez = Unit(Dir(inv, k.Z)); var ey = Cross(ez, ex);
        var p = SW.Open(pieza);
        var old = (Feature)((PartDoc)p).FeatureByName(k.Nombre);
        if (old != null) { p.ClearSelection2(true); old.Select2(false, 0); p.EditDelete(); }
        // R columnas = ejes; probar convenciones de ángulos hasta que el sistema creado coincida
        double[,] Rm = { { ex[0], ey[0], ez[0] }, { ex[1], ey[1], ez[1] }, { ex[2], ey[2], ez[2] } };
        Feature f = null; string conv = "";
        foreach (var c in new[] { "XYZ", "ZYX" })
        {
            double ax, ay, az;
            if (c == "ZYX") { ay = Math.Asin(-Rm[2, 0]); ax = Math.Atan2(Rm[2, 1], Rm[2, 2]); az = Math.Atan2(Rm[1, 0], Rm[0, 0]); }   // R = Rz Ry Rx
            else if (Math.Abs(Rm[0, 2]) > 0.99999) { double sg = Math.Sign(Rm[0, 2]); ay = sg * Math.PI / 2; az = 0; ax = Math.Atan2(sg * Rm[1, 0], Rm[1, 1]); }   // bloqueo de cardán
            else { ay = Math.Asin(Rm[0, 2]); ax = Math.Atan2(-Rm[1, 2], Rm[2, 2]); az = Math.Atan2(-Rm[0, 1], Rm[0, 0]); }            // R = Rx Ry Rz
            p.ClearSelection2(true);
            f = (Feature)p.FeatureManager.CreateCoordinateSystemUsingNumericalValues(true, o[0] / 1000, o[1] / 1000, o[2] / 1000, true, ax, ay, az);
            if (f == null) { Console.WriteLine("  no se pudo crear " + k.Nombre); break; }
            f.Name = k.Nombre;
            var t = (double[])((MathTransform)p.Extension.GetCoordinateSystemTransformByName(k.Nombre)).ArrayData;
            // el transform lleva del modelo al sistema: filas = ejes del sistema en el modelo (o columnas, se prueban las dos)
            double e1 = 0, e2 = 0;
            for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) { e1 += Math.Abs(t[i * 3 + j] - Rm[j, i]); e2 += Math.Abs(t[i * 3 + j] - Rm[i, j]); }
            Console.WriteLine(string.Format("  {0} en {1} ({2}): error filas {3:0.000} columnas {4:0.000}", k.Nombre, Path.GetFileName(pieza), c, e1, e2));
            if (Math.Min(e1, e2) < 1e-3) { conv = c; break; }
            p.ClearSelection2(true); f.Select2(false, 0); p.EditDelete(); f = null;
        }
        if (f != null)
        {
            int e = 0, w = 0; p.ForceRebuild3(false);
            Console.WriteLine("  guardado " + p.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w) + " (" + conv + ")");
        }
        SW.App.CloseDoc(p.GetTitle());
    }
}
