using System; using SolidWorks.Interop.sldworks; using SolidWorks.Interop.swconst;
// Imp2.exe in.step out.SLDPRT knitOpt repair : STEP import with check&repair + knit-to-solid options; reports body types
class Imp2 { static void Main(string[] a) { SW.Connect();
  var app = SW.App;
  int oldK = app.GetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swImportNeutral_KnitOption);
  int oldR = app.GetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swImportCheckAndRepair);
  Console.WriteLine("prefs knit=" + oldK + " repair=" + oldR);
  app.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swImportNeutral_KnitOption, int.Parse(a[2]));
  app.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swImportCheckAndRepair, int.Parse(a[3]));
  int err = 0; var imp = (ImportStepData)app.GetImportFileData(a[0]);
  var m = (ModelDoc2)app.LoadFile4(a[0], "r", imp, ref err);
  app.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swImportNeutral_KnitOption, oldK);
  app.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swImportCheckAndRepair, oldR);
  if (m == null) { Console.WriteLine("fail " + err); return; }
  SW.Created.Add(m.GetTitle());
  var p = m as PartDoc;
  if (p != null) { object[] s = (object[])p.GetBodies2(0, false); object[] h = (object[])p.GetBodies2(1, false);
    Console.WriteLine("solid=" + (s == null ? 0 : s.Length) + " sheet=" + (h == null ? 0 : h.Length));
    if (s != null && s.Length > 3) { int e = 0, w = 0; Console.WriteLine("save " + m.Extension.SaveAs3(a[1], 0, 1, null, null, ref e, ref w)); } }
  SW.App.CloseDoc(m.GetTitle());
}}
