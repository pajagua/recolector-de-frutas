# Espacio de trabajo

Cómo se verifica que CitriBot alcanza toda la esfera del árbol, y con qué números quedó la v7 del CAD.
Para recalcular: `python control/espacio_trabajo.py` (con `--carro 50` si el carro del riel levanta el robot 50 mm, y `--figura` para regenerar la imagen).

![Corte perpendicular al riel](imagenes/espacio_trabajo.png)

## 1. Qué hay que alcanzar

El TPF pide una **esfera de 30 cm de diámetro elevada del suelo**. El boceto del grupo la ubica así:

- a **5 cm del borde de la base**, del lado del árbol;
- con el **centro a 35 cm del suelo**.

En el CAD la base es un tablero de 400 × 300: 400 mm a lo largo del riel y 300 mm hacia el árbol. Desde la v7.9 el eje J1 está en el centro del tablero: a 150 mm de cada borde del lado de 300 y a 200 mm de cada borde del lado de 400. La esfera se deja a 385 mm de J1, como en la v7.7, así que queda a **85 mm** del borde y no a los 50 mm del boceto. Con 50 mm, el lado cercano de la esfera queda 17 mm fuera de alcance (ver §4).

| Dato | Valor | De dónde sale |
|---|---|---|
| Eje J1 → borde del tablero (lado del árbol) | 150 mm | CAD (CB-111, `Base3040.cs`, v7.9) |
| Separación borde → esfera | 85 mm | el boceto dice 50; se agranda para no perder alcance (v7.9) |
| Radio de la esfera | 150 mm | TPF |
| **Eje J1 → centro de la esfera (horizontal)** | 150 + 85 + 150 = **385 mm** | |
| **Suelo → centro de la esfera** | **350 mm** | boceto |
| Suelo → cara del tablero | 80 mm (patas + tablero, sin riel) | CAD |
| Cara del tablero → eje J2 | 355,5 mm | CAD |
| **Suelo → eje J2** | **435,5 mm** + alto del carro del riel | |

## 2. Qué alcanza el brazo

CitriBot es P-RRP-RR. Para la posición de la ventosa importan cuatro articulaciones:

- **J0 (riel)** lleva el robot a lo largo del árbol. Por eso cualquier punto del árbol se puede mirar "de frente", y alcanza con estudiar **un corte vertical perpendicular al riel**.
- **J1 (cintura)** apunta el brazo hacia el árbol. El plano del brazo está corrido 83 mm del eje J1, y el riel compensa ese corrimiento.
- **J2 (hombro)** fija el ángulo θ del brazo respecto de la horizontal.
- **J3 (prismático)** fija la distancia ρ entre el eje J2 y la ventosa.

Con la ventosa alineada con el brazo (muñeca a −90°), la distancia es:

```
ρ = 105 mm (eje J2 → cara de la brida de E3, J3 retraído) + q3 + 122,8 mm (brida → labio de la ventosa SAB40)
q3 = 0 … 340 mm   →   ρ = 227,8 … 567,8 mm
```

Entonces, en el corte perpendicular al riel, la ventosa barre un **sector de corona circular** con centro en el eje J2:

- radios de 227,8 a 567,8 mm;
- ángulos entre los límites de J2 sin choques: **−45° a +45°** con el brazo apuntando al árbol, verificado cada 15° con J3 retraído y extendido (`Sweep7.cs`). No se probó más allá de ±45°.

Ese sector es la zona celeste de la figura. Al girar J1 se forma un casquete esférico grueso alrededor de la torre, y al mover el riel ese casquete se estira a lo largo del árbol. Eso es el espacio de trabajo del robot.

## 3. La condición

La esfera entra si todos sus puntos caen dentro del sector. Si **d** es la distancia del eje J2 al centro de la esfera y R = 150 mm, se calcula así:

```
d   = √( 385² + (350 − 435,5)² ) = 394,4 mm
ρ   necesario: d − R … d + R             = 254,0 … 554,0 mm
θ   necesario: atan2(−85,5, 385) ± asin(R/d) = −12,5° ± 22,4° = −34,9° … +9,8°
```

| | Necesita | Tiene el brazo (v7.7) | Margen |
|---|---|---|---|
| ρ mínimo (lado cercano de la esfera) | 244,5 mm | 227,8 mm | 17 mm |
| ρ máximo (lado lejano) | 544,3 mm | 567,8 mm | 24 mm |
| Ángulo de J2 | −34,9° … +9,8° | −45° … +45° | 10° abajo, 35° arriba |

El script revisa uno por uno los puntos del corte central de la esfera, porque los demás cortes son discos más chicos. Con estos datos alcanza el **100 %** de los puntos.

**Carro del riel.** Todavía no está diseñado, y si sube el robot, el lado lejano se aleja. El brazo cubre toda la esfera mientras el carro mida **hasta 77 mm** de alto (con el tablero de 550 × 550 eran 51 mm; con la v6, de 350 mm de carrera, eran 79 mm). Con 50 mm el margen lejano baja a 9,7 mm. Si el carro tiene que ser más alto, alargar 10 mm el tubo y la cola de E2 devuelve los 350 mm de carrera.

## 4. Por qué hubo que cambiar el CAD (v5 → v6 → v7)

En la v5 la ventosa quedaba entre 357 y 411 mm del eje J2: solo 5,35 cm de carrera, con E3 y el tornillo TSF del Rx-rcp-003. Eso no llegaba ni al lado cercano (254 mm) ni al lejano (554 mm). Hacía falta una carrera de al menos 300 mm.

Además, el brazo tenía que poder **acortarse** hasta ≈ 23 cm. Con un prismático que solo sale hacia adelante, el largo retraído ya es mayor que eso. Por eso en la v6:

- **E2** se cortó a 93 mm por delante del eje J2 y se le agregó una cola de 55 × 55 hacia atrás. El **tubo de E3** (aluminio 30 × 30 × 2, 460 mm) se retrae dentro de la cola, por detrás del hombro.
- J3 lo mueve un **NEMA 17 con husillo T8 de 400 mm** (avance 8 mm), como en el cálculo del grupo. El motor queda al final de la cola, a 41 cm detrás del eje J2. Además funciona de contrapeso del brazo.
- La carrera es de **350 mm**. Con extensión máxima siguen guiados 108 mm de tubo dentro de E2.

Detalle de piezas y montaje: [cad/solidworks/README.md](../cad/solidworks/README.md).

### v6 → v7

La v6 dejaba 451,5 mm de brazo detrás del hombro: la cola de E2 más el NEMA 17 en la punta. En la v7 el tubo de E3 (440 mm) se mueve con una **correa GT2** y el NEMA 17 sube al cubo de E2, así que la cola termina a **377 mm** detrás del eje J2. La carrera queda en **340 mm** y la ventosa va de 227,8 a 567,8 mm. Comparación con la variante telescópica (204 mm detrás de J2): [v7_prismatico.md](v7_prismatico.md).

### v7.6 → v7.7: tablero de 400 × 300

El tablero de 550 × 550 del BCN3D pasa a 400 × 300. El eje J1 queda a **185 mm** del borde del árbol (antes 194,8), así que la esfera se acerca 9,8 mm al hombro. Los márgenes quedan más parejos: 17 mm del lado cercano y 24 mm del lejano (antes 26 y 14), y el carro del riel puede medir hasta 77 mm (antes 51). La bandeja de electrónica pasa al costado de J1, y el mástil de las cámaras a la esquina trasera. Detalle en el README del CAD (v7.7).

### v7.7 → v7.9: J1 en el centro del tablero

El tablero se corre 35 mm hacia atrás para que J1 quede en el centro: 150 mm a cada borde del lado de 300. La esfera se deja donde estaba respecto de J1 (385 mm), así que el alcance, los márgenes y la puntería de las cámaras no cambian. Lo que cambia es la separación entre el borde y la esfera, que pasa de 50 a 85 mm.

Si se respetan los 50 mm del boceto, la esfera se acerca 35 mm al brazo. El lado cercano pide entonces ρ = 210 mm, menos que los 227,8 mm de J3 retraído, y queda afuera el 1,8 % de la esfera.

## 5. Cuidados

- En el CAD la ventosa es la medida (`CB-618`, Ø40 × 22). Su parte de arriba entra 6,8 mm en la punta del eje hueco y el labio queda a 41,2 mm del eje de cabeceo, con el eje de cabeceo a 67,6 mm de la brida (ver [sistemas_de_referencia.md](sistemas_de_referencia.md)). Con ella ρ va de 213,8 a 553,8 mm: con el tablero de 400 × 300 cubre toda la esfera, con 9,5 mm de margen del lado lejano (con el de 550 × 550 quedaba 0,2 mm afuera).
- El cálculo usa la **ventosa alineada con el brazo** (acercamiento de frente). Con la muñeca en otra posición la punta queda más cerca del eje J2.
- Los límites de J2 son con el brazo **apuntando al árbol**. Si J1 apunta para otro lado, la cola o la herramienta pueden tocar la bandeja de electrónica o el tablero antes: ver los rangos en el README del CAD.
- J1 se gira para que el brazo apunte de frente al árbol. Si el riel no se puede mover para compensar los 83 mm de corrimiento lateral, el brazo entra un poco en diagonal. Eso cambia ρ en menos de 1 cm.
