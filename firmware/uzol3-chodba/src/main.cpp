#include <WiFi.h>
#include <PubSubClient.h>

const char* WIFI_SSID = "Wokwi-GUEST";
const char* WIFI_PASS = "";

const char* MQTT_BROKER = "broker.hivemq.com";
const int   MQTT_PORT = 1883;
const char* MQTT_CLIENT_ID = "esp32-chodba";

const char* TOPIC_POHYB = "dom/chodba/pohyb";
const char* TOPIC_STAV = "dom/chodba/stav";

const int PIN_PIR = 27;
const int PIN_LED = 18;

WiFiClient espClient;
PubSubClient mqtt(espClient);

int poslednyStav = -1;

void pripojWiFi() {
  Serial.print("Pripajam na WiFi");
  WiFi.begin(WIFI_SSID, WIFI_PASS);
  while (WiFi.status() != WL_CONNECTED) { delay(300); Serial.print("."); }
  Serial.println(" OK");
}

void pripojMQTT() {
  while (!mqtt.connected()) {
    Serial.print("Pripajam na MQTT...");
    if (mqtt.connect(MQTT_CLIENT_ID, TOPIC_STAV, 1, true, "offline")) {
      Serial.println(" OK");
      mqtt.publish(TOPIC_STAV, "online", true);
      poslednyStav = -1;
    } else {
      Serial.printf(" zlyhalo (rc=%d)\n", mqtt.state());
      delay(2000);
    }
  }
}

void setup() {
  Serial.begin(115200);
  pinMode(PIN_PIR, INPUT);
  pinMode(PIN_LED, OUTPUT);

  pripojWiFi();
  mqtt.setServer(MQTT_BROKER, MQTT_PORT);
}

void loop() {
  if (!mqtt.connected()) pripojMQTT();
  mqtt.loop();

  int stav = digitalRead(PIN_PIR);
  digitalWrite(PIN_LED, stav);

  if (stav != poslednyStav) {
    poslednyStav = stav;
    mqtt.publish(TOPIC_POHYB, stav == HIGH ? "1" : "0");
    Serial.println(stav == HIGH ? "Pohyb detekovany" : "Bez pohybu");
  }

  delay(50);
}