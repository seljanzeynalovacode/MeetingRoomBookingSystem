// Mobil alt-menyu davranışı (hamburger əvəzinə bottom tab bar):
//  - "Asistan" tabı mövcud chat widget-ini açır (#chatWidgetFab-i click edir)
//  - Otaqlar/Rezervlər filtr kartı mobildə yığcamdır (başlığa vurub açılır)
export function initMobileNav() {
  const bnAssistant = document.getElementById("bnAssistant");
  if (bnAssistant) {
    bnAssistant.addEventListener("click", () => {
      const fab = document.getElementById("chatWidgetFab");
      if (fab) fab.click();
    });
  }

  document
    .querySelectorAll(".dash-layout .filter-card, .mybookings-layout .filter-card, .calendar-layout .filter-card")
    .forEach((card) => {
      const head = card.querySelector(".filter-card-head");
      if (!head) return;
      head.classList.add("is-collapsible");
      head.addEventListener("click", (e) => {
        if (e.target.closest("button")) return; // reset/clear düymələrinə toxunma
        card.classList.toggle("is-open");
      });
    });
}

export function initAdminMobileNav() {
  const toggle = document.getElementById("adminNavToggle");
  const sidebar = document.querySelector(".admin-sidebar");
  if (!toggle || !sidebar) return;

  toggle.addEventListener("click", (e) => {
    e.stopPropagation();
    sidebar.classList.toggle("open");
  });

  sidebar.querySelectorAll(".admin-nav a, .admin-sidebar-footer a, .admin-sidebar-footer button").forEach((el) => {
    el.addEventListener("click", () => sidebar.classList.remove("open"));
  });

  document.addEventListener("click", (e) => {
    if (!sidebar.contains(e.target)) {
      sidebar.classList.remove("open");
    }
  });
}

export function initAdminAccountMenu() {
  const trigger = document.getElementById("adminAccountTrigger");
  const menu = document.getElementById("adminAccountMenu");
  if (!trigger || !menu) return;

  trigger.addEventListener("click", (e) => {
    e.stopPropagation();
    menu.classList.toggle("open");
  });
  document.addEventListener("click", () => menu.classList.remove("open"));
}
