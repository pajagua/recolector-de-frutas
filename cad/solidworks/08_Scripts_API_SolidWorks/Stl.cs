using System; using SolidWorks.Interop.sldworks; using SolidWorks.Interop.swconst;
// Stl.exe in.stl out.SLDPRT : import STL as solid body (mm)
class Stl { static void Main(string[] a) {
  SW.Connect(); SW.CloseMine();
  SW.App.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swImportStlVrmlModelType, (int)swImportStlVrmlModelType_e.swImportStlVrmlModelType_Solid);
  SW.App.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swImportStlVrmlUnits, (int)swLengthUnit_e.swMM);
  int err = 0; var m = (ModelDoc2)SW.App.LoadFile4(a[0], "r", null, ref err);
  Console.WriteLine("load err=" + err + " doc=" + (m != null));
  if (m == null) return;
  SW.Created.Add(m.GetTitle());
  object[] bs = (object[])((PartDoc)m).GetBodies2((int)swBodyType_e.swSolidBody, true);
  Console.WriteLine("solid bodies " + (bs == null ? 0 : bs.Length));
  if (bs != null) foreach (Body2 b in bs) Console.WriteLine("  " + SW.Box((double[])b.GetBodyBox()) + " faces=" + b.GetFaceCount());
  int e = 0, w = 0; bool ok = m.Extension.SaveAs3(a[1], 0, 1, null, null, ref e, ref w);
  Console.WriteLine("save " + ok + " e=" + e);
  SW.CloseMine();
}}
