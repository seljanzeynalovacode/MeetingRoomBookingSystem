import { requireAdmin, getDisplayIdentity } from "../components/adminGuard.js";
import { initAdminMobileNav, initAdminAccountMenu } from "../components/mobileNav.js";
import { getAllRooms, getAllBuildings } from "../api/roomsApi.js";
import { logout } from "../api/authApi.js";
import { getAllRoomBlackouts, createRoomBlackout, updateRoomBlackout, deleteRoomBlackout } from "../api/roomBlackoutsApi.js";
import { showAlert, clearAlert } from "../components/alert.js";
import { initDropdown } from "../components/customDropdown.js";
import { escapeHtml } from "../utils/escapeHtml.js";
import { initThemeToggle } from "../components/theme.js";
import { t, getLang } from "../components/i18n.js";

requireAdmin();
initAdminMobileNav();
initAdminAccountMenu();
initThemeToggle(document.getElementById("themeToggleBtn"));

{
  const { initials, name } = getDisplayIdentity();
  const adminAccountInitials = document.getElementById("adminAccountInitials");
  const adminAccountName = document.getElementById("adminAccountName");
  if (adminAccountInitials) adminAccountInitials.textContent = initials;
  if (adminAccountName) adminAccountName.textContent = name;
}

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
const startDateInput = document.getElementById("blackoutStartDate");
const startTimeInput = document.getElementById("blackoutStartTime");
const endDateInput = document.getElementById("blackoutEndDate");
const endTimeInput = document.getElementById("blackoutEndTime");
const reasonInput = document.getElementById("blackoutReason");
const submitBtn = document.getElementById("blackoutModalSubmitBtn");

const filterDate = document.getElementById("filterDate");
const clearFiltersBtn = document.getElementById("clearFiltersBtn");

function populateModalRoomOptions(buildingId) {
  if (!buildingId) {
    roomDropdown.setOptions([], "");
    roomDropdown.setPlaceholder(t("dashboard.select_building_first"));
    roomDropdown.setDisabled(true);
    return [];
  }
  roomDropdown.setDisabled(false);
  roomDropdown.setPlaceholder(t("common.select"));
  const filtered = rooms.filter((r) => r.buildingId === buildingId);
  roomDropdown.setOptions(
    filtered.map((r) => ({ value: r.id, label: r.name })),
    ""
  );
  return filtered;
}

const modalBuildingDropdown = initDropdown({
  btn: document.getElementById("blackoutBuildingBtn"),
  panel: document.getElementById("blackoutBuildingPanel"),
  options: [],
  placeholder: t("common.select"),
  onChange: (value) => populateModalRoomOptions(value),
});

const roomDropdown = initDropdown({
  btn: document.getElementById("blackoutRoomBtn"),
  panel: document.getElementById("blackoutRoomPanel"),
  options: [],
  placeholder: t("common.select"),
  onChange: () => {},
});

const filterBuildingDropdown = initDropdown({
  btn: document.getElementById("filterBuildingBtn"),
  panel: document.getElementById("filterBuildingPanel"),
  options: [],
  placeholder: t("common.all"),
  onChange: (value) => {
    populateRoomFilterOptions(value);
    renderBlackouts();
  },
});

const filterRoomDropdown = initDropdown({
  btn: document.getElementById("filterRoomBtn"),
  panel: document.getElementById("filterRoomPanel"),
  options: [],
  placeholder: t("common.all"),
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

const STATUS_LABELS = { ongoing: t("blackout.status.ongoing"), upcoming: t("blackout.status.upcoming"), past: t("blackout.status.past") };
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
  modalTitle.textContent = blackout ? t("adminblackouts.edit") : t("adminblackouts.add");
  submitBtn.textContent = blackout ? t("profile.save") : t("common.create");

  if (blackout) {
    const room = rooms.find((r) => r.id === blackout.roomId);
    modalBuildingDropdown.setValue(room ? room.buildingId : "");
    modalBuildingDropdown.setDisabled(true);
    populateModalRoomOptions(room ? room.buildingId : "");
    roomDropdown.setValue(blackout.roomId);
    roomDropdown.setDisabled(true);

    const start = new Date(blackout.startDate);
    const end = new Date(blackout.endDate);
    startDateInput.value = toDateOnlyString(start);
    startTimeInput.value = start.toTimeString().slice(0, 5);
    endDateInput.value = toDateOnlyString(end);
    endTimeInput.value = end.toTimeString().slice(0, 5);
    reasonInput.value = blackout.reason;
  } else {
    modalBuildingDropdown.setDisabled(false);
    modalBuildingDropdown.setValue("");
    populateModalRoomOptions("");

    const today = toDateOnlyString(new Date());
    startDateInput.value = today;
    endDateInput.value = today;
    startTimeInput.value = "09:00";
    endTimeInput.value = "10:00";
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

  const startDateVal = startDateInput.value;
  const startTimeVal = startTimeInput.value;
  const endDateVal = endDateInput.value;
  const endTimeVal = endTimeInput.value;
  const reason = reasonInput.value.trim();

  if (!startDateVal || !startTimeVal || !endDateVal || !endTimeVal || !reason || !roomDropdown.getValue()) {
    showAlert(modalAlert, t("adminblackouts.fill_all_fields"), "warning");
    return;
  }

  const startDate = new Date(`${startDateVal}T${startTimeVal}`);
  const endDate = new Date(`${endDateVal}T${endTimeVal}`);

  if (startDate >= endDate) {
    showAlert(modalAlert, t("adminblackouts.end_after_start"), "warning");
    return;
  }

  submitBtn.disabled = true;
  submitBtn.textContent = t("common.saving");

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
    submitBtn.textContent = editingId ? t("profile.save") : t("common.create");
  }
});

function createBlackoutCardHtml(b) {
  const status = getBlackoutStatus(b.startDate, b.endDate);
  const start = new Date(b.startDate);
  const end = new Date(b.endDate);
  const locale = getLang() === "en" ? "en-US" : "az-AZ";
  const monthAbbr = start.toLocaleDateString(locale, { month: "short" });
  const dayNum = start.toLocaleDateString(locale, { day: "2-digit" });
  const isMultiDay = toDateOnlyString(start) !== toDateOnlyString(end);
  const timeRange = isMultiDay
    ? `${start.toLocaleDateString(locale, { day: "2-digit", month: "short" })} ${start.toTimeString().slice(0, 5)} – ${end.toLocaleDateString(locale, { day: "2-digit", month: "short" })} ${end.toTimeString().slice(0, 5)}`
    : `${start.toTimeString().slice(0, 5)}–${end.toTimeString().slice(0, 5)}`;

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
          <button class="icon-action-btn edit-btn" data-id="${b.id}" title="${t("common.edit")}"><i class="bi bi-pencil"></i></button>
          <button class="icon-action-btn delete-btn" data-id="${b.id}" title="${t("common.delete")}"><i class="bi bi-trash"></i></button>
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
      const rowEnd = new Date(row.endDate);
      if (rowEnd < dayStart || rowStart > dayEnd) return false;
    }
    return true;
  });
}

function renderBlackouts() {
  const rows = applyFilters(allRows);

  if (!rows.length) {
    blackoutsList.innerHTML = `<div class="empty-state"><i class="bi bi-calendar-x"></i>${t("adminblackouts.no_blackouts")}</div>`;
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
      if (!confirm(t("adminblackouts.confirm_delete"))) return;
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

    modalBuildingDropdown.setOptions(buildings.map((b) => ({ value: b.id, label: b.name })), "");
    populateModalRoomOptions("");

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