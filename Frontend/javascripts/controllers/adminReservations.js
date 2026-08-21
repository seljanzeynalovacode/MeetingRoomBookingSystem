import { isLoggedIn } from "../api/apiClient.js";
import { logout } from "../api/authApi.js";
import { getAllRooms, getAllBuildings } from "../api/roomsApi.js";
import { getAllUsersWithRoles } from "../api/adminApi.js";
import { getAllReservationsAdmin, cancelReservation } from "../api/reservationsApi.js";
import { getParticipantsByReservation } from "../api/participantsApi.js";
import { showAlert, clearAlert } from "../components/alert.js";

if (!isLoggedIn()) {
  window.location.href = "login.html";
}

const alertBox = document.getElementById("alertBox");
const logoutBtn = document.getElementById("logoutBtn");
const tableBody = document.getElementById("reservationsTableBody");

const filterBuilding = document.getElementById("filterBuilding");
const filterRoom = document.getElementById("filterRoom");
const filterStatus = document.getElementById("filterStatus");
const filterFrom = document.getElementById("filterFrom");
const filterTo = document.getElementById("filterTo");
const clearFiltersBtn = document.getElementById("clearFiltersBtn");

const detailsModal = document.getElementById("detailsModal");
const detailsModalBody = document.getElementById("detailsModalBody");
const detailsModalClose = document.getElementById("detailsModalClose");

let reservations = [];
let users = [];
let rooms = [];
let buildings = [];
let allRows = [];

function escapeHtml(str) {
  const div = document.createElement("div");
  div.textContent = str ?? "";
  return div.innerHTML;
}

function statusBadgeHtml(status) {
  if (status === "Cancelled") return `<span class="badge badge-booked"><span class="badge-dot"></span>Ləğv edilib</span>`;
  return `<span class="badge badge-available"><span class="badge-dot"></span>Aktiv</span>`;
}

function formatRange(startIso, endIso) {
  const start = new Date(startIso);
  const end = new Date(endIso);
  const dateStr = start.toLocaleDateString("az-AZ", { day: "2-digit", month: "short", year: "numeric" });
  return `${dateStr}, ${start.toTimeString().slice(0, 5)}–${end.toTimeString().slice(0, 5)}`;
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
      fullName: user ? `${user.firstName} ${user.lastName}` : r.reservedByUserName,
      userName: user ? user.userName : "—",
      email: user ? user.email : "—",
    };
  });
}

function populateRoomFilterOptions(buildingName) {
  const filtered = buildingName ? rooms.filter((r) => r.buildingName === buildingName) : rooms;
  filterRoom.innerHTML =
    '<option value="">Hamısı</option>' + filtered.map((r) => `<option value="${r.id}">${escapeHtml(r.name)}</option>`).join("");
}

function applyFilters(rows) {
  return rows.filter((row) => {
    if (filterBuilding.value && row.buildingName !== filterBuilding.value) return false;
    if (filterRoom.value && row.roomId !== filterRoom.value) return false;
    if (filterStatus.value && row.status !== filterStatus.value) return false;
    if (filterFrom.value && new Date(row.startDate) < new Date(filterFrom.value)) return false;
    if (filterTo.value) {
      const toDate = new Date(filterTo.value);
      toDate.setHours(23, 59, 59, 999);
      if (new Date(row.startDate) > toDate) return false;
    }
    return true;
  });
}

function renderTable() {
  const rows = applyFilters(allRows);

  if (!rows.length) {
    tableBody.innerHTML = `<tr><td colspan="7" class="text-muted" style="text-align:center; padding:2rem;">Nəticə tapılmadı.</td></tr>`;
    return;
  }

  tableBody.innerHTML = rows
    .map(
      (row) => `
    <tr>
      <td>${escapeHtml(row.fullName)}</td>
      <td>${escapeHtml(row.userName)}</td>
      <td>${escapeHtml(row.email)}</td>
      <td>${escapeHtml(row.roomName)}</td>
      <td>${formatRange(row.startDate, row.endDate)}</td>
      <td>${statusBadgeHtml(row.status)}</td>
      <td>
        <div class="row-actions">
          <button class="btn btn-secondary btn-sm view-btn" data-id="${row.id}" title="Ətraflı bax"><i class="bi bi-eye"></i></button>
          ${row.status === "Confirmed" ? `<button class="btn btn-outline btn-sm cancel-btn" data-id="${row.id}" title="Ləğv et"><i class="bi bi-x-circle"></i></button>` : ""}
        </div>
      </td>
    </tr>`
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
    <div class="detail-row"><span class="dr-label">Status</span><span class="dr-value">${statusBadgeHtml(row.status)}</span></div>
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
  } catch {
    const box = document.getElementById("detailsParticipants");
    if (box) box.textContent = "Yüklənə bilmədi.";
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

filterBuilding.addEventListener("change", () => {
  populateRoomFilterOptions(filterBuilding.value);
  renderTable();
});
[filterRoom, filterStatus, filterFrom, filterTo].forEach((el) => el.addEventListener("change", renderTable));

clearFiltersBtn.addEventListener("click", () => {
  filterBuilding.value = "";
  populateRoomFilterOptions("");
  filterRoom.value = "";
  filterStatus.value = "";
  filterFrom.value = "";
  filterTo.value = "";
  renderTable();
});

logoutBtn.addEventListener("click", async () => {
  await logout();
  window.location.href = "login.html";
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

    filterBuilding.innerHTML =
      '<option value="">Hamısı</option>' + buildings.map((b) => `<option value="${escapeHtml(b.name)}">${escapeHtml(b.name)}</option>`).join("");
    populateRoomFilterOptions("");

    renderTable();
  } catch (err) {
    showAlert(alertBox, err.message);
  }
}

loadData();