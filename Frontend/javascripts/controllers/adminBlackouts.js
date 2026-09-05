import { requireAdmin } from "../components/adminGuard.js";
import { initAdminMobileNav } from "../components/mobileNav.js";
import { getAllRooms, getAllBuildings } from "../api/roomsApi.js";
import { logout } from "../api/authApi.js";
import { getAllRoomBlackouts, createRoomBlackout, updateRoomBlackout, deleteRoomBlackout } from "../api/roomBlackoutsApi.js";
import { showAlert, clearAlert } from "../components/alert.js";
import { initDropdown } from "../components/customDropdown.js";
import { escapeHtml } from "../utils/escapeHtml.js";

requireAdmin();
initAdminMobileNav();

const alertBox = document.getElementById("alertBox");
const logoutBtn = document.getElementById("logoutBtn");
const blackoutsList = document.getElementById("blackoutsList");
const addBlackoutBtn = document.getElementById("addBlackoutBtn");

const modal = document.getElementById("blackoutModal");
const modalTitle = document.getElementById("blackoutModalTitle");
const modalAlert = document.getElementById("blackoutModalAlert");
const modalClose = document.getElementById("blackoutModalClose");
const modalCancelBtn = document.getElementById("blackoutModalCancelBtn");
const form = document.getElementById("blackoutForm");
const dateInput = document.getElementById("blackoutDate");
const startInput = document.getElementById("blackoutStart");
const endInput = document.getElementById("blackoutEnd");
const reasonInput = document.getElementById("blackoutReason");
const submitBtn = document.getElementById("blackoutModalSubmitBtn");

const filterDate = document.getElementById("filterDate");
const clearFiltersBtn = document.getElementById("clearFiltersBtn");

const roomDropdown = initDropdown({
  btn: document.getElementById("blackoutRoomBtn"),
  panel: document.getElementById("blackoutRoomPanel"),
  options: [],
  placeholder: "Seçin",
  onChange: () => {},
});

const filterBuildingDropdown = initDropdown({
  btn: document.getElementById("filterBuildingBtn"),
  panel: document.getElementById("filterBuildingPanel"),
  options: [],
  placeholder: "Hamısı",
  onChange: (value) => {
    populateRoomFilterOptions(value);
    renderBlackouts();
  },
});

const filterRoomDropdown = initDropdown({
  btn: document.getElementById("filterRoomBtn"),
  panel: document.getElementById("filterRoomPanel"),
  options: [],
  placeholder: "Hamısı",
  onChange: () => renderBlackouts(),
});

let blackouts = [];
let rooms = [];
let buildings = [];
let allRows = [];
let editingId = null;
function toDateOnlyString(date) {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, "0");
  const d = String(date.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

const STATUS_LABELS = { ongoing: "Davam edir", upcoming: "Planlaşdırılıb", past: "Bitib" };
const STATUS_BADGE_CLASS = { ongoing: "badge-booked", upcoming: "badge-neutral", past: "badge-completed" };

function getBlackoutStatus(startIso, endIso) {
  const now = new Date();
  const start = new Date(startIso);
  const end = new Date(endIso);
  if (now < start) return "upcoming";
  if (now > end) return "past";
  return "ongoing";
}

function openModal(blackout) {
  editingId = blackout ? blackout.id : null;
  modalTitle.textContent = blackout ? "Bloklamanı redaktə et" : "Yeni bloklama";
  submitBtn.textContent = blackout ? "Yadda saxla" : "Yarat";

  if (blackout) {
    roomDropdown.setValue(blackout.roomId);
    roomDropdown.setDisabled(true);
    const start = new Date(blackout.startDate);
    const end = new Date(blackout.endDate);
    dateInput.value = toDateOnlyString(start);
    startInput.value = start.toTimeString().slice(0, 5);
    endInput.value = end.toTimeString().slice(0, 5);
    reasonInput.value = blackout.reason;
  } else {
    roomDropdown.setDisabled(false);
    roomDropdown.setValue(rooms[0]?.id || "");
    dateInput.value = toDateOnlyString(new Date());
    startInput.value = "09:00";
    endInput.value = "10:00";
    reasonInput.value = "";
  }

  modalAlert.innerHTML = "";
  modal.classList.add("open");
}

function closeModal() {
  modal.classList.remove("open");
}

modalClose.addEventListener("click", closeModal);
modalCancelBtn.addEventListener("click", closeModal);
modal.addEventListener("click", (e) => {
  if (e.target === modal) closeModal();
});

addBlackoutBtn.addEventListener("click", () => openModal(null));

form.addEventListener("submit", async (e) => {
  e.preventDefault();
  modalAlert.innerHTML = "";

  const date = dateInput.value;
  const start = startInput.value;
  const end = endInput.value;
  const reason = reasonInput.value.trim();

  if (!date || !start || !end || !reason) {
    showAlert(modalAlert, "Bütün sahələri doldurun.", "warning");
    return;
  }

  const startDate = new Date(`${date}T${start}`);
  const endDate = new Date(`${date}T${end}`);

  if (startDate >= endDate) {
    showAlert(modalAlert, "Bitmə vaxtı başlanğıcdan sonra olmalıdır.", "warning");
    return;
  }

  submitBtn.disabled = true;
  submitBtn.textContent = "Yadda saxlanılır...";

  try {
    if (editingId) {
      await updateRoomBlackout(editingId, {
        StartDate: startDate.toISOString(),
        EndDate: endDate.toISOString(),
        Reason: reason,
      });
    } else {
      await createRoomBlackout({
        RoomId: roomDropdown.getValue(),
        StartDate: startDate.toISOString(),
        EndDate: endDate.toISOString(),
        Reason: reason,
      });
    }
    closeModal();
    await loadData();
  } catch (err) {
    showAlert(modalAlert, err.message);
  } finally {
    submitBtn.disabled = false;
    submitBtn.textContent = editingId ? "Yadda saxla" : "Yarat";
  }
});

function createBlackoutCardHtml(b) {
  const status = getBlackoutStatus(b.startDate, b.endDate);
  const start = new Date(b.startDate);
  const end = new Date(b.endDate);
  const monthAbbr = start.toLocaleDateString("az-AZ", { month: "short" });
  const dayNum = start.toLocaleDateString("az-AZ", { day: "2-digit" });
  const timeRange = `${start.toTimeString().slice(0, 5)}–${end.toTimeString().slice(0, 5)}`;

  return `
    <div class="card blackout-card">
      <div class="blackout-date-block ${status}">
        <span class="blackout-date-month">${escapeHtml(monthAbbr)}</span>
        <span class="blackout-date-day">${dayNum}</span>
      </div>
      <div class="blackout-card-content">
        <div class="blackout-card-top">
          <div>
            <h4>${escapeHtml(b.roomName)}</h4>
            <p class="blackout-card-building">${escapeHtml(b.buildingName || "")}</p>
          </div>
          <span class="badge ${STATUS_BADGE_CLASS[status]}"><span class="badge-dot"></span>${STATUS_LABELS[status]}</span>
        </div>
        <div class="blackout-card-time"><i class="bi bi-clock"></i>${timeRange}</div>
        <span class="chip">${escapeHtml(b.reason)}</span>
        <div class="row-actions">
          <button class="icon-action-btn edit-btn" data-id="${b.id}" title="Redaktə et"><i class="bi bi-pencil"></i></button>
          <button class="icon-action-btn delete-btn" data-id="${b.id}" title="Sil"><i class="bi bi-trash"></i></button>
        </div>
      </div>
    </div>`;
}

function populateRoomFilterOptions(buildingName) {
  const filtered = buildingName ? rooms.filter((r) => r.buildingName === buildingName) : rooms;
  filterRoomDropdown.setOptions(filtered.map((r) => ({ value: r.id, label: r.name })), "");
}

function buildRows() {
  return blackouts.map((b) => {
    const room = rooms.find((r) => r.id === b.roomId);
    return { ...b, buildingName: room ? room.buildingName : "" };
  });
}

function applyFilters(rows) {
  return rows.filter((row) => {
    const buildingFilter = filterBuildingDropdown.getValue();
    const roomFilter = filterRoomDropdown.getValue();
    if (buildingFilter && row.buildingName !== buildingFilter) return false;
    if (roomFilter && row.roomId !== roomFilter) return false;
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

function renderBlackouts() {
  const rows = applyFilters(allRows);

  if (!rows.length) {
    blackoutsList.innerHTML = `<div class="empty-state"><i class="bi bi-calendar-x"></i>Bloklama tapılmadı.</div>`;
    return;
  }

  blackoutsList.innerHTML = rows.map(createBlackoutCardHtml).join("");

  blackoutsList.querySelectorAll(".edit-btn").forEach((btn) => {
    btn.addEventListener("click", () => {
      const b = allRows.find((x) => x.id === btn.dataset.id);
      if (b) openModal(b);
    });
  });

  blackoutsList.querySelectorAll(".delete-btn").forEach((btn) => {
    btn.addEventListener("click", async () => {
      if (!confirm("Bu bloklamanı silmək istədiyinizə əminsiniz?")) return;
      clearAlert(alertBox);
      try {
        await deleteRoomBlackout(btn.dataset.id);
        await loadData();
      } catch (err) {
        showAlert(alertBox, err.message);
      }
    });
  });
}

async function loadData() {
  clearAlert(alertBox);
  try {
    const [blackoutList, roomList, buildingList] = await Promise.all([getAllRoomBlackouts(), getAllRooms(), getAllBuildings()]);
    blackouts = blackoutList;
    rooms = roomList;
    buildings = buildingList;
    allRows = buildRows();

    roomDropdown.setOptions(rooms.map((r) => ({ value: r.id, label: `${r.name} — ${r.buildingName}` })));

    filterBuildingDropdown.setOptions(buildings.map((b) => ({ value: b.name, label: b.name })), "");
    populateRoomFilterOptions("");

    renderBlackouts();
  } catch (err) {
    showAlert(alertBox, err.message);
  }
}

filterDate.addEventListener("change", renderBlackouts);

clearFiltersBtn.addEventListener("click", () => {
  filterBuildingDropdown.setValue("");
  populateRoomFilterOptions("");
  filterRoomDropdown.setValue("");
  filterDate.value = "";
  renderBlackouts();
});

logoutBtn.addEventListener("click", async () => {
  await logout();
  window.location.href = "adminLogin.html";
});

loadData();