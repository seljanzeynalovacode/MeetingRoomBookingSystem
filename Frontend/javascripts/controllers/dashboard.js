import { isLoggedIn, getAccessToken } from "../api/apiClient.js";
import { getAllRooms, searchAvailableRooms, getAllBuildings, getAllFloors, getAllEquipment } from "../api/roomsApi.js";
import { getAllRoomEquipment } from "../api/roomEquipmentApi.js";
import { logout } from "../api/authApi.js";
import { openBookingModal } from "../components/bookingModal.js";
import { showAlert, clearAlert } from "../components/alert.js";

if (!isLoggedIn()) {
  window.location.href = "login.html";
}

const alertBox = document.getElementById("alertBox");
const toastEl = document.getElementById("toast");

const searchForm = document.getElementById("searchForm");
const searchDateInput = document.getElementById("searchDate");
const searchStartInput = document.getElementById("searchStart");
const searchEndInput = document.getElementById("searchEnd");
const searchBtn = document.getElementById("searchBtn");
const resetSearchBtn = document.getElementById("resetSearchBtn");

const avatarChip = document.getElementById("avatarChip");
const avatarMenu = document.getElementById("avatarMenu");
const avatarInitials = document.getElementById("avatarInitials");
const avatarName = document.getElementById("avatarName");
const logoutBtn = document.getElementById("logoutBtn");


const buildingSelect = document.getElementById("buildingSelect");
const floorSelect = document.getElementById("floorSelect");
const capacityInput = document.getElementById("capacityInput");
const equipmentChipsEl = document.getElementById("equipmentChips");
const equipmentSelect = document.getElementById("equipmentSelect");
const applyFiltersBtn = document.getElementById("applyFiltersBtn");
const resetFiltersBtn = document.getElementById("resetFiltersBtn");

const resultsTitle = document.getElementById("resultsTitle");
const roomGrid = document.getElementById("roomGrid");

let allFloors = [];
let allCatalogRooms = [];
let allRoomEquipment = [];
let allEquipmentTypes = [];
let currentRoomsList = [];
const selectedEquipmentIds = new Set();
const ROOM_ART_VARIANTS = ["rv-1", "rv-2", "rv-3", "rv-4"];

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
    floorName: r.floorName,
    buildingName: r.buildingName,
    equipment: equipmentNames.length ? equipmentNames : null,
    isActive: r.isActive,
    mode: "catalog",
  };
}

function getFilteredCatalogRooms() {
  const buildingId = buildingSelect.value;
  const floorId = floorSelect.value;
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
    floorName: r.floorName,
    buildingName: r.buildingName,
    equipment: r.equipment,
    isActive: true,
    mode: "search",
  };
}

function populateFloorOptions(buildingId) {
  const filtered = buildingId ? allFloors.filter((f) => f.buildingId === buildingId) : allFloors;
  floorSelect.innerHTML =
    '<option value="">Bütün mərtəbələr</option>' +
    filtered.map((f) => `<option value="${f.id}">${escapeHtml(f.name)}</option>`).join("");
}

function populateEquipmentSelect() {
  const available = allEquipmentTypes.filter((eq) => !selectedEquipmentIds.has(eq.id));
  equipmentSelect.innerHTML =
    '<option value="">Avadanlıq seçin</option>' +
    available.map((eq) => `<option value="${eq.id}">${escapeHtml(eq.name)}</option>`).join("");
}

function renderSelectedEquipmentChips() {
  equipmentChipsEl.innerHTML = Array.from(selectedEquipmentIds)
    .map((id) => {
      const eq = allEquipmentTypes.find((e) => e.id === id);
      if (!eq) return "";
      return `<span class="chip-toggle active"><i class="bi bi-check2"></i>${escapeHtml(eq.name)}<button type="button" class="remove-equipment-chip" data-id="${id}" aria-label="Sil"><i class="bi bi-x"></i></button></span>`;
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

equipmentSelect.addEventListener("change", () => {
  if (!equipmentSelect.value) return;
  selectedEquipmentIds.add(equipmentSelect.value);
  renderSelectedEquipmentChips();
  populateEquipmentSelect();
});

function setHeader(mode, count) {
  const label = mode === "search" ? "Axtarış nəticələri" : "Otaqlar";
  resultsTitle.innerHTML = `${label} <span class="count">— ${count} nəticə</span>`;

  const catalogHint = document.getElementById("catalogHint");
  catalogHint.style.display = mode === "catalog" ? "" : "none";
}

function createRoomCardHtml(room, idx) {
  const variant = ROOM_ART_VARIANTS[idx % ROOM_ART_VARIANTS.length];

  let statusBadge;
  if (room.mode === "search") {
    statusBadge = `<span class="badge badge-available status-badge"><span class="badge-dot"></span>Bu vaxtda boşdur</span>`;
  } else if (room.isActive) {
    statusBadge = `<span class="badge badge-neutral status-badge"><span class="badge-dot"></span>Aktiv</span>`;
  } else {
    statusBadge = `<span class="badge badge-inactive status-badge"><span class="badge-dot"></span>Deaktiv</span>`;
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
        <button type="button" class="room-equipment-link" data-room-id="${room.id}"><i class="bi bi-projector"></i>Avadanlıqlar</button>
        <div class="room-footer">
          <button class="btn btn-primary book-btn" data-room-id="${room.id}" ${bookDisabled}>Rezerv et</button>
        </div>
      </div>
    </div>`;

}

function renderRooms(list, mode = "catalog") {
  currentRoomsList = list;
  setHeader(mode, list.length);
  if (!list.length) {
    roomGrid.innerHTML = `<div class="empty-state"><i class="bi bi-search"></i>Uyğun otaq tapılmadı.</div>`;
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

searchForm.addEventListener("submit", async (e) => {
  e.preventDefault();
  clearAlert(alertBox);

  const buildingId = buildingSelect.value;
  const date = searchDateInput.value;
  const start = searchStartInput.value;
  const end = searchEndInput.value;

  if (!buildingId) {
    showAlert(alertBox, "Axtarış üçün bina seçin.", "warning");
    return;
  }
  if (!date || !start || !end) {
    showAlert(alertBox, "Tarix və saat aralığını daxil edin.", "warning");
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
    FloorId: floorSelect.value || null,
    EquipmentIds: Array.from(selectedEquipmentIds),
    Capacity: Number(capacityInput.value) || 1,
  };

  searchBtn.disabled = true;
  searchBtn.innerHTML = '<i class="bi bi-hourglass-split"></i>';

  try {
    const results = await searchAvailableRooms(dto);
    renderRooms(results.map(normalizeSearchRoom), "search");
  } catch (err) {
    showAlert(alertBox, err.message);
  } finally {
    searchBtn.disabled = false;
    searchBtn.innerHTML = '<i class="bi bi-search"></i>Axtar';
  }
});

resetSearchBtn.addEventListener("click", () => {
  clearAlert(alertBox);
  loadCatalog();
});

applyFiltersBtn.addEventListener("click", applyCatalogFilters);

resetFiltersBtn.addEventListener("click", () => {
  buildingSelect.value = "";
  populateFloorOptions("");
  capacityInput.value = 1;
  selectedEquipmentIds.clear();
  renderSelectedEquipmentChips();
  populateEquipmentSelect();
  clearAlert(alertBox);
  loadCatalog();
});

buildingSelect.addEventListener("change", () => {
  populateFloorOptions(buildingSelect.value);
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
          return `<div class="ep-row"><i class="bi bi-check2"></i>${escapeHtml(eq ? eq.name : "Naməlum")} × ${re.quantity}</div>`;
        })
        .join("")
    : `<div class="ep-row"><i class="bi bi-slash-circle"></i>Avadanlıq təyin edilməyib</div>`;

  pop.innerHTML = `
    <div class="ep-head">
      <strong>Avadanlıqlar</strong>
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
    room: { id: room.id, name: room.name },
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
  window.location.href = "login.html";
});



async function init() {
  const { initials, name } = getDisplayIdentity();
  avatarInitials.textContent = initials;
  avatarName.textContent = name;

    if (getRoles().includes("Admin")) {
    document.getElementById("adminPanelLink").style.display = "";
  }

  const today = new Date().toISOString().slice(0, 10);
  searchDateInput.value = today;

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
    buildingSelect.innerHTML =
      '<option value="">Bina seçin</option>' +
      buildings.map((b) => `<option value="${b.id}">${escapeHtml(b.name)}</option>`).join("");
    populateFloorOptions("");
    populateEquipmentSelect();
  } catch (err) {
    showAlert(alertBox, err.message);
  }

  await loadCatalog();
}

init();