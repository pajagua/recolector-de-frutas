# BOM

| Archivo | Qué es |
|---|---|
| `BOM_CitriBot.csv` | **Fuente**: la única que se edita |
| `BOM.md` | Tablas para leer en GitHub (generado) |
| `BOM_CitriBot.xlsx` | Planilla con filtros y resumen (generado) |
| `generar_bom.py` | Regenera `BOM.md` y el Excel desde el CSV |

Ver la BOM: **[BOM.md](BOM.md)**.

## Cómo editar

1. Abrí `BOM_CitriBot.csv` (VS Code, o importalo en Excel como CSV separado por comas, UTF-8).
2. Cambiá o agregá filas. Usá solo estos valores:
   - **Origen**: `Préstamo laboratorio`, `Compra grupal`, `Propio`, `Impresión 3D`, `Por comprar`, `Por definir`.
   - **Estado**: `En mano`, `Prestado`, `Pedido`, `Falta`, `Por confirmar`.
3. Corré desde la raíz del repo (necesita `pip install openpyxl`):

   ```
   python bom/generar_bom.py
   ```

4. Commit de los tres archivos.

## Costos del pedido grupal

Según la planilla de pedidos del grupo (sin envío):

| Proveedor | USD | Gs |
|---|---|---|
| AliExpress | 110,81 | 682.253 |
| Amazon | 24,88 | 144.304 |
| **Total** | **135,69** | **826.557** |
| Por integrante (4) | | 206.639 |
