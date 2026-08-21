import { isLoggedIn } from "../api/apiClient.js";
import { logout } from "../api/authApi.js";
import { getAllBuildings, createBuilding, updateBuilding, deleteBuilding } from "../api/roomsApi.js";
import { showAlert, clearAlert } from "../components/alert.js";

if (!isLoggedIn()) {
  window.location.href = "login.html";
}

const alertBox = document.getElementById("alertBox");
const logoutBtn = document.getElementById("logoutBtn");
const tableBody = document.getElementById("buildingsTableBody");
const addBuildingBtn = document.getElementById("addBuildingBtn");

const modal = document.getElementById("buildingModal");
const modalTitle = document.getElementById("buildingModalTitle");
const modalAlert = document.getElementById("buildingModalAlert");
const modalClose = document.getElementById("buildingModalClose");
const modalCancelBtn = document.getElementById("buildingModalCancelBtn");
const form = document.getElementById("buildingForm");
const nameInput = document.getElementById("buildingName");
const addressInput = document.getElementById("buildingAddress");
const submitBtn = document.getElementById("buildingModalSubmitBtn");

let buildings = [];
let editingId = null;

function escapeHtml(str) {
  const div = document.createElement("div");
  div.textContent = str ?? "";
  return div.innerHTML;
}

function openModal(building) {
  editingId = building ? building.id : null;
  modalTitle.textContent = building ? "Binanı redaktə et" : "Yeni bina";
  submitBtn.textContent = building ? "Yadda saxla" : "Yarat";
  nameInput.value = building ? building.name : "";
  addressInput.value = building ? building.address : "";
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

addBuildingBtn.addEventListener("click", () => openModal(null));

form.addEventListener("submit", async (e) => {
  e.preventDefault();
  modalAlert.innerHTML = "";

  const dto = { Name: nameInput.value.trim(), Address: addressInput.value.trim() };

  submitBtn.disabled = true;
  submitBtn.textContent = "Yadda saxlanılır...";

  try {
    if (editingId) {
      await updateBuilding(editingId, dto);
    } else {
      await createBuilding(dto);
    }
    closeModal();
    await loadBuildings();
  } catch (err) {
    modalAlert.innerHTML = `<div class="alert alert-danger"><i class="bi bi-exclamation-circle"></i><span>${escapeHtml(err.message)}</span></div>`;
  } finally {
    submitBtn.disabled = false;
    submitBtn.textContent = editingId ? "Yadda saxla" : "Yarat";
  }
});

function renderBuildings() {
  if (!buildings.length) {
    tableBody.innerHTML = `<tr><td colspan="3" class="text-muted" style="text-align:center; padding:2rem;">Bina tapılmadı.</td></tr>`;
    return;
  }
  tableBody.innerHTML = buildings
    .map(
      (b) => `
    <tr>
      <td>${escapeHtml(b.name)}</td>
      <td>${escapeHtml(b.address)}</td>
      <td>
        <div class="row-actions">
          <button class="icon-action-btn edit-btn" data-id="${b.id}" title="Redaktə et"><i class="bi bi-pencil"></i></button>
          <button class="icon-action-btn delete-btn" data-id="${b.id}" title="Sil"><i class="bi bi-trash"></i></button>
        </div>
      </td>
    </tr>`
    )
    .join("");

  tableBody.querySelectorAll(".edit-btn").forEach((btn) => {
    btn.addEventListener("click", () => {
      const b = buildings.find((x) => x.id === btn.dataset.id);
      if (b) openModal(b);
    });
  });

  tableBody.querySelectorAll(".delete-btn").forEach((btn) => {
    btn.addEventListener("click", async () => {
      if (!confirm("Bu binanı silmək istədiyinizə əminsiniz?")) return;
      clearAlert(alertBox);
      try {
        await deleteBuilding(btn.dataset.id);
        await loadBuildings();
      } catch (err) {
        showAlert(alertBox, err.message);
      }
    });
  });
}

async function loadBuildings() {
  clearAlert(alertBox);
  try {
    buildings = await getAllBuildings();
    renderBuildings();
  } catch (err) {
    showAlert(alertBox, err.message);
  }
}

logoutBtn.addEventListener("click", async () => {
  await logout();
  window.location.href = "login.html";
});

loadBuildings();