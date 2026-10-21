using System; using System.IO; using System.Linq; using System.Collections.Generic; using SolidWorks.Interop.sldworks;
// Rename.exe dir mapfile(old|new names) : renames files on disk and fixes references inside the assemblies of dir (docs must be closed)
class Rename { static void Main(string[] a) { SW.Connect(); string dir = a[0];
  var map = File.ReadAllLines(a[1]).Where(l => l.Contains("|")).Select(l => l.Split('|')).ToDictionary(p => p[0].Trim(), p => p[1].Trim());
  foreach (var kv in map) File.Move(Path.Combine(dir, kv.Key), Path.Combine(dir, kv.Value));
  foreach (var asm in map.Values.Where(v => v.EndsWith(".SLDASM")).Concat(Directory.GetFiles(dir, "*.SLDASM").Select(Path.GetFileName)).Distinct()) {
    string ap = Path.Combine(dir, asm);
    foreach (var kv in map) {
      bool ok = SW.App.ReplaceReferencedDocument(ap, Path.Combine(dir, kv.Key), Path.Combine(dir, kv.Value));
      if (ok) Console.WriteLine(asm + ": " + kv.Key + " -> " + kv.Value); } } } }
