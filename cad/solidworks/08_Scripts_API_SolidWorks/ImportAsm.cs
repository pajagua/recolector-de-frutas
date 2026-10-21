using System; using System.IO; using System.Linq; using SolidWorks.Interop.sldworks; using SolidWorks.Interop.swconst;
// ImportAsm.exe in.step outdir out.SLDASM : import STEP assembly keeping parts; save assembly + parts into outdir
class ImportAsm { static void Main(string[] a) { SW.Connect();
  int err = 0; var imp = (ImportStepData)SW.App.GetImportFileData(a[0]);
  var m = (ModelDoc2)SW.App.LoadFile4(a[0], "r", imp, ref err); Console.WriteLine("import err=" + err + " type=" + m.GetType());
  SW.Created.Add(m.GetTitle());
  var asm = (AssemblyDoc)m; object[] comps = (object[])asm.GetComponents(false);
  foreach (Component2 c in comps) Console.WriteLine("  " + c.Name2 + " | " + Path.GetFileName(c.GetPathName()) + " | " + SW.Xf(c.Transform2));
  int e = 0, w = 0;
  bool ok = m.Extension.SaveAs3(Path.Combine(a[1], a[2]), 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent | (int)swSaveAsOptions_e.swSaveAsOptions_SaveReferenced, null, null, ref e, ref w);
  Console.WriteLine("save " + ok + " e=" + e + " w=" + w);
  foreach (Component2 c in (object[])asm.GetComponents(false)) Console.WriteLine("  ref -> " + c.GetPathName());
  SW.App.CloseDoc(m.GetTitle()); } }
