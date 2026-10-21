using System; using System.Linq; using System.Collections.Generic;
using SolidWorks.Interop.sldworks; using SolidWorks.Interop.swconst;
// Wrong.exe : READ-ONLY. Reports What's Wrong of the active doc and of every referenced doc (no rebuild, no save, no close)
class Wrong {
  static HashSet<string> seen = new HashSet<string>();
  static void Report(ModelDoc2 m, string tag) {
    if (m == null || !seen.Add(m.GetPathName().ToLower())) return;
    int n = m.Extension.GetWhatsWrongCount();
    if (n > 0) {
      object feats, codes, warns; m.Extension.GetWhatsWrong(out feats, out codes, out warns);
      var fs = (object[])feats; var cs = (int[])codes; var ws = (bool[])warns;
      for (int i = 0; i < n; i++) { var f = fs[i] as Feature; Console.WriteLine((ws[i] ? "WARN " : "ERR  ") + System.IO.Path.GetFileName(m.GetPathName()) + " :: " + (f != null ? f.Name + " (" + f.GetTypeName2() + ")" : "?") + " code=" + (swFeatureError_e)cs[i]); }
    }
  }
  static void Main(string[] args) {
    SW.Connect(); var app = SW.App;
    var m = args.Length > 0 ? SW.Open(args[0]) : (ModelDoc2)app.ActiveDoc; if (args.Length > 0) m.ForceRebuild3(false); Console.WriteLine("activo: " + m.GetPathName() + " dirty=" + m.GetSaveFlag());
    Report(m, "top");
    var a = m as AssemblyDoc;
    if (a != null) foreach (Component2 c in (object[])a.GetComponents(false)) {
      int sup = c.GetSuppression(); if (sup == (int)swComponentSuppressionState_e.swComponentSuppressed) continue;
      var d = (ModelDoc2)c.GetModelDoc2(); if (d == null) { Console.WriteLine("NODOC " + c.Name2 + " " + c.GetPathName()); continue; }
      Report(d, c.Name2);
    }
    Console.WriteLine("revisados " + seen.Count);
  }
}
