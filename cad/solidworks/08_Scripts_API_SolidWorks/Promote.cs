using System;
using System.IO;
using SolidWorks.Interop.sldworks;

// Promote.exe <root> : la variante v7A pasa a ser el ensamblaje principal (nombres oficiales CB-000/300/301/400/401/403/514).
// Copia los archivos de 10_v7A_Prismatico_Correa sobre los nombres oficiales y cambia las referencias con
// ReplaceReferencedDocument (los documentos tienen que estar cerrados). No borra nada.
class Promote
{
    static string R;
    static string P(string rel) { return Path.Combine(R, rel); }
    static void Copy(string src, string dst)
    {
        File.Copy(P(src), P(dst), true);
        File.SetAttributes(P(dst), FileAttributes.Normal);
        Console.WriteLine("copiado " + src + " -> " + dst);
    }
    static void Ref(string asm, string oldRef, string newRef)
    {
        bool ok = SW.App.ReplaceReferencedDocument(P(asm), P(oldRef), P(newRef));
        Console.WriteLine((ok ? "ref ok   " : "REF FALLO ") + asm + ": " + Path.GetFileName(oldRef) + " -> " + Path.GetFileName(newRef));
        if (!ok) throw new Exception("fallo ReplaceReferencedDocument");
    }
    static void Main(string[] args)
    {
        R = args[0];
        SW.Connect();
        var docs = SW.App.GetDocuments() as object[];
        if (docs != null && docs.Length > 0) throw new Exception("hay documentos abiertos en SolidWorks: cerrarlos primero");
        const string A = "10_v7A_Prismatico_Correa\\", H = "03_Hombro_E2_J2\\", J = "04_Prismatico_E3_J3\\", C = "05_Comerciales\\";
        string e2a = A + "CB-301A_Eslabon_E2_v7A_Correa.SLDPRT", tuboA = A + "CB-421A_Tubo_E3_Aluminio_30x30x2_L440.SLDPRT",
               tapA = A + "CB-422A_Tapon_Trasero_Pinza_Correa.SLDPRT", corA = A + "CB-441A_Correa_GT2_6mm_J3.SLDPRT",
               h300A = A + "CB-300A_Hombro_E2_v7A.SLDASM", p400A = A + "CB-400A_Prismatico_E3_v7A.SLDASM", top0A = "CB-000A_CitriBot_v7A_Correa.SLDASM";
        string e2 = H + "CB-301_Eslabon_E2_mod5840.SLDPRT", tubo = J + "CB-401_Tubo_E3_Aluminio_30x30x2.SLDPRT",
               tap = J + "CB-403_Tapon_Trasero_Pinza_Correa.SLDPRT", cor = C + "CB-514_Correa_GT2_6mm_J3.SLDPRT",
               h300 = H + "CB-300_Hombro_E2_J2.SLDASM", p400 = J + "CB-400_Prismatico_E3_J3.SLDASM", top0 = "CB-000_CitriBot_Ensamblaje_General.SLDASM";
        Copy(e2a, e2); Copy(tuboA, tubo); Copy(tapA, tap); Copy(corA, cor);
        Copy(h300A, h300); Copy(p400A, p400); Copy(top0A, top0);
        Ref(h300, e2a, e2); Ref(h300, corA, cor);
        Ref(p400, tuboA, tubo); Ref(p400, tapA, tap);
        Ref(top0, h300A, h300); Ref(top0, p400A, p400);
        Console.WriteLine("listo");
    }
}
