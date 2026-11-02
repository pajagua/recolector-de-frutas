using System;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Lista.exe <asm> [mates] : componentes de primer nivel (nombre, archivo, fijo/oculto) y, con "mates", las relaciones con sus errores
class Lista
{
    static void Main(string[] a)
    {
        SW.Connect();
        bool yaAbierto = SW.App.GetOpenDocumentByName(a[0]) != null;
        var m = SW.Open(a[0]); var asm = (AssemblyDoc)m; asm.ResolveAllLightWeightComponents(false);
        foreach (Component2 c in (object[])asm.GetComponents(true))
            Console.WriteLine(string.Format("  {0,-45} {1}{2}{3}", c.Name2, System.IO.Path.GetFileName(c.GetPathName()), c.IsFixed() ? "  [fijo]" : "", c.IsSuppressed() ? "  [suprimido]" : ""));
        if (a.Length > 1 && a[1] == "mates")
        {
            for (var f = (Feature)m.FirstFeature(); f != null; f = (Feature)f.GetNextFeature())
            {
                if (f.GetTypeName2() != "MateGroup") continue;
                for (var s = (Feature)f.GetFirstSubFeature(); s != null; s = (Feature)s.GetNextSubFeature())
                {
                    bool warn; int err = s.GetErrorCode2(out warn);
                    var mate = s.GetSpecificFeature2() as Mate2;
                    string ents = "";
                    if (mate != null)
                        for (int i = 0; i < mate.GetMateEntityCount(); i++)
                        {
                            var me = mate.MateEntity(i); var rc = me.ReferenceComponent;
                            ents += " | " + (rc == null ? "?" : rc.Name2) + " (" + me.ReferenceType2 + ")";
                        }
                    Console.WriteLine(string.Format("  mate {0,-24} tipo {1} err {2}{3}{4}", s.Name, mate == null ? -1 : mate.Type, err, warn ? " (aviso)" : "", ents));
                }
            }
        }
        if (!yaAbierto) SW.App.CloseDoc(m.GetTitle());
    }
}
