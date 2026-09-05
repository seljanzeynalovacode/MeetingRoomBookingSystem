export function initMobileNav() {
  const toggle = document.getElementById("mobileNavToggle");
  const navLinks = document.querySelector(".nav-links");
  if (!toggle || !navLinks) return;

  toggle.addEventListener("click", (e) => {
    e.stopPropagation();
    navLinks.classList.toggle("open");
  });

  navLinks.querySelectorAll("a").forEach((link) => {
    link.addEventListener("click", () => navLinks.classList.remove("open"));
  });

  document.addEventListener("click", (e) => {
    if (!navLinks.contains(e.target) && e.target !== toggle) {
      navLinks.classList.remove("open");
    }
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
