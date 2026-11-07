using System;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Pos.exe <asm> <filtro,...> : caja (mm, coordenadas del ensamblaje) y eje z local de cada componente (todos los niveles) cuyo nombre contiene un filtro
class Pos
{
    static void Main(string[] a)
    {
        SW.Connect();
        bool ya = SW.App.GetOpenDocumentByName(a[0]) != null;
        var m = SW.Open(a[0]); var asm = (AssemblyDoc)m; asm.ResolveAllLightWeightComponents(false);
        var f = a[1].Split(',');
        foreach (Component2 c in (object[])asm.GetComponents(false))
        {
            if (!f.Any(x => c.Name2.Contains(x))) continue;
            var b = (double[])c.GetBox(false, false); var x0 = Xf.FromComp(c);
            var o = x0.Apply(new[] { 0.0, 0, 0 }); var z = x0.Apply(new[] { 0.0, 0, 1 });
            Console.WriteLine(string.Format("{0,-70} centro ({1:0.0}, {2:0.0}, {3:0.0})  caja z {4:0.0}..{5:0.0}  tam {6:0.0}x{7:0.0}  origen ({8:0.0},{9:0.0},{10:0.0}) z({11:0.00},{12:0.00},{13:0.00})",
                c.Name2.Length > 70 ? c.Name2.Substring(c.Name2.Length - 70) : c.Name2,
                (b[0] + b[3]) * 500, (b[1] + b[4]) * 500, (b[2] + b[5]) * 500, b[2] * 1000, b[5] * 1000, (b[3] - b[0]) * 1000, (b[4] - b[1]) * 1000,
                o[0], o[1], o[2], z[0] - o[0], z[1] - o[1], z[2] - o[2]));
        }
        if (!ya) SW.App.CloseDoc(m.GetTitle());
    }
}
