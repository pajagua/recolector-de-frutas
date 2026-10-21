using System; using System.Linq; using System.Collections.Generic;
using SolidWorks.Interop.sldworks; using SolidWorks.Interop.swconst;
// Box.exe part [holeMaxD] : overall box, biggest bodies, small through-holes (mounting) of the biggest body
class Box { static void Main(string[] a) {
  SW.Connect();
  var m = SW.Open(a[0]); double hmax = a.Length > 1 ? double.Parse(a[1]) : 4.0;
  object[] bs = (object[])((PartDoc)m).GetBodies2(0, true);
  double[] r = null; var list = new List<Tuple<double,Body2>>();
  foreach (Body2 b in bs) { double[] x = (double[])b.GetBodyBox(); if (r == null) r = (double[])x.Clone(); else for (int i=0;i<3;i++){ r[i]=Math.Min(r[i],x[i]); r[i+3]=Math.Max(r[i+3],x[i+3]); }
    double[] mp = (double[])b.GetMassProperties(1.0); list.Add(Tuple.Create(mp[3]*1e9, b)); }
  Console.WriteLine("bodies=" + bs.Length + " overall " + SW.Box(r));
  foreach (var t in list.OrderByDescending(t => t.Item1).Take(6)) Console.WriteLine("  vol " + t.Item1.ToString("0") + " " + t.Item2.Name + " " + SW.Box((double[])t.Item2.GetBodyBox()));
  var big = list.OrderByDescending(t => t.Item1).First().Item2;
  var seen = new HashSet<string>();
  foreach (var t in list.OrderByDescending(t => t.Item1).Take(3))
  foreach (Face2 f in (object[])t.Item2.GetFaces()) { var s = (Surface)f.GetSurface(); if (!s.IsCylinder()) continue; double[] p = (double[])s.CylinderParams; double d = p[6]*2000; if (d > hmax || d < 1.5) continue;
    double[] fb = (double[])f.GetBox(); string k = string.Format("D={0:0.##} c=({1:0.##},{2:0.##},{3:0.##}) ax=({4:0.#},{5:0.#},{6:0.#})", d, (fb[0]+fb[3])*500, (fb[1]+fb[4])*500, (fb[2]+fb[5])*500, p[3],p[4],p[5]);
    if (seen.Add(k)) Console.WriteLine("  hole? " + t.Item2.Name.Split('/').Last() + " " + k); }
  SW.App.CloseDoc(m.GetTitle());
}}
