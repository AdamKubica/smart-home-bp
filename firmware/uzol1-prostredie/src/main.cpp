#include <WiFi.h>
#include <PubSubClient.h>

const char* WIFI_SSID = "Wokwi-GUEST";
const char* WIFI_PASS = "";

const char* MQTT_BROKER    = "broker.hivemq.com";
const int   MQTT_PORT      = 1883;
const char* MQTT_CLIENT_ID = "esp32-izba1";

const char* TOPIC_TEPLOTA = "dom/izba1/teplota";
const char* TOPIC_SVETLO  = "dom/izba1/svetlo";
const char* TOPIC_PRIKAZ  = "dom/izba1/svetlo/prikaz";

const int PIN_PHOTO = 34;
const int PIN_NTC   = 35;
const int PIN_RELAY = 26;

const float VCC_SENZOR = 3.3;
const float BETA  = 3950.0;
const float RL10  = 50.0;
const float GAMMA = 0.7;

WiFiClient espClient;
PubSubClient mqtt(espClient);
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
  digitalWrite(PIN_RELAY, zapnut ? LOW : HIGH);
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
    Serial.print("Pripajam na MQTT broker...");
    if (mqtt.connect(MQTT_CLIENT_ID)) {
      Serial.println(" OK");
      mqtt.subscribe(TOPIC_PRIKAZ);
    } else {
      Serial.printf(" zlyhalo, rc=%d, skusam znova o 2s\n", mqtt.state());
      delay(2000);
    }
  }
}

float citajTeplotu() {
  int adc = analogRead(PIN_NTC);
  return 1.0 / (log(1.0 / (4095.0 / adc - 1.0)) / BETA + 1.0 / 298.15) - 273.15;
}

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
    dtostrf(teplota, 4, 1, buf);
    mqtt.publish(TOPIC_TEPLOTA, buf);

    dtostrf(svetlo, 4, 1, buf);
    mqtt.publish(TOPIC_SVETLO, buf);

    Serial.printf("Teplota: %.1f C | Svetlo: %.1f lux | Rele: %s\n",
                  teplota, svetlo, releZapnute ? "ZAPNUTE" : "VYPNUTE");
  }
}