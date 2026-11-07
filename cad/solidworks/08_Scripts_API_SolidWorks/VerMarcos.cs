using System;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// VerMarcos.exe <CB-000> <png_prefijo> : verifica el origen y los ejes de los sistemas SR0..SR5 en el ensamblaje general
// y saca imágenes con los sistemas de coordenadas visibles (sin guardar).
class VerMarcos
{
    static void Main(string[] a)
    {
        SW.Connect();
        var m = SW.Open(a[0]); var asm = (AssemblyDoc)m; asm.ResolveAllLightWeightComponents(false);
        int er = 0; SW.App.ActivateDoc3(m.GetTitle(), false, 0, ref er);
        var comps = ((object[])asm.GetComponents(false)).Cast<Component2>().ToList();
        var lista = new[] { Tuple.Create("CB-121", "SR0_base_eje_J1"), Tuple.Create("CB-210", "SR1_cintura_eje_J2"), Tuple.Create("CB-301", "SR2_E2_eje_J3"),
                            Tuple.Create("CB-401", "SR3_E3_eje_J4"), Tuple.Create("CB-603", "SR4_caja_eje_J5"), Tuple.Create("CB-606", "SR5_ventosa_TCP") };
        foreach (var it in lista)
        {
            var c = comps.First(k => k.Name2.Split('/').Last().StartsWith(it.Item1));
            var doc = (ModelDoc2)c.GetModelDoc2(); var xc = Xf.FromComp(c);
            var t = (double[])((MathTransform)doc.Extension.GetCoordinateSystemTransformByName(it.Item2)).ArrayData;
            // filas = ejes del sistema en la pieza; origen: probar T directo y T llevado por R
            var T = new[] { t[9] * 1000, t[10] * 1000, t[11] * 1000 };
            var o2 = new double[3]; for (int j = 0; j < 3; j++) { o2[j] = 0; for (int k = 0; k < 3; k++) o2[j] -= T[k] * t[j * 3 + k]; }
            var o3 = new double[3]; for (int j = 0; j < 3; j++) { o3[j] = 0; for (int k = 0; k < 3; k++) o3[j] -= T[k] * t[k * 3 + j]; }
            Func<double[], string> F = v => { var w = xc.Apply(v); return string.Format("({0:0.00}, {1:0.00}, {2:0.00})", w[0], w[1], w[2]); };
            var z = xc.Apply(new[] { t[6], t[7], t[8] }); var o = xc.Apply(new[] { 0.0, 0, 0 });
            Console.WriteLine(string.Format("{0,-20} T {1}  -T·R {2}  -T·Rt {3}  z {4:0.00} {5:0.00} {6:0.00}", it.Item2, F(T), F(o2), F(o3), z[0] - o[0], z[1] - o[1], z[2] - o[2]));
        }
        m.Extension.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swDisplayCoordSystems, 0, true);
        SW.App.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swDisplayCoordSystems, true);
        m.GraphicsRedraw2();
        foreach (var v in new[] { "Trimetric", "Front", "Right" })
        {
            m.ShowNamedView2("*" + v, -1); m.ViewZoomtofit2(); m.GraphicsRedraw2();
            int e = 0, w = 0; string p = a[1] + "_" + v + ".png";
            Console.WriteLine((m.Extension.SaveAs3(p, 0, 1, null, null, ref e, ref w) ? "saved " : "FAIL ") + p);
        }
        SW.App.CloseDoc(m.GetTitle());
    }
}
