using System; using System.Linq; using SolidWorks.Interop.sldworks; using SolidWorks.Interop.swconst;
// VerStep.exe <archivo.step> : lo importa, cuenta componentes y cuerpos y cierra todo lo importado sin guardar
class VerStep { static void Main(string[] a) { SW.Connect(); var t0 = DateTime.Now;
  var imp = (ImportStepData)SW.App.GetImportFileData(a[0]); int err = 0;
  var m = (ModelDoc2)SW.App.LoadFile4(a[0], "r", imp, ref err);
  if (m == null) { Console.WriteLine("FALLO importar err=" + err); return; }
  Console.WriteLine("importado " + m.GetTitle() + " tipo " + m.GetType() + " en " + (DateTime.Now - t0).TotalSeconds.ToString("0") + " s");
  if (m.GetType() == (int)swDocumentTypes_e.swDocASSEMBLY) {
    var cs = ((object[])((AssemblyDoc)m).GetComponents(false)).Cast<Component2>().ToList();
    int hojas = cs.Count(c => (c.GetChildren() as object[] ?? new object[0]).Length == 0);
    int top = ((object[])((AssemblyDoc)m).GetComponents(true)).Length;
    Console.WriteLine("componentes " + cs.Count + " (hojas " + hojas + ", primer nivel " + top + ")");
    var b = (double[])((AssemblyDoc)m).GetBox((int)swBoundingBoxOptions_e.swBoundingBoxIncludeRefPlanes); if (b != null) Console.WriteLine("caja " + SW.Box(b)); }
  var titulos = ((object[])SW.App.GetDocuments()).Cast<ModelDoc2>().Where(d => string.IsNullOrEmpty(d.GetPathName())).Select(d => d.GetTitle()).ToList();
  foreach (var t in titulos.OrderBy(t => t.ToUpper().Contains("ASM") ? 0 : 1)) SW.App.QuitDoc(t);
  Console.WriteLine("cerrados " + titulos.Count + " documentos importados"); } }
