"""Genera BOM.md y BOM_CitriBot.xlsx a partir de BOM_CitriBot.csv.

Uso (desde la raíz del repo):
    python bom/generar_bom.py

Editá solo el CSV y volvé a correr este script. Requiere openpyxl.
"""
import csv
from collections import Counter, OrderedDict
from pathlib import Path

from openpyxl import Workbook
from openpyxl.styles import Alignment, Font, PatternFill
from openpyxl.utils import get_column_letter

DIR = Path(__file__).resolve().parent
CSV = DIR / "BOM_CitriBot.csv"
MD = DIR / "BOM.md"
XLSX = DIR / "BOM_CitriBot.xlsx"

ORIGENES = ["Préstamo laboratorio", "Compra grupal", "Propio", "Impresión 3D", "Por comprar", "Por definir"]
ESTADOS = ["En mano", "Prestado", "Pedido", "Falta", "Por confirmar"]


def leer():
    with open(CSV, encoding="utf-8", newline="") as f:
        filas = list(csv.DictReader(f))
    for f_ in filas:
        assert f_["Origen"] in ORIGENES, f"{f_['ID']}: origen desconocido '{f_['Origen']}'"
        assert f_["Estado"] in ESTADOS, f"{f_['ID']}: estado desconocido '{f_['Estado']}'"
    return filas


def precio(f_):
    return float(f_["Precio_USD"]) if f_["Precio_USD"] else 0.0


def celda(txt):
    return str(txt).replace("|", "\\|").replace("\n", " ")


def tabla(filas, cols, titulos=None):
    titulos = titulos or cols
    out = ["| " + " | ".join(titulos) + " |", "|" + "---|" * len(cols)]
    for f_ in filas:
        out.append("| " + " | ".join(celda(f_.get(c, "")) for c in cols) + " |")
    return "\n".join(out)


def escribir_md(filas):
    total = sum(precio(f_) for f_ in filas)
    por_origen = Counter(f_["Origen"] for f_ in filas)
    por_estado = Counter(f_["Estado"] for f_ in filas)

    md = ["# BOM completa – CitriBot", "",
          "> Generado por `bom/generar_bom.py` desde `bom/BOM_CitriBot.csv`. **No editar a mano**: editá el CSV y volvé a correr el script.",
          "",
          "## Resumen", "",
          "| Origen | Ítems |", "|---|---|"]
    md += [f"| {o} | {por_origen[o]} |" for o in ORIGENES if por_origen[o]]
    md += ["", "| Estado | Ítems |", "|---|---|"]
    md += [f"| {e} | {por_estado[e]} |" for e in ESTADOS if por_estado[e]]
    md += ["", f"**Gastado (compra grupal, sin envío): USD {total:.2f}**", ""]

    prest = [f_ for f_ in filas if f_["Origen"] == "Préstamo laboratorio"]
    md += ["## Préstamos del laboratorio (a devolver)", "",
           "Todo lo de esta tabla es del laboratorio y se devuelve al terminar el TPF.", ""]
    md.append(tabla([dict(f_, Devuelto="☐") for f_ in prest],
                    ["ID", "Componente", "Especificacion", "Cantidad", "Uso", "Estado", "Devuelto"],
                    ["ID", "Componente", "Especificación", "Cant.", "Uso", "Estado", "Devuelto"]))
    md.append("")

    compras = [f_ for f_ in filas if f_["Origen"] == "Compra grupal"]
    md += ["## Compras del grupo", ""]
    compras = [dict(f_, Precio_USD=f"{precio(f_):.2f}" if f_["Precio_USD"] else "") for f_ in compras]
    md.append(tabla(compras, ["ID", "Componente", "Cantidad", "Proveedor", "Precio_USD", "Estado", "Notas"],
                    ["ID", "Componente", "Cant.", "Proveedor", "USD", "Estado", "Notas"]))
    md += ["", f"**Total: USD {total:.2f}**", ""]

    falta = [f_ for f_ in filas if f_["Origen"] in ("Por comprar", "Por definir")]
    md += ["## Falta comprar o definir", ""]
    md.append(tabla(falta, ["ID", "Componente", "Especificacion", "Cantidad", "Unidad", "Origen", "Notas"],
                    ["ID", "Componente", "Especificación", "Cant.", "Un.", "Origen", "Notas"]))
    md.append("")

    conf = [f_ for f_ in filas if f_["Estado"] == "Por confirmar"]
    md += ["## Por confirmar", ""]
    md.append(tabla(conf, ["ID", "Componente", "Origen", "Notas"]))
    md.append("")

    grupos = OrderedDict()
    for f_ in filas:
        grupos.setdefault(f_["Subsistema"], []).append(f_)
    md += ["## BOM por subsistema", ""]
    for sub, items in grupos.items():
        md += [f"### {sub}", ""]
        md.append(tabla(items, ["ID", "Componente", "Especificacion", "Cantidad", "Unidad", "Uso", "Origen", "Estado", "Notas"],
                        ["ID", "Componente", "Especificación", "Cant.", "Un.", "Uso", "Origen", "Estado", "Notas"]))
        md.append("")

    MD.write_text("\n".join(md), encoding="utf-8")


def escribir_xlsx(filas):
    wb = Workbook()
    ws = wb.active
    ws.title = "BOM"
    cols = list(filas[0].keys())
    ws.append(cols)
    for f_ in filas:
        fila = []
        for c in cols:
            v = f_[c]
            if c == "Cantidad" and v:
                v = float(v) if "." in v else int(v)
            elif c == "Precio_USD":
                v = float(v) if v else None
            fila.append(v)
        ws.append(fila)

    cab = PatternFill("solid", fgColor="F28C28")
    for c in ws[1]:
        c.font = Font(bold=True, color="FFFFFF")
        c.fill = cab
    anchos = {"ID": 6, "Subsistema": 22, "Componente": 34, "Especificacion": 34, "Cantidad": 9, "Unidad": 7,
              "Uso": 40, "Origen": 20, "Estado": 14, "Precio_USD": 11, "Proveedor": 22, "Link": 30, "Notas": 60}
    for i, c in enumerate(cols, start=1):
        ws.column_dimensions[get_column_letter(i)].width = anchos.get(c, 15)
    for fila in ws.iter_rows(min_row=2):
        for c in fila:
            c.alignment = Alignment(vertical="top", wrap_text=True)
    col_precio = get_column_letter(cols.index("Precio_USD") + 1)
    for c in ws[col_precio][1:]:
        c.number_format = '"USD" #,##0.00'
    ws.freeze_panes = "C2"
    ws.auto_filter.ref = ws.dimensions

    # Resumen con fórmulas: se actualiza solo si se edita la hoja BOM en Excel.
    rs = wb.create_sheet("Resumen")
    col_origen = get_column_letter(cols.index("Origen") + 1)
    col_estado = get_column_letter(cols.index("Estado") + 1)
    rs.append(["Origen", "Ítems"])
    for o in ORIGENES:
        rs.append([o, f'=COUNTIF(BOM!{col_origen}:{col_origen},"{o}")'])
    rs.append([])
    rs.append(["Estado", "Ítems"])
    for e in ESTADOS:
        rs.append([e, f'=COUNTIF(BOM!{col_estado}:{col_estado},"{e}")'])
    rs.append([])
    rs.append(["Gastado (compra grupal, USD)", f"=SUM(BOM!{col_precio}:{col_precio})"])
    rs[f"B{rs.max_row}"].number_format = '"USD" #,##0.00'
    for fila in rs.iter_rows():
        if fila[0].value in ("Origen", "Estado"):
            for c in fila:
                c.font = Font(bold=True)
    rs.column_dimensions["A"].width = 30
    rs.column_dimensions["B"].width = 14

    wb.save(XLSX)


if __name__ == "__main__":
    filas = leer()
    escribir_md(filas)
    escribir_xlsx(filas)
    print(f"{len(filas)} items -> {MD.name}, {XLSX.name}")
