using System; using SolidWorks.Interop.sldworks; using SolidWorks.Interop.swconst;
class Exp { static void Main(string[] a) {
  SW.Connect(); SW.CloseMine();
  var m = SW.Open(a[0]); ((AssemblyDoc)m).ResolveAllLightWeightComponents(false);
  int e = 0, w = 0;
  bool ok = m.Extension.SaveAs3(a[1], 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent | (int)swSaveAsOptions_e.swSaveAsOptions_Copy, null, null, ref e, ref w);
  Console.WriteLine("export " + ok + " e=" + e + " w=" + w + " -> " + a[1]);
  SW.CloseMine();
}}
