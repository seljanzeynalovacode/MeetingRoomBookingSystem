import { az } from "../i18n/az.js";
import { en } from "../i18n/en.js";

const STORAGE_KEY = "mrb_lang";
const dictionaries = { az, en };

export function getLang() {
  try { return localStorage.getItem(STORAGE_KEY) || "az"; }
  catch { return "az"; }
}

export function t(key) {
  return dictionaries[getLang()][key] || dictionaries.az[key] || key;
}

function translatePage() {
  document.querySelectorAll("[data-i18n]").forEach((el) => {
    el.textContent = t(el.getAttribute("data-i18n"));
  });
  document.querySelectorAll("[data-i18n-placeholder]").forEach((el) => {
    el.setAttribute("placeholder", t(el.getAttribute("data-i18n-placeholder")));
  });
}

export function applyLang(lang) {
  document.documentElement.setAttribute("lang", lang);
  try { localStorage.setItem(STORAGE_KEY, lang); } catch {}
  translatePage();
}

export function initLangToggle(btn) {
  if (!btn) return;
  const sync = () => { btn.textContent = getLang() === "az" ? "EN" : "AZ"; };
  translatePage();
  sync();
  btn.addEventListener("click", () => {
    const next = getLang() === "az" ? "en" : "az";
    document.documentElement.setAttribute("lang", next);
    try { localStorage.setItem(STORAGE_KEY, next); } catch {}
    location.reload();
  });
}
