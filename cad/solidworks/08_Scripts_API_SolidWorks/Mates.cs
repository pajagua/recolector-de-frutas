using System;
using System.IO;
using System.Linq;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Mates.exe <ensamblaje> [filtro] : relaciones del nivel superior con su estado y sus entidades (componente, tipo, parámetros)
// Abre sin ventana; si lo abrió, lo cierra sin guardar.
class Mates
{
    static void Main(string[] a)
    {
        SW.Connect();
        bool ya = SW.App.GetOpenDocumentByName(a[0]) != null;
        SW.App.DocumentVisible(false, (int)swDocumentTypes_e.swDocASSEMBLY);
        ModelDoc2 m;
        try { m = SW.Open(a[0]); } finally { SW.App.DocumentVisible(true, (int)swDocumentTypes_e.swDocASSEMBLY); }
        ((AssemblyDoc)m).ResolveAllLightWeightComponents(false);
        m.ForceRebuild3(false);
        for (Feature f = (Feature)m.FirstFeature(); f != null; f = (Feature)f.GetNextFeature())
            if (f.GetTypeName2() == "MateGroup")
                for (Feature sf = (Feature)f.GetFirstSubFeature(); sf != null; sf = (Feature)sf.GetNextSubFeature())
                {
                    if (a.Length > 1 && !sf.Name.Contains(a[1])) continue;
                    bool w; int e = sf.GetErrorCode2(out w);
                    var mt = sf.GetSpecificFeature2() as Mate2;
                    Console.WriteLine(sf.Name + "  " + sf.GetTypeName2() + "  " + (e == 0 ? "OK" : (w ? "ADV " : "ERR ") + (swFeatureError_e)e) + (sf.IsSuppressed() ? " (suprimida)" : ""));
                    if (mt == null) continue;
                    for (int i = 0; i < mt.GetMateEntityCount(); i++)
                    {
                        var me = mt.MateEntity(i);
                        var c = me.ReferenceComponent; var p = me.EntityParams as double[];
                        Console.WriteLine(string.Format("   [{0}] {1}  tipo {2}  {3}", i, c == null ? "?" : c.Name2, (swSelectType_e)me.ReferenceType2,
                            p == null ? "" : string.Join(" ", p.Select(v => (v * 1000).ToString("0.###")))));
                    }
                }
        if (!ya) SW.App.QuitDoc(m.GetTitle());
    }
}
