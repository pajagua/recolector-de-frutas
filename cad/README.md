# CAD

| Carpeta | Contenido |
|---|---|
| [solidworks/](solidworks/README.md) | **Ensamblaje completo** del brazo con la herramienta (SolidWorks 2026). Abrir `CB-000_CitriBot_Ensamblaje_General.SLDASM`. STEP del conjunto en `solidworks/07_STEP/` |
| [herramienta/](herramienta/) | Fuente paramétrica OpenSCAD de la herramienta (muñeca + torsión) y sus STL |
| [imprimir/](imprimir/README.md) | STL/3MF finales, listos para imprimir |

## Convención de nombres

`CB-XYZ_Descripcion` (CB = CitriBot). La centena indica el eslabón:
`1xx` base/J0 · `2xx` cintura/J1 · `3xx` hombro-E2/J2 · `4xx` prismático-E3/J3 · `5xx` comerciales · `6xx` herramienta.
`HW_` = tornillería y piezas normalizadas.

## Reglas

- Mové o copiá siempre la carpeta `solidworks/` completa: las referencias del ensamblaje son relativas a ella.
- No guardes versiones con otro nombre (`_v2`, `_final`): Git guarda el historial.
- Antes de editar una pieza, avisá al grupo o usá `git lfs lock <archivo>`.
