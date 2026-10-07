#include <WiFi.h>
#include <PubSubClient.h>

const char* WIFI_SSID = "Wokwi-GUEST";
const char* WIFI_PASS = "";

const char* MQTT_BROKER    = "test.mosquitto.org";
const int   MQTT_PORT      = 1883;
const char* MQTT_CLIENT_ID = "esp32-brana";

const char* TOPIC_RFID    = "dom/brana/rfid";
const char* TOPIC_ODPOVED = "dom/brana/rfid/odpoved";

// Simulované karty (pri reálnom HW nahradí RC522)
const int   PIN_KARTY[]  = {32, 33, 25};
const char* UID_KARTY[]  = {"A1B2C3D4", "11223344", "DEADBEEF"};
const int   POCET_KARIET = 3;

const int PIN_LED_OK  = 18;
const int PIN_LED_NOK = 19;
const int PIN_BUZZER  = 21;

const unsigned long TIMEOUT_MS = 5000;  // pri ručnom teste z MQTT Explorera daj 15000

WiFiClient espClient;
PubSubClient mqtt(espClient);

bool cakamNaOdpoved = false;
unsigned long casOdoslania = 0;
int predoslyStav[POCET_KARIET];

void pripojWiFi() {
  Serial.print("Pripajam na WiFi");
  WiFi.begin(WIFI_SSID, WIFI_PASS);
  while (WiFi.status() != WL_CONNECTED) { delay(300); Serial.print("."); }
  Serial.println(" OK");
}

void signalizuj(bool povolene) {
  int pin = povolene ? PIN_LED_OK : PIN_LED_NOK;
  digitalWrite(pin, HIGH);
  tone(PIN_BUZZER, povolene ? 2000 : 400, povolene ? 150 : 600);
  delay(1000);
  digitalWrite(pin, LOW);
}

void mqttCallback(char* topic, byte* payload, unsigned int length) {
  String sprava;
  for (unsigned int i = 0; i < length; i++) sprava += (char)payload[i];

  if (String(topic) != TOPIC_ODPOVED || !cakamNaOdpoved) return;

  unsigned long latencia = millis() - casOdoslania;
  cakamNaOdpoved = false;

  Serial.printf("Odpoved: %s | latencia: %lu ms\n", sprava.c_str(), latencia);
  signalizuj(sprava == "OK");
}

void pripojMQTT() {
  while (!mqtt.connected()) {
    Serial.print("Pripajam na MQTT...");
    if (mqtt.connect(MQTT_CLIENT_ID)) {
      Serial.println(" OK");
      mqtt.subscribe(TOPIC_ODPOVED);
    } else {
      Serial.printf(" zlyhalo (rc=%d)\n", mqtt.state());
      delay(2000);
    }
  }
}

// Jediná funkcia, ktorá sa zmení pri prechode na RC522
const char* precitajKartu() {
  for (int i = 0; i < POCET_KARIET; i++) {
    int stav = digitalRead(PIN_KARTY[i]);
    bool stlacene = (predoslyStav[i] == HIGH && stav == LOW);
    predoslyStav[i] = stav;
    if (stlacene) return UID_KARTY[i];
  }
  return nullptr;
}

void setup() {
  Serial.begin(115200);
  for (int i = 0; i < POCET_KARIET; i++) {
    pinMode(PIN_KARTY[i], INPUT_PULLUP);
    predoslyStav[i] = HIGH;
  }
  pinMode(PIN_LED_OK, OUTPUT);
  pinMode(PIN_LED_NOK, OUTPUT);

  pripojWiFi();
  mqtt.setServer(MQTT_BROKER, MQTT_PORT);
  mqtt.setCallback(mqttCallback);
}

void loop() {
  if (!mqtt.connected()) pripojMQTT();
  mqtt.loop();

  // Timeout — backend neodpovedal
  if (cakamNaOdpoved && millis() - casOdoslania > TIMEOUT_MS) {
    cakamNaOdpoved = false;
    Serial.println("Backend neodpovedal (timeout) -> pristup zamietnuty");
    signalizuj(false);
  }

  if (cakamNaOdpoved) return;  // počas čakania ignoruj ďalšie karty

  const char* uid = precitajKartu();
  if (uid != nullptr) {
    casOdoslania = millis();
    cakamNaOdpoved = true;
    mqtt.publish(TOPIC_RFID, uid);
    Serial.printf("Karta %s odoslana\n", uid);
  }
  delay(20);
}