#include <WiFi.h>
#include <PubSubClient.h>
#include <OneWire.h>
#include <DallasTemperature.h>
#include "config.h"   // Wi-Fi a adresa brokera (firmware/spolocne)

const char* MQTT_CLIENT_ID = "esp32-izba1";

const char* TOPIC_TEPLOTA = "dom/izba1/teplota";
const char* TOPIC_SVETLO  = "dom/izba1/svetlo";
const char* TOPIC_PRIKAZ  = "dom/izba1/svetlo/prikaz";
const char* TOPIC_STAV    = "dom/izba1/stav";   // online/offline

const int PIN_PHOTO = 34;   // fotorezistor (ADC1, funguje aj pri zapnutom Wi-Fi)
const int PIN_TEPLOTA = 4;  // DS18B20 (zbernica 1-Wire)
const int PIN_RELAY = 26;

// Relé v simulátore Wokwi zapína úroveň LOW,
// reálny modul má jumper na H, takže zapína úroveň HIGH
#ifdef REALNY_HW
  const int RELE_ZAPNI = HIGH;
#else
  const int RELE_ZAPNI = LOW;
#endif

const float VCC_SENZOR = 3.3;
const float RL10  = 50.0;   // odpor fotorezistora pri 10 lux [kOhm]
const float GAMMA = 0.7;

WiFiClient espClient;
PubSubClient mqtt(espClient);
OneWire oneWire(PIN_TEPLOTA);
DallasTemperature teplomer(&oneWire);
bool releZapnute = false;

void pripojWiFi() {
  Serial.print("Pripajam na WiFi");
  WiFi.begin(WIFI_SSID, WIFI_PASS);
  while (WiFi.status() != WL_CONNECTED) {
    delay(300);
    Serial.print(".");
  }
  Serial.println(" OK, IP: " + WiFi.localIP().toString());
}

void nastavRele(bool zapnut) {
  releZapnute = zapnut;
  digitalWrite(PIN_RELAY, zapnut ? RELE_ZAPNI : !RELE_ZAPNI);
}

void mqttCallback(char* topic, byte* payload, unsigned int length) {
  String sprava;
  for (unsigned int i = 0; i < length; i++) sprava += (char)payload[i];
  Serial.printf("Prisla sprava [%s]: %s\n", topic, sprava.c_str());

  if (String(topic) == TOPIC_PRIKAZ) {
    if (sprava == "ON") nastavRele(true);
    else if (sprava == "OFF") nastavRele(false);
  }
}

void pripojMQTT() {
  while (!mqtt.connected()) {
    Serial.print("Pripajam na MQTT...");
    // Last Will: ak uzol vypadne, broker za neho pošle "offline"
    if (mqtt.connect(MQTT_CLIENT_ID, TOPIC_STAV, 1, true, "offline")) {
      Serial.println(" OK");
      mqtt.publish(TOPIC_STAV, "online", true);
      mqtt.subscribe(TOPIC_PRIKAZ);
    } else {
      Serial.printf(" zlyhalo (rc=%d)\n", mqtt.state());
      delay(2000);
    }
  }
}

// Vráti teplotu v stup. C, pri chybe senzora NAN
float citajTeplotu() {
  teplomer.requestTemperatures();
  float t = teplomer.getTempCByIndex(0);
  if (t == DEVICE_DISCONNECTED_C) return NAN;
  return t;
}

// Fotorezistor je zapojený proti GND, 10 kOhm rezistor ide na 3,3 V
float citajSvetelnost() {
  int adc = analogRead(PIN_PHOTO);
  float voltage = (adc / 4095.0) * VCC_SENZOR;
  float resistance = 10000.0 * voltage / (VCC_SENZOR - voltage);
  return pow(RL10 * 1000.0 * pow(10, GAMMA) / resistance, 1.0 / GAMMA);
}

unsigned long poslednaPublikacia = 0;
const unsigned long INTERVAL_MS = 5000;

void setup() {
  Serial.begin(115200);
  pinMode(PIN_RELAY, OUTPUT);
  nastavRele(false);

  teplomer.begin();
  Serial.printf("Pocet teplomerov DS18B20: %d\n", teplomer.getDeviceCount());

  pripojWiFi();
  mqtt.setServer(MQTT_BROKER, MQTT_PORT);
  mqtt.setCallback(mqttCallback);
}

void loop() {
  if (!mqtt.connected()) pripojMQTT();
  mqtt.loop();

  unsigned long teraz = millis();
  if (teraz - poslednaPublikacia >= INTERVAL_MS) {
    poslednaPublikacia = teraz;

    float teplota = citajTeplotu();
    float svetlo  = citajSvetelnost();
    char buf[16];

    if (isnan(teplota)) {
      Serial.println("DS18B20 neodpoveda - skontroluj zapojenie!");
    } else {
      dtostrf(teplota, 4, 1, buf);
      mqtt.publish(TOPIC_TEPLOTA, buf);
    }

    dtostrf(svetlo, 4, 1, buf);
    mqtt.publish(TOPIC_SVETLO, buf);

    if (releZapnute){
       Serial.printf("Teplota: %.1f C | Svetlo: %.1f lux | Rele: %s\n",
                  teplota, svetlo,"ZAPNUTE");
    } else {
      Serial.printf("Teplota: %.1f C | Svetlo: %.1f lux | Rele: %s\n",
                  teplota, svetlo,"VYPNUTE");
    }
  }
}