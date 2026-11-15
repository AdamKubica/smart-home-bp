// Spoločné nastavenie siete pre všetky uzly.
//
// Predvolené hodnoty platia pre simulátor Wokwi. Pri reálnom hardvéri sa
// vedľa tohto súboru vytvorí secrets.h (vzor je v secrets.example.h).
// Súbor secrets.h sa do Gitu neukladá, lebo obsahuje heslo k Wi-Fi.
#pragma once

#if __has_include("secrets.h")
  #include "secrets.h"
#else
  #define WIFI_SSID   "Wokwi-GUEST"
  #define WIFI_PASS   ""
  #define MQTT_BROKER "broker.hivemq.com"
  #define MQTT_PORT   1883
#endif