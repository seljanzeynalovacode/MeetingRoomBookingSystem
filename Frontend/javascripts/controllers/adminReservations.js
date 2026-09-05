import { requireAdmin } from "../components/adminGuard.js";
import { initAdminMobileNav } from "../components/mobileNav.js";
import { logout } from "../api/authApi.js";
import { getAccessToken, API_BASE_URL } from "../api/apiClient.js";
import { getAllRooms, getAllBuildings } from "../api/roomsApi.js";
import { getAllUsersWithRoles } from "../api/adminApi.js";
import { getAllReservationsAdmin, cancelReservation } from "../api/reservationsApi.js";
import { getParticipantsByReservation } from "../api/participantsApi.js";
import { showAlert, clearAlert } from "../components/alert.js";
import { initDropdown } from "../components/customDropdown.js";
import { escapeHtml } from "../utils/escapeHtml.js";

requireAdmin();
initAdminMobileNav();

const alertBox = document.getElementById("alertBox");
const logoutBtn = document.getElementById("logoutBtn");
const tableBody = document.getElementById("reservationsTableBody");

const filterUser = document.getElementById("filterUser");
const filterDate = document.getElementById("filterDate");
const clearFiltersBtn = document.getElementById("clearFiltersBtn");

const filterBuildingDropdown = initDropdown({
  btn: document.getElementById("filterBuildingBtn"),
  panel: document.getElementById("filterBuildingPanel"),
  options: [],
  placeholder: "Hamısı",
  portal: true,
  onChange: (value) => {
    populateRoomFilterOptions(value);
    renderTable();
  },
});

const filterRoomDropdown = initDropdown({
  btn: document.getElementById("filterRoomBtn"),
  panel: document.getElementById("filterRoomPanel"),
  options: [],
  placeholder: "Hamısı",
  portal: true,
  onChange: () => renderTable(),
});

const filterStatusDropdown = initDropdown({
  btn: document.getElementById("filterStatusBtn"),
  panel: document.getElementById("filterStatusPanel"),
  options: [
    { value: "", label: "Hamısı" },
    { value: "Confirmed", label: "Aktiv" },
    { value: "Completed", label: "Tamamlanıb" },
    { value: "Cancelled", label: "Ləğv edilib" },
  ],
  value: "",
  placeholder: "Hamısı",
  portal: true,
  onChange: () => renderTable(),
});

const detailsModal = document.getElementById("detailsModal");
const detailsModalBody = document.getElementById("detailsModalBody");
const detailsModalClose = document.getElementById("detailsModalClose");

let reservations = [];
let users = [];
let rooms = [];
let buildings = [];
let allRows = [];
function getEffectiveStatus(status, endDate) {
  if (status === "Cancelled") return "Cancelled";
  return new Date(endDate) < new Date() ? "Completed" : "Confirmed";
}

function statusBadgeHtml(status, endDate) {
  const effective = getEffectiveStatus(status, endDate);
  if (effective === "Cancelled") return `<span class="badge badge-booked"><span class="badge-dot"></span>Ləğv edilib</span>`;
  if (effective === "Completed") return `<span class="badge badge-completed"><span class="badge-dot"></span>Tamamlanıb</span>`;
  return `<span class="badge badge-available"><span class="badge-dot"></span>Aktiv</span>`;
}

function formatRange(startIso, endIso) {
  const start = new Date(startIso);
  const end = new Date(endIso);
  const dateStr = start.toLocaleDateString("az-AZ", { day: "2-digit", month: "short", year: "numeric" });
  return `${dateStr}, ${start.toTimeString().slice(0, 5)}–${end.toTimeString().slice(0, 5)}`;
}

function getInitials(fullName) {
  const parts = (fullName || "").trim().split(/\s+/).filter(Boolean);
  if (!parts.length) return "?";
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

function buildRows() {
  return reservations.map((r) => {
    const user = users.find((u) => u.id === r.reservedByUserId);
    const room = rooms.find((rm) => rm.id === r.roomId);
    return {
      id: r.id,
      title: r.title,
      startDate: r.startDate,
      endDate: r.endDate,
      status: r.status,
      roomId: r.roomId,
      roomName: r.roomName,
      buildingName: room ? room.buildingName : "",
      floorName: room ? room.floorName : "",
      fullName: user ? `${user.firstName} ${user.lastName}` : r.reservedByUserName,
      userName: user ? user.userName : "—",
      email: user ? user.email : "—",
    };
  });
}

function populateRoomFilterOptions(buildingName) {
  const filtered = buildingName ? rooms.filter((r) => r.buildingName === buildingName) : rooms;
  filterRoomDropdown.setOptions(filtered.map((r) => ({ value: r.id, label: r.name })), "");
}

function applyFilters(rows) {
  const userTerm = filterUser.value.trim().toLocaleLowerCase("az");
  const buildingFilter = filterBuildingDropdown.getValue();
  const roomFilter = filterRoomDropdown.getValue();
  const statusFilter = filterStatusDropdown.getValue();
  return rows.filter((row) => {
    if (userTerm) {
      const haystack = `${row.fullName} ${row.email} ${row.userName}`.toLocaleLowerCase("az");
      if (!haystack.includes(userTerm)) return false;
    }
    if (buildingFilter && row.buildingName !== buildingFilter) return false;
    if (roomFilter && row.roomId !== roomFilter) return false;
    if (statusFilter && getEffectiveStatus(row.status, row.endDate) !== statusFilter) return false;
    if (filterDate.value) {
      const dayStart = new Date(filterDate.value);
      dayStart.setHours(0, 0, 0, 0);
      const dayEnd = new Date(filterDate.value);
      dayEnd.setHours(23, 59, 59, 999);
      const rowStart = new Date(row.startDate);
      if (rowStart < dayStart || rowStart > dayEnd) return false;
    }
    return true;
  });
}

function renderTable() {
  const rows = applyFilters(allRows);

  if (!rows.length) {
    tableBody.innerHTML = `<div class="empty-state" style="padding:2rem;"><i class="bi bi-calendar-x"></i>Nəticə tapılmadı.</div>`;
    return;
  }

  tableBody.innerHTML = rows
    .map(
      (row) => `
    <div class="reservation-row">
      <div class="row-avatar">${getInitials(row.fullName)}</div>
      <div class="reservation-user">
        <p class="reservation-name">${escapeHtml(row.fullName)}</p>
        <p class="reservation-email">${escapeHtml(row.email)}</p>
      </div>
      <div class="reservation-room">
        <p><span class="reservation-room-label">Otaq:</span> ${escapeHtml(row.roomName)}</p>
        <p class="reservation-building"><span class="reservation-room-label">Bina:</span> ${escapeHtml(row.buildingName || "")}${row.floorName ? ` <span class="reservation-floor">(${escapeHtml(row.floorName)})</span>` : ""}</p>
      </div>
      <div class="reservation-time"><i class="bi bi-calendar3"></i>${formatRange(row.startDate, row.endDate)}</div>
      ${statusBadgeHtml(row.status, row.endDate)}
      <div class="row-actions">
        <button class="icon-action-btn view-btn" data-id="${row.id}" title="Ətraflı bax"><i class="bi bi-eye"></i></button>
        <button class="icon-action-btn cancel-btn" data-id="${row.id}" title="Ləğv et" ${getEffectiveStatus(row.status, row.endDate) !== "Confirmed" ? "disabled" : ""}><i class="bi bi-x-circle"></i></button>
      </div>
    </div>`
    )
    .join("");

  tableBody.querySelectorAll(".view-btn").forEach((btn) => btn.addEventListener("click", () => openDetails(btn.dataset.id)));
  tableBody.querySelectorAll(".cancel-btn").forEach((btn) => btn.addEventListener("click", () => handleCancel(btn.dataset.id)));
}

async function openDetails(id) {
  const row = allRows.find((r) => r.id === id);
  if (!row) return;

  detailsModalBody.innerHTML = `
    <div class="detail-row"><span class="dr-label">Başlıq</span><span class="dr-value">${escapeHtml(row.title)}</span></div>
    <div class="detail-row"><span class="dr-label">Otaq</span><span class="dr-value">${escapeHtml(row.roomName)}${row.buildingName ? " · " + escapeHtml(row.buildingName) : ""}</span></div>
    <div class="detail-row"><span class="dr-label">Tarix/Saat</span><span class="dr-value">${formatRange(row.startDate, row.endDate)}</span></div>
    <div class="detail-row"><span class="dr-label">Təşkilatçı</span><span class="dr-value">${escapeHtml(row.fullName)}</span></div>
    <div class="detail-row"><span class="dr-label">Email</span><span class="dr-value">${escapeHtml(row.email)}</span></div>
    <div class="detail-row"><span class="dr-label">İstifadəçi adı</span><span class="dr-value">${escapeHtml(row.userName)}</span></div>
    <div class="detail-row"><span class="dr-label">Status</span><span class="dr-value">${statusBadgeHtml(row.status, row.endDate)}</span></div>
    <div class="detail-participants">
      <h4>Qonaqlar</h4>
      <div id="detailsParticipants" class="text-muted" style="font-size:12.5px;">Yüklənir...</div>
    </div>
  `;
  detailsModal.classList.add("open");

  try {
    const participants = await getParticipantsByReservation(id);
    const box = document.getElementById("detailsParticipants");
    if (!participants.length) {
      box.textContent = "Qonaq yoxdur.";
    } else {
      box.innerHTML = participants
        .map(
          (p) =>
            `<div class="detail-row"><span class="dr-label">${escapeHtml(p.userName || p.guestName || p.guestEmail || "—")}</span><span class="dr-value">${escapeHtml(p.responseStatus)}</span></div>`
        )
        .join("");
    }
  } catch (err) {
    const box = document.getElementById("detailsParticipants");
    if (box) box.textContent = err?.message || "Yüklənə bilmədi.";
  }
}

detailsModalClose.addEventListener("click", () => detailsModal.classList.remove("open"));
detailsModal.addEventListener("click", (e) => {
  if (e.target === detailsModal) detailsModal.classList.remove("open");
});

async function handleCancel(id) {
  if (!confirm("Bu rezervasiyanı ləğv etmək istədiyinizə əminsiniz?")) return;
  clearAlert(alertBox);
  try {
    await cancelReservation(id);
    await loadData();
  } catch (err) {
    showAlert(alertBox, err.message);
  }
}

filterDate.addEventListener("change", renderTable);
filterUser.addEventListener("input", renderTable);

clearFiltersBtn.addEventListener("click", () => {
  filterUser.value = "";
  filterBuildingDropdown.setValue("");
  populateRoomFilterOptions("");
  filterRoomDropdown.setValue("");
  filterStatusDropdown.setValue("");
  filterDate.value = "";
  renderTable();
});

logoutBtn.addEventListener("click", async () => {
  await logout();
  window.location.href = "adminLogin.html";
});

async function loadData() {
  clearAlert(alertBox);
  try {
    const [res, userList, roomList, buildingList] = await Promise.all([
      getAllReservationsAdmin(),
      getAllUsersWithRoles(),
      getAllRooms(),
      getAllBuildings(),
    ]);
    reservations = res;
    users = userList;
    rooms = roomList;
    buildings = buildingList;
    allRows = buildRows();

    filterBuildingDropdown.setOptions(buildings.map((b) => ({ value: b.name, label: b.name })), "");
    populateRoomFilterOptions("");

    renderTable();
  } catch (err) {
    showAlert(alertBox, err.message);
  }
}

loadData();

const hubConnection = new signalR.HubConnectionBuilder()
  .withUrl(`${API_BASE_URL.replace("/api", "")}/hubs/room-calendar`, {
    accessTokenFactory: () => getAccessToken(),
  })
  .withAutomaticReconnect()
  .build();

hubConnection.on("ReservationsChanged", () => {
  loadData();
});

hubConnection.onreconnecting(() => {
  showAlert(alertBox, "Canlı yeniləmə bağlantısı kəsildi, yenidən qoşulmağa çalışılır...", "warning");
});

hubConnection.onreconnected(() => {
  clearAlert(alertBox);
});

hubConnection.onclose(() => {
  showAlert(alertBox, "Canlı yeniləmə bağlantısı kəsildi. Yeni dəyişiklikləri görmək üçün səhifəni yeniləyin.", "warning");
});

hubConnection.start().catch((err) => console.error("SignalR bağlantısı qurula bilmədi:", err));