import { isLoggedIn } from "../api/apiClient.js";
import { logout } from "../api/authApi.js";
import { getAllFloors, getAllBuildings, createFloor, updateFloor, deleteFloor } from "../api/roomsApi.js";
import { showAlert, clearAlert } from "../components/alert.js";

if (!isLoggedIn()) {
  window.location.href = "login.html";
}

const alertBox = document.getElementById("alertBox");
const logoutBtn = document.getElementById("logoutBtn");
const tableBody = document.getElementById("floorsTableBody");
const addFloorBtn = document.getElementById("addFloorBtn");
const filterBuilding = document.getElementById("filterBuilding");

const modal = document.getElementById("floorModal");
const modalTitle = document.getElementById("floorModalTitle");
const modalAlert = document.getElementById("floorModalAlert");
const modalClose = document.getElementById("floorModalClose");
const modalCancelBtn = document.getElementById("floorModalCancelBtn");
const form = document.getElementById("floorForm");
const nameInput = document.getElementById("floorName");
const levelInput = document.getElementById("floorLevel");
const buildingSelect = document.getElementById("floorBuilding");
const submitBtn = document.getElementById("floorModalSubmitBtn");

let floors = [];
let buildings = [];
let editingId = null;

function escapeHtml(str) {
  const div = document.createElement("div");
  div.textContent = str ?? "";
  return div.innerHTML;
}

function openModal(floor) {
  editingId = floor ? floor.id : null;
  modalTitle.textContent = floor ? "Mərtəbəni redaktə et" : "Yeni mərtəbə";
  submitBtn.textContent = floor ? "Yadda saxla" : "Yarat";
  nameInput.value = floor ? floor.name : "";
  levelInput.value = floor ? floor.levelNumber : "";
  buildingSelect.value = floor ? floor.buildingId : filterBuilding.value || buildings[0]?.id || "";
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

addFloorBtn.addEventListener("click", () => openModal(null));

form.addEventListener("submit", async (e) => {
  e.preventDefault();
  modalAlert.innerHTML = "";

  const dto = {
    Name: nameInput.value.trim(),
    LevelNumber: Number(levelInput.value),
    BuildingId: buildingSelect.value,
  };

  submitBtn.disabled = true;
  submitBtn.textContent = "Yadda saxlanılır...";

  try {
    if (editingId) {
      await updateFloor(editingId, dto);
    } else {
      await createFloor(dto);
    }
    closeModal();
    await loadFloors();
  } catch (err) {
    modalAlert.innerHTML = `<div class="alert alert-danger"><i class="bi bi-exclamation-circle"></i><span>${escapeHtml(err.message)}</span></div>`;
  } finally {
    submitBtn.disabled = false;
    submitBtn.textContent = editingId ? "Yadda saxla" : "Yarat";
  }
});

function getFilteredFloors() {
  return filterBuilding.value ? floors.filter((f) => f.buildingId === filterBuilding.value) : floors;
}

function renderFloors() {
  const rows = getFilteredFloors();

  if (!rows.length) {
    tableBody.innerHTML = `<tr><td colspan="4" class="text-muted" style="text-align:center; padding:2rem;">Mərtəbə tapılmadı.</td></tr>`;
    return;
  }
  tableBody.innerHTML = rows
    .map(
      (f) => `
    <tr>
      <td>${escapeHtml(f.name)}</td>
      <td>${f.levelNumber}</td>
      <td>${escapeHtml(f.buildingName)}</td>
      <td>
        <div class="row-actions">
          <button class="icon-action-btn edit-btn" data-id="${f.id}" title="Redaktə et"><i class="bi bi-pencil"></i></button>
          <button class="icon-action-btn delete-btn" data-id="${f.id}" title="Sil"><i class="bi bi-trash"></i></button>
        </div>
      </td>
    </tr>`
    )
    .join("");

  tableBody.querySelectorAll(".edit-btn").forEach((btn) => {
    btn.addEventListener("click", () => {
      const f = floors.find((x) => x.id === btn.dataset.id);
      if (f) openModal(f);
    });
  });

  tableBody.querySelectorAll(".delete-btn").forEach((btn) => {
    btn.addEventListener("click", async () => {
      if (!confirm("Bu mərtəbəni silmək istədiyinizə əminsiniz?")) return;
      clearAlert(alertBox);
      try {
        await deleteFloor(btn.dataset.id);
        await loadFloors();
      } catch (err) {
        showAlert(alertBox, err.message);
      }
    });
  });
}

filterBuilding.addEventListener("change", renderFloors);

async function loadFloors() {
  clearAlert(alertBox);
  try {
    const [floorList, buildingList] = await Promise.all([getAllFloors(), getAllBuildings()]);
    floors = floorList;
    buildings = buildingList;

    buildingSelect.innerHTML = buildings.map((b) => `<option value="${b.id}">${escapeHtml(b.name)}</option>`).join("");
    filterBuilding.innerHTML =
      '<option value="">Hamısı</option>' + buildings.map((b) => `<option value="${b.id}">${escapeHtml(b.name)}</option>`).join("");

    renderFloors();
  } catch (err) {
    showAlert(alertBox, err.message);
  }
}

logoutBtn.addEventListener("click", async () => {
  await logout();
  window.location.href = "login.html";
});

loadFloors();