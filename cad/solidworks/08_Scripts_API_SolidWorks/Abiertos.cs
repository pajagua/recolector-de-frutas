using System;
using SolidWorks.Interop.sldworks;
// Abiertos.exe : documentos abiertos en SolidWorks (visible, modificado, ruta)
class Abiertos
{
    static void Main()
    {
        SW.Connect();
        var docs = SW.App.GetDocuments() as object[];
        if (docs == null) { Console.WriteLine("ninguno"); return; }
        foreach (ModelDoc2 d in docs) Console.WriteLine((d.Visible ? "visible " : "oculto  ") + (d.GetSaveFlag() ? "MODIFICADO " : "           ") + d.GetPathName());
    }
}
