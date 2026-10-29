#pragma once

// Pines, relaciones de transmisión, límites y velocidades de cada eje.
constexpr unsigned long SERIAL_BAUD = 115200;

// ---- J3 prismático (v7): NEMA 17 + correa GT2, polea de 20 dientes (40 mm por vuelta), A4988 a 1/16 ----
constexpr float J3_PASOS_POR_MM = 200.0f * 16.0f / 40.0f;   // 80 pasos/mm
constexpr float J3_CARRERA_MM = 340.0f;                     // q3 = 0 (retraído, final de carrera) ... 340
