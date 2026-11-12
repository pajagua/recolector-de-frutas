using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// VerMontaje5840.exe <root solidworks> [step del usuario] [e2]   (v7.13, solo lectura: no guarda nada)
// Verifica el montaje del motor del hombro (5840) en la torre CB-210 y la pieza de prueba CB-211, con cuerpos temporales
// en coordenadas de la torre (eje J2 a lo largo de y en (XA, ZC); pared de y 181.2 a 193.2, el motor apoya en 193.2):
//   1. CB-211 contra la torre: que sea exactamente el trozo de la torre (diferencia de volumen) y qué agujeros tiene.
//   2. Agujeros alineados: torre, CB-211, motor CB-502, cara medida por el usuario (STEP) y los 2 tornillos M4x10.
//   3. Interferencias: motor, cara del usuario y tornillos contra la torre y contra CB-211; rosca de los tornillos en el motor.
//   4. Apoyo de cada resalte del motor sobre la pared (área con material debajo).
//   5. Lugar del motor en la torre: juego alrededor de la caja y la lata (corriéndolas, sin el buje) y si se puede meter en línea recta.
//   6. Camino de la cabeza de cada M4x10 desde afuera hasta el avellanado.
//   7. Asiento del buje: diámetros sobre el eje J2, cuánto entra el buje (modelo y medido) y juego de costado.
//   8. Con "e2": E2 en CB-000 (sin ventana) contra el asiento: cubo de E2, buje CB-305 y eje CB-506.
class VerMontaje5840
{
    const double XA = 190.171, ZC = -86.723, YP = 193.2, Y0 = 181.2;
    static Modeler mod;
    static string R;

    static double Vol(Body2 b) { if (b == null) return 0; var mp = (double[])b.GetMassProperties(1.0); return mp == null ? 0 : mp[3] * 1e9; }
    static double Inter(Body2 a, Body2 b)
    {
        int err; var r = ((Body2)a.Copy()).Operations2((int)swBodyOperationType_e.SWBODYINTERSECT, (Body2)b.Copy(), out err) as object[];
        double v = 0; if (r != null) foreach (Body2 x in r) v += Vol(x); return v;
    }
    static double InterN(Body2 a, IEnumerable<Body2> bs) { return bs.Sum(b => Inter(a, b)); }
    static Body2 Comun(Body2 a, Body2 b)
    {
        int err; var r = ((Body2)a.Copy()).Operations2((int)swBodyOperationType_e.SWBODYINTERSECT, (Body2)b.Copy(), out err) as object[];
        return r == null || r.Length == 0 ? null : (Body2)r[0];
    }
    static Body2 Cortar(Body2 a, Body2 b)
    {
        int err; var r = ((Body2)a.Copy()).Operations2((int)swBodyOperationType_e.SWBODYCUT, (Body2)b.Copy(), out err) as object[];
        return r == null || r.Length == 0 ? null : r.Cast<Body2>().OrderByDescending(x => Vol(x)).First();
    }
    static Body2 Mover(Body2 b, double dx, double dy, double dz) { var c = (Body2)b.Copy(); c.ApplyTransform(Xf.Tr(dx, dy, dz).ToMath()); return c; }
    static Body2 Cil(double[] c, double[] ax, double d, double h)
    { return (Body2)mod.CreateBodyFromCyl(new double[] { c[0] / 1000, c[1] / 1000, c[2] / 1000, ax[0], ax[1], ax[2], d / 2000, h / 1000 }); }
    static List<Body2> Cuerpos(ModelDoc2 m)
    { return ((object[])((PartDoc)m).GetBodies2((int)swBodyType_e.swSolidBody, true)).Cast<Body2>().Select(b => (Body2)b.Copy()).ToList(); }
    static List<Body2> Pasar(List<Body2> bs, Xf x) { foreach (var b in bs) b.ApplyTransform(x.ToMath()); return bs; }

    // cilindros con eje paralelo a y: diámetro, centro (x, z) relativo al eje J2 y rango en y
    static List<double[]> Agujeros(IEnumerable<Body2> bs)
    {
        var l = new List<double[]>();
        foreach (var b in bs)
            foreach (Face2 f in (object[])b.GetFaces())
            {
                var s = (Surface)f.GetSurface(); if (!s.IsCylinder()) continue;
                var p = (double[])s.CylinderParams;
                if (Math.Abs(Math.Abs(p[4]) - 1) > 1e-6) continue;
                var bx = (double[])f.GetBox();
                l.Add(new[] { p[6] * 2000, p[0] * 1000 - XA, p[2] * 1000 - ZC, bx[1] * 1000, bx[4] * 1000 });
            }
        return l.GroupBy(h => string.Format("{0:0.00} {1:0.00} {2:0.00}", h[0], h[1], h[2])).Select(g => new[] { g.First()[0], g.First()[1], g.First()[2], g.Min(h => h[3]), g.Max(h => h[4]) })
                .OrderBy(h => -h[0]).ThenBy(h => h[1]).ThenBy(h => h[2]).ToList();
    }
    static string F(double v) { return v.ToString("0.00"); }

    static void Main(string[] a)
    {
        R = a[0];
        bool conE2 = a.Skip(1).Any(x => x == "e2");
        string step = a.Skip(1).FirstOrDefault(x => x != "e2") ?? @"C:\Users\tikip\Desktop\CaraCajaReductora_5840.step";
        SW.Connect();
        mod = (Modeler)SW.App.GetModeler();
        Func<string, string, string> P = (s, f) => Path.Combine(R, s, f);

        // ---- cuerpos en coordenadas de la torre ----
        var asm = SW.Open(P("02_Cintura_J1", "CB-200_Cintura_J1.SLDASM")); var ad = (AssemblyDoc)asm; ad.ResolveAllLightWeightComponents(false);
        var comps = ((object[])ad.GetComponents(true)).Cast<Component2>().ToList();
        var cT = comps.First(c => c.Name2.StartsWith("CB-210")); var cM = comps.First(c => c.Name2.StartsWith("CB-502"));
        var cS = comps.Where(c => c.Name2.StartsWith("HW_M4x10")).ToList();
        var a2t = Xf.FromComp(cT).Inv();
        Func<Component2, List<Body2>> enTorre = c => Pasar(Cuerpos((ModelDoc2)c.GetModelDoc2()), Xf.FromComp(c).Then(a2t));
        var T = enTorre(cT)[0];
        var M = enTorre(cM)[0];
        var S = cS.Select(c => enTorre(c)[0]).ToList();
        var m2t = Xf.FromComp(cM).Then(a2t);   // motor -> torre

        var pz = SW.Open(P("12_Pruebas", "CB-211_Prueba_Montaje_5840.SLDPRT"));
        var marco = Xf.Rows(new double[] { -1, 0, 0 }, new double[] { 0, 0, 1 }, new double[] { 0, 1, 0 }, new double[] { XA, Y0, ZC });   // prueba -> torre (Montaje5840.cs)
        string marcoProp, marcoRes; bool marcoCache;
        pz.Extension.CustomPropertyManager[""].Get5("Marco", false, out marcoProp, out marcoRes, out marcoCache);
        if (!string.IsNullOrEmpty(marcoProp))
        {
            var v = marcoProp.Split(' ').Select(t => double.Parse(t, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            marco = Xf.Rows(new[] { v[0], v[1], v[2] }, new[] { v[3], v[4], v[5] }, new[] { v[6], v[7], v[8] }, new[] { v[9], v[10], v[11] });
        }
        var Pb = Pasar(Cuerpos(pz), marco)[0];

        Body2 U = null;
        if (File.Exists(step))
        {
            int err = 0;
            var imp = (ImportStepData)SW.App.GetImportFileData(step);
            var um = (ModelDoc2)SW.App.LoadFile4(step, "r", imp, ref err);
            if (um != null)
            {
                U = Pasar(Cuerpos(um), Xf.Tr(-20, -38, -2).Then(m2t))[0];   // cara del usuario: esquina (0,0), eje en (20, 38), apoyo de los resaltes en z = 2
                SW.App.CloseDoc(um.GetTitle());
            }
            Console.WriteLine("STEP del usuario: " + (U != null ? "cargado, vol " + Vol(U).ToString("0") + " mm3" : "NO se pudo importar (err " + err + ")"));
        }

        // ---- 1. la pieza de prueba es la torre ----
        var bbP = (double[])Pb.GetBodyBox();
        double vP = Vol(Pb), vPT = Inter(Pb, T);
        Console.WriteLine("\n1. CB-211 contra la torre");
        Console.WriteLine("   volumen CB-211 " + vP.ToString("0") + " mm3, comun con la torre " + vPT.ToString("0") + " mm3 (" + (100 * vPT / vP).ToString("0.00") + " %)");
        var boxT = (Body2)mod.CreateBodyFromBox3(new double[] { (bbP[0] + bbP[3]) / 2, bbP[1], (bbP[2] + bbP[5]) / 2, 0, 1, 0, bbP[3] - bbP[0], bbP[5] - bbP[2], bbP[4] - bbP[1] });
        double vTB = Inter(T, boxT);
        Console.WriteLine("   torre dentro de la caja de CB-211 " + vTB.ToString("0") + " mm3 -> diferencia " + (vTB - vPT).ToString("0.0") + " mm3");
        Console.WriteLine("   caja de CB-211 (coordenadas de la torre, relativo al eje J2): x " + F(bbP[0] * 1000 - XA) + " .. " + F(bbP[3] * 1000 - XA) +
            ", y " + F(bbP[1] * 1000) + " .. " + F(bbP[4] * 1000) + ", z " + F(bbP[2] * 1000 - ZC) + " .. " + F(bbP[5] * 1000 - ZC));
        Console.WriteLine("   agujeros de CB-211 (D, x, z respecto del eje J2, y):");
        foreach (var h in Agujeros(new[] { Pb })) Console.WriteLine("     D" + F(h[0]) + "  (" + F(h[1]) + ", " + F(h[2]) + ")  y " + F(h[3]) + " .. " + F(h[4]));

        // ---- 2. alineación ----
        Console.WriteLine("\n2. Centros de los agujeros del par de abajo (x, z respecto del eje J2)");
        Action<string, IEnumerable<Body2>, double> lista = (n, bs, d) =>
        {
            var hs = Agujeros(bs).Where(h => Math.Abs(h[0] - d) < 0.05 && h[1] < -20).ToList();
            Console.WriteLine("   " + n.PadRight(26) + string.Join("   ", hs.Select(h => "(" + F(h[1]) + ", " + F(h[2]) + ")")));
        };
        lista("torre CB-210 D4.5", new[] { T }, 4.5);
        lista("prueba CB-211 D4.5", new[] { Pb }, 4.5);
        lista("motor CB-502 D3.3", new[] { M }, 3.3);
        if (U != null) lista("cara medida (STEP) D4", new[] { U }, 4.0);
        lista("tornillos M4x10 D4", S, 4.0);
        lista("avellanado torre D11.5", new[] { T }, 11.5);

        // ---- 3. interferencias ----
        Console.WriteLine("\n3. Interferencias (mm3)");
        Console.WriteLine("   motor CB-502   / torre " + F(Inter(M, T)) + "   / prueba " + F(Inter(M, Pb)));
        if (U != null) Console.WriteLine("   cara medida    / torre " + F(Inter(U, T)) + "   / prueba " + F(Inter(U, Pb)));
        for (int i = 0; i < S.Count; i++)
            Console.WriteLine("   tornillo " + (i + 1) + "     / torre " + F(Inter(S[i], T)) + "   / prueba " + F(Inter(S[i], Pb)) + "   / motor " + F(Inter(S[i], M)) +
                (U != null ? "   / cara medida " + F(Inter(S[i], U)) : "") + "   (rosca modelada concentrica: pi (2^2 - 1.65^2) 4.5 = 18.1)");

        // ---- 4. apoyo de los resaltes ----
        Console.WriteLine("\n4. Apoyo de los resaltes del motor (D7) sobre la pared: area con material en los 0.1 mm de la cara (de 38.5 mm2)");
        var res = new[] { new[] { -13.5, -28.5, 1 }, new[] { 13.5, -28.5, 1 }, new[] { -13.5, 10.5, 0 }, new[] { 13.5, 10.5, 0 }, new[] { -5.5, -15.5, 0 }, new[] { 5.5, -15.5, 0 }, new[] { -9.26, -24.26, 0 } };
        var dirIn = m2t.Apply(new double[] { 0, 0, 1 }); var o0 = m2t.Apply(new double[3]); var nIn = new[] { dirIn[0] - o0[0], dirIn[1] - o0[1], dirIn[2] - o0[2] };   // +z del motor = hacia la pared
        foreach (var r in res)
        {
            var c = m2t.Apply(new[] { r[0], r[1], 0.0 });
            var disco = Cil(c, nIn, 7, 0.1);
            double aT = Inter(disco, T) / 0.1, aP = Inter(disco, Pb) / 0.1;
            Console.WriteLine("   resalte (" + F(r[0]) + ", " + F(r[1]) + ")" + (r[2] > 0 ? " con tornillo" : (r[0] == -9.26 ? " punta colisa " : "             ")) +
                "  torre " + aT.ToString("0.0") + "  prueba " + aP.ToString("0.0") + "   (cara a y = " + F(c[1]) + ")");
        }

        // ---- 5. lugar del motor ----
        Console.WriteLine("\n5. Lugar del motor en la torre (cuerpo del motor CB-502, sin el eje de salida)");
        double sep = 0.2;
        Console.WriteLine("   separado " + sep + " mm de la pared: choque con la torre " + F(Inter(Mover(M, 0, sep, 0), T)) + " mm3");
        // el buje frontal (D18 x 5 en el modelo, D18 x 4 medido) va en el asiento D18.3: el juego de costado del buje se mide en 7
        var zonaBuje = Cil(new[] { XA, 186.0, ZC }, new double[] { 0, 1, 0 }, 21, 195.15 - 186.0);
        var Mc = Cortar(M, zonaBuje) ?? M;
        Console.WriteLine("   caja y lata, sin el buje (" + F(Vol(M) - Vol(Mc)) + " mm3 sacados):");
        foreach (var d in new[] { new[] { 1.0, 0, 0, 0 }, new[] { -1.0, 0, 0, 0 }, new[] { 0, 0, 1.0, 0 }, new[] { 0, 0, -1.0, 0 } })
        {
            double libre = -1;
            foreach (var s in new[] { 0.5, 1, 2, 3, 5, 8 })
            {
                if (Inter(Mover(Mc, d[0] * s, sep, d[2] * s), T) > 0.01) break;
                libre = s;
            }
            string n = d[0] > 0 ? "+x (hacia arriba)" : d[0] < 0 ? "-x (hacia la lata)" : d[2] > 0 ? "+z" : "-z";
            Console.WriteLine("   corrido hacia " + n.PadRight(20) + (libre < 0 ? "choca antes de 0.5 mm" : "libre hasta " + libre + " mm (probado 0.5/1/2/3/5/8)"));
        }
        // meterlo: alejarlo de la pared en +y hasta salir del alojamiento
        double pasoLibre = 0;
        for (double s = 1; s <= 120; s += 1)
        {
            if (Inter(Mover(M, 0, s, 0), T) > 0.01) { Console.WriteLine("   en linea recta por +y (entrando desde adentro de la torre): choca a " + s + " mm de su lugar"); pasoLibre = -s; break; }
            pasoLibre = s;
        }
        if (pasoLibre > 0) Console.WriteLine("   en linea recta por +y: libre al menos " + pasoLibre + " mm (cuerpo de la torre hasta y = " + F(((double[])T.GetBodyBox())[4] * 1000) + ")");
        // ---- 6. acceso de la cabeza del tornillo desde afuera ----
        Console.WriteLine("\n6. Camino de la cabeza del M4x10 (D7, DIN 912) desde afuera hasta el avellanado, a lo largo del eje del tornillo");
        foreach (var s in S)
        {
            var hs = Agujeros(new[] { s }).Where(h => Math.Abs(h[0] - 4.0) < 0.05).ToList(); if (hs.Count == 0) continue;
            double cx = XA + hs[0][1], cz = ZC + hs[0][2];
            double rr = Math.Sqrt(hs[0][1] * hs[0][1] + hs[0][2] * hs[0][2]);
            Console.Write("   tornillo en (" + F(hs[0][1]) + ", " + F(hs[0][2]) + "), a " + F(rr) + " del eje J2:");
            foreach (var d in new[] { 7.0, 7.4, 8.0 })
            {
                var cam = Cil(new[] { cx, 165.0, cz }, new double[] { 0, 1, 0 }, d, 187.7 - 165.0 - 4.0);   // de y 165 hasta donde queda la cabeza (183.7)
                Console.Write("  D" + d.ToString("0.0") + " choca torre " + F(Inter(cam, T)) + " / prueba " + F(Inter(cam, Pb)) + " mm3;");
            }
            Console.WriteLine();
        }
        // ---- 7. asiento del buje ----
        Console.WriteLine("\n7. Asiento del buje (cilindros sobre el eje J2, D de 17 a 33)");
        Action<string, Body2> ejes = (n, b) =>
        {
            foreach (var h in Agujeros(new[] { b }).Where(h => h[0] > 17 && h[0] < 33 && Math.Abs(h[1]) < 0.5 && Math.Abs(h[2]) < 0.5))
                Console.WriteLine("   " + n.PadRight(8) + " D" + F(h[0]) + "  y " + F(h[3]) + " .. " + F(h[4]));
        };
        ejes("torre", T); ejes("prueba", Pb);
        var bujes = new List<Tuple<string, Body2>> { Tuple.Create("buje del modelo (CB-502)", Comun(M, zonaBuje)) };
        if (U != null) bujes.Add(Tuple.Create("buje medido (STEP)", Comun(U, zonaBuje)));
        foreach (var t in bujes)
        {
            if (t.Item2 == null) { Console.WriteLine("   " + t.Item1 + ": no se encontro"); continue; }
            var hb = Agujeros(new[] { t.Item2 }).Where(h => h[0] > 17).ToList();   // cara exterior del buje
            var bb = (double[])t.Item2.GetBodyBox();
            double yb0 = hb.Count > 0 ? hb[0][3] : bb[1] * 1000, yb1 = hb.Count > 0 ? hb[0][4] : bb[4] * 1000;
            double entra = Math.Max(0, Math.Min(yb1, YP) - Math.Max(yb0, YP - 2.0));
            Console.WriteLine("   " + t.Item1 + ": D" + (hb.Count > 0 ? F(hb[0][0]) : "?") + ", y " + F(yb0) + " .. " + F(yb1) +
                ": entra " + F(entra) + " mm en el asiento (y " + F(YP - 2.0) + " .. " + F(YP) + ")");
            Console.Write("     corrido de costado (el peor de +-x y +-z), choque con torre / prueba en mm3:");
            foreach (var s in new[] { 0.10, 0.14, 0.16, 0.20, 0.30 })
            {
                double vt = 0, vp = 0;
                foreach (var d in new[] { new[] { 1.0, 0 }, new[] { -1.0, 0 }, new[] { 0, 1.0 }, new[] { 0, -1.0 } })
                {
                    var mv = Mover(t.Item2, d[0] * s, 0, d[1] * s);
                    vt = Math.Max(vt, Inter(mv, T)); vp = Math.Max(vp, Inter(mv, Pb));
                }
                Console.Write("  " + s.ToString("0.00") + ": " + F(vt) + " / " + F(vp));
            }
            Console.WriteLine();
        }
        SW.App.CloseDoc(pz.GetTitle());
        SW.App.CloseDoc(asm.GetTitle());   // sin guardar: solo se leyó
        if (conE2) VerE2();
    }

    // ---- 8. E2 contra el asiento (CB-000 abierto sin ventana; si ya estaba abierto no se cierra) ----
    static void VerE2()
    {
        Console.WriteLine("\n8. E2 contra el asiento del buje (CB-000)");
        string pa = Path.Combine(R, "CB-000_CitriBot_Ensamblaje_General.SLDASM");
        var abierto = (ModelDoc2)SW.App.GetOpenDocumentByName(pa);
        ModelDoc2 m = abierto;
        if (m == null)
        {
            SW.App.DocumentVisible(false, (int)swDocumentTypes_e.swDocASSEMBLY);
            try { m = SW.Open(pa); } finally { SW.App.DocumentVisible(true, (int)swDocumentTypes_e.swDocASSEMBLY); }
        }
        if (m == null) { Console.WriteLine("   no se pudo abrir CB-000"); return; }
        var ad = (AssemblyDoc)m; ad.ResolveAllLightWeightComponents(false);
        var comps = ((object[])ad.GetComponents(false)).Cast<Component2>().Where(c => !c.IsSuppressed()).ToList();
        Func<Component2, string> arch = c => Path.GetFileName(c.GetPathName() ?? "");
        var cT = comps.FirstOrDefault(c => arch(c).StartsWith("CB-210"));
        if (cT == null) { Console.WriteLine("   no esta la torre"); if (abierto == null) SW.App.CloseDoc(m.GetTitle()); return; }
        var a2t = Xf.FromComp(cT).Inv();
        Func<Component2, List<Body2>> enTorre = c => Pasar(Cuerpos((ModelDoc2)c.GetModelDoc2()), Xf.FromComp(c).Then(a2t));
        var T = enTorre(cT)[0];
        var asiento = Comun(T, Cil(new[] { XA, YP - 2.0, ZC }, new double[] { 0, 1, 0 }, 32.0, 2.0));   // anillo D32/D18.3
        Console.WriteLine("   asiento en la torre de CB-000: " + (asiento == null ? "NO ESTA" : F(Vol(asiento)) + " mm3"));
        foreach (var pre in new[] { "CB-301B", "CB-305", "CB-506", "CB-502" })
            foreach (var c in comps.Where(c => arch(c).StartsWith(pre)))
            {
                var bs = enTorre(c);
                double vT = bs.Sum(b => Inter(b, T)), vA = asiento == null ? 0 : bs.Sum(b => Inter(b, asiento));
                string hueco = "";
                if (asiento != null && pre != "CB-502")
                {
                    double libre = -1;
                    foreach (var s in new[] { 0.5, 1.0, 1.5, 2.0, 2.5, 3.0, 4.0, 5.0, 7.0, 10.0 })
                    {
                        if (bs.Sum(b => Inter(Mover(b, 0, s, 0), asiento)) > 0.01) break;
                        libre = s;
                    }
                    hueco = "; corrido hacia el motor: " + (libre < 0 ? "toca el asiento antes de 0.5 mm" : libre >= 10 ? "no llega al asiento (probado 10 mm)" : "libre hasta " + libre + " mm");
                }
                double ymax = bs.Max(b => ((double[])b.GetBodyBox())[4]) * 1000;
                Console.WriteLine("   " + arch(c).PadRight(40) + " choque con la torre " + F(vT) + " mm3, con el asiento " + F(vA) + " mm3" + hueco + " (y max " + F(ymax) + ")");
            }
        if (abierto == null) SW.App.CloseDoc(m.GetTitle());
    }
}
