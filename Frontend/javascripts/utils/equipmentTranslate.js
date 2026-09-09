import { getLang } from "../components/i18n.js";

const KNOWN_EQUIPMENT = [
  { az: "Proyektor", en: "Projector" },
  { az: "Video kamera", en: "Video camera" },
  { az: "Kamera", en: "Camera" },
  { az: "Mikrofon", en: "Microphone" },
  { az: "Ağ lövhə", en: "Whiteboard" },
  { az: "Televizor", en: "TV" },
  { az: "Monitor", en: "Monitor" },
  { az: "Kondisioner", en: "Air conditioner" },
  { az: "Telefon", en: "Phone" },
  { az: "Printer", en: "Printer" },
  { az: "Skaner", en: "Scanner" },
  { az: "Səsucaldıcı", en: "Speaker" },
  { az: "Flipchart", en: "Flip chart" },
  { az: "Ekran", en: "Screen" },
  { az: "Noutbuk", en: "Laptop" },
  { az: "Konfrans telefonu", en: "Conference phone" },
  { az: "Kabel", en: "Cable" },
  { az: "HDMI kabeli", en: "HDMI cable" },
  { az: "Adapter", en: "Adapter" },
  { az: "Şəbəkə kabeli", en: "Network cable" },
  { az: "Proyeksiya ekranı", en: "Projection screen" },
  { az: "Router", en: "Router" },
  { az: "Şarj cihazı", en: "Charger" },
  { az: "Wi-Fi", en: "Wi-Fi" },
];

const LOOKUP = new Map();
for (const pair of KNOWN_EQUIPMENT) {
  LOOKUP.set(pair.az.toLocaleLowerCase("az"), pair);
  LOOKUP.set(pair.en.toLowerCase(), pair);
}

export function translateEquipmentName(name) {
  if (!name) return name;
  const match = LOOKUP.get(name.trim().toLocaleLowerCase("az")) || LOOKUP.get(name.trim().toLowerCase());
  if (!match) return name;
  return getLang() === "en" ? match.en : match.az;
}
