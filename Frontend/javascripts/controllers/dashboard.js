import { isLoggedIn, getAccessToken } from "../api/apiClient.js";
import { getAllRooms, searchAvailableRooms, getAllBuildings, getAllFloors, getAllEquipment } from "../api/roomsApi.js";
import { getAllRoomEquipment } from "../api/roomEquipmentApi.js";
import { logout } from "../api/authApi.js";
import { openBookingModal } from "../components/bookingModal.js";
import { showAlert, clearAlert } from "../components/alert.js";
import { initNotificationBell } from "../components/notificationBell.js";
import { initChatWidget, clearChatState } from "../components/chatWidget.js";
import { initDropdown } from "../components/customDropdown.js";
import { initMobileNav } from "../components/mobileNav.js";
import { initThemeToggle } from "../components/theme.js";
import { escapeHtml } from "../utils/escapeHtml.js";
import { t } from "../components/i18n.js";
import { floorLabel } from "../utils/floorLabel.js";
import { translateEquipmentName } from "../utils/equipmentTranslate.js";

if (!isLoggedIn()) {
  window.location.href = "landing.html";
  throw new Error("Not authenticated");
}

initNotificationBell();
initChatWidget();
initMobileNav();
initThemeToggle(document.getElementById("themeToggleBtn"));

const alertBox = document.getElementById("alertBox");
const toastEl = document.getElementById("toast");

const searchDateInput = document.getElementById("searchDate");
const searchStartInput = document.getElementById("searchStart");
const searchEndInput = document.getElementById("searchEnd");

let currentToday = new Date().toISOString().slice(0, 10);

function refreshTodayIfDateRolledOver() {
  const today = new Date().toISOString().slice(0, 10);
  if (today !== currentToday) {
    if (searchDateInput.value === currentToday) {
      searchDateInput.value = today;
    }
    currentToday = today;
  }
}

setInterval(refreshTodayIfDateRolledOver, 60000);

const avatarChip = document.getElementById("avatarChip");
const avatarMenu = document.getElementById("avatarMenu");
const avatarInitials = document.getElementById("avatarInitials");
const avatarName = document.getElementById("avatarName");
const logoutBtn = document.getElementById("logoutBtn");


const capacityInput = document.getElementById("capacityInput");
const equipmentChipsEl = document.getElementById("equipmentChips");
const applyFiltersBtn = document.getElementById("applyFiltersBtn");
const resetFiltersBtn = document.getElementById("resetFiltersBtn");

const buildingDropdown = initDropdown({
  btn: document.getElementById("buildingSelectBtn"),
  panel: document.getElementById("buildingSelectPanel"),
  options: [],
  placeholder: t("dashboard.select_building"),
  onChange: (value) => populateFloorOptions(value),
});

const floorDropdown = initDropdown({
  btn: document.getElementById("floorSelectBtn"),
  panel: document.getElementById("floorSelectPanel"),
  options: [],
  placeholder: "Əvvəlcə bina seçin",
  onChange: () => {},
});
floorDropdown.setDisabled(true);

const equipmentDropdown = initDropdown({
  btn: document.getElementById("equipmentSelectBtn"),
  panel: document.getElementById("equipmentSelectPanel"),
  options: [],
  placeholder: t("dashboard.select_equipment"),
  onChange: (value) => {
    selectedEquipmentIds.add(value);
    renderSelectedEquipmentChips();
    populateEquipmentSelect();
  },
});

const resultsTitle = document.getElementById("resultsTitle");
const roomGrid = document.getElementById("roomGrid");

let allFloors = [];
let allCatalogRooms = [];
let allRoomEquipment = [];
let allEquipmentTypes = [];
let currentRoomsList = [];
const selectedEquipmentIds = new Set();
const ROOM_ART_VARIANTS = ["rv-1", "rv-2", "rv-3", "rv-4"];
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
  toastEl.innerHTML = `<div class="alert alert-warning" style="margin:0;"><i class="bi bi-info-circle"></i><span>${escapeHtml(message)}</span></div>`;
  toastEl.classList.add("show");
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => toastEl.classList.remove("show"), 2600);
}

function normalizeCatalogRoom(r) {
  const roomEquipmentIds = allRoomEquipment.filter((re) => re.roomId === r.id).map((re) => re.equipmentId);
  const equipmentNames = roomEquipmentIds
    .map((id) => allEquipmentTypes.find((eq) => eq.id === id)?.name)
    .filter(Boolean);
  return {
    id: r.id,
    name: r.name,
    capacity: r.capacity,
    floorId: r.floorId,
    floorName: (() => {
      const floor = allFloors.find((f) => f.id === r.floorId);
      return floor ? floorLabel(floor.levelNumber) : r.floorName;
    })(),
    buildingId: r.buildingId,
    buildingName: r.buildingName,
    equipment: equipmentNames.length ? equipmentNames : null,
    isActive: r.isActive,
    mode: "catalog",
  };
}

function getFilteredCatalogRooms() {
  const buildingId = buildingDropdown.getValue();
  const floorId = floorDropdown.getValue();
  const minCapacity = Number(capacityInput.value) || 1;

  return allCatalogRooms.filter((r) => {
    if (floorId) {
      if (r.floorId !== floorId) return false;
    } else if (buildingId) {
      const floor = allFloors.find((f) => f.id === r.floorId);
      if (!floor || floor.buildingId !== buildingId) return false;
    }
    if (r.capacity < minCapacity) return false;
    if (selectedEquipmentIds.size) {
      const roomEquipmentIds = new Set(allRoomEquipment.filter((re) => re.roomId === r.id).map((re) => re.equipmentId));
      for (const eqId of selectedEquipmentIds) {
        if (!roomEquipmentIds.has(eqId)) return false;
      }
    }
    return true;
  });
}

function applyCatalogFilters() {
  clearAlert(alertBox);
  const filtered = getFilteredCatalogRooms();
  renderRooms(filtered.map(normalizeCatalogRoom), "catalog");
}

function normalizeSearchRoom(r) {
  return {
    id: r.roomId,
    name: r.roomName,
    capacity: r.capacity,
    floorName: r.floorLevelNumber != null ? floorLabel(r.floorLevelNumber) : r.floorName,
    buildingId: r.buildingId,
    buildingName: r.buildingName,
    equipment: r.equipment,
    isActive: true,
    mode: "search",
  };
}

function populateFloorOptions(buildingId) {
  if (!buildingId) {
    floorDropdown.setOptions([], "");
    floorDropdown.setPlaceholder(t("dashboard.select_building_first"));
    floorDropdown.setDisabled(true);
    return;
  }

  floorDropdown.setDisabled(false);
  floorDropdown.setPlaceholder(t("dashboard.all_floors"));
  const filtered = allFloors.filter((f) => f.buildingId === buildingId);
  floorDropdown.setOptions(
    filtered.map((f) => ({ value: f.id, label: floorLabel(f.levelNumber) })),
    ""
  );
}

function populateEquipmentSelect() {
  const available = allEquipmentTypes.filter((eq) => !selectedEquipmentIds.has(eq.id));
  equipmentDropdown.setOptions(
    available.map((eq) => ({ value: eq.id, label: translateEquipmentName(eq.name) })),
    ""
  );
}

function renderSelectedEquipmentChips() {
  equipmentChipsEl.innerHTML = Array.from(selectedEquipmentIds)
    .map((id) => {
      const eq = allEquipmentTypes.find((e) => e.id === id);
      if (!eq) return "";
      return `<span class="chip-toggle active"><i class="bi bi-check2"></i>${escapeHtml(translateEquipmentName(eq.name))}<button type="button" class="remove-equipment-chip" data-id="${id}" aria-label="Sil"><i class="bi bi-x"></i></button></span>`;
    })
    .join("");

  equipmentChipsEl.querySelectorAll(".remove-equipment-chip").forEach((btn) => {
    btn.addEventListener("click", (e) => {
      e.stopPropagation();
      selectedEquipmentIds.delete(btn.dataset.id);
      renderSelectedEquipmentChips();
      populateEquipmentSelect();
    });
  });
}

function setHeader(mode, count) {
  const label = mode === "search" ? t("dashboard.search_results") : t("nav.rooms");
  resultsTitle.innerHTML = `${label} <span class="count">— ${count} ${t("dashboard.results_unit")}</span>`;

  const catalogHint = document.getElementById("catalogHint");
  catalogHint.style.display = mode === "catalog" ? "" : "none";
}

function createRoomCardHtml(room, idx) {
  const variant = ROOM_ART_VARIANTS[idx % ROOM_ART_VARIANTS.length];

  let statusBadge;
  if (room.mode === "search") {
    statusBadge = `<span class="badge badge-available status-badge"><span class="badge-dot"></span>${t("dashboard.available_now")}</span>`;
  } else if (room.isActive) {
    statusBadge = `<span class="badge badge-neutral status-badge"><span class="badge-dot"></span>${t("room.status.active")}</span>`;
  } else {
    statusBadge = `<span class="badge badge-inactive status-badge"><span class="badge-dot"></span>${t("room.status.inactive")}</span>`;
  }

  const bookDisabled = !room.isActive ? "disabled" : "";

  return `
    <div class="card card-hover room-card">
      <div class="room-art ${variant}">
        <div class="shape a"></div><div class="shape b"></div>
        ${statusBadge}
      </div>
      <div class="room-body">
        <h4 class="r-title">${escapeHtml(room.name)}</h4>
        <div class="r-meta">
          <span><i class="bi bi-building"></i>${escapeHtml(room.buildingName)}, ${escapeHtml(room.floorName)}</span>
          <span><i class="bi bi-people"></i>${room.capacity}</span>
        </div>
        <button type="button" class="room-equipment-link" data-room-id="${room.id}"><i class="bi bi-projector"></i>${t("filters.equipment")}</button>
        <div class="room-footer">
          <button class="btn btn-primary book-btn" data-room-id="${room.id}" ${bookDisabled}>${t("mock.reserve_btn")}</button>
        </div>
      </div>
    </div>`;

}

function renderRooms(list, mode = "catalog") {
  currentRoomsList = list;
  setHeader(mode, list.length);
  if (!list.length) {
    roomGrid.innerHTML = `<div class="empty-state"><i class="bi bi-search"></i>${t("dashboard.no_matching_room")}</div>`;
    return;
  }
  roomGrid.innerHTML = list.map((room, idx) => createRoomCardHtml(room, idx)).join("");
}

async function loadCatalog() {
  try {
    const rooms = await getAllRooms();
    allCatalogRooms = rooms;
    renderRooms(rooms.map(normalizeCatalogRoom), "catalog");
  } catch (err) {
    showAlert(alertBox, err.message);
  }
}

function updateApplyButtonLabel() {
  const hasSearchCriteria = searchDateInput.value && searchStartInput.value && searchEndInput.value;
  applyFiltersBtn.innerHTML = hasSearchCriteria
    ? '<i class="bi bi-search"></i>Axtar'
    : '<i class="bi bi-funnel"></i>Filtrlə';
}

[searchDateInput, searchStartInput, searchEndInput].forEach((el) =>
  el.addEventListener("input", updateApplyButtonLabel)
);

async function handleApplyFilters() {
  clearAlert(alertBox);

  const date = searchDateInput.value;
  const start = searchStartInput.value;
  const end = searchEndInput.value;

  if (!date && !start && !end) {
    applyCatalogFilters();
    return;
  }

  if (!date || !start || !end) {
    showAlert(alertBox, "Mövcudluq axtarışı üçün tarix və saat aralığının hamısını doldurun.", "warning");
    return;
  }

  const buildingId = buildingDropdown.getValue();
  if (!buildingId) {
    showAlert(alertBox, "Axtarış üçün bina seçin.", "warning");
    return;
  }

  const startDate = new Date(`${date}T${start}`);
  const endDate = new Date(`${date}T${end}`);

  if (startDate >= endDate) {
    showAlert(alertBox, "Bitmə saatı başlama saatından sonra olmalıdır.", "warning");
    return;
  }

  const dto = {
    StartDate: startDate.toISOString(),
    EndDate: endDate.toISOString(),
    BuildingId: buildingId,
    FloorId: floorDropdown.getValue() || null,
    EquipmentIds: Array.from(selectedEquipmentIds),
    Capacity: Number(capacityInput.value) || 1,
  };

  applyFiltersBtn.disabled = true;
  applyFiltersBtn.innerHTML = '<i class="bi bi-hourglass-split"></i>';

  try {
    const results = await searchAvailableRooms(dto);
    renderRooms(results.map(normalizeSearchRoom), "search");
  } catch (err) {
    showAlert(alertBox, err.message);
  } finally {
    applyFiltersBtn.disabled = false;
    updateApplyButtonLabel();
  }
}

applyFiltersBtn.addEventListener("click", handleApplyFilters);

resetFiltersBtn.addEventListener("click", () => {
  buildingDropdown.setValue("");
  populateFloorOptions("");
  capacityInput.value = 1;
  selectedEquipmentIds.clear();
  renderSelectedEquipmentChips();
  populateEquipmentSelect();
  searchDateInput.value = currentToday;
  searchStartInput.value = "";
  searchEndInput.value = "";
  updateApplyButtonLabel();
  clearAlert(alertBox);
  loadCatalog();
});

let roomEquipmentPopoverOutsideHandler = null;
function closeRoomEquipmentPopover() {
  const existing = document.getElementById("roomEquipmentPopover");
  if (existing) existing.remove();
  if (roomEquipmentPopoverOutsideHandler) {
    document.removeEventListener("click", roomEquipmentPopoverOutsideHandler);
    roomEquipmentPopoverOutsideHandler = null;
  }
}

function showRoomEquipmentPopover(roomId, anchorEl) {
  closeRoomEquipmentPopover();
  const rect = anchorEl.getBoundingClientRect();

  const items = allRoomEquipment.filter((re) => re.roomId === roomId);

  const pop = document.createElement("div");
  pop.className = "event-popover";
  pop.id = "roomEquipmentPopover";

  const bodyHtml = items.length
    ? items
        .map((re) => {
          const eq = allEquipmentTypes.find((e) => e.id === re.equipmentId);
          return `<div class="ep-row"><i class="bi bi-check2"></i>${escapeHtml(eq ? translateEquipmentName(eq.name) : t("common.unknown"))} × ${re.quantity}</div>`;
        })
        .join("")
    : `<div class="ep-row"><i class="bi bi-slash-circle"></i>${t("adminrooms.no_equipment")}</div>`;

  pop.innerHTML = `
    <div class="ep-head">
      <strong>${t("filters.equipment")}</strong>
      <button type="button" class="ep-close" aria-label="Bağla"><i class="bi bi-x"></i></button>
    </div>
    ${bodyHtml}
  `;
  document.body.appendChild(pop);

  const top = window.scrollY + rect.bottom + 8;
  const maxLeft = window.scrollX + document.documentElement.clientWidth - 276;
  const left = Math.min(window.scrollX + rect.left, Math.max(maxLeft, 12));
  pop.style.top = `${top}px`;
  pop.style.left = `${left}px`;

  pop.querySelector(".ep-close").addEventListener("click", closeRoomEquipmentPopover);

  roomEquipmentPopoverOutsideHandler = (e) => {
    if (!pop.contains(e.target) && e.target !== anchorEl) closeRoomEquipmentPopover();
  };
  setTimeout(() => document.addEventListener("click", roomEquipmentPopoverOutsideHandler), 0);
}

roomGrid.addEventListener("click", (e) => {
  const equipmentBtn = e.target.closest(".room-equipment-link");
  if (equipmentBtn) {
    e.stopPropagation();
    showRoomEquipmentPopover(equipmentBtn.dataset.roomId, equipmentBtn);
    return;
  }

  const btn = e.target.closest(".book-btn");
  if (!btn || btn.disabled) return;
  const room = currentRoomsList.find((r) => r.id === btn.dataset.roomId);
  if (!room) return;

  openBookingModal({
    mode: "create",
    isAdmin: getRoles().includes("Admin"),
    room: { id: room.id, name: room.name, buildingId: room.buildingId },
    prefillDate: searchDateInput.value,
    prefillStart: searchStartInput.value,
    prefillEnd: searchEndInput.value,
    onSuccess: () => {
      showToast("Rezervasiya yaradıldı.");
      loadCatalog();
    },
  });
});

avatarChip.addEventListener("click", (e) => {
  e.stopPropagation();
  avatarMenu.classList.toggle("open");
});
document.addEventListener("click", () => avatarMenu.classList.remove("open"));

logoutBtn.addEventListener("click", async () => {
  await logout();
  clearChatState();
  window.location.href = "landing.html";
});



async function init() {
  const { initials, name } = getDisplayIdentity();
  avatarInitials.textContent = initials;
  avatarName.textContent = name;

    if (getRoles().includes("Admin")) {
    document.getElementById("adminPanelLink").style.display = "";
  }

  searchDateInput.value = currentToday;
  updateApplyButtonLabel();

  try {
    const [buildings, floors, equipmentTypes, roomEquipmentListRes] = await Promise.all([
      getAllBuildings(),
      getAllFloors(),
      getAllEquipment(),
      getAllRoomEquipment(),
    ]);
    allFloors = floors;
    allEquipmentTypes = equipmentTypes;
    allRoomEquipment = roomEquipmentListRes;
    buildingDropdown.setOptions(buildings.map((b) => ({ value: b.id, label: b.name })), "");
    populateFloorOptions("");
    populateEquipmentSelect();
  } catch (err) {
    showAlert(alertBox, err.message);
  }

  await loadCatalog();
}

init();