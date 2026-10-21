using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Geo.exe <part|step> [-img out.png] [-view name]
// Lists bodies box, cylindrical faces (holes/bosses) and big planar faces. Optional screenshot.
class Geo
{
    static void Main(string[] args)
    {
        SW.Connect();
        string path = args[0];
        ModelDoc2 m;
        if (path.ToUpper().EndsWith(".STEP") || path.ToUpper().EndsWith(".STP"))
        {
            int err = 0;
            var imp = (ImportStepData)SW.App.GetImportFileData(path);
            m = (ModelDoc2)SW.App.LoadFile4(path, "r", imp, ref err);
            Console.WriteLine("import err=" + err + " type=" + (m == null ? -1 : m.GetType()));
        }
        else m = SW.Open(path);
        if (m == null) return;
        string img = null, view = "*Isometric";
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "-img") img = args[++i];
            if (args[i] == "-view") view = args[++i];
        }
        List<Body2> bodies = new List<Body2>();
        if (m.GetType() == (int)swDocumentTypes_e.swDocPART)
        {
            object[] bs = (object[])((PartDoc)m).GetBodies2((int)swBodyType_e.swSolidBody, true);
            if (bs != null) foreach (Body2 b in bs) bodies.Add(b);
        }
        else if (m.GetType() == (int)swDocumentTypes_e.swDocASSEMBLY)
        {
            object[] comps = (object[])((AssemblyDoc)m).GetComponents(false);
            foreach (Component2 c in comps)
            {
                Console.WriteLine("comp " + c.Name2 + " " + c.GetPathName() + " " + SW.Xf(c.Transform2));
                object bi; object[] bs = (object[])c.GetBodies3((int)swBodyType_e.swSolidBody, out bi);
                if (bs != null) foreach (Body2 b in bs) bodies.Add(b);
            }
        }
        foreach (Body2 b in bodies)
        {
            Console.WriteLine("BODY " + b.Name + " box " + SW.Box((double[])b.GetBodyBox()));
            var cyl = new SortedDictionary<string, int>();
            var pl = new List<Tuple<double, string>>();
            object[] faces = (object[])b.GetFaces();
            foreach (Face2 f in faces)
            {
                Surface s = (Surface)f.GetSurface();
                double area = f.GetArea();
                if (s.IsCylinder())
                {
                    double[] p = (double[])s.CylinderParams; // origin xyz, axis xyz, radius
                    // canonical: project origin so that the axis component is irrelevant -> report origin as given + face box
                    double[] fb = (double[])f.GetBox();
                    bool inner = false;
                    // find a point on the face and the normal to tell hole (normal toward axis) vs boss
                    double[] uv = (double[])f.GetUVBounds();
                    double[] ev = (double[])s.Evaluate((uv[0] + uv[1]) / 2, (uv[2] + uv[3]) / 2, 0, 0);
                    double[] nrm = (double[])f.Normal; // not valid for curved faces
                    var pt = new double[] { ev[0], ev[1], ev[2] };
                    double[] ax = { p[3], p[4], p[5] };
                    double[] v = { pt[0] - p[0], pt[1] - p[1], pt[2] - p[2] };
                    double d = v[0] * ax[0] + v[1] * ax[1] + v[2] * ax[2];
                    double[] radial = { v[0] - d * ax[0], v[1] - d * ax[1], v[2] - d * ax[2] };
                    double[] sn = (double[])s.EvaluateAtPoint(pt[0], pt[1], pt[2]);
                    // sn: [x,y,z, nx,ny,nz,...]
                    double dot = radial[0] * sn[3] + radial[1] * sn[4] + radial[2] * sn[5];
                    if (f.FaceInSurfaceSense()) dot = -dot;
                    inner = dot < 0;
                    // axis point closest to origin plane for stable key
                    string key = string.Format("{0} D={1} axis=({2},{3},{4}) at ({5},{6},{7}) span[{8}..{9},{10}..{11},{12}..{13}]",
                        inner ? "HOLE" : "BOSS", SW.F(2 * p[6]), SW.Fa(Math.Round(ax[0], 3)), SW.Fa(Math.Round(ax[1], 3)), SW.Fa(Math.Round(ax[2], 3)),
                        SW.F(p[0]), SW.F(p[1]), SW.F(p[2]), SW.F(fb[0]), SW.F(fb[3]), SW.F(fb[1]), SW.F(fb[4]), SW.F(fb[2]), SW.F(fb[5]));
                    cyl[key] = cyl.ContainsKey(key) ? cyl[key] + 1 : 1;
                }
                else if (s.IsPlane())
                {
                    double[] p = (double[])s.PlaneParams; // normal xyz, point xyz
                    double[] fb = (double[])f.GetBox();
                    double[] n = (double[])f.Normal;
                    pl.Add(Tuple.Create(area, string.Format("PLANE n=({0},{1},{2}) area={3}mm2 box[{4}..{5},{6}..{7},{8}..{9}]",
                        SW.Fa(Math.Round(n[0], 3)), SW.Fa(Math.Round(n[1], 3)), SW.Fa(Math.Round(n[2], 3)), (area * 1e6).ToString("0"),
                        SW.F(fb[0]), SW.F(fb[3]), SW.F(fb[1]), SW.F(fb[4]), SW.F(fb[2]), SW.F(fb[5]))));
                }
            }
            foreach (var kv in cyl) Console.WriteLine("  " + kv.Key + (kv.Value > 1 ? " x" + kv.Value : ""));
            foreach (var t in pl.OrderByDescending(t => t.Item1).Take(25)) Console.WriteLine("  " + t.Item2);
        }
        if (img != null)
        {
            m.ShowNamedView2(view, -1);
            m.ViewZoomtofit2();
            m.GraphicsRedraw2();
            int e = 0, w = 0;
            bool ok = m.Extension.SaveAs3(img, 0, 1, null, null, ref e, ref w);
            Console.WriteLine("img " + ok + " " + e);
        }
    }
}
