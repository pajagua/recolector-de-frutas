# Piezas para imprimir

Un STL por pieza impresa, con el mismo nombre que su `.SLDPRT` y separados por subconjunto como en `cad/solidworks/`. La herramienta va primero (`00_Herramienta` = `09_Herramienta` del CAD).

- Material: PETG, ≥ 4 perímetros, salvo que la nota diga otra cosa. Impresora: Ender 3 V2 (220 × 220 × 250 mm).
- Los STL salen en mm, en las coordenadas de la pieza: orientarlos en el laminador.
- Lista de piezas, estado y cantidades: [bom/BOM.md](../../bom/BOM.md#piezas-impresas).

## 00_Herramienta (CB-600)

| Archivo | Cant. | Notas |
|---|---|---|
| `CB-601_Horquilla_U.stl` | 1 | tiene dos cuerpos (la horquilla y una pestaña chica), igual que el STL original. v7.7: agujeros de las pestañas del servo de muñeca en 9 × 49 (medido), pasantes de 3,4 con tuerca M3 atrapada en la cara interior del brazo: M3 × 12 Allen desde afuera |
| `CB-602_Porta_Servo.stl` | 1 | con la ranura para el cable del MG996R de la torsión (v7.3). El tornillo de abajo del lado del cable agarra solo 5,5 mm |
| `CB-603_Caja_Torsion.stl` | 1 | con la ventana para el cable del servo de torsión (v7.4). v7.7: suplemento de 2 mm con asiento para el horn de plástico (Ø21, resalte de 8,5 × 1) y 4 pilotos de Ø2: el horn se atornilla desde afuera |
| `CB-604_Tapa_Caja.stl` | 1 | |
| `CB-605_Polea_Servo_GT2.stl` | 1 | v7.7: asiento de 4 mm para el horn de plástico (Ø21, resalte de 8,5 × 1) y 4 pilotos de Ø2. El horn se atornilla desde abajo antes de calzarlo en el servo |
| `CB-606_Eje_Hueco_Polea.stl` | 1 | alojamiento doble D para la ventosa, fondo de 3 mm para el racor y ranura para el seguro DIN 471-30 (v7.4: punta 2 mm más larga). Polea abajo, 100 % de relleno, sin soportes |
| `CB-607_Separador.stl` | 1 | |
| `CB-620_Llave_Montaje_Racor.stl` | 1 | herramienta de montaje, no va en el robot. Hexágono de 12.4 para el racor de 12. De pie, el mango arriba |
| `CB-621_Separador_Eje_Hueco.stl` | 1 | entre el cubo del eje y el rodamiento de la tapa; 100 % de relleno |
| `CB-622_Arandela_Seguro_Eje.stl` | 1 | entre el rodamiento del piso y el seguro DIN 471-30; 100 % de relleno |

**Horn (v7.7).** Las piezas siguen el horn de plástico redondo del MG996R, medido: disco de Ø21 × 2,5, alto total 6, resalte de Ø8,5 × 1 en la cara plana, 12 agujeros en Ø14,5. La cara plana (la del resalte) va contra la pieza. Se usan 4 agujeros, uno de cada tres: girar el horn hasta que coincidan con los pilotos. Tornillos del kit del servo (M2–M2,5); si quedan cortos, M2,5 × 8. El horn metálico Ø20 del CAD (CB-614) ya no se usa.

El anillo de retención de la `.scad` (BOM P20) no hace falta: en el CAD el eje lo frenan el separador CB-621 y el seguro con la arandela CB-622.

## 01_Base_J0 (CB-100)

| Archivo | Cant. | Notas |
|---|---|---|
| `CB-112_Pata_Base.stl` | 4 | |
| `CB-121_Placa_Fija_J1_1M2A.stl` | 1 | |
| `CB-131_Soporte_Motor_J1_37D.stl` | 1 | |
| `CB-141_Bandeja_Electronica.stl` | 1 | |
| `CB-142_Soporte_PCA9685.stl` | 1 | soporte del PCA9685 al lado de la bandeja (v7.11); placa sobre la cama |
| `CB-151_Pie_Mastil_Camaras.stl` | 1 | |
| `CB-153_Cabezal_Mastil.stl` | 1 | |
| `CB-154_Adaptador_Soporte_Estereo.stl` | 1 | adaptador del soporte estéreo al pivote del mástil; 100 % de relleno |
| `CB-156_Soporte_Estereo_C270_Base.stl` | 1 | base del soporte StereoVision_03 (remodelada) con los 4 agujeros para el adaptador |
| `CB-157_Soporte_Estereo_C270_Tapa.stl` | 2 | tapa del soporte StereoVision_03 (STL original del usuario) |
| `CB-161_Soporte_Final_Carrera_J1.stl` | 1 | poste del final de carrera de J1 (v7.11); parado sobre la base |
| `CB-162_Soporte_Final_Carrera_J0.stl` | 1 | placa del final de carrera de J0, provisoria (v7.11); plana |

## 02_Cintura_J1 (CB-200)

| Archivo | Cant. | Notas |
|---|---|---|
| `CB-201_Plato_Giratorio_Polea_J1_1M1B.stl` | 1 | |
| `CB-210_Torre_Cintura_2M1D_mod.stl` | 1 | **no entra en la Ender 3 V2** (318 × 178 × 162 mm, con la leva del final de carrera de J1 en la base desde la v7.11): hay que partirla o imprimirla en una impresora más grande. La leva queda sobre la cama. v7.13: asiento Ø18,3 del buje del 5840 y leva de J1 más corta (de 243,6° a 272,9° respecto del brazo, rampas de 4 × 4); el switch de J1 se monta girado 180° respecto de la v7.11 |
| `CB-232_Soporte_Iman_J2.stl` | 1 | |

## 03_Hombro_E2_J2 (CB-300)

| Archivo | Cant. | Notas |
|---|---|---|
| `CB-301_Eslabon_E2_mod5840.stl` | 1 | **solo variante A** (alternativa): no imprimir para el telescópico. **no entra en la Ender 3 V2** (470 × 93 × 87 mm): hay que partirlo o imprimirlo en una impresora más grande. Cola con 3 perímetros y 15 %. v7.11: paso de cables Ø6 en la pared trasera |
| `CB-303_Tapa_E2_Soporte_AS5600.stl` | 1 | |
| `CB-305_Buje_D8_Hombro.stl` | 1 | |
| `CB-306_Placa_Soporte_NEMA17_Prismatico.stl` | 1 | |
| `CB-307_Soporte_Final_Carrera_J3.stl` | 1 | **solo variante A** (alternativa): no imprimir para el telescópico. soporte del final de carrera de J3 en el fondo del canal de E2 (v7.11); apoyado sobre la cara del brazo del switch |

## 04_Prismatico_E3_J3 (CB-400, variante A: alternativa)

Solo para la variante A (correa y un tubo). Para el telescópico, que es el oficial desde la v7.12, no se imprimen.

| Archivo | Cant. | Notas |
|---|---|---|
| `CB-402_Brida_E3_Herramienta.stl` | 1 | |
| `CB-403_Tapon_Trasero_Pinza_Correa.stl` | 1 | |

## 11_v7B_Prismatico_Telescopico (prismático oficial desde la v7.12)

Piezas del prismático telescópico 2:1. Lista completa (también lo que hay que comprar): [docs/telescopica_lista_componentes.md](../../docs/telescopica_lista_componentes.md).

| Archivo | Cant. | Notas |
|---|---|---|
| `CB-301B_Eslabon_E2_v7B_Telescopico.stl` | 1 | 305 mm (con el alojamiento del final de carrera de J3 en la pared trasera, v7.12): no entra en la Ender 3 V2, partir en 2 tramos. Cubo ≥ 6 perímetros y 50 %, manga 4 perímetros y 25 % |
| `CB-412B_Tapon_Trasero_Intermedio_Polea.stl` | 1 | 100 % |
| `CB-413B_Soporte_Polea_Delantera.stl` | 1 | 100 % |
| `CB-414B_Buje_Delantero_Intermedio.stl` | 1 | 100 % |
| `CB-415B_Pinza_Correa_Etapa1.stl` | 1 | 100 % |
| `CB-422B_Buje_Trasero_Interior_Pinza_2a1.stl` | 1 | 100 % |
| `CB-432B_Brida_E3_Herramienta_20x20.stl` | 1 | 100 % en el tapón |

La placa del motor CB-306 es la de `03_Hombro_E2_J2`.

## 12_Pruebas (no van en el robot)

| Archivo | Cant. | Notas |
|---|---|---|
| `CB-211_Prueba_Montaje_5840.stl` | 1 | **prueba del motor del hombro antes de imprimir la torre** (v7.13): el trozo de CB-210 donde va el motor, idéntico a la torre: pared de 12 mm con el agujero Ø32 del buje y el asiento Ø18,3 × 2 del lado del motor, los 2 agujeros de los M4 con sus avellanados Ø11,5 y el anillo Ø44/Ø56 de la cara de afuera. 62 × 20 × 73,5 mm. **Imprimir parada, como viene en el STL** (igual que la torre: agujeros horizontales), con el mismo perfil y una orla (brim). Probar: buje calzado en el asiento (sin juego y sin forzar), resaltes apoyados en la pared, 2 × M4x10 que entran derechos con el buje calzado y cuya cabeza pasa por el anillo |
| `CB-158_Adaptador_Rotula_Bola12.stl` | 1 | soporte estéreo sobre la rótula del trípode, con bola Ø11,8 integrada (cuello macizo), 4 × M3x12 a la base. Cara de arriba en la cama, soportes bajo la bola |

No hay STL de las piezas compradas (`05_Comerciales`), del tablero de madera (CB-111), ni de los tubos de aluminio (CB-152, CB-401, CB-411B, CB-421B).

Para volver a exportar: `cad/solidworks/08_Scripts_API_SolidWorks/ExportStl.cs` (manda las piezas de `09_Herramienta` a `00_Herramienta`).
