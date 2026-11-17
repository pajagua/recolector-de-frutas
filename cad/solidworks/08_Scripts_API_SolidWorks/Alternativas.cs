using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
using SolidWorks.Interop.swmotionstudy;   // compilar con -r:SolidWorks.Interop.swmotionstudy.dll

// Alternativas.exe <root solidworks> [piezasC|piezasD|subC|subD|asmC|asmD|estudioC|estudioD ...]   (v7.14, 2026-11-17)
// Dos prismáticos J3 solos, sobre la variante A (un tubo 30x30x2 L440, NEMA 17 vertical en el cubo de E2, x = -58):
//   C · cremallera y piñón: piñón m1 de 18 dientes (D18 primitivo) en el eje del NEMA, en el lugar de la polea GT2;
//       cremallera impresa en 2 tramos de 58 dientes (182.2 mm, entran en la Ender) atornillada arriba del tubo.
//       E2-C = copia de CB-301 con una ranura para que la cremallera salga por el frente con el tubo.
//   D · cable con tambor: tambor D16 x 13 en el eje del NEMA, polea acanalada D16 en la cola (lugar de la polea loca),
//       cable D1 con los dos extremos en la pinza del tapón CB-403 (mismo recorrido que la correa de la v7A). E2 sin cambios.
// Todo en coordenadas de E2 (eje del prismático = X, y hacia arriba del brazo, eje J2 = Z en x = -80), J3 retraído (q3 = 0).
class Alternativas
{
    static string R;
    const string DIR = "14_Prismatico_Alternativas", COM = "05_Comerciales", H = "03_Hombro_E2_J2", J = "04_Prismatico_E3_J3";
    static string P(string sub, string f) { return Path.Combine(R, sub, f); }
    static double[] V(params double[] v) { return v; }

    const double Stroke = 340, XFront = 13, XFlIn = 15, XFlOut = 25;
    const double XMotor = -58, YFace = 47, XIdler = -442, YIdler = 21;
    // cremallera / piñón (módulo 1, ángulo de presión 20°)
    const double Mod = 1, ZPin = 18, RPas = 9, Juego = 0.1;
    const double ZPrim = -(RPas + Juego);              // línea primitiva de la cremallera
    const int DientesTramo = 58; static double LTramo = DientesTramo * Math.PI * Mod;
    const double XCrem0 = -410;                         // atrás de la cremallera con q3 = 0
    const double YPin0 = 19;                            // cara de abajo del piñón (dientes 19..27, cubo 27..35)
    // tambor / cable
    const double RTam = 6.5, YTam0 = 16, RCable = 0.5, YCable = 23.5;   // núcleo D13: el cable (centro r 7) corre en z 6.5..7.5, dentro de la ranura de la pinza (z 6..7.8)
    static double RCab { get { return RTam + RCable; } }

    static ModelDoc2 M; static AssemblyDoc A;

    static void Main(string[] a)
    {
        R = a[0]; SW.Connect();
        Directory.CreateDirectory(Path.Combine(R, DIR));
        foreach (var k in a.Skip(1))
        {
            if (k == "cerrar")   // cierra los documentos nuevos sin guardar (sin ruta) que dejó una corrida que falló
                foreach (string t in ((object[])SW.App.GetDocuments() ?? new object[0]).Cast<ModelDoc2>().Where(d => string.IsNullOrEmpty(d.GetPathName())).Select(d => d.GetTitle()).ToList())
                { Console.WriteLine("cierro " + t); SW.App.CloseDoc(t); }
            if (k == "infoC" || k == "infoD")
            {
                var m = SW.Open(P(DIR, k == "infoC" ? "CB-450C_Prismatico_Cremallera_Solo.SLDASM" : "CB-450D_Prismatico_Cable_Solo.SLDASM"));
                foreach (var c in Comps(m)) Console.WriteLine(c.Name2 + "  " + Xf.FromComp(c));
                SW.App.CloseDoc(m.GetTitle());
            }
            if (k == "copiarC") CopiarRobotC();
            if (k == "armarC") ArmarRobotC();
            if (k == "infoCam") InfoCam();
            if (k == "camC") CamarasC();
            if (k.StartsWith("caras:"))   // caras planas de una pieza con normal +z o -z: caras:<ruta relativa>
            {
                var m = SW.Open(Q(k.Substring(6)));
                foreach (Body2 b in (object[])((PartDoc)m).GetBodies2((int)swBodyType_e.swSolidBody, true))
                    foreach (Face2 f in (object[])b.GetFaces())
                    {
                        if (!((Surface)f.GetSurface()).IsPlane()) continue;
                        double[] n = (double[])f.Normal; if (Math.Abs(Math.Abs(n[2]) - 1) > 1e-6) continue;
                        double[] bx = (double[])f.GetBox(); double ar = f.GetArea() * 1e6; if (ar < 50) continue;
                        Console.WriteLine("  n " + n[2].ToString("+0;-0") + " z " + (bx[2] * 1000).ToString("0.0") + "  x " + (bx[0] * 1000).ToString("0") + ".." + (bx[3] * 1000).ToString("0") + "  y " + (bx[1] * 1000).ToString("0") + ".." + (bx[4] * 1000).ToString("0") + "  " + ar.ToString("0") + " mm2");
                    }
                SW.App.CloseDoc(m.GetTitle());
            }
            if (k == "finJ2") FinalJ2();
            if (k == "piezasC") { E2C(); Cremallera(); Pinon(); }
            if (k == "piezasD") { Tambor(); PoleaAcanalada(); Cable(); }
            if (k == "poleaD") PoleaAcanalada();
            if (k == "subC") SubE3("C");
            if (k == "subD") SubE3("D");
            if (k == "asmC") Asm("C");
            if (k == "asmD") Asm("D");
            if (k == "verC") Ver("C");
            if (k == "verD") Ver("D");
            if (k == "estudioC") Estudio("C");
            if (k == "estudioD") Estudio("D");
        }
    }

    // ================= robot completo con la cremallera (CB-000C) =================
    const string TOP_A = "CB-000A_CitriBot_v7A_Correa.SLDASM", TOP_C = "CB-000C_CitriBot_v7C_Cremallera.SLDASM";
    static string H300 = Path.Combine(H, "CB-300_Hombro_E2_J2.SLDASM"), P400 = Path.Combine(J, "CB-400_Prismatico_E3_J3.SLDASM"),
                  B100 = Path.Combine("01_Base_J0", "CB-100_Base_J0.SLDASM"), C150 = Path.Combine("01_Base_J0", "CB-150_Modulo_Camaras.SLDASM"),
                  E2A = Path.Combine(H, "CB-301_Eslabon_E2_mod5840.SLDPRT");
    static string H300C = Path.Combine(DIR, "CB-300C_Hombro_E2_Cremallera.SLDASM"), P400C = Path.Combine(DIR, "CB-400C_Prismatico_E3_Cremallera.SLDASM"),
                  B100C = Path.Combine(DIR, "CB-100C_Base_J0_Camaras_Atras.SLDASM"), C150C = Path.Combine(DIR, "CB-150C_Modulo_Camaras_Atras.SLDASM"),
                  E2C_ = Path.Combine(DIR, "CB-301C_Eslabon_E2_Cremallera.SLDPRT");
    static string Q(string rel) { return Path.Combine(R, rel); }
    static void Copiar(string src, string dst)
    {
        if (File.Exists(Q(dst))) throw new Exception("ya existe " + dst);
        File.Copy(Q(src), Q(dst)); File.SetAttributes(Q(dst), FileAttributes.Normal); Console.WriteLine("copiado " + src + " -> " + dst);
    }
    static void Ref(string asm, string oldRef, string newRef)
    {
        bool ok = SW.App.ReplaceReferencedDocument(Q(asm), Q(oldRef), Q(newRef));
        Console.WriteLine((ok ? "ref ok   " : "REF FALLO ") + Path.GetFileName(asm) + ": " + Path.GetFileName(oldRef) + " -> " + Path.GetFileName(newRef));
        if (!ok) throw new Exception("fallo ReplaceReferencedDocument");
    }
    // copias de CB-000A y sus subconjuntos que cambian; los documentos tienen que estar cerrados
    static void CopiarRobotC()
    {
        // las copias son archivos nuevos: no pueden estar abiertas. Los originales solo se leen (pueden estar abiertos por el usuario).
        Copiar(TOP_A, TOP_C); Copiar(H300, H300C); Copiar(P400, P400C); Copiar(B100, B100C); Copiar(C150, C150C);
        Ref(H300C, E2A, E2C_);
        Ref(B100C, C150, C150C);
        Ref(TOP_C, H300, H300C); Ref(TOP_C, P400, P400C); Ref(TOP_C, B100, B100C);
    }
    // CB-300C: sin poleas ni correa, con el piñón; CB-400C: con los 2 tramos de cremallera
    static void ArmarRobotC()
    {
        var m = SW.Open(Q(H300C)); M = m; A = (AssemblyDoc)m; Activar(m);
        foreach (var c in Comps(m).Where(c => c.Name2.StartsWith("CB-432C")).ToList()) { m.ClearSelection2(true); c.Select4(false, null, false); m.EditDelete(); Console.WriteLine("  saco " + c.Name2); }
        {
            foreach (var c in Comps(m).Where(c => c.Name2.StartsWith("HW_Polea_GT2_20T") || c.Name2.StartsWith("HW_Polea_Loca_GT2") || c.Name2.StartsWith("CB-514")).ToList())
            { Console.WriteLine("  saco " + c.Name2); m.ClearSelection2(true); c.Select4(false, null, false); m.EditDelete(); }
            // CB-300 no está en el marco de E2: el piñón va respecto de CB-301C
            var xe2 = Xf.FromComp(Comps(m).First(c => c.Name2.StartsWith("CB-301")));
            Add(P(DIR, "CB-432C_Pinon_m1_18d.SLDPRT"), Xf.Tr(XMotor, YPin0, 0).Then(xe2), true);
            Desc("Hombro E2 de la alternativa C (cremallera y pinon): CB-300 con E2-C (CB-301C) y el pinon m1 18T en el NEMA, sin poleas ni correa. Coordenadas de E2.");
            Guardar(m);
        }
        Console.WriteLine("  E2: " + Comps(m).First(c => c.Name2.StartsWith("CB-301")).GetPathName());
        SW.App.CloseDoc(m.GetTitle());
        m = SW.Open(Q(P400C)); M = m; A = (AssemblyDoc)m; Activar(m);
        foreach (var c in Comps(m).Where(c => c.Name2.StartsWith("CB-431C")).ToList()) { m.ClearSelection2(true); c.Select4(false, null, false); m.EditDelete(); Console.WriteLine("  saco " + c.Name2); }
        {
            var xt = Xf.FromComp(Comps(m).First(c => c.Name2.StartsWith("CB-401")));   // el tubo está modelado en el marco de E2
            Add(P(DIR, "CB-431C_Cremallera_m1_58d.SLDPRT"), xt, true);
            Add(P(DIR, "CB-431C_Cremallera_m1_58d.SLDPRT"), Xf.Tr(LTramo, 0, 0).Then(xt), true);
            Desc("Prismatico E3 de la alternativa C: tubo 30x30x2 L440 + brida + tapon + 2 tramos de cremallera m1 + herramienta. Coordenadas de E2, J3 retraido.");
            Guardar(m);
        }
        SW.App.CloseDoc(m.GetTitle());
    }
    // cajas de los componentes del módulo de cámaras (marco árbol) y de lo que hay en el tablero
    static void InfoCam()
    {
        foreach (var f in new[] { C150, B100 })
        {
            var m = SW.Open(Q(f)); ((AssemblyDoc)m).ResolveAllLightWeightComponents(false);
            Console.WriteLine("== " + f);
            foreach (var c in Comps(m))
            {
                var b = (double[])c.GetBox(false, false);
                if (b != null) Console.WriteLine("  " + c.Name2.PadRight(46) + " " + SW.Box(b));
                if (c.Name2.StartsWith("CB-150")) Console.WriteLine("     xf " + Xf.FromComp(c));
            }
            SW.App.CloseDoc(m.GetTitle());
        }
    }

    // ================= cámaras fuera del barrido de la cola (CB-150C) =================
    // La cola de E2 (377 mm detrás de J2, plano del brazo a 83 mm de J1, E2 de ±50) barre un círculo de ≈ 400 mm de radio
    // alrededor de J1 y todo el tablero (esquinas a 250 mm) queda adentro. El mástil va afuera, a 431 mm de J1, atrás y del lado +y
    // (opuesto al plano del brazo cuando apunta al árbol), sobre un brazo de tubo 20x20x1.5 apoyado en el tablero.
    // Marco árbol (el de CB-150): origen en J1 sobre el tablero, x hacia el árbol, y a lo largo del riel, z arriba.
    const double MX = -415, MY = 115, DZ = 16;                     // eje del mástil; el mástil sube 16 mm (apoya en el codo, no en el tablero)
    const double YAWN = -8.18, PITCHN = 31.43;                      // al centro de la esfera (385, 0, 270): rumbo como aim.py, cabeceo +4.65° (corrección medida en el montaje viejo)
    const double FX1 = -80, FY1 = 130, YAW1 = -15.62, PITCH1 = 43.82, HM = 638;
    static double Rad(double d) { return d * Math.PI / 180; }
    static Xf Mast(double fx, double fy, double yaw) { double c = Math.Cos(Rad(yaw)), s = Math.Sin(Rad(yaw)); return Xf.Rows(V(c, s, 0), V(-s, c, 0), V(0, 0, 1), V(fx, fy, 0)); }
    static Xf Bar(double fx, double fy, double yaw, double pitch) { double c = Math.Cos(Rad(pitch)), s = Math.Sin(Rad(pitch)); return Xf.Rows(V(c, 0, -s), V(0, 1, 0), V(s, 0, c), V(0, 0, HM + 15)).Then(Mast(fx, fy, yaw)); }
    const double XBar0 = MX - 12, XBar1 = -60;
    static void BrazoMastil()
    {
        string dst = P(DIR, "CB-163C_Brazo_Mastil_20x20x1.5_L367.SLDPRT");
        PB.NewPart();
        PB.Sketch('R'); PB.Rect(0, MY - 10, 20, MY + 10); PB.Rect(1.5, MY - 8.5, 18.5, MY + 8.5); PB.Extrude(XBar0, XBar1 - XBar0, false, "Tubo_20x20x1.5");
        PB.Sketch('F'); foreach (double x in new[] { -80.0, -140.0 }) PB.Circle(x, MY, 5.3); PB.Extrude(-1, 22, true, "Agujeros_M5_al_tablero");
        PB.Sketch('T'); PB.Circle(MX + 10, 10, 4.4); PB.Extrude(MY - 12, 24, true, "Agujero_M4_codo");
        PB.PartColor(0.78, 0.8, 0.83);
        PB.Prop("Descripcion", "Brazo del mastil de las camaras (alternativa C): tubo de aluminio 20x20x1.5 de 367 mm, apoyado en el tablero, saca el mastil 265 mm detras del borde");
        PB.Prop("Material", "Aluminio 20x20x1.5 (mismo tubo que el mastil CB-152)");
        PB.Prop("Notas", "Marco arbol: x " + XBar0 + ".." + XBar1 + ", y " + (MY - 10) + ".." + (MY + 10) + ", z 0..20. 2 x M5 pasantes al tablero en x -80 y -140 (agujerear el tablero al montar), M4 al codo CB-165C.");
        PB.Report("Brazo mastil"); PB.Save(dst); SW.App.CloseDoc(PB.M.GetTitle());
    }
    static void CodoMastil()
    {
        string dst = P(DIR, "CB-165C_Codo_Mastil.SLDPRT");
        PB.NewPart();
        PB.Sketch('F'); PB.Rect(MX - 22, MY - 22, MX + 22, MY + 22); PB.Extrude(-6, 62, false, "Bloque_44x44x62");
        PB.Sketch('R'); PB.Rect(-0.2, MY - 10.2, 20.2, MY + 10.2); PB.Extrude(MX - 12, 35, true, "Encastre_brazo_20.4");
        // encastre del mástil girado el rumbo de las cámaras (el cabezal no gira respecto del tubo)
        double c = Math.Cos(Rad(YAWN)), s = Math.Sin(Rad(YAWN)), h = 10.2;
        var q = new List<double>();
        foreach (var uv in new[] { V(-h, -h), V(h, -h), V(h, h), V(-h, h) }) { q.Add(MX + uv[0] * c - uv[1] * s); q.Add(MY + uv[0] * s + uv[1] * c); }
        PB.Sketch('F'); PB.Poly(q.ToArray()); PB.Extrude(DZ + 6, 35, true, "Encastre_mastil_20.4");
        PB.Sketch('T'); PB.Circle(MX + 10, 10, 4.4); PB.Extrude(MY - 23, 46, true, "Agujero_M4_brazo");
        PB.Sketch('T'); PB.Circle(MX, 42, 4.4); PB.Extrude(MY - 23, 46, true, "Agujero_M4_mastil");
        PB.PartColor(0.95, 0.55, 0.1);
        PB.Prop("Descripcion", "Codo del mastil de las camaras (alternativa C): une el brazo CB-163C con el mastil CB-152, girado " + YAWN + " grados hacia la esfera");
        PB.Prop("Material", "PETG, 40 % de relleno, 4 paredes");
        PB.Prop("Notas", "Marco arbol, eje del mastil en (" + MX + ", " + MY + "). Encastres 20.4: brazo de x " + (MX - 12) + " a " + (MX + 22) + ", mastil desde z " + (DZ + 6) + ". 2 x M4x50 pasantes con tuerca.");
        PB.Report("Codo"); PB.Save(dst); SW.App.CloseDoc(PB.M.GetTitle());
    }
    static void CamarasC()
    {
        BrazoMastil(); CodoMastil();
        var m = SW.Open(Q(C150C)); M = m; A = (AssemblyDoc)m; Activar(m); A.ResolveAllLightWeightComponents(false);
        if (Comps(m).Any(c => c.Name2.StartsWith("CB-165C"))) { Console.WriteLine("CB-150C ya tiene el codo"); SW.App.CloseDoc(m.GetTitle()); return; }
        var mast = Mast(FX1, FY1, YAW1).Inv().Then(Mast(MX, MY, YAWN)).Then(Xf.Tr(0, 0, DZ));
        var bar = Bar(FX1, FY1, YAW1, PITCH1).Inv().Then(Bar(MX, MY, YAWN, PITCHN)).Then(Xf.Tr(0, 0, DZ));
        foreach (var c in Comps(m).ToList())
        {
            if (c.Name2.StartsWith("CB-151")) { m.ClearSelection2(true); c.Select4(false, null, false); m.EditDelete(); Console.WriteLine("  saco " + c.Name2); continue; }
            bool fijo = c.Name2.StartsWith("CB-152") || c.Name2.StartsWith("CB-153");
            c.Transform2 = Xf.FromComp(c).Then(fijo ? mast : bar).ToMath();
            Console.WriteLine("  " + c.Name2 + (fijo ? " mastil" : " cabezal"));
        }
        Add(P(DIR, "CB-163C_Brazo_Mastil_20x20x1.5_L367.SLDPRT"), new Xf(), true);
        Add(P(DIR, "CB-165C_Codo_Mastil.SLDPRT"), new Xf(), true);
        m.EditRebuild3();
        Desc("Modulo de camaras estereo para la alternativa C (cola de 377 mm): mastil fuera del barrido de la cola, a 431 mm de J1 (x " + MX + ", y " + MY +
             "), en el codo CB-165C al final del brazo CB-163C. Rumbo " + YAWN + ", " + PITCHN + " grados hacia abajo, al centro de la esfera. Marco arbol.");
        Guardar(m);
        foreach (var c in Comps(m)) { var b = (double[])c.GetBox(false, false); if (b != null) Console.WriteLine("  " + c.Name2.PadRight(46) + " " + SW.Box(b)); }
        SW.App.CloseDoc(m.GetTitle());
        // tornillos del pie viejo en CB-100C (cerca de (-80, 130) en el marco árbol)
        m = SW.Open(Q(B100C)); M = m; A = (AssemblyDoc)m; Activar(m);
        var arbol = Xf.FromComp(Comps(m).First(c => c.Name2.StartsWith("CB-150"))).Inv();
        foreach (var c in Comps(m).Where(c => c.Name2.StartsWith("HW_")))
        {
            var t = arbol.Apply(Xf.FromComp(c).T);
            if (Math.Abs(t[0] - FX1) < 45 && Math.Abs(t[1] - FY1) < 45) Console.WriteLine("  cerca del pie viejo: " + c.Name2 + " (" + t[0].ToString("0") + ", " + t[1].ToString("0") + ", " + t[2].ToString("0") + ")");
        }
        SW.App.CloseDoc(m.GetTitle());
    }

    // ================= final de carrera de J2 (v7.15): switch en una oreja de la tapa CB-303, lo aprieta el brazo del imán CB-232 =================
    // Marco de E2 (las dos variantes: eje J2 = z en (-80, 0), cara del cubo en z 35, tapa CB-303 en coordenadas de E2).
    // El brazo de CB-232 (fijo a la torre) pasa por afuera de la tapa en z 46.5 … 52, 23.6 mm de ancho, a -91.7° del mundo (1.7° de la vertical).
    // En el marco de E2 está a -91.7 - J2; con J2 = +45 apunta a -136.7° y su borde del lado de la cola queda a 10.8 mm del eje J2 (medido con Slice).
    // El switch se pone con la palanca hacia ese borde: opera (13.1 sobre la base) justo en J2 = +45 y la punta libre toca ≈ 4° antes.
    const double J2Home = 45, AngBrazo = -91.7, NBorde = 10.8, S0 = 18, ZSw = 49.25, ZCubo = 35;
    static Xf SwitchJ2()
    {
        double a = Rad(AngBrazo - J2Home), dx = Math.Cos(a), dy = Math.Sin(a);
        double[] n = { dy, -dx };                                   // perpendicular hacia la cola (lado del switch)
        double nb = NBorde + 13.1;                                  // base del switch (Y = 0); la palanca opera en Y = 13.1
        double px = -80 + S0 * dx + nb * n[0], py = S0 * dy + nb * n[1];
        return Xf.Rows(V(dx, dy, 0), V(-n[0], -n[1], 0), V(0, 0, 1), V(px, py, ZSw));
    }
    static void FinalJ2()
    {
        var sw = SwitchJ2();
        // 1) oreja en la tapa CB-303
        var m = SW.Open(Q(Path.Combine(H, "CB-303_Tapa_E2_Soporte_AS5600.SLDPRT"))); m.Visible = true; Activar(m); PB.Use(m);
        if (((PartDoc)m).FeatureByName("CB_v715_Oreja_final_J2") == null)
        {
            PB.Report("CB-303 antes");
            var q = new List<double>();
            foreach (var xy in new[] { V(-1.5, -3), V(21.3, -3), V(21.3, 9.5), V(-1.5, 9.5) }) { var p = sw.Apply(V(xy[0], xy[1], 0)); q.Add(p[0]); q.Add(p[1]); }
            double zTop = ZSw - 3.2;
            PB.Sketch('F'); PB.Poly(q.ToArray()); PB.Extrude(ZCubo, zTop - ZCubo, false, "CB_v715_Oreja_final_J2");
            PB.Sketch('F'); foreach (var hx in new[] { 5.15, 14.65 }) { var p = sw.Apply(V(hx, 2.9, 0)); PB.Circle(p[0], p[1], 1.7); }
            PB.Extrude(zTop - 9, 9.5, true, "CB_v715_Pilotos_M2_final_J2");
            PB.Report("CB-303 despues");
            PB.Prop("Notas", "v7.15: oreja con el final de carrera de J2 (CB-516, 2 x M2x12 autorroscantes). Lo aprieta el brazo del iman CB-232 con J2 = +45 (home arriba).");
            Guardar(m);
        }
        SW.App.CloseDoc(m.GetTitle());
        // 2) switch en los dos hombros
        foreach (var h in new[] { Path.Combine("11_v7B_Prismatico_Telescopico", "CB-300B_Hombro_E2_v7B.SLDASM"), H300C })
        {
            m = SW.Open(Q(h)); M = m; A = (AssemblyDoc)m; Activar(m);
            var e2 = Comps(m).First(c => c.Name2.StartsWith("CB-301"));
            var tapa = Comps(m).First(c => c.Name2.StartsWith("CB-303"));
            Console.WriteLine("  tapa respecto de E2: " + Xf.FromComp(tapa).Then(Xf.FromComp(e2).Inv()));
            var xsw = sw.Then(Xf.FromComp(e2));
            if (!Comps(m).Any(c => c.Name2.StartsWith("CB-516") && Math.Abs(Xf.FromComp(c).T[0] - xsw.T[0]) < 0.5 && Math.Abs(Xf.FromComp(c).T[1] - xsw.T[1]) < 0.5))
            {
                var c = Add(P(COM, Path.Combine("Electronica", "CB-516_Microswitch_Palanca_19.8x6.4.SLDPRT")), xsw, true);
                Console.WriteLine("  switch de J2 en " + Path.GetFileName(h) + ": " + c.Name2);
                Guardar(m);
            }
            SW.App.CloseDoc(m.GetTitle());
        }
    }

    // ================= piezas C =================
    static void E2C()
    {
        string dst = P(DIR, "CB-301C_Eslabon_E2_Cremallera.SLDPRT");
        if (!File.Exists(dst)) { File.Copy(P(H, "CB-301_Eslabon_E2_mod5840.SLDPRT"), dst); File.SetAttributes(dst, FileAttributes.Normal); }
        var m = SW.Open(dst); PB.Use(m);
        if (((PartDoc)m).FeatureByName("CB_C_Ranura_cremallera") == null)
        {
            PB.Report("E2-C antes");
            // aleta y dientes de la cremallera: z -14.5 … -7.5, y 14.8 … 28.5, de x -415 hasta el frente
            PB.Sketch('R'); PB.Rect(-14.5, 14.8, -7.5, 28.5); PB.Extrude(-415, 430, true, "CB_C_Ranura_cremallera");
            // pie de la cremallera en el tramo delantero, donde E2 no tiene cavidad arriba del canal
            PB.Sketch('R'); PB.Rect(-14.5, 14.8, 6.5, 18); PB.Extrude(-62, 77, true, "CB_C_Ranura_pie_cremallera");
            PB.Report("E2-C despues");
            PB.Prop("Descripcion", "Eslabon E2 para el prismatico con cremallera y pinon (alternativa C): copia de CB-301 (v7A) con ranura para la cremallera");
            PB.Prop("Notas", "Ranura z -14.5..-7.5, y 14.8..28.5 desde x -415 hasta el frente, y ranura del pie z -14.5..6.5, y 14.8..18 desde x -62. Coordenadas de E2.");
            Guardar(m);
        }
        SW.App.CloseDoc(m.GetTitle());
    }
    static void Cremallera()
    {
        string dst = P(DIR, "CB-431C_Cremallera_m1_58d.SLDPRT");
        PB.NewPart();
        double p = Math.PI * Mod, t = Math.Tan(Math.PI / 9), ha = Mod, hf = 1.25 * Mod;
        double zTip = ZPrim + ha, zRoot = ZPrim - hf, x0 = XCrem0, x1 = XCrem0 + LTramo;
        // dientes (perfil recto exacto de cremallera) + aleta, extruidos en y 17.5 … 28
        var pts = new List<double>(); pts.AddRange(V(x0, -14)); pts.AddRange(V(x0, zRoot));
        for (int i = 0; i < DientesTramo; i++)
        {
            double xc = x0 + p / 2 + i * p, hr = p / 4 + hf * t, ht = p / 4 - ha * t;
            pts.AddRange(V(xc - hr, zRoot, xc - ht, zTip, xc + ht, zTip, xc + hr, zRoot));
        }
        pts.AddRange(V(x1, zRoot)); pts.AddRange(V(x1, -14));
        PB.Sketch('T'); PB.Poly(pts.ToArray()); PB.Extrude(17.5, 10.5, false, "Dientes_m1_58");
        // pie apoyado sobre el tubo: z -14 … 6, y 15 … 17.5
        PB.Sketch('R'); PB.Rect(-14, 15, 6, 17.5); PB.Extrude(x0, LTramo, false, "Pie_sobre_tubo");
        // 3 agujeros M3 avellanados en el pie (z = 2), para remaches o tornillos al tubo
        PB.Sketch('T'); foreach (double xx in new[] { x0 + 20, x0 + LTramo / 2, x1 - 20 }) PB.Circle(xx, 2, 3.4); PB.Extrude(14, 5, true, "Agujeros_M3");
        PB.Sketch('T'); foreach (double xx in new[] { x0 + 20, x0 + LTramo / 2, x1 - 20 }) PB.Circle(xx, 2, 6.4); PB.Extrude(16, 3, true, "Avellanado_cabezas");
        PB.PartColor(0.95, 0.55, 0.1);
        PB.Prop("Descripcion", "Cremallera modulo 1 (20 grados), tramo de 58 dientes (" + LTramo.ToString("0.0") + " mm): van 2 tramos seguidos arriba del tubo de E3 (alternativa C)");
        PB.Prop("Material", "PETG, 100 % de relleno, impresa de costado (dientes hacia arriba en la cama no: aleta vertical)");
        PB.Prop("Notas", "Coordenadas de E2 con J3 retraido: x " + x0 + ".." + x1.ToString("0.00") + ", linea primitiva en z " + ZPrim + ". El 2do tramo va " + LTramo.ToString("0.000") +
            " mm mas adelante (largo = numero entero de pasos: el diente sigue sin saltos). Al tubo con 3 remaches o tornillos M3 por tramo.");
        PB.Report("Cremallera"); PB.Save(dst); SW.App.CloseDoc(PB.M.GetTitle());
    }
    // piñón m1 de 18 dientes con perfil de evolvente (polilínea), eje local +Y, dientes y 0..8, cubo 8..16
    static void Pinon()
    {
        string dst = P(DIR, "CB-432C_Pinon_m1_18d.SLDPRT");
        PB.NewPart();
        double rp = RPas, rb = rp * Math.Cos(Math.PI / 9), ra = rp + Mod, rf = rp - 1.25 * Mod;
        Func<double, double> inv = r => { double al = Math.Acos(Math.Min(1, rb / r)); return Math.Tan(al) - al; };
        double medio = Math.PI / (2 * ZPin) + inv(rp);           // semiángulo del diente en el círculo base + evolvente
        // ángulo del primer diente (en el plano x,z de la pieza): que un diente apunte a la hendidura de la cremallera en x = -58
        double xg = XCrem0 + Math.PI * Mod * Math.Round((XMotor - XCrem0) / (Math.PI * Mod)) - XMotor;   // corrimiento al hueco más cercano
        double fi0 = Math.Atan2(ZPrim, xg);
        var pts = new List<double>();
        int nf = 8;
        for (int i = 0; i < ZPin; i++)
        {
            double c = fi0 + i * 2 * Math.PI / ZPin;
            var izq = new List<double[]>(); var der = new List<double[]>();
            // pie: del círculo de raíz al base en línea radial
            double r0 = Math.Max(rf, rb);
            for (int k = 0; k <= nf; k++)
            {
                double r = r0 + (ra - r0) * k / nf;
                double ps = medio - inv(r);
                izq.Add(V(r * Math.Cos(c - ps), r * Math.Sin(c - ps))); der.Add(V(r * Math.Cos(c + ps), r * Math.Sin(c + ps)));
            }
            double psr = medio - inv(r0);
            pts.AddRange(V(rf * Math.Cos(c - psr - 0.06), rf * Math.Sin(c - psr - 0.06)));
            foreach (var q in izq) pts.AddRange(q);
            der.Reverse(); foreach (var q in der) pts.AddRange(q);
            pts.AddRange(V(rf * Math.Cos(c + psr + 0.06), rf * Math.Sin(c + psr + 0.06)));
        }
        PB.Sketch('T'); PB.Poly(pts.ToArray()); PB.Extrude(0, 8, false, "Dientes_m1_18");
        PB.Sketch('T'); PB.Circle(0, 0, 12); PB.Extrude(8, 8, false, "Cubo_D12x8");
        PB.Sketch('T'); PB.Circle(0, 0, 5.1); PB.Extrude(-1, 18, true, "Agujero_D5");
        PB.PartColor(0.2, 0.55, 0.85);
        PB.Prop("Descripcion", "Pinon modulo 1, 18 dientes (D18 primitivo, 56.5 mm por vuelta), agujero D5 para el NEMA 17 (alternativa C)");
        PB.Prop("Material", "PETG o nailon, 100 % de relleno; o comprado (pinon m1 18T de aluminio)");
        PB.Prop("Notas", "Eje local +Y: dientes y 0..8, cubo D12 y 8..16 con prisionero M3 sobre el plano del eje. Perfil de evolvente aproximado con polilinea.");
        PB.Report("Pinon"); PB.Save(dst); SW.App.CloseDoc(PB.M.GetTitle());
    }

    // ================= piezas D =================
    static void Tambor()
    {
        string dst = P(DIR, "CB-441D_Tambor_D13x13.SLDPRT");
        PB.NewPart();
        PB.Sketch('T'); PB.Circle(0, 0, 21); PB.Extrude(0, 1, false, "Pestana_inferior_D21");
        PB.Sketch('T'); PB.Circle(0, 0, 2 * RTam); PB.Extrude(1, 13, false, "Nucleo_D13x13");
        PB.Sketch('T'); PB.Circle(0, 0, 21); PB.Extrude(14, 1, false, "Pestana_superior_D21");
        PB.Sketch('T'); PB.Circle(0, 0, 12); PB.Extrude(15, 6, false, "Cubo_D12x6");
        PB.Sketch('T'); PB.Circle(0, 0, 5.1); PB.Extrude(-1, 23, true, "Agujero_D5");
        PB.Sketch('T'); foreach (double s in new[] { -1.0, 1.0 }) PB.Circle(s * 4.5, 0, 1.6); PB.Extrude(0.5, 14, true, "Anclajes_cable_D1.6");
        PB.PartColor(0.2, 0.55, 0.85);
        PB.Prop("Descripcion", "Tambor D13 x 13 para el cable del prismatico (alternativa D): 44 mm por vuelta con cable D1, entra en el lugar de la polea GT2");
        PB.Prop("Material", "PETG o aluminio torneado, 100 %");
        PB.Prop("Notas", "Eje local +Y: pestanas D21 en y 0..1 y 14..15, nucleo y 1..14, cubo y 15..21 con prisionero M3. Los dos extremos del cable se atan en los agujeros D1.6 " +
            "y se enrollan en sentidos contrarios: 7.7 vueltas de recorrido + 2 de reserva por lado, paso 1 mm (12 de 13 mm de nucleo).");
        PB.Report("Tambor"); PB.Save(dst); SW.App.CloseDoc(PB.M.GetTitle());
    }
    static void PoleaAcanalada()
    {
        string dst = P(DIR, "CB-442D_Polea_Acanalada_D13.SLDPRT");
        PB.NewPart();
        PB.Sketch('T'); PB.Circle(0, 0, 18); PB.Extrude(0, 1, false, "Pestana_inferior_D18");
        PB.Sketch('T'); PB.Circle(0, 0, 2 * RTam); PB.Extrude(1, 7, false, "Garganta_D13");
        PB.Sketch('T'); PB.Circle(0, 0, 18); PB.Extrude(8, 1, false, "Pestana_superior_D18");
        PB.Sketch('T'); PB.Circle(0, 0, 5.1); PB.Extrude(-1, 11, true, "Agujero_D5");
        PB.PartColor(0.84, 0.85, 0.88);
        PB.Prop("Descripcion", "Polea acanalada D13 con rodamiento 625ZZ, eje M5 (alternativa D): reemplaza la polea loca GT2 en la cola de E2");
        PB.Prop("Notas", "Eje local +Y, ancho 9 mm, igual que HW_Polea_Loca_GT2_20T_D5 (mismo tornillo M5 de la cola). Pestanas D18: con D21 tocan la pared trasera de E2.");
        PB.Report("Polea acanalada"); PB.Save(dst); SW.App.CloseDoc(PB.M.GetTitle());
    }
    static void Cable()
    {
        string dst = P(DIR, "CB-443D_Cable_D1.SLDPRT");
        PB.NewPart();
        double r1 = RCab - RCable, r2 = RCab + RCable;
        PB.Sketch('T');
        foreach (double r in new[] { r2, r1 })
        { PB.Line(XIdler, -r, XMotor, -r); PB.Arc(XMotor, 0, XMotor, -r, XMotor, r); PB.Line(XMotor, r, XIdler, r); PB.Arc(XIdler, 0, XIdler, r, XIdler, -r); }
        PB.Extrude(YCable - RCable, 2 * RCable, false, "Cable_D1");
        PB.PartColor(0.1, 0.1, 0.12);
        double L = 2 * (XMotor - XIdler) + Math.PI * 2 * RCab;
        PB.Prop("Descripcion", "Cable D1 (Dyneema / acero 7x7) del prismatico (alternativa D): lazo de " + L.ToString("0") + " mm + vueltas en el tambor (~700 mm de cable)");
        PB.Prop("Notas", "Coordenadas de E2, J3 retraido. Dibujo simplificado (lazo cerrado, seccion cuadrada de 1 mm). Los dos extremos van a la pinza del tapon CB-403 con un terminal prensado.");
        PB.Report("Cable"); PB.Save(dst); SW.App.CloseDoc(PB.M.GetTitle());
    }

    // ================= tramo E3 (se mueve) =================
    static void SubE3(string v)
    {
        string dst = P(DIR, v == "C" ? "CB-420C_Tramo_E3_Cremallera.SLDASM" : "CB-420D_Tramo_E3_Cable.SLDASM");
        if (File.Exists(dst)) { Console.WriteLine("ya existe " + dst); return; }
        NuevoAsm();
        Add(P(J, "CB-401_Tubo_E3_Aluminio_30x30x2.SLDPRT"), new Xf(), true);
        Add(P(J, "CB-402_Brida_E3_Herramienta.SLDPRT"), new Xf(), true);
        Add(P(J, "CB-403_Tapon_Trasero_Pinza_Correa.SLDPRT"), new Xf(), true);
        if (v == "C") { Add(P(DIR, "CB-431C_Cremallera_m1_58d.SLDPRT"), new Xf(), true); Add(P(DIR, "CB-431C_Cremallera_m1_58d.SLDPRT"), Xf.Tr(LTramo, 0, 0), true); }
        Desc(v == "C" ? "Tramo E3 (alternativa C): tubo 30x30x2 L440 + brida + tapon + 2 tramos de cremallera m1. Coordenadas de E2, J3 retraido."
                      : "Tramo E3 (alternativa D): tubo 30x30x2 L440 + brida + tapon con la pinza de los extremos del cable. Coordenadas de E2, J3 retraido.");
        GuardarComo(dst);
    }

    // ================= ensamblaje solo del prismático =================
    static void Asm(string v)
    {
        string dst = P(DIR, v == "C" ? "CB-450C_Prismatico_Cremallera_Solo.SLDASM" : "CB-450D_Prismatico_Cable_Solo.SLDASM");
        if (File.Exists(dst)) throw new Exception("ya existe " + dst);
        NuevoAsm();
        var e2 = Add(v == "C" ? P(DIR, "CB-301C_Eslabon_E2_Cremallera.SLDPRT") : P(H, "CB-301_Eslabon_E2_mod5840.SLDPRT"), new Xf(), true);
        Add(P(H, "CB-306_Placa_Soporte_NEMA17_Prismatico.SLDPRT"), Xf.Tr(XMotor, YFace - 5, 0), true);
        Add(P(COM, "CB-505_Motor_NEMA17_Prismatico.SLDPRT"), Xf.Rows(V(0, -1, 0), V(1, 0, 0), V(0, 0, 1), V(XMotor, YFace - 491.5, 0)), true);
        Component2 rueda;
        if (v == "C") rueda = Add(P(DIR, "CB-432C_Pinon_m1_18d.SLDPRT"), Xf.Tr(XMotor, YPin0, 0), false);
        else
        {
            rueda = Add(P(DIR, "CB-441D_Tambor_D13x13.SLDPRT"), Xf.Tr(XMotor, YTam0, 0), false);
            Add(P(DIR, "CB-442D_Polea_Acanalada_D13.SLDPRT"), Xf.Tr(XIdler, YIdler, 0), true);
            Add(P(DIR, "CB-443D_Cable_D1.SLDPRT"), new Xf(), true);
        }
        var e3 = Add(P(DIR, v == "C" ? "CB-420C_Tramo_E3_Cremallera.SLDASM" : "CB-420D_Tramo_E3_Cable.SLDASM"), new Xf(), false);
        M.EditRebuild3();
        e2.SetMaterialPropertyValues2(new double[] { 0.92, 0.92, 0.94, 1, 1, 0.3, 0.3, 0.7, 0 }, (int)swInConfigurationOpts_e.swAllConfiguration, null);
        var br = Child(e3, "CB-402");
        var front = FindPlaneFace(e2, 0, 1, XFront, 1000, 9000, "frente de E2 x=13");
        Mate("E3_plano_medio", (int)swMateType_e.swMateCOINCIDENT, RefPlane(br, 2), RefPlane(e2, 2));
        Mate("E3_plano_eje", (int)swMateType_e.swMateCOINCIDENT, RefPlane(br, 1), RefPlane(e2, 1));
        Mate("E3_carrera_0-340", (int)swMateType_e.swMateDISTANCE, FindPlaneFace(br, 0, -1, XFlIn, 4000, 6500, "brida cara interior x=15"), front, XFlIn - XFront, XFlIn - XFront + Stroke, XFlIn - XFront);
        // la rueda (piñón o tambor) gira en el eje del motor y la arrastra el tramo E3
        var motor = ((object[])A.GetComponents(true)).Cast<Component2>().First(c => c.Name2.StartsWith("CB-505"));
        Mate("Rueda_eje_motor", (int)swMateType_e.swMateCONCENTRIC, FindCyl(rueda, 2.55, "agujero D5.1 de la rueda"), FindCyl(motor, 2.5, "eje D5 del NEMA"));
        double yR = v == "C" ? YPin0 : YTam0;
        var alt = Mate("Rueda_altura", (int)swMateType_e.swMateDISTANCE, RefPlane(rueda, 1), RefPlane(e2, 1), yR);
        M.EditRebuild3();
        if (Math.Abs(Xf.FromComp(rueda).T[1] - yR) > 0.05)
        {
            Console.WriteLine("  rueda en y " + Xf.FromComp(rueda).T[1].ToString("0.0") + ": invierto la relacion de altura");
            M.ClearSelection2(true); alt.Select2(false, 0); M.EditDelete();
            Mate("Rueda_altura", (int)swMateType_e.swMateDISTANCE, RefPlane(rueda, 1), RefPlane(e2, 1), yR, 0, 0, true);
            M.EditRebuild3();
        }
        Console.WriteLine("  rueda: " + Xf.FromComp(rueda));
        var tubo = Child(e3, "CB-401");
        var md = (RackPinionMateFeatureData)A.CreateMateData((int)swMateType_e.swMateRACKPINION);
        md.set_EntitiesToMate((int)swRackPinionMateEntityType_e.swRackPinionMateEntityType_Rack, FindLineX(tubo, 400, "arista del tubo"));
        md.set_EntitiesToMate((int)swRackPinionMateEntityType_e.swRackPinionMateEntityType_Pinion, FindCyl(rueda, 2.55, "agujero de la rueda"));
        md.DiameterType = (int)swRackPinionMateDistanceOptions_e.swPinionPitchDiameter;
        md.DiameterVal = (v == "C" ? 2 * RPas : 2 * RCab) / 1000;
        md.Reverse = v == "C";
        var rp = A.CreateMate(md);
        Console.WriteLine("relacion cremallera-pinon: " + (rp == null ? "FALLO" : "ok"));
        if (rp != null) { var last = Mates().Last(); last.Name = v == "C" ? "Pinon_cremallera_D18" : "Tambor_cable_D14"; }
        M.EditRebuild3();
        foreach (var mt in Mates()) { bool ww; Console.WriteLine("  " + mt.Name + " err=" + mt.GetErrorCode2(out ww)); }
        Desc(v == "C" ? "Prismatico J3 con cremallera y pinon (alternativa C) solo: E2-C (transparente) + NEMA 17 + pinon m1 18T + tramo E3 con 2 tramos de cremallera. Estudio Movimiento_cremallera (Play)."
                      : "Prismatico J3 con cable y tambor (alternativa D) solo: E2 (transparente) + NEMA 17 + tambor D16 + polea acanalada + cable D1 + tramo E3. Estudio Movimiento_cable (Play).");
        M.ShowNamedView2("*Trimetric", -1); M.ViewZoomtofit2();
        GuardarComo(dst);
    }

    // ================= estudio de movimiento + choques =================
    const double Duracion = 5;
    static void Estudio(string v)
    {
        string dst = P(DIR, v == "C" ? "CB-450C_Prismatico_Cremallera_Solo.SLDASM" : "CB-450D_Prismatico_Cable_Solo.SLDASM");
        var m = SW.Open(dst); M = m; A = (AssemblyDoc)m; Activar(m); A.ResolveAllLightWeightComponents(false);
        var cs = Comps(m);
        var e2 = cs.First(c => c.Name2.StartsWith("CB-301"));
        var e3 = cs.First(c => c.Name2.StartsWith("CB-420"));
        var br = Child(e3, "CB-402");
        var rueda = cs.First(c => c.Name2.StartsWith("CB-432") || c.Name2.StartsWith("CB-441"));
        double r = v == "C" ? RPas : RCab;
        var msm = (MotionStudyManager)m.Extension.GetMotionStudyManager();
        var nombres = (object[])msm.GetMotionStudyNames();
        MotionStudy ms = nombres != null && nombres.Length > 0 ? msm.GetMotionStudy(nombres[0].ToString()) : msm.CreateMotionStudy();
        ms.Activate(); ms.StudyType = (int)swMotionStudyType_e.swMotionStudyTypeAssembly; ms.SetDuration(Duracion);
        var d = (SimulationMotorFeatureData)ms.CreateDefinition((int)swFeatureNameID_e.swFmAEMLinearMotor);
        var cara = FindPlaneFace(br, 0, 1, XFlOut, 4000, 6800, "brida cara exterior x=25");
        d.Location = cara; d.DirectionReference = cara; d.ReverseDirection = false; d.RelativeComponent = e2;
        d.DistanceMotor(Stroke / 1000, 0, Duracion);
        Console.WriteLine("  motor: " + (ms.CreateFeature(d) == null ? "FALLO" : "ok"));
        Console.WriteLine("  calcular " + ms.Calculate()); ms.Stop();
        bool ok = true;
        foreach (double tt in new[] { 0.0, 0.1, 2.5, 4.99 })
        {
            ms.SetTime(tt); System.Threading.Thread.Sleep(2500); M.GraphicsRedraw2(); System.Threading.Thread.Sleep(500);
            double x = Xf.FromComp(e3).T[0];
            var rr = Xf.FromComp(rueda).R; double ang = Math.Atan2(-rr[2], rr[0]) * 180 / Math.PI;
            double esperado = (v == "C" ? -1 : 1) * x / r * 180 / Math.PI;
            esperado = ((esperado % 360) + 540) % 360 - 180;
            Console.WriteLine("  t = " + tt + " s: E3 x = " + x.ToString("0.0") + "  giro rueda " + ang.ToString("0.0") + " (esperado " + esperado.ToString("0.0") + ")");
            Choques();
            if (tt > 4) ok &= Math.Abs(x - Stroke) < 2;
        }
        ms.SetTime(0);
        if (!ok) { Console.WriteLine("NO llega a 340: queda sin guardar"); return; }
        ms.Name = v == "C" ? "Movimiento_cremallera" : "Movimiento_cable";
        ms.PlayMode = (int)swAnimationPlayMode_e.swAnimationPlayModeReciprocate;
        int e = 0, w = 0; M.ForceRebuild3(false);
        Console.WriteLine((M.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w) ? "guardado " : "NO GUARDADO ") + M.GetTitle());
        SW.App.CloseDoc(M.GetTitle());
    }
    // abre el ensamblaje, recorre el estudio existente (choques en 0, 170 y 340) y saca imágenes; cierra sin guardar
    static void Ver(string v)
    {
        string n = v == "C" ? "CB-450C_Prismatico_Cremallera_Solo" : "CB-450D_Prismatico_Cable_Solo";
        string img = Path.Combine(R, "06_Imagenes", n);
        var m = SW.Open(P(DIR, n + ".SLDASM")); M = m; A = (AssemblyDoc)m; Activar(m); A.ResolveAllLightWeightComponents(false);
        var cs = Comps(m);
        var e3 = cs.First(c => c.Name2.StartsWith("CB-420"));
        var motor = cs.First(c => c.Name2.StartsWith("CB-505")); var placa = cs.First(c => c.Name2.StartsWith("CB-306"));
        var rueda = cs.First(c => c.Name2.StartsWith("CB-432") || c.Name2.StartsWith("CB-441"));
        foreach (double q in new[] { 0.0, Stroke })
        {
            e3.Transform2 = Xf.Tr(q, 0, 0).ToMath(); m.EditRebuild3();
            var rr = Xf.FromComp(rueda).R;
            Console.WriteLine("  q3 = " + Xf.FromComp(e3).T[0].ToString("0.0") + "  giro rueda " + (Math.Atan2(-rr[2], rr[0]) * 180 / Math.PI).ToString("0.0"));
            string suf = q == 0 ? "" : "_extendido";
            Foto(m, img + suf + "_trimetric.png", "*Trimetric", null);
            Foto(m, img + suf + "_top.png", "*Top", null);
            motor.Visible = 0; placa.Visible = 0;
            Foto(m, img + suf + "_mecanismo.png", "*Trimetric", new[] { rueda });
            if (v == "D") Foto(m, img + suf + "_cola.png", "*Trimetric", new[] { cs.First(c => c.Name2.StartsWith("CB-442")) });
            motor.Visible = 1; placa.Visible = 1;
        }
        SW.App.CloseDoc(m.GetTitle());   // sin guardar
    }
    static void Foto(ModelDoc2 m, string outp, string vista, Component2[] zoom)
    {
        m.ShowNamedView2(vista, -1);
        m.ViewZoomtofit2();
        if (zoom != null) { m.ClearSelection2(true); foreach (var c in zoom) c.Select4(true, null, false); m.ViewZoomToSelection(); m.ClearSelection2(true); for (int i = 0; i < 2; i++) m.ViewZoomout(); }
        m.GraphicsRedraw2(); System.Threading.Thread.Sleep(500);
        int e = 0, w = 0;
        Console.WriteLine((m.Extension.SaveAs3(outp, 0, 1, null, null, ref e, ref w) ? "  imagen " : "  FALLO imagen ") + Path.GetFileName(outp));
    }
    static void Choques()
    {
        var idm = A.InterferenceDetectionManager;
        idm.TreatCoincidenceAsInterference = false; idm.TreatSubAssembliesAsComponents = false; idm.IncludeMultibodyPartInterferences = false;
        var ints = (object[])idm.GetInterferences();
        Console.WriteLine("    choques: " + (ints == null ? 0 : ints.Length));
        if (ints != null) foreach (IInterference it in ints)
            Console.WriteLine("      " + (it.Volume * 1e9).ToString("0.00") + " mm3  " + string.Join(" <-> ", ((object[])it.Components).Cast<Component2>().Select(c => c.Name2.Split('/').Last())));
        idm.Done();
    }

    // ================= ayudas (como Tele.cs) =================
    static void Activar(ModelDoc2 m) { int er = 0; SW.App.ActivateDoc3(m.GetTitle(), false, 0, ref er); }
    static Component2[] Comps(ModelDoc2 m) { return ((object[])((AssemblyDoc)m).GetComponents(true)).Cast<Component2>().ToArray(); }
    static void NuevoAsm()
    {
        string tpl = SW.App.GetUserPreferenceStringValue((int)swUserPreferenceStringValue_e.swDefaultTemplateAssembly);
        M = (ModelDoc2)SW.App.NewDocument(tpl, 0, 0, 0); A = (AssemblyDoc)M; SW.Created.Add(M.GetTitle());
    }
    static Component2 Add(string path, Xf x, bool fix)
    {
        int e = 0, w = 0;
        int type = path.ToUpper().EndsWith(".SLDASM") ? (int)swDocumentTypes_e.swDocASSEMBLY : (int)swDocumentTypes_e.swDocPART;
        SW.App.DocumentVisible(false, type);
        SW.App.OpenDoc6(path, type, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref e, ref w);
        SW.App.DocumentVisible(true, type);
        var c = (Component2)A.AddComponent5(path, (int)swAddComponentConfigOptions_e.swAddComponentConfigOptions_CurrentSelectedConfig, "", false, "", 0, 0, 0);
        if (c == null) throw new Exception("AddComponent fallo " + path);
        c.Transform2 = x.ToMath();
        if (fix) { M.ClearSelection2(true); c.Select4(false, null, false); A.FixComponent(); M.ClearSelection2(true); }
        return c;
    }
    static void Desc(string d) { M.Extension.CustomPropertyManager[""].Add3("Descripcion", (int)swCustomInfoType_e.swCustomInfoText, d, 2); }
    static void Guardar(ModelDoc2 m)
    {
        int e = 0, w = 0; m.ForceRebuild3(false);
        Console.WriteLine((m.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w) ? "guardado " : "NO GUARDADO ") + Path.GetFileName(m.GetPathName()));
    }
    static void GuardarComo(string dst)
    {
        int e = 0, w = 0; M.ForceRebuild3(false);
        bool ok = M.Extension.SaveAs3(dst, 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent, null, null, ref e, ref w);
        Console.WriteLine((ok ? "guardado " : "NO GUARDADO ") + dst + " e=" + e);
        SW.App.CloseDoc(M.GetTitle());
    }
    static Component2 Child(Component2 parent, string fileStart)
    {
        foreach (Component2 c in (object[])parent.GetChildren())
            if (Path.GetFileName(c.GetPathName()).StartsWith(fileStart, StringComparison.OrdinalIgnoreCase)) return c;
        throw new Exception("no esta " + fileStart + " en " + parent.Name2);
    }
    static bool Near(double a, double b, double tol = 0.05) { return Math.Abs(a - b) < tol; }
    static Entity FindPlaneFace(Component2 comp, int axis, double sign, double at, double areaMin, double areaMax, string what)
    {
        var part = (PartDoc)comp.GetModelDoc2();
        foreach (Body2 b in (object[])part.GetBodies2((int)swBodyType_e.swSolidBody, true))
            foreach (Face2 f in (object[])b.GetFaces())
            {
                var s = (Surface)f.GetSurface(); if (!s.IsPlane()) continue;
                double[] n = (double[])f.Normal; if (Math.Abs(n[axis] - sign) > 1e-6) continue;
                double[] bx = (double[])f.GetBox();
                if (!Near(bx[axis] * 1000, at) || !Near(bx[axis + 3] * 1000, at)) continue;
                double ar = f.GetArea() * 1e6;
                if (ar < areaMin || ar > areaMax) { Console.WriteLine("  (cara descartada " + what + ": " + ar.ToString("0") + " mm2)"); continue; }
                Console.WriteLine("  cara ok: " + what + " (" + ar.ToString("0") + " mm2)");
                return (Entity)comp.GetCorrespondingEntity(f);
            }
        throw new Exception("cara no encontrada: " + what);
    }
    static Entity FindCyl(Component2 comp, double r, string what)
    {
        var part = (PartDoc)comp.GetModelDoc2();
        foreach (Body2 b in (object[])part.GetBodies2((int)swBodyType_e.swSolidBody, true))
            foreach (Face2 f in (object[])b.GetFaces())
            {
                var s = (Surface)f.GetSurface(); if (!s.IsCylinder()) continue;
                double[] cp = (double[])s.CylinderParams;
                if (!Near(cp[6] * 1000, r, 0.03)) continue;
                Console.WriteLine("  cilindro ok: " + what);
                return (Entity)comp.GetCorrespondingEntity(f);
            }
        throw new Exception("cilindro no encontrado: " + what);
    }
    static Entity FindLineX(Component2 comp, double minLen, string what)
    {
        var part = (PartDoc)comp.GetModelDoc2();
        foreach (Body2 b in (object[])part.GetBodies2((int)swBodyType_e.swSolidBody, true))
            foreach (Edge e in (object[])b.GetEdges())
            {
                var c = (Curve)e.GetCurve(); if (!c.IsLine()) continue;
                double[] lp = (double[])c.LineParams;
                if (Math.Abs(Math.Abs(lp[3]) - 1) > 1e-6) continue;
                double[] p0 = (double[])((Vertex)e.GetStartVertex()).GetPoint(), p1 = (double[])((Vertex)e.GetEndVertex()).GetPoint();
                if (Math.Abs(p1[0] - p0[0]) * 1000 < minLen) continue;
                Console.WriteLine("  arista ok: " + what);
                return (Entity)comp.GetCorrespondingEntity(e);
            }
        throw new Exception("arista no encontrada: " + what);
    }
    static Feature RefPlane(Component2 comp, int axis)
    {
        for (Feature f = (Feature)comp.FirstFeature(); f != null; f = (Feature)f.GetNextFeature())
            if (f.GetTypeName2() == "RefPlane")
            {
                var rp = (RefPlane)f.GetSpecificFeature2();
                double[] t = (double[])((MathTransform)rp.Transform).ArrayData;
                if (Math.Abs(Math.Abs(t[6 + axis]) - 1) < 1e-6) return f;
            }
        throw new Exception("sin plano normal a " + axis + " en " + comp.Name2);
    }
    static Feature Mate(string name, int type, object e1, object e2, double dist = 0, double dmax = 0, double dmin = 0, bool flip = false)
    {
        M.ClearSelection2(true);
        var sd = (SelectData)((SelectionMgr)M.SelectionManager).CreateSelectData(); sd.Mark = 1;
        bool s1 = e1 is Entity ? ((Entity)e1).Select4(true, sd) : ((Feature)e1).Select2(true, 1);
        bool s2 = e2 is Entity ? ((Entity)e2).Select4(true, sd) : ((Feature)e2).Select2(true, 1);
        int err;
        var mt = (Mate2)A.AddMate5(type, (int)swMateAlign_e.swMateAlignCLOSEST, flip, dist / 1000, dmax / 1000, dmin / 1000, 0, 0, 0, 0, 0, false, false, 0, out err);
        Console.WriteLine("relacion " + name + ": sel " + s1 + "/" + s2 + " err=" + err + (mt == null ? " FALLO" : ""));
        M.ClearSelection2(true);
        if (mt == null) return null;
        var last = Mates().LastOrDefault(); if (last != null) last.Name = name; return last;
    }
    static List<Feature> Mates()
    {
        var r = new List<Feature>();
        for (Feature f = (Feature)M.FirstFeature(); f != null; f = (Feature)f.GetNextFeature())
            if (f.GetTypeName2() == "MateGroup")
                for (Feature sf = (Feature)f.GetFirstSubFeature(); sf != null; sf = (Feature)sf.GetNextSubFeature()) r.Add(sf);
        return r;
    }
}
