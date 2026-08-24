import { requireAdmin } from "../components/adminGuard.js";
import { getAllRooms, getAllFloors, getAllBuildings, getAllEquipment, createRoom, updateRoom, deleteRoom } from "../api/roomsApi.js";
import { getAllRoomEquipment, createRoomEquipment, updateRoomEquipment, deleteRoomEquipment } from "../api/roomEquipmentApi.js";
import { logout } from "../api/authApi.js";
import { showAlert, clearAlert } from "../components/alert.js";

requireAdmin();

const alertBox = document.getElementById("alertBox");
const logoutBtn = document.getElementById("logoutBtn");
const roomsContainer = document.getElementById("roomsContainer");
const addRoomBtn = document.getElementById("addRoomBtn");

const filterBuilding = document.getElementById("filterBuilding");
const filterFloor = document.getElementById("filterFloor");
const filterStatus = document.getElementById("filterStatus");

const modal = document.getElementById("roomModal");
const modalTitle = document.getElementById("roomModalTitle");
const modalAlert = document.getElementById("roomModalAlert");
const modalClose = document.getElementById("roomModalClose");
const modalCancelBtn = document.getElementById("roomModalCancelBtn");
const form = document.getElementById("roomForm");
const nameInput = document.getElementById("roomName");
const capacityInput = document.getElementById("roomCapacity");
const modalBuildingSelect = document.getElementById("roomModalBuilding");
const modalFloorSelect = document.getElementById("roomModalFloor");
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
let editingId = null;

function escapeHtml(str) {
  const div = document.createElement("div");
  div.textContent = str ?? "";
  return div.innerHTML;
}

function populateFilterFloorOptions(buildingId) {
  const filtered = buildingId ? floors.filter((f) => f.buildingId === buildingId) : floors;
  filterFloor.innerHTML =
    '<option value="">Hamısı</option>' + filtered.map((f) => `<option value="${f.id}">${escapeHtml(f.name)}</option>`).join("");
}

function populateModalFloorOptions(buildingId) {
  const filtered = buildingId ? floors.filter((f) => f.buildingId === buildingId) : floors;
  modalFloorSelect.innerHTML = filtered.map((f) => `<option value="${f.id}">${escapeHtml(f.name)}</option>`).join("");
}

filterBuilding.addEventListener("change", () => {
  populateFilterFloorOptions(filterBuilding.value);
  filterFloor.value = "";
  renderRooms();
});
filterFloor.addEventListener("change", renderRooms);
filterStatus.addEventListener("change", renderRooms);

modalBuildingSelect.addEventListener("change", () => {
  populateModalFloorOptions(modalBuildingSelect.value);
});

function renderRoomEquipmentChips() {
  roomEquipmentChips.innerHTML = "";
  equipmentList.forEach((eq) => {
    const assignment = currentRoomEquipment.find((re) => re.equipmentId === eq.id);
    const chip = document.createElement("span");
    chip.className = assignment ? "chip-toggle active" : "chip-toggle";
    chip.innerHTML = `
      <span class="chip-toggle-label"><i class="bi bi-check2"></i>${escapeHtml(eq.name)}</span>
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
    modalAlert.innerHTML = `<div class="alert alert-danger"><i class="bi bi-exclamation-circle"></i><span>${escapeHtml(err.message)}</span></div>`;
  }
}

async function updateRoomEquipmentQuantity(equipmentId, quantity) {
  const existing = currentRoomEquipment.find((re) => re.equipmentId === equipmentId);
  if (!existing) return;
  try {
    const updated = await updateRoomEquipment(existing.id, { Quantity: quantity });
    existing.quantity = updated.quantity;
    const globalEntry = roomEquipmentList.find((re) => re.id === existing.id);
    if (globalEntry) globalEntry.quantity = updated.quantity;
  } catch (err) {
    modalAlert.innerHTML = `<div class="alert alert-danger"><i class="bi bi-exclamation-circle"></i><span>${escapeHtml(err.message)}</span></div>`;
  }
}

function openModal(room) {
  editingId = room ? room.id : null;
  modalTitle.textContent = room ? "Otağı redaktə et" : "Yeni otaq";
  submitBtn.textContent = room ? "Yadda saxla" : "Yarat";
  nameInput.value = room ? room.name : "";
  capacityInput.value = room ? room.capacity : 1;
  isActiveInput.checked = room ? room.isActive : true;

  let buildingId;
  if (room) {
    const floor = floors.find((f) => f.id === room.floorId);
    buildingId = floor ? floor.buildingId : buildings[0]?.id || "";
  } else {
    buildingId = filterBuilding.value || buildings[0]?.id || "";
  }
  modalBuildingSelect.value = buildingId;
  populateModalFloorOptions(buildingId);
  modalFloorSelect.value = room ? room.floorId : filterFloor.value || "";

  if (room) {
    roomEquipmentField.style.display = "";
    currentRoomEquipment = roomEquipmentList.filter((re) => re.roomId === room.id);
    renderRoomEquipmentChips();
  } else {
    roomEquipmentField.style.display = "none";
  }

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

form.addEventListener("submit", async (e) => {
  e.preventDefault();
  modalAlert.innerHTML = "";

  const dto = {
    Name: nameInput.value.trim(),
    Capacity: Number(capacityInput.value),
    FloorId: modalFloorSelect.value,
    IsActive: isActiveInput.checked,
  };

  submitBtn.disabled = true;
  submitBtn.textContent = "Yadda saxlanılır...";

  try {
    if (editingId) {
      await updateRoom(editingId, dto);
    } else {
      await createRoom(dto);
    }
    closeModal();
    await loadRooms();
  } catch (err) {
    modalAlert.innerHTML = `<div class="alert alert-danger"><i class="bi bi-exclamation-circle"></i><span>${escapeHtml(err.message)}</span></div>`;
  } finally {
    submitBtn.disabled = false;
    submitBtn.textContent = editingId ? "Yadda saxla" : "Yarat";
  }
});

function getFilteredRooms() {
  return rooms.filter((r) => {
    if (filterFloor.value && r.floorId !== filterFloor.value) return false;
    if (filterBuilding.value) {
      const floor = floors.find((f) => f.id === r.floorId);
      if (!floor || floor.buildingId !== filterBuilding.value) return false;
    }
    if (filterStatus.value === "active" && !r.isActive) return false;
    if (filterStatus.value === "inactive" && r.isActive) return false;
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
    ? items.map((re) => `<div class="ep-row"><i class="bi bi-check2"></i>${escapeHtml(re.equipmentName)} × ${re.quantity}</div>`).join("")
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

  pop.querySelector(".ep-close").addEventListener("click", closeEquipmentPopover);

  equipmentPopoverOutsideHandler = (e) => {
    if (!pop.contains(e.target) && e.target !== anchorEl) closeEquipmentPopover();
  };
  setTimeout(() => document.addEventListener("click", equipmentPopoverOutsideHandler), 0);
}

function createRoomMiniCardHtml(r) {
  const badge = r.isActive
    ? `<span class="badge badge-neutral"><span class="badge-dot"></span>Aktiv</span>`
    : `<span class="badge badge-inactive"><span class="badge-dot"></span>Deaktiv</span>`;

  return `
    <div class="room-mini-card">
      <div class="room-mini-top">
        <div>
          <h4>${escapeHtml(r.name)}</h4>
          <p>${escapeHtml(r.floorName)}</p>
        </div>
        ${badge}
      </div>
      <div class="room-mini-meta" style="display:flex; align-items:center; justify-content:space-between;">
        <span><i class="bi bi-people"></i>${r.capacity}</span>
        <button type="button" class="room-mini-equipment-link" data-room-id="${r.id}"><i class="bi bi-projector"></i>Avadanlıqlar</button>
      </div>
      <div class="room-mini-actions">
        <button class="icon-action-btn edit-room-btn" data-id="${r.id}" title="Redaktə et"><i class="bi bi-pencil"></i></button>
        <button class="icon-action-btn delete-room-btn" data-id="${r.id}" title="Sil"><i class="bi bi-trash"></i></button>
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

    filterBuilding.innerHTML =
      '<option value="">Hamısı</option>' + buildings.map((b) => `<option value="${b.id}">${escapeHtml(b.name)}</option>`).join("");
    populateFilterFloorOptions("");
    modalBuildingSelect.innerHTML = buildings.map((b) => `<option value="${b.id}">${escapeHtml(b.name)}</option>`).join("");

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
