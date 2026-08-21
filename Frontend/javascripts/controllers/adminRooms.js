import { isLoggedIn } from "../api/apiClient.js";
import { logout } from "../api/authApi.js";
import { getAllRooms, getAllFloors, getAllBuildings, createRoom, updateRoom, deleteRoom } from "../api/roomsApi.js";
import { showAlert, clearAlert } from "../components/alert.js";

if (!isLoggedIn()) {
  window.location.href = "login.html";
}

const alertBox = document.getElementById("alertBox");
const logoutBtn = document.getElementById("logoutBtn");
const tableBody = document.getElementById("roomsTableBody");
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

let rooms = [];
let floors = [];
let buildings = [];
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

function statusBadgeHtml(isActive) {
  return isActive
    ? `<span class="badge badge-neutral"><span class="badge-dot"></span>Aktiv</span>`
    : `<span class="badge badge-inactive"><span class="badge-dot"></span>Deaktiv</span>`;
}

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

function renderRooms() {
  const rows = getFilteredRooms();

  if (!rows.length) {
    tableBody.innerHTML = `<tr><td colspan="6" class="text-muted" style="text-align:center; padding:2rem;">Otaq tapılmadı.</td></tr>`;
    return;
  }

  tableBody.innerHTML = rows
    .map(
      (r) => `
    <tr>
      <td>${escapeHtml(r.name)}</td>
      <td>${r.capacity}</td>
      <td>${escapeHtml(r.floorName)}</td>
      <td>${escapeHtml(r.buildingName)}</td>
      <td>${statusBadgeHtml(r.isActive)}</td>
      <td>
        <div class="row-actions">
          <button class="icon-action-btn edit-btn" data-id="${r.id}" title="Redaktə et"><i class="bi bi-pencil"></i></button>
          <button class="icon-action-btn delete-btn" data-id="${r.id}" title="Sil"><i class="bi bi-trash"></i></button>
        </div>
      </td>
    </tr>`
    )
    .join("");

  tableBody.querySelectorAll(".edit-btn").forEach((btn) => {
    btn.addEventListener("click", () => {
      const r = rooms.find((x) => x.id === btn.dataset.id);
      if (r) openModal(r);
    });
  });

  tableBody.querySelectorAll(".delete-btn").forEach((btn) => {
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
    const [roomList, floorList, buildingList] = await Promise.all([getAllRooms(), getAllFloors(), getAllBuildings()]);
    rooms = roomList;
    floors = floorList;
    buildings = buildingList;

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
  window.location.href = "login.html";
});

loadRooms();