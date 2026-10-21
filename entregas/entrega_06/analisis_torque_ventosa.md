# Análisis Matemático: Dimensionamiento de Ventosa para Recolección de Naranjas

Este documento presenta el análisis analítico para determinar la viabilidad de una ventosa de 40 mm de diámetro para un mecanismo *end-effector* que combina sujeción por vacío y torsión mecánica.

## 1. Parámetros Iniciales del Sistema

*   **Masa de la naranja ($m$):** $0.3 \, \text{kg}$ (peso máximo estimado).
*   **Aceleración de la gravedad ($g$):** $9.81 \, \text{m/s}^2$.
*   **Diámetro propuesto de la ventosa ($D$):** $40 \, \text{mm} \implies \text{Radio } (r) = 0.02 \, \text{m}$.
*   **Nivel de vacío estimado ($\Delta P$):** $-60 \, \text{kPa} = 60,000 \, \text{N/m}^2$.
*   **Coeficiente de fricción estática ($\mu$):** $0.5$ (estimado para NBR/PU sobre cáscara porosa seca).
*   **Torque de ruptura del pedúnculo ($T_{req}$):** $0.4 \, \text{Nm}$ a $1.0 \, \text{Nm}$.

## 2. Soporte de Carga (*Payload Support*)

La fuerza teórica de sujeción por vacío ($F_v$) ejercida por la ventosa está definida por la presión diferencial multiplicada por el área efectiva de contacto:

$$ F_v = \Delta P \cdot A = \Delta P \cdot (\pi \cdot r^2) $$

Sustituyendo los valores de diseño:

$$ F_v = 60,000 \cdot \pi \cdot (0.02)^2 \approx 75.40 \, \text{N} $$

Para validar la viabilidad del *payload*, calculamos la fuerza peso ($W$) de la naranja:

$$ W = m \cdot g = 0.3 \cdot 9.81 = 2.943 \, \text{N} $$

Aplicando un factor de seguridad dinámico de aceleración del brazo robótico ($S_f = 2$):

$$ F_{requerida} = W \cdot S_f = 2.943 \cdot 2 \approx 5.88 \, \text{N} $$

**Conclusión Parcial:** Dado que $F_v \gg F_{requerida}$ ($75.40 \, \text{N} > 5.88 \, \text{N}$), el diámetro de 40 mm proporciona un factor de seguridad enorme para la sujeción puramente gravitacional.

## 3. Transmisión de Torsión (*Slip Torque*)

El *end-effector* no solo sujeta, sino que debe aplicar un torque rotacional para arrancar la fruta. Para transferir este torque sin resbalar, el torque límite por fricción estática (*Slip Torque*, $T_s$) debe ser mayor al torque necesario para romper el tallo ($T_{req}$).

Asumiendo una distribución uniforme de la presión debajo de la ventosa, el diferencial de la fuerza normal es $dN = \Delta P \cdot 2\pi \cdot \rho \cdot d\rho$. Aplicando la ley de fricción de Coulomb sobre el área circular, la integral del torque es:

$$ T_s = \int_{0}^{r} (\mu \cdot dN) \cdot \rho $$

$$ T_s = \int_{0}^{r} \mu \cdot (\Delta P \cdot 2\pi \cdot \rho) \cdot \rho \cdot d\rho $$

$$ T_s = 2\pi \cdot \mu \cdot \Delta P \int_{0}^{r} \rho^2 d\rho = 2\pi \cdot \mu \cdot \Delta P \left[ \frac{\rho^3}{3} \right]_{0}^{r} = \frac{2}{3} \cdot \pi \cdot \mu \cdot \Delta P \cdot r^3 $$

Dado que $F_v = \Delta P \cdot \pi \cdot r^2$, podemos reescribir la ecuación en función de la fuerza de vacío:

$$ T_s = \frac{2}{3} \cdot \mu \cdot F_v \cdot r $$

Sustituyendo los valores para la ventosa de 40 mm ($r = 0.02 \, \text{m}$):

$$ T_s = \frac{2}{3} \cdot 0.5 \cdot 75.40 \cdot 0.02 \approx 0.502 \, \text{Nm} $$

## 4. Análisis Crítico y Trade-offs

El límite de deslizamiento teórico de la ventosa de 40 mm es de aproximadamente **$0.5 \, \text{Nm}$**. 

Si analizamos la relación matemática del *Slip Torque*:

$$ T_s \propto r^3 $$

El torque transmitido escala de manera cúbica respecto al radio de la ventosa. Si incrementamos el diámetro a **50 mm** ($r = 0.025 \, \text{m}$), manteniendo el mismo nivel de vacío:

*   **Nueva Fuerza de Sujeción ($F_v$):** $117.81 \, \text{N}$
*   **Nuevo Slip Torque ($T_s$):**
    $$ T_s = \frac{2}{3} \cdot 0.5 \cdot 117.81 \cdot 0.025 \approx 0.98 \, \text{Nm} $$

### Veredicto

El diámetro de 40 mm presenta un alto riesgo de deslizamiento sobre la cáscara si el pedúnculo requiere torques cercanos a $1.0 \, \text{Nm}$. Este deslizamiento provocará esfuerzos cortantes (*shear stress*) excesivos que degradarán prematuramente el material del labio de la ventosa y dañarán comercialmente la fruta.

Se recomienda utilizar una ventosa de **50 mm de diámetro** en **Poliuretano (PU) de doble dureza** o **NBR**, lo cual duplica la capacidad de transferencia de torque ($0.98 \, \text{Nm}$) sin requerir bombas de vacío con flujos energéticamente ineficientes.
