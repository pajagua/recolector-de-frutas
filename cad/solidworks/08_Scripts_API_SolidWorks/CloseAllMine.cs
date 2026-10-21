using System; using SolidWorks.Interop.sldworks;
class CloseAllMine { static void Main() { SW.Connect(); object[] r = (object[])SW.App.GetDocuments(); if (r == null) return;
  foreach (ModelDoc2 d in r) { string p = d.GetPathName(); if (!d.GetSaveFlag() && p.StartsWith(@"C:\Users\tikip\Desktop\CitriBot_Ensamblaje", StringComparison.OrdinalIgnoreCase)) SW.App.CloseDoc(d.GetTitle()); }
  r = (object[])SW.App.GetDocuments(); Console.WriteLine("docs abiertos: " + (r == null ? 0 : r.Length)); } }
