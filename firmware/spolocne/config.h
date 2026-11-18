// Spoločné nastavenie siete pre všetky uzly.
//
// Prostredie esp32dev (predvolené) je pre simulátor Wokwi.
// Prostredie hw je pre reálnu dosku: berie údaje zo súboru secrets.h
// (vzor je v secrets.example.h). Súbor secrets.h sa do Gitu neukladá,
// lebo obsahuje heslo k Wi-Fi.
#pragma once

#ifdef REALNY_HW
  #include "secrets.h"
#else
  #define WIFI_SSID   "Wokwi-GUEST"
  #define WIFI_PASS   ""
  #define MQTT_BROKER "broker.hivemq.com"
  #define MQTT_PORT   1883
#endif