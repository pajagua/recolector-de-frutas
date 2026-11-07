#pragma once

// Pines, relaciones de transmisión, límites y velocidades de cada eje.
constexpr unsigned long SERIAL_BAUD = 115200;

// ---- J3 prismático telescópico 2:1 (v7B): NEMA 17 + correa GT2, polea de 20 dientes (40 mm por vuelta en el tramo
// intermedio, 80 mm por vuelta en la brida), A4988 a 1/16. q3 se mide en la brida ----
constexpr float J3_PASOS_POR_MM = 200.0f * 16.0f / 80.0f;   // 40 pasos por mm de q3
constexpr float J3_CARRERA_MM = 340.0f;                     // q3 = 0 (retraído, final de carrera) ... 340 (170 + 170)
constexpr float J3_RPM_MAX = 150.0f;                        // con el 17HS4401: ≈ 200 mm/s en la brida
