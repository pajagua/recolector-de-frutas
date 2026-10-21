using System; using System.Linq; using SolidWorks.Interop.sldworks; using SolidWorks.Interop.swconst;
// AddTool.exe : adds CB-600 (tool) to CB-400 (E3 link): U-yoke plate face on the E3 end-plate outer face, same 4-hole diamond (r=28.5),
// pitch axis parallel to J2, cup perpendicular to the prismatic axis pointing down (natural pose)
class AddTool { static void Main() { SW.Connect();
  string R = @"C:\Users\tikip\Desktop\CitriBot_Ensamblaje\";
  string toolPath = R + @"09_Herramienta\CB-600_Herramienta.SLDASM", e3Path = R + @"04_Prismatico_E3_J3\CB-400_Prismatico_E3_J3.SLDASM";
  var t = SW.Open(toolPath);
  var yoke = ((object[])((AssemblyDoc)t).GetComponents(true)).Cast<Component2>().First(c => c.Name2.StartsWith("CB-601"));
  var yokeInTool = Xf.FromComp(yoke); Console.WriteLine("yoke in tool " + yokeInTool);
  var m = SW.Open(e3Path); var a = (AssemblyDoc)m;
  foreach (Component2 c in (object[])a.GetComponents(true)) if (c.Name2.StartsWith("CB-600")) { Console.WriteLine("ya estaba"); return; }
  var e3 = ((object[])a.GetComponents(true)).Cast<Component2>().First(c => c.Name2.StartsWith("CB-401"));
  var e3InAsm = Xf.FromComp(e3);
  // yoke frame -> E3 part frame: hole-pattern centre (-102.89, 40, 0) on (-120.245, 238.221, -431.516)
  var rot = Xf.Rows(new double[] { 1, 0, 0 }, new double[] { 0, -1, 0 }, new double[] { 0, 0, -1 }, new double[] { 0, 0, 0 });
  var pc = rot.Apply(new double[] { -102.89, 40, 0 });
  var yokeInE3 = rot.Then(Xf.Tr(-120.245 - pc[0], 238.221 - pc[1], -431.516 - pc[2]));
  var toolInAsm = yokeInTool.Inv().Then(yokeInE3).Then(e3InAsm);
  var comp = (Component2)a.AddComponent5(toolPath, 0, "", false, "", 0, 0, 0);
  comp.Transform2 = toolInAsm.ToMath();
  m.ClearSelection2(true); comp.Select4(false, null, false); a.FixComponent(); m.ClearSelection2(true);
  m.EditRebuild3();
  int e = 0, w = 0; Console.WriteLine("save " + m.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w) + " e=" + e + " w=" + w);
  SW.App.CloseDoc(m.GetTitle()); SW.App.CloseDoc(t.GetTitle()); } }
