import { requireAdmin, getDisplayIdentity } from "../components/adminGuard.js";
import { initAdminMobileNav, initAdminAccountMenu } from "../components/mobileNav.js";
import { getAllRooms, getAllFloors, getAllBuildings, getAllEquipment, createRoom, updateRoom, deleteRoom } from "../api/roomsApi.js";
import { getAllRoomEquipment, createRoomEquipment, updateRoomEquipment, deleteRoomEquipment } from "../api/roomEquipmentApi.js";
import { logout } from "../api/authApi.js";
import { showAlert, clearAlert } from "../components/alert.js";
import { initDropdown } from "../components/customDropdown.js";
import { escapeHtml } from "../utils/escapeHtml.js";
import { initThemeToggle } from "../components/theme.js";
import { t } from "../components/i18n.js";
import { floorLabel } from "../utils/floorLabel.js";
import { translateEquipmentName } from "../utils/equipmentTranslate.js";

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
const roomsContainer = document.getElementById("roomsContainer");
const addRoomBtn = document.getElementById("addRoomBtn");
const resetFiltersBtn = document.getElementById("resetFiltersBtn");

const modal = document.getElementById("roomModal");
const modalTitle = document.getElementById("roomModalTitle");
const modalAlert = document.getElementById("roomModalAlert");
const modalClose = document.getElementById("roomModalClose");
const modalCancelBtn = document.getElementById("roomModalCancelBtn");
const form = document.getElementById("roomForm");
const nameInput = document.getElementById("roomName");
const capacityInput = document.getElementById("roomCapacity");
const isActiveInput = document.getElementById("roomIsActive");
const submitBtn = document.getElementById("roomModalSubmitBtn");
const roomEquipmentField = document.getElementById("roomEquipmentField");
const roomEquipmentChips = document.getElementById("roomEquipmentChips");

let rooms = [];
let floors = [];
let buildings = [];
let equipmentList = [];
let roomEquipmentList = [];
let currentRoomEquipment = [];
let pendingEquipment = [];
let editingId = null;
function populateFilterFloorOptions(buildingId) {
  const filtered = buildingId ? floors.filter((f) => f.buildingId === buildingId) : floors;
  filterFloorDropdown.setOptions(filtered.map((f) => ({ value: f.id, label: floorLabel(f.levelNumber) })), "");
}

function populateModalFloorOptions(buildingId) {
  const filtered = buildingId ? floors.filter((f) => f.buildingId === buildingId) : floors;
  modalFloorDropdown.setOptions(filtered.map((f) => ({ value: f.id, label: floorLabel(f.levelNumber) })));
}

const filterBuildingDropdown = initDropdown({
  btn: document.getElementById("filterBuildingBtn"),
  panel: document.getElementById("filterBuildingPanel"),
  options: [],
  placeholder: t("common.all"),
  onChange: (value) => {
    populateFilterFloorOptions(value);
    renderRooms();
  },
});

const filterFloorDropdown = initDropdown({
  btn: document.getElementById("filterFloorBtn"),
  panel: document.getElementById("filterFloorPanel"),
  options: [],
  placeholder: t("common.all"),
  onChange: () => renderRooms(),
});

const filterStatusDropdown = initDropdown({
  btn: document.getElementById("filterStatusBtn"),
  panel: document.getElementById("filterStatusPanel"),
  options: [
    { value: "", label: t("common.all") },
    { value: "active", label: t("room.status.active") },
    { value: "inactive", label: t("room.status.inactive") },
  ],
  value: "",
  placeholder: t("common.all"),
  onChange: () => renderRooms(),
});

const modalBuildingDropdown = initDropdown({
  btn: document.getElementById("roomModalBuildingBtn"),
  panel: document.getElementById("roomModalBuildingPanel"),
  options: [],
  placeholder: t("common.select"),
  onChange: (value) => populateModalFloorOptions(value),
});

const modalFloorDropdown = initDropdown({
  btn: document.getElementById("roomModalFloorBtn"),
  panel: document.getElementById("roomModalFloorPanel"),
  options: [],
  placeholder: t("common.select"),
  onChange: () => {},
});

function renderRoomEquipmentChips() {
  roomEquipmentChips.innerHTML = "";
  const source = editingId ? currentRoomEquipment : pendingEquipment;
  equipmentList.forEach((eq) => {
    const assignment = source.find((re) => re.equipmentId === eq.id);
    const chip = document.createElement("span");
    chip.className = assignment ? "chip-toggle active" : "chip-toggle";
    chip.innerHTML = `
      <span class="chip-toggle-label"><i class="bi bi-check2"></i>${escapeHtml(translateEquipmentName(eq.name))}</span>
      <input type="number" class="chip-qty" min="1" max="1000" value="${assignment ? assignment.quantity : 1}" />
    `;
    chip.querySelector(".chip-toggle-label").addEventListener("click", () => toggleRoomEquipment(eq.id));
    const qtyInput = chip.querySelector(".chip-qty");
    qtyInput.addEventListener("click", (e) => e.stopPropagation());
    qtyInput.addEventListener("change", () => updateRoomEquipmentQuantity(eq.id, Number(qtyInput.value)));
    roomEquipmentChips.appendChild(chip);
  });
}

async function toggleRoomEquipment(equipmentId) {
  if (!editingId) {
    const idx = pendingEquipment.findIndex((re) => re.equipmentId === equipmentId);
    if (idx >= 0) pendingEquipment.splice(idx, 1);
    else pendingEquipment.push({ equipmentId, quantity: 1 });
    renderRoomEquipmentChips();
    return;
  }

  const existing = currentRoomEquipment.find((re) => re.equipmentId === equipmentId);
  try {
    if (existing) {
      await deleteRoomEquipment(existing.id);
      currentRoomEquipment = currentRoomEquipment.filter((re) => re.id !== existing.id);
      roomEquipmentList = roomEquipmentList.filter((re) => re.id !== existing.id);
    } else {
      const created = await createRoomEquipment({ RoomId: editingId, EquipmentId: equipmentId, Quantity: 1 });
      currentRoomEquipment.push(created);
      roomEquipmentList.push(created);
    }
    renderRoomEquipmentChips();
  } catch (err) {
    showAlert(modalAlert, err.message);
  }
}

async function updateRoomEquipmentQuantity(equipmentId, quantity) {
  if (!editingId) {
    const existing = pendingEquipment.find((re) => re.equipmentId === equipmentId);
    if (existing) existing.quantity = quantity;
    return;
  }

  const existing = currentRoomEquipment.find((re) => re.equipmentId === equipmentId);
  if (!existing) return;
  try {
    const updated = await updateRoomEquipment(existing.id, { Quantity: quantity });
    existing.quantity = updated.quantity;
    const globalEntry = roomEquipmentList.find((re) => re.id === existing.id);
    if (globalEntry) globalEntry.quantity = updated.quantity;
  } catch (err) {
    showAlert(modalAlert, err.message);
  }
}

function openModal(room) {
  editingId = room ? room.id : null;
  modalTitle.textContent = room ? t("adminrooms.edit_room") : t("adminrooms.add_room");
  submitBtn.textContent = room ? t("profile.save") : t("common.create");
  nameInput.value = room ? room.name : "";
  capacityInput.value = room ? room.capacity : 1;
  isActiveInput.checked = room ? room.isActive : true;

  let buildingId;
  if (room) {
    const floor = floors.find((f) => f.id === room.floorId);
    buildingId = floor ? floor.buildingId : buildings[0]?.id || "";
  } else {
    buildingId = filterBuildingDropdown.getValue() || buildings[0]?.id || "";
  }
  modalBuildingDropdown.setValue(buildingId);
  populateModalFloorOptions(buildingId);
  modalFloorDropdown.setValue(room ? room.floorId : filterFloorDropdown.getValue() || "");

  roomEquipmentField.style.display = "";
  if (room) {
    currentRoomEquipment = roomEquipmentList.filter((re) => re.roomId === room.id);
  } else {
    pendingEquipment = [];
  }
  renderRoomEquipmentChips();

  modalAlert.innerHTML = "";
  modal.classList.add("open");
  nameInput.focus();
}

function closeModal() {
  modal.classList.remove("open");
}

modalClose.addEventListener("click", closeModal);
modalCancelBtn.addEventListener("click", closeModal);
modal.addEventListener("click", (e) => {
  if (e.target === modal) closeModal();
});

addRoomBtn.addEventListener("click", () => openModal(null));

resetFiltersBtn.addEventListener("click", () => {
  filterBuildingDropdown.setValue("");
  populateFilterFloorOptions("");
  filterFloorDropdown.setValue("");
  filterStatusDropdown.setValue("");
  renderRooms();
});

form.addEventListener("submit", async (e) => {
  e.preventDefault();
  modalAlert.innerHTML = "";

  const dto = {
    Name: nameInput.value.trim(),
    Capacity: Number(capacityInput.value),
    FloorId: modalFloorDropdown.getValue(),
    IsActive: isActiveInput.checked,
  };

  submitBtn.disabled = true;
  submitBtn.textContent = t("common.saving");

  try {
    if (editingId) {
      await updateRoom(editingId, dto);
    } else {
      const created = await createRoom(dto);
      if (pendingEquipment.length) {
        await Promise.all(
          pendingEquipment.map((pe) =>
            createRoomEquipment({ RoomId: created.id, EquipmentId: pe.equipmentId, Quantity: pe.quantity })
          )
        );
      }
    }
    closeModal();
    await loadRooms();
  } catch (err) {
    showAlert(modalAlert, err.message);
  } finally {
    submitBtn.disabled = false;
    submitBtn.textContent = editingId ? t("profile.save") : t("common.create");
  }
});

function getFilteredRooms() {
  const floorFilter = filterFloorDropdown.getValue();
  const buildingFilter = filterBuildingDropdown.getValue();
  const statusFilter = filterStatusDropdown.getValue();

  return rooms.filter((r) => {
    if (floorFilter && r.floorId !== floorFilter) return false;
    if (buildingFilter) {
      const floor = floors.find((f) => f.id === r.floorId);
      if (!floor || floor.buildingId !== buildingFilter) return false;
    }
    if (statusFilter === "active" && !r.isActive) return false;
    if (statusFilter === "inactive" && r.isActive) return false;
    return true;
  });
}

let equipmentPopoverOutsideHandler = null;
function closeEquipmentPopover() {
  const existing = document.getElementById("equipmentPopover");
  if (existing) existing.remove();
  if (equipmentPopoverOutsideHandler) {
    document.removeEventListener("click", equipmentPopoverOutsideHandler);
    equipmentPopoverOutsideHandler = null;
  }
}

function showEquipmentPopover(roomId, anchorEl) {
  closeEquipmentPopover();
  const rect = anchorEl.getBoundingClientRect();

  const items = roomEquipmentList.filter((re) => re.roomId === roomId);

  const pop = document.createElement("div");
  pop.className = "event-popover";
  pop.id = "equipmentPopover";

  const bodyHtml = items.length
    ? items.map((re) => `<div class="ep-row"><i class="bi bi-check2"></i>${escapeHtml(translateEquipmentName(re.equipmentName))} × ${re.quantity}</div>`).join("")
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

  pop.querySelector(".ep-close").addEventListener("click", closeEquipmentPopover);

  equipmentPopoverOutsideHandler = (e) => {
    if (!pop.contains(e.target) && e.target !== anchorEl) closeEquipmentPopover();
  };
  setTimeout(() => document.addEventListener("click", equipmentPopoverOutsideHandler), 0);
}

function createRoomMiniCardHtml(r) {
  const badge = r.isActive
    ? `<span class="badge badge-neutral"><span class="badge-dot"></span>${t("room.status.active")}</span>`
    : `<span class="badge badge-inactive"><span class="badge-dot"></span>${t("room.status.inactive")}</span>`;
  const floor = floors.find((f) => f.id === r.floorId);
  const floorText = floor ? floorLabel(floor.levelNumber) : r.floorName;

  return `
    <div class="room-mini-card">
      <div class="room-mini-top">
        <div>
          <h4>${escapeHtml(r.name)}</h4>
          <p>${escapeHtml(floorText)}</p>
        </div>
        ${badge}
      </div>
      <div class="room-mini-meta" style="display:flex; align-items:center; justify-content:space-between;">
        <span><i class="bi bi-people"></i>${r.capacity}</span>
        <button type="button" class="room-mini-equipment-link" data-room-id="${r.id}"><i class="bi bi-projector"></i>${t("filters.equipment")}</button>
      </div>
      <div class="room-mini-actions">
        <button class="icon-action-btn edit-room-btn" data-id="${r.id}" title="${t("common.edit")}"><i class="bi bi-pencil"></i></button>
        <button class="icon-action-btn delete-room-btn" data-id="${r.id}" title="${t("common.delete")}"><i class="bi bi-trash"></i></button>
      </div>
    </div>`;
}

function createRoomSectionHtml(buildingName, roomsInBuilding) {
  const cardsHtml = roomsInBuilding.map(createRoomMiniCardHtml).join("");
  return `
    <div class="room-section">
      <div class="room-section-header">
        <i class="bi bi-building"></i>
        <h3>${escapeHtml(buildingName)}</h3>
        <span class="room-section-count">${roomsInBuilding.length} otaq</span>
      </div>
      <div class="room-mini-grid">${cardsHtml}</div>
    </div>`;
}

function renderRooms() {
  const rows = getFilteredRooms();

  if (!rows.length) {
    roomsContainer.innerHTML = `<div class="empty-state"><i class="bi bi-door-open"></i>Otaq tapılmadı.</div>`;
    return;
  }

  const grouped = new Map();
  rows.forEach((r) => {
    if (!grouped.has(r.buildingName)) grouped.set(r.buildingName, []);
    grouped.get(r.buildingName).push(r);
  });

  roomsContainer.innerHTML = Array.from(grouped.entries())
    .map(([buildingName, roomsInBuilding]) => createRoomSectionHtml(buildingName, roomsInBuilding))
    .join("");

  roomsContainer.querySelectorAll(".room-mini-equipment-link").forEach((btn) => {
    btn.addEventListener("click", (e) => {
      e.stopPropagation();
      showEquipmentPopover(btn.dataset.roomId, btn);
    });
  });

  roomsContainer.querySelectorAll(".edit-room-btn").forEach((btn) => {
    btn.addEventListener("click", () => {
      const r = rooms.find((x) => x.id === btn.dataset.id);
      if (r) openModal(r);
    });
  });

  roomsContainer.querySelectorAll(".delete-room-btn").forEach((btn) => {
    btn.addEventListener("click", async () => {
      if (!confirm("Bu otağı silmək istədiyinizə əminsiniz?")) return;
      clearAlert(alertBox);
      try {
        await deleteRoom(btn.dataset.id);
        await loadRooms();
      } catch (err) {
        showAlert(alertBox, err.message);
      }
    });
  });
}

async function loadRooms() {
  clearAlert(alertBox);
  try {
    const [roomList, floorList, buildingList, equipmentListRes, roomEquipmentListRes] = await Promise.all([
      getAllRooms(),
      getAllFloors(),
      getAllBuildings(),
      getAllEquipment(),
      getAllRoomEquipment(),
    ]);
    rooms = roomList;
    floors = floorList;
    buildings = buildingList;
    equipmentList = equipmentListRes;
    roomEquipmentList = roomEquipmentListRes;

    filterBuildingDropdown.setOptions(buildings.map((b) => ({ value: b.id, label: b.name })), "");
    populateFilterFloorOptions("");
    modalBuildingDropdown.setOptions(buildings.map((b) => ({ value: b.id, label: b.name })));

    renderRooms();
  } catch (err) {
    showAlert(alertBox, err.message);
  }
}

logoutBtn.addEventListener("click", async () => {
  await logout();
  window.location.href = "adminLogin.html";
});
loadRooms();
