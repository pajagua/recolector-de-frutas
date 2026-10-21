using System; using System.IO; using System.Linq; using System.Collections.Generic;
using SolidWorks.Interop.sldworks; using SolidWorks.Interop.swconst;
class ImportTool { static void Main(string[] a) { SW.Connect();
  string dir = a[1];
  var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
    {"HorquillaU.step", "CB-601_Horquilla_U"}, {"PortaServo.step", "CB-602_Porta_Servo"}, {"Caja.step", "CB-603_Caja_Torsion"},
    {"Tapa.step", "CB-604_Tapa_Caja"}, {"PoleaServo.step", "CB-605_Polea_Servo_GT2"}, {"EjeHueco_Polea.step", "CB-606_Eje_Hueco_Polea"},
    {"Separador.step", "CB-607_Separador"}, {"MG996R_1-0.step", "CB-610_Servo_MG996R_Muneca"}, {"MG996R.step", "CB-611_Servo_MG996R_Torsion"},
    {"MG996R_1.step", "CB-612_MG996R_Cuerpo"}, {"Component1.step", "CB-613_MG996R_Componente1"}, {"servo_Horn_round_20mm.step", "CB-614_Horn_Servo_D20"},
    {"5972K217_Steel Ball Bearing.step", "CB-615_Rodamiento_5972K217"}, {"5972K91_Steel Ball Bearing.step", "CB-616_Rodamiento_5972K91"},
    {"5427A638_Vacuum Cup.step", "CB-617_Ventosa_5427A638"} };
  int err = 0; var imp = (ImportStepData)SW.App.GetImportFileData(a[0]);
  var top = (ModelDoc2)SW.App.LoadFile4(a[0], "r", imp, ref err); Console.WriteLine("import err=" + err);
  var root = ((object[])((AssemblyDoc)top).GetComponents(true)).Cast<Component2>().First();
  var inner = (ModelDoc2)root.GetModelDoc2();
  Console.WriteLine("inner " + inner.GetTitle());
  var docs = new Dictionary<string, Tuple<ModelDoc2,int>>(StringComparer.OrdinalIgnoreCase);
  foreach (Component2 c in (object[])((AssemblyDoc)inner).GetComponents(false)) {
    var d = (ModelDoc2)c.GetModelDoc2(); if (d == null) continue; string k = Path.GetFileNameWithoutExtension(d.GetPathName());
    int depth = c.Name2.Count(ch => ch == '/');
    if (!docs.ContainsKey(k) || docs[k].Item2 < depth) docs[k] = Tuple.Create(d, depth); }
  // save parts first, then sub-assemblies, deepest first
  foreach (var kv in docs.OrderBy(kv => kv.Value.Item1.GetType() == (int)swDocumentTypes_e.swDocPART ? 0 : 1).ThenByDescending(kv => kv.Value.Item2)) {
    string nm = map.ContainsKey(kv.Key) ? map[kv.Key] : "CB-6xx_" + kv.Key.Replace(".step", "");
    string ext = kv.Value.Item1.GetType() == (int)swDocumentTypes_e.swDocPART ? ".SLDPRT" : ".SLDASM";
    int e = 0, w = 0; bool ok = kv.Value.Item1.Extension.SaveAs3(Path.Combine(dir, nm + ext), 0, 1, null, null, ref e, ref w);
    Console.WriteLine((ok ? "ok   " : "FAIL ") + kv.Key + " -> " + nm + ext + " e=" + e); }
  int e2 = 0, w2 = 0; bool ok2 = inner.Extension.SaveAs3(Path.Combine(dir, "CB-600_Herramienta.SLDASM"), 0, 1, null, null, ref e2, ref w2);
  Console.WriteLine("herramienta " + ok2 + " e=" + e2);
  foreach (Component2 c in (object[])((AssemblyDoc)inner).GetComponents(false)) Console.WriteLine("  " + c.Name2 + " -> " + c.GetPathName());
  SW.App.CloseDoc(top.GetTitle()); SW.App.CloseDoc(inner.GetTitle()); } }
