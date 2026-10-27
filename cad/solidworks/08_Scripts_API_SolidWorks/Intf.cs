using System;
using System.Linq;
using SolidWorks.Interop.sldworks;

// Intf.exe <asm>... : lista todas las interferencias de cada ensamblaje (sin guardar)
class Intf
{
    static void Main(string[] args)
    {
        SW.Connect();
        foreach (var path in args)
        {
            var m = SW.Open(path); if (m == null) continue;
            var a = (AssemblyDoc)m; a.ResolveAllLightWeightComponents(false);
            var idm = a.InterferenceDetectionManager;
            idm.TreatCoincidenceAsInterference = false; idm.TreatSubAssembliesAsComponents = false; idm.IncludeMultibodyPartInterferences = false;
            object[] ints = (object[])idm.GetInterferences();
            Console.WriteLine("== " + System.IO.Path.GetFileName(path) + ": " + (ints == null ? 0 : ints.Length) + " interferencias");
            if (ints != null) foreach (IInterference it in ints)
                {
                    var names = ((object[])it.Components).Cast<Component2>().Select(c => c.Name2.Split('/').Last()).ToArray();
                    if (names.All(n => n.StartsWith("CB-6"))) continue;
                    Console.WriteLine("  " + (it.Volume * 1e9).ToString("0.0") + " mm3  " + string.Join(" <-> ", names));
                }
            idm.Done();
            SW.App.CloseDoc(m.GetTitle());
        }
    }
}
