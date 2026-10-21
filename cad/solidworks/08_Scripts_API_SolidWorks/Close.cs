using System; using SolidWorks.Interop.sldworks;
class Close { static void Main() { SW.Connect(); SW.CloseMine(); object[] d = (object[])SW.App.GetDocuments(); Console.WriteLine("docs abiertos: " + (d == null ? 0 : d.Length)); if (d != null) foreach (ModelDoc2 m in d) Console.WriteLine("  " + m.GetPathName()); } }
