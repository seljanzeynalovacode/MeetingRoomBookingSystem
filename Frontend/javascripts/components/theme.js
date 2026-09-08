const STORAGE_KEY = "mrb_theme";

export function getTheme() {
  try {
    return localStorage.getItem(STORAGE_KEY) || "light";
  } catch {
    return "light";
  }
}

export function applyTheme(theme) {
  document.documentElement.setAttribute("data-theme", theme);
  try {
    localStorage.setItem(STORAGE_KEY, theme);
  } catch {
    // localStorage yoxdursa sakitcə keç
  }
}

export function initThemeToggle(btn) {
  if (!btn) return;
  const icon = btn.querySelector("i");

  function sync() {
    const isDark = document.documentElement.getAttribute("data-theme") === "dark";
    icon.className = isDark ? "bi bi-sun" : "bi bi-moon-stars";
    btn.setAttribute("aria-label", isDark ? "İşıqlı rejimə keç" : "Tünd rejimə keç");
  }

  sync();
  btn.addEventListener("click", () => {
    const next = document.documentElement.getAttribute("data-theme") === "dark" ? "light" : "dark";
    applyTheme(next);
    sync();
  });
}
