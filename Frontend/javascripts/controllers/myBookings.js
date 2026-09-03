import { isLoggedIn, getAccessToken } from "../api/apiClient.js";
import { getMyReservations, cancelReservation, updateReservation } from "../api/reservationsApi.js";
import { logout } from "../api/authApi.js";
import { openBookingModal } from "../components/bookingModal.js";
import { showAlert, clearAlert } from "../components/alert.js";
import { initNotificationBell } from "../components/notificationBell.js";
import { initChatWidget, clearChatState } from "../components/chatWidget.js";
import { initDropdown } from "../components/customDropdown.js";

if (!isLoggedIn()) {
  window.location.href = "login.html";
  throw new Error("Not authenticated");
}

initNotificationBell();
initChatWidget();

const alertBox = document.getElementById("alertBox");
const toastEl = document.getElementById("toast");
const listEl = document.getElementById("reservationList");

const filterDate = document.getElementById("filterDate");
const clearFiltersBtn = document.getElementById("clearFiltersBtn");
const statConfirmed = document.getElementById("statConfirmed");
const statCompleted = document.getElementById("statCompleted");
const statCancelled = document.getElementById("statCancelled");

const filterStatusDropdown = initDropdown({
  btn: document.getElementById("filterStatusBtn"),
  panel: document.getElementById("filterStatusPanel"),
  options: [
    { value: "", label: "Hamısı" },
    { value: "Confirmed", label: "Təsdiqlənib" },
    { value: "Completed", label: "Tamamlanıb" },
    { value: "Cancelled", label: "Ləğv edilib" },
  ],
  value: "",
  placeholder: "Hamısı",
  onChange: () => renderReservations(),
});

const avatarChip = document.getElementById("avatarChip");
const avatarMenu = document.getElementById("avatarMenu");
const avatarInitials = document.getElementById("avatarInitials");
const avatarName = document.getElementById("avatarName");
const logoutBtn = document.getElementById("logoutBtn");

let reservations = [];

function escapeHtml(str) {
  const div = document.createElement("div");
  div.textContent = str ?? "";
  return div.innerHTML;
}

function parseJwt(token) {
  try {
    const payload = token.split(".")[1];
    const json = decodeURIComponent(
      atob(payload.replace(/-/g, "+").replace(/_/g, "/"))
        .split("")
        .map((c) => "%" + c.charCodeAt(0).toString(16).padStart(2, "0"))
        .join("")
    );
    return JSON.parse(json);
  } catch {
    return null;
  }
}

function getDisplayIdentity() {
  const token = getAccessToken();
  if (!token) return { initials: "--", name: "İstifadəçi" };

  const claims = parseJwt(token) || {};
  const givenName = claims["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname"] || "";
  const surname = claims["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname"] || "";
  const email = claims["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] || claims["email"] || "";

  const name = givenName ? `${givenName} ${surname}`.trim() : email ? email.split("@")[0] : "İstifadəçi";
  const initials = givenName && surname ? (givenName[0] + surname[0]).toUpperCase() : name.slice(0, 2).toUpperCase();

  return { initials, name };
}

function getRoles() {
  const token = getAccessToken();
  if (!token) return [];
  const claims = parseJwt(token) || {};
  const roleClaim = claims["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
  if (!roleClaim) return [];
  return Array.isArray(roleClaim) ? roleClaim : [roleClaim];
}

let toastTimer = null;
function showToast(message) {
  toastEl.innerHTML = `<div class="alert alert-success" style="margin:0;"><i class="bi bi-check-circle"></i><span>${escapeHtml(message)}</span></div>`;
  toastEl.classList.add("show");
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => toastEl.classList.remove("show"), 2600);
}

function formatRange(startIso, endIso) {
  const start = new Date(startIso);
  const end = new Date(endIso);
  const dateStr = start.toLocaleDateString("az-AZ", { day: "2-digit", month: "short", year: "numeric" });
  const startTime = start.toTimeString().slice(0, 5);
  const endTime = end.toTimeString().slice(0, 5);
  return `${dateStr}, ${startTime}–${endTime}`;
}

function getEffectiveStatus(status, endDate) {
  if (status === "Cancelled") return "Cancelled";
  return new Date(endDate) < new Date() ? "Completed" : "Confirmed";
}

function isOngoing(r) {
  const now = new Date();
  return now >= new Date(r.startDate) && now < new Date(r.endDate);
}

function isCurrentlyOngoing(r) {
  return getEffectiveStatus(r.status, r.endDate) === "Confirmed" && isOngoing(r);
}

function statusBadgeHtml(r) {
  const effective = getEffectiveStatus(r.status, r.endDate);
  if (effective === "Cancelled") {
    return `<span class="badge badge-booked"><span class="badge-dot"></span>Ləğv edilib</span>`;
  }
  if (effective === "Completed") {
    return `<span class="badge badge-completed"><span class="badge-dot"></span>Tamamlanıb</span>`;
  }
  if (isCurrentlyOngoing(r)) {
    return `<span class="badge badge-live"><span class="badge-dot"></span>Davam edir</span>`;
  }
  return `<span class="badge badge-available"><span class="badge-dot"></span>Təsdiqlənib</span>`;
}

function dateBlockHtml(startIso) {
  const start = new Date(startIso);
  const month = start.toLocaleDateString("az-AZ", { month: "short" });
  const day = start.toLocaleDateString("az-AZ", { day: "2-digit" });
  return `<div class="mb2-date"><span class="mb2-date-month">${escapeHtml(month)}</span><span class="mb2-date-day">${day}</span></div>`;
}

function createReservationCardHtml(r) {
  const effectiveStatus = getEffectiveStatus(r.status, r.endDate);
  const ongoing = isCurrentlyOngoing(r);
  const cardClass = ongoing
    ? "is-live"
    : effectiveStatus === "Completed"
      ? "is-completed"
      : effectiveStatus === "Cancelled"
        ? "is-cancelled"
        : "";

  const actionsHtml =
    effectiveStatus === "Confirmed"
      ? `<div class="mb2-actions">
           <button class="btn btn-secondary btn-sm edit-btn" data-id="${r.id}"><i class="bi bi-pencil"></i>Redaktə et</button>
           ${ongoing ? `<button class="btn btn-outline btn-sm end-now-btn" data-id="${r.id}"><i class="bi bi-stop-circle"></i>İndi bitir</button>` : ""}
           <button class="btn btn-outline btn-sm cancel-btn" data-id="${r.id}"><i class="bi bi-x-circle"></i>Ləğv et</button>
         </div>`
      : `<div class="mb2-quiet-actions"></div>`;

  return `
    <div class="mb2-card ${cardClass}">
      ${dateBlockHtml(r.startDate)}
      <div class="mb2-main">
        <div class="mb2-main-top">
          <h4 class="mb2-title">${escapeHtml(r.title)}</h4>
          ${statusBadgeHtml(r)}
        </div>
        <div class="mb2-meta">
          <span><i class="bi bi-door-open"></i>${r.buildingName ? `${escapeHtml(r.buildingName)} · ` : ""}<span class="mb2-room-label">${escapeHtml(r.roomName)}</span></span>
          <span><i class="bi bi-clock"></i>${formatRange(r.startDate, r.endDate)}</span>
        </div>
      </div>
      ${actionsHtml}
    </div>`;
}

function applyFilters(rows) {
  return rows.filter((r) => {
    const statusFilter = filterStatusDropdown.getValue();
    if (statusFilter && getEffectiveStatus(r.status, r.endDate) !== statusFilter) return false;
    if (filterDate.value) {
      const dayStart = new Date(filterDate.value);
      dayStart.setHours(0, 0, 0, 0);
      const dayEnd = new Date(filterDate.value);
      dayEnd.setHours(23, 59, 59, 999);
      const rowStart = new Date(r.startDate);
      if (rowStart < dayStart || rowStart > dayEnd) return false;
    }
    return true;
  });
}

function sectionHtml(titleHtml, rows, extraClass = "") {
  if (!rows.length) return "";
  return `
    <div class="mb2-section">
      <div class="mb2-section-title${extraClass ? ` ${extraClass}` : ""}">${titleHtml}</div>
      <div class="mb2-list">${rows.map(createReservationCardHtml).join("")}</div>
    </div>`;
}

function renderReservations() {
  if (!reservations.length) {
    listEl.innerHTML = `<div class="empty-state"><i class="bi bi-calendar-x"></i>Hələ rezervasiyanız yoxdur.</div>`;
    return;
  }

  const filtered = applyFilters(reservations);

  if (!filtered.length) {
    listEl.innerHTML = `<div class="empty-state"><i class="bi bi-filter"></i>Filtrə uyğun rezervasiya tapılmadı.</div>`;
    return;
  }

  const ongoing = filtered.filter(isCurrentlyOngoing);
  const upcoming = filtered
    .filter((r) => !isCurrentlyOngoing(r) && getEffectiveStatus(r.status, r.endDate) === "Confirmed")
    .sort((a, b) => new Date(a.startDate) - new Date(b.startDate));
  const past = filtered
    .filter((r) => !isCurrentlyOngoing(r) && getEffectiveStatus(r.status, r.endDate) !== "Confirmed")
    .sort((a, b) => new Date(b.startDate) - new Date(a.startDate));

  listEl.innerHTML =
    sectionHtml(`<i class="bi bi-broadcast"></i>Hazırda davam edir`, ongoing, "is-live") +
    sectionHtml(`<i class="bi bi-calendar-event"></i>Gələcək rezervasiyalar`, upcoming) +
    sectionHtml(`<i class="bi bi-clock-history"></i>Keçmiş`, past);
}

function renderStats() {
  const counts = { Confirmed: 0, Completed: 0, Cancelled: 0 };
  reservations.forEach((r) => {
    const status = getEffectiveStatus(r.status, r.endDate);
    counts[status]++;
  });
  statConfirmed.textContent = counts.Confirmed;
  statCompleted.textContent = counts.Completed;
  statCancelled.textContent = counts.Cancelled;
}

async function loadReservations() {
  try {
    reservations = await getMyReservations();
    renderReservations();
    renderStats();
  } catch (err) {
    showAlert(alertBox, err.message);
  }
}

listEl.addEventListener("click", (e) => {
  const editBtn = e.target.closest(".edit-btn");
  const cancelBtn = e.target.closest(".cancel-btn");
  const endNowBtn = e.target.closest(".end-now-btn");

  if (editBtn) {
    const r = reservations.find((x) => x.id === editBtn.dataset.id);
    if (!r) return;
    openBookingModal({
      mode: "edit",
      isAdmin: getRoles().includes("Admin"),
      reservation: r,
      onSuccess: () => {
        showToast("Rezervasiya yeniləndi.");
        loadReservations();
      },
    });
  }

  if (cancelBtn) {
    if (!confirm("Bu rezervasiyanı ləğv etmək istədiyinizə əminsiniz?")) return;
    clearAlert(alertBox);
    cancelReservation(cancelBtn.dataset.id)
      .then(() => {
        showToast("Rezervasiya ləğv edildi.");
        loadReservations();
      })
      .catch((err) => showAlert(alertBox, err.message));
  }

  if (endNowBtn) {
    const r = reservations.find((x) => x.id === endNowBtn.dataset.id);
    if (!r) return;
    if (!confirm("İclası indi bitirmək istədiyinizə əminsiniz? Otaq dərhal boşalacaq.")) return;
    clearAlert(alertBox);
    updateReservation(r.id, {
      Title: r.title,
      StartDate: r.startDate,
      EndDate: new Date().toISOString(),
      RoomId: r.roomId,
    })
      .then(() => {
        showToast("Rezervasiya bağlandı, otaq boşaldı.");
        loadReservations();
      })
      .catch((err) => showAlert(alertBox, err.message));
  }
});

avatarChip.addEventListener("click", (e) => {
  e.stopPropagation();
  avatarMenu.classList.toggle("open");
});
document.addEventListener("click", () => avatarMenu.classList.remove("open"));

logoutBtn.addEventListener("click", async () => {
  await logout();
  clearChatState();
  window.location.href = "login.html";
});

const { initials, name } = getDisplayIdentity();
avatarInitials.textContent = initials;
avatarName.textContent = name;

if (getRoles().includes("Admin")) {
  document.getElementById("adminPanelLink").style.display = "";
}

filterDate.addEventListener("change", renderReservations);
clearFiltersBtn.addEventListener("click", () => {
  filterStatusDropdown.setValue("");
  filterDate.value = "";
  renderReservations();
});

loadReservations();