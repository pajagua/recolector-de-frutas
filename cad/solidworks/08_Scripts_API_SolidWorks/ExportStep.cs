using System; using SolidWorks.Interop.sldworks; using SolidWorks.Interop.swconst;
// ExportStep.exe <doc> <salida> : abre (si no está abierto), exporta una copia (STEP AP214 con colores, o la extensión que tenga la salida) y cierra sin guardar
class ExportStep { static void Main(string[] a) { SW.Connect();
  if (SW.App.GetOpenDocumentByName(a[0]) != null) { Console.WriteLine("ya está abierto: no se toca"); return; }
  var m = SW.Open(a[0]); if (m.GetType() == (int)swDocumentTypes_e.swDocASSEMBLY) ((AssemblyDoc)m).ResolveAllLightWeightComponents(false);
  SW.App.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swStepAP, 214);
  SW.App.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swStepExportFaceEdgeProps, true);
  SW.App.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swStepExportAppearances, true);
  int e = 0, w = 0; bool ok = m.Extension.SaveAs3(a[1], 0, (int)swSaveAsOptions_e.swSaveAsOptions_Silent | (int)swSaveAsOptions_e.swSaveAsOptions_Copy, null, null, ref e, ref w);
  Console.WriteLine((ok ? "exportado " : "FALLO ") + a[1] + " e=" + e + " w=" + w);
  SW.App.QuitDoc(m.GetTitle()); } }
