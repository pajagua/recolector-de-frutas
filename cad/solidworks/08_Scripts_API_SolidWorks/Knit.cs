using System; using System.Linq; using SolidWorks.Interop.sldworks; using SolidWorks.Interop.swconst;
// Knit.exe part : sew all sheet bodies trying to form solids; delete remaining tiny sheets; save only if solids were formed
class Knit { static void Main(string[] a) { SW.Connect(); var m = SW.Open(a[0]); var p = (PartDoc)m;
  Func<int,int> cnt = k => { object[] b = (object[])p.GetBodies2(k, false); return b == null ? 0 : b.Length; };
  Console.WriteLine("antes solid=" + cnt((int)swBodyType_e.swSolidBody) + " sheet=" + cnt((int)swBodyType_e.swSheetBody));
  m.ClearSelection2(true);
  foreach (Body2 b in (object[])p.GetBodies2((int)swBodyType_e.swSheetBody, false)) if (b.GetFaceCount() > 10) b.Select2(true, null);
  var f = (Feature)m.FeatureManager.InsertSewRefSurface(false, true, true, 0.00005, 0);
  Console.WriteLine("knit feature " + (f != null));
  m.ClearSelection2(true); m.ForceRebuild3(false);
  int solids = cnt((int)swBodyType_e.swSolidBody);
  Console.WriteLine("despues solid=" + solids + " sheet=" + cnt((int)swBodyType_e.swSheetBody));
  if (f != null && solids > 3) {
    f.Name = "CB_Coser_a_solido";
    object[] sh = (object[])p.GetBodies2((int)swBodyType_e.swSheetBody, false);
    if (sh != null && sh.Length > 0) { foreach (Body2 b in sh) b.Select2(true, null); var d = (Feature)m.FeatureManager.InsertDeleteBody2(false); if (d != null) d.Name = "CB_Quitar_superficies_sueltas"; m.ClearSelection2(true); }
    Console.WriteLine("final solid=" + cnt((int)swBodyType_e.swSolidBody) + " sheet=" + cnt((int)swBodyType_e.swSheetBody) + " whatswrong=" + m.Extension.GetWhatsWrongCount());
    int e = 0, w = 0; Console.WriteLine("save " + m.Save3(1, ref e, ref w));
  }
  SW.App.CloseDoc(m.GetTitle()); } }
