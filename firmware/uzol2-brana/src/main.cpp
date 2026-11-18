#include <WiFi.h>
#include <PubSubClient.h>
#include <SPI.h>
#include <MFRC522.h>
#include "config.h"   // Wi-Fi a adresa brokera (firmware/spolocne)

const char* MQTT_CLIENT_ID = "esp32-brana";
const char* TOPIC_RFID    = "dom/brana/rfid";
const char* TOPIC_ODPOVED = "dom/brana/rfid/odpoved";
const char* TOPIC_STAV    = "dom/brana/stav";   // online/offline

// Čítačka RC522 na zbernici SPI: SCK = 18, MISO = 19, MOSI = 23
const int PIN_RFID_SS  = 5;
const int PIN_RFID_RST = 21;

const int PIN_LED_OK  = 32;
const int PIN_LED_NOK = 33;
const int PIN_BUZZER  = 25;

const unsigned long TIMEOUT_MS = 5000;

WiFiClient espClient;
PubSubClient mqtt(espClient);
MFRC522 rfid(PIN_RFID_SS, PIN_RFID_RST);

bool cakamNaOdpoved = false;
unsigned long casOdoslania = 0;
char uidText[21];

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
    // Last Will: ak uzol vypadne, broker za neho pošle "offline"
    if (mqtt.connect(MQTT_CLIENT_ID, TOPIC_STAV, 1, true, "offline")) {
      Serial.println(" OK");
      mqtt.publish(TOPIC_STAV, "online", true);
      mqtt.subscribe(TOPIC_ODPOVED);
    } else {
      Serial.printf(" zlyhalo (rc=%d)\n", mqtt.state());
      delay(2000);
    }
  }
}

// Kontrola zapojenia: čítačka musí vrátiť číslo verzie čipu
void skontrolujCitacku() {
  byte verzia = rfid.PCD_ReadRegister(MFRC522::VersionReg);
  if (verzia == 0x00 || verzia == 0xFF)
    Serial.println("RC522 neodpoveda - skontroluj zapojenie!");
  else
    Serial.printf("RC522 pripravena (verzia 0x%02X)\n", verzia);
}

// Vráti UID priloženej karty ako text, napr. "A1B2C3D4", inak nullptr
const char* precitajKartu() {
  if (!rfid.PICC_IsNewCardPresent() || !rfid.PICC_ReadCardSerial())
    return nullptr;

  for (byte i = 0; i < rfid.uid.size; i++)
    sprintf(&uidText[i * 2], "%02X", rfid.uid.uidByte[i]);
  uidText[rfid.uid.size * 2] = '\0';

  // Karta sa uspí, takže kým ju neodložíš, neprečíta sa znova
  rfid.PICC_HaltA();
  return uidText;
}

void setup() {
  Serial.begin(115200);
  pinMode(PIN_LED_OK, OUTPUT);
  pinMode(PIN_LED_NOK, OUTPUT);

  SPI.begin();
  rfid.PCD_Init();
  skontrolujCitacku();

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

  if (cakamNaOdpoved) return;

  const char* uid = precitajKartu();
  if (uid != nullptr) {
    casOdoslania = millis();
    cakamNaOdpoved = true;
    mqtt.publish(TOPIC_RFID, uid);
    Serial.printf("Karta %s odoslana\n", uid);
  }
  delay(20);
}