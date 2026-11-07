using System;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
// Frames.exe <asm> <filtro,...> : transformación completa (origen y ejes x, y, z en el ensamblaje) de cada componente
class Frames
{
    static void Main(string[] a)
    {
        SW.Connect();
        bool ya = SW.App.GetOpenDocumentByName(a[0]) != null;
        var m = SW.Open(a[0]); var asm = (AssemblyDoc)m; asm.ResolveAllLightWeightComponents(false);
        var f = a[1].Split(',');
        foreach (Component2 c in (object[])asm.GetComponents(false))
            if (f.Any(x => c.Name2.Split('/').Last().StartsWith(x))) Console.WriteLine(c.Name2 + "  " + Xf.FromComp(c));
        if (!ya) SW.App.CloseDoc(m.GetTitle());
    }
}
