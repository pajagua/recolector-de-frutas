#include <Arduino.h>
#include "config.h"

void setup() {
  Serial.begin(SERIAL_BAUD);
  Serial.println(F("CitriBot listo"));
}

void loop() {
}
