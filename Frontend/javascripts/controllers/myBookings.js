import { isLoggedIn, getAccessToken } from "../api/apiClient.js";
import { getMyReservations, cancelReservation, updateReservation } from "../api/reservationsApi.js";
import { logout } from "../api/authApi.js";
import { openBookingModal } from "../components/bookingModal.js";
import { showAlert, clearAlert } from "../components/alert.js";

if (!isLoggedIn()) {
  window.location.href = "login.html";
  throw new Error("Not authenticated");
}

const alertBox = document.getElementById("alertBox");
const toastEl = document.getElementById("toast");
const listEl = document.getElementById("reservationList");

const filterStatus = document.getElementById("filterStatus");
const filterFrom = document.getElementById("filterFrom");
const filterTo = document.getElementById("filterTo");
const clearFiltersBtn = document.getElementById("clearFiltersBtn");

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

function createReservationCardHtml(r) {
  const effectiveStatus = getEffectiveStatus(r.status, r.endDate);
  const ongoing = isCurrentlyOngoing(r);
  const actions =
    effectiveStatus === "Confirmed"
      ? `<button class="btn btn-secondary btn-sm edit-btn" data-id="${r.id}">Redaktə et</button>
         ${ongoing ? `<button class="btn btn-outline btn-sm end-now-btn" data-id="${r.id}">İndi bitir</button>` : ""}
         <button class="btn btn-outline btn-sm cancel-btn" data-id="${r.id}">Ləğv et</button>`
      : "";

  return `
    <div class="card reservation-card${ongoing ? " reservation-card-ongoing" : ""}">
      <div class="reservation-card-body">
        <div class="reservation-card-head">
          <h4>${escapeHtml(r.title)}</h4>
          ${statusBadgeHtml(r)}
        </div>
        <div class="reservation-card-meta">
          <span><i class="bi bi-door-open"></i>${escapeHtml(r.roomName)}</span>
          <span><i class="bi bi-clock"></i>${formatRange(r.startDate, r.endDate)}</span>
        </div>
        <div class="reservation-card-actions">${actions}</div>
      </div>
    </div>`;
}

function applyFilters(rows) {
  return rows.filter((r) => {
    if (filterStatus.value && getEffectiveStatus(r.status, r.endDate) !== filterStatus.value) return false;
    if (filterFrom.value && new Date(r.startDate) < new Date(filterFrom.value)) return false;
    if (filterTo.value) {
      const toDate = new Date(filterTo.value);
      toDate.setHours(23, 59, 59, 999);
      if (new Date(r.startDate) > toDate) return false;
    }
    return true;
  });
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
  const rest = filtered.filter((r) => !isCurrentlyOngoing(r));

  const ongoingHtml = ongoing.length
    ? `<div class="ongoing-section">
         <h3 class="ongoing-section-title"><i class="bi bi-broadcast"></i>Hazırda davam edir</h3>
         <div class="ongoing-section-grid">${ongoing.map(createReservationCardHtml).join("")}</div>
       </div>`
    : "";

  listEl.innerHTML = ongoingHtml + rest.map(createReservationCardHtml).join("");
}

async function loadReservations() {
  try {
    reservations = await getMyReservations();
    renderReservations();
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
  window.location.href = "login.html";
});

const { initials, name } = getDisplayIdentity();
avatarInitials.textContent = initials;
avatarName.textContent = name;

if (getRoles().includes("Admin")) {
  document.getElementById("adminPanelLink").style.display = "";
}

[filterStatus, filterFrom, filterTo].forEach((el) => el.addEventListener("change", renderReservations));
clearFiltersBtn.addEventListener("click", () => {
  filterStatus.value = "";
  filterFrom.value = "";
  filterTo.value = "";
  renderReservations();
});

loadReservations();