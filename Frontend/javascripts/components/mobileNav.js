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
