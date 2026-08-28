import { getMyNotifications, markNotificationAsRead } from "../api/notificationsApi.js";

const POLL_INTERVAL_MS = 30000;

function timeAgo(dateString) {
  const diffMin = Math.floor((Date.now() - new Date(dateString).getTime()) / 60000);
  if (diffMin < 1) return "indi";
  if (diffMin < 60) return `${diffMin} dəqiqə əvvəl`;
  const diffHour = Math.floor(diffMin / 60);
  if (diffHour < 24) return `${diffHour} saat əvvəl`;
  const diffDay = Math.floor(diffHour / 24);
  if (diffDay === 1) return "Dünən";
  return `${diffDay} gün əvvəl`;
}

function escapeHtml(str) {
  const div = document.createElement("div");
  div.textContent = str ?? "";
  return div.innerHTML;
}

export function initNotificationBell() {
  const btn = document.getElementById("notificationBtn");
  const panel = document.getElementById("notificationPanel");
  const badge = document.getElementById("notificationBadge");
  const list = document.getElementById("notificationList");
  const head = panel ? panel.querySelector(".notification-panel-head") : null;

  if (!btn || !panel || !badge || !list || !head) return;

  head.innerHTML = `<span>Bildirişlər</span><span class="notification-mark-all" id="notificationMarkAll">Hamısını oxunmuş et</span>`;
  const markAllBtn = document.getElementById("notificationMarkAll");

  let notifications = [];

  function render() {
    if (!notifications.length) {
      list.innerHTML = `<div class="notification-empty">Bildiriş yoxdur.</div>`;
      return;
    }

    list.innerHTML = notifications
      .map(
        (n) => `
        <div class="notification-item${n.isRead ? "" : " unread"}" data-id="${n.id}">
          <span class="notification-dot"></span>
          <div>
            <div class="notification-title">${escapeHtml(n.title)}</div>
            <div class="notification-message">${escapeHtml(n.message)}</div>
            <div class="notification-time">${timeAgo(n.createdAt)}</div>
          </div>
        </div>`
      )
      .join("");

    list.querySelectorAll(".notification-item.unread").forEach((el) => {
      el.addEventListener("click", async () => {
        const id = el.dataset.id;
        try {
          await markNotificationAsRead(id);
          const n = notifications.find((x) => x.id === id);
          if (n) n.isRead = true;
          render();
          updateBadge();
        } catch {
          // sakitcə keç — istifadəçi yenidən klikləyə bilər
        }
      });
    });
  }

  function updateBadge() {
    const unreadCount = notifications.filter((n) => !n.isRead).length;
    badge.textContent = unreadCount > 9 ? "9+" : String(unreadCount);
    badge.style.display = unreadCount > 0 ? "flex" : "none";
  }

  async function refresh() {
    try {
      notifications = await getMyNotifications();
      updateBadge();
      if (panel.classList.contains("open")) render();
    } catch {
      // sakitcə keç, növbəti poll-da yenidən cəhd olunacaq
    }
  }

  async function markAllRead() {
    const unread = notifications.filter((n) => !n.isRead);
    if (!unread.length) return;

    await Promise.allSettled(unread.map((n) => markNotificationAsRead(n.id)));
    notifications.forEach((n) => (n.isRead = true));
    render();
    updateBadge();
  }

  btn.addEventListener("click", (e) => {
    e.stopPropagation();
    panel.classList.toggle("open");
    if (panel.classList.contains("open")) render();
  });

  markAllBtn.addEventListener("click", (e) => {
    e.stopPropagation();
    markAllRead();
  });

  document.addEventListener("click", () => panel.classList.remove("open"));
  panel.addEventListener("click", (e) => e.stopPropagation());

  refresh();
  setInterval(refresh, POLL_INTERVAL_MS);
}