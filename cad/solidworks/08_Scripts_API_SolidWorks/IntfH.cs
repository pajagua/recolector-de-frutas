using System;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// IntfH.exe <asm> [excluir,...] : interferencias del ensamblaje (subconjuntos como componentes sueltos), con la ruta de cada
// componente, el volumen y la caja de la interferencia. Omite las que tocan subconjuntos excluidos (prefijo del nombre en la ruta).
class IntfH
{
    static string Ruta(Component2 c)
    {
        string r = c.Name2; var p = c.GetParent();
        while (p != null) { r = p.Name2 + "/" + r; p = p.GetParent(); }
        return r;
    }
    static void Main(string[] a)
    {
        SW.Connect();
        bool ya = SW.App.GetOpenDocumentByName(a[0]) != null;
        var m = SW.Open(a[0]); var asm = (AssemblyDoc)m; asm.ResolveAllLightWeightComponents(false);
        var excl = a.Length > 1 ? a[1].Split(',') : new string[0];
        var idm = asm.InterferenceDetectionManager;
        idm.TreatCoincidenceAsInterference = false; idm.TreatSubAssembliesAsComponents = false; idm.IncludeMultibodyPartInterferences = false;
        object[] ints = (object[])idm.GetInterferences();
        Console.WriteLine("interferencias: " + (ints == null ? 0 : ints.Length));
        if (ints != null)
            foreach (IInterference it in ints)
            {
                var rs = ((object[])it.Components).Cast<Component2>().Select(Ruta).ToArray();
                if (rs.Any(r => excl.Any(e => r.Contains(e)))) continue;
                var ib = (Body2)it.GetInterferenceBody(); string caja = "";
                if (ib != null) { var b = (double[])ib.GetBodyBox(); caja = string.Format("[{0:0.0},{1:0.0},{2:0.0}]..[{3:0.0},{4:0.0},{5:0.0}]", b[0] * 1000, b[1] * 1000, b[2] * 1000, b[3] * 1000, b[4] * 1000, b[5] * 1000); }
                Console.WriteLine(string.Format("{0,8:0.0} mm3  {1}  <->  {2}  {3}", it.Volume * 1e9, rs[0], rs.Length > 1 ? rs[1] : "", caja));
            }
        idm.Done();
        if (!ya) SW.App.CloseDoc(m.GetTitle());
    }
}
