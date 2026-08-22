import { requireAdmin } from "../components/adminGuard.js";
import { logout } from "../api/authApi.js";
import {
  getAllBuildings,
  createBuilding,
  updateBuilding,
  deleteBuilding,
  getAllFloors,
  createFloor,
  updateFloor,
  deleteFloor,
} from "../api/roomsApi.js";
import { showAlert, clearAlert } from "../components/alert.js";

requireAdmin();

const alertBox = document.getElementById("alertBox");
const logoutBtn = document.getElementById("logoutBtn");
const buildingsGrid = document.getElementById("buildingsGrid");
const buildingSearchInput = document.getElementById("buildingSearchInput");
const resetSearchBtn = document.getElementById("resetSearchBtn");
buildingSearchInput.addEventListener("input", renderBuildings);
resetSearchBtn.addEventListener("click", () => {
  buildingSearchInput.value = "";
  renderBuildings();
});
const addBuildingBtn = document.getElementById("addBuildingBtn");
const addFloorBtn = document.getElementById("addFloorBtn");

const buildingModal = document.getElementById("buildingModal");
const buildingModalTitle = document.getElementById("buildingModalTitle");
const buildingModalAlert = document.getElementById("buildingModalAlert");
const buildingModalClose = document.getElementById("buildingModalClose");
const buildingModalCancelBtn = document.getElementById("buildingModalCancelBtn");
const buildingForm = document.getElementById("buildingForm");
const buildingNameInput = document.getElementById("buildingName");
const buildingAddressInput = document.getElementById("buildingAddress");
const buildingSubmitBtn = document.getElementById("buildingModalSubmitBtn");

const floorModal = document.getElementById("floorModal");
const floorModalTitle = document.getElementById("floorModalTitle");
const floorModalAlert = document.getElementById("floorModalAlert");
const floorModalClose = document.getElementById("floorModalClose");
const floorModalCancelBtn = document.getElementById("floorModalCancelBtn");
const floorForm = document.getElementById("floorForm");
const floorBuildingSelect = document.getElementById("floorBuilding");
const floorNameInput = document.getElementById("floorName");
const floorLevelInput = document.getElementById("floorLevel");
const floorSubmitBtn = document.getElementById("floorModalSubmitBtn");

let buildings = [];
let floors = [];
let editingBuildingId = null;
let editingFloorId = null;

const ORDINAL_SUFFIXES = {
  1: "ci", 2: "ci", 3: "cü", 4: "cü", 5: "ci", 6: "cı", 7: "ci", 8: "ci", 9: "cu", 10: "cu",
  11: "ci", 12: "ci", 13: "cü", 14: "cü", 15: "ci", 16: "cı", 17: "ci", 18: "ci", 19: "cu", 20: "ci",
};

function suggestFloorName(nextLevel) {
  const suffix = ORDINAL_SUFFIXES[nextLevel] || "ci";
  return `${nextLevel}-${suffix} mərtəbə`;
}

function getNextLevelForBuilding(buildingId) {
  const buildingFloors = floors.filter((f) => f.buildingId === buildingId);
  if (!buildingFloors.length) return 1;
  return Math.max(...buildingFloors.map((f) => f.levelNumber)) + 1;
}

function applySuggestedFloorDefaults() {
  if (editingFloorId) return;
  const buildingId = floorBuildingSelect.value;
  if (!buildingId) return;
  const nextLevel = getNextLevelForBuilding(buildingId);
  floorNameInput.value = suggestFloorName(nextLevel);
  floorLevelInput.value = nextLevel;
}

floorBuildingSelect.addEventListener("change", applySuggestedFloorDefaults);

function escapeHtml(str) {
  const div = document.createElement("div");
  div.textContent = str ?? "";
  return div.innerHTML;
}

/* ---------- Building modal ---------- */

function openBuildingModal(building) {
  editingBuildingId = building ? building.id : null;
  buildingModalTitle.textContent = building ? "Binanı redaktə et" : "Yeni bina";
  buildingSubmitBtn.textContent = building ? "Yadda saxla" : "Yarat";
  buildingNameInput.value = building ? building.name : "";
  buildingAddressInput.value = building ? building.address : "";
  buildingModalAlert.innerHTML = "";
  buildingModal.classList.add("open");
  buildingNameInput.focus();
}

function closeBuildingModal() {
  buildingModal.classList.remove("open");
}

buildingModalClose.addEventListener("click", closeBuildingModal);
buildingModalCancelBtn.addEventListener("click", closeBuildingModal);
buildingModal.addEventListener("click", (e) => {
  if (e.target === buildingModal) closeBuildingModal();
});
addBuildingBtn.addEventListener("click", () => openBuildingModal(null));

buildingForm.addEventListener("submit", async (e) => {
  e.preventDefault();
  buildingModalAlert.innerHTML = "";

  const dto = { Name: buildingNameInput.value.trim(), Address: buildingAddressInput.value.trim() };

  buildingSubmitBtn.disabled = true;
  buildingSubmitBtn.textContent = "Yadda saxlanılır...";

  try {
    if (editingBuildingId) {
      await updateBuilding(editingBuildingId, dto);
    } else {
      await createBuilding(dto);
    }
    closeBuildingModal();
    await loadData();
  } catch (err) {
    buildingModalAlert.innerHTML = `<div class="alert alert-danger"><i class="bi bi-exclamation-circle"></i><span>${escapeHtml(err.message)}</span></div>`;
  } finally {
    buildingSubmitBtn.disabled = false;
    buildingSubmitBtn.textContent = editingBuildingId ? "Yadda saxla" : "Yarat";
  }
});

/* ---------- Floor modal ---------- */

function openFloorModal(floor) {
  editingFloorId = floor ? floor.id : null;
  floorModalTitle.textContent = floor ? "Mərtəbəni redaktə et" : "Yeni mərtəbə";
  floorSubmitBtn.textContent = floor ? "Yadda saxla" : "Yarat";

  if (floor) {
    floorBuildingSelect.value = floor.buildingId;
    floorNameInput.value = floor.name;
    floorLevelInput.value = floor.levelNumber;
  } else {
    floorBuildingSelect.value = buildings[0]?.id || "";
    applySuggestedFloorDefaults();
  }

  floorModalAlert.innerHTML = "";
  floorModal.classList.add("open");
  floorNameInput.focus();
  floorNameInput.select();
}

function closeFloorModal() {
  floorModal.classList.remove("open");
}

floorModalClose.addEventListener("click", closeFloorModal);
floorModalCancelBtn.addEventListener("click", closeFloorModal);
floorModal.addEventListener("click", (e) => {
  if (e.target === floorModal) closeFloorModal();
});
addFloorBtn.addEventListener("click", () => openFloorModal(null));

floorForm.addEventListener("submit", async (e) => {
  e.preventDefault();
  floorModalAlert.innerHTML = "";

  const dto = {
    Name: floorNameInput.value.trim(),
    LevelNumber: Number(floorLevelInput.value),
    BuildingId: floorBuildingSelect.value,
  };

  floorSubmitBtn.disabled = true;
  floorSubmitBtn.textContent = "Yadda saxlanılır...";

  try {
    if (editingFloorId) {
      await updateFloor(editingFloorId, dto);
    } else {
      await createFloor(dto);
    }
    closeFloorModal();
    await loadData();
  } catch (err) {
    floorModalAlert.innerHTML = `<div class="alert alert-danger"><i class="bi bi-exclamation-circle"></i><span>${escapeHtml(err.message)}</span></div>`;
  } finally {
    floorSubmitBtn.disabled = false;
    floorSubmitBtn.textContent = editingFloorId ? "Yadda saxla" : "Yarat";
  }
});

/* ---------- Rendering ---------- */

function createBuildingCardHtml(building, buildingFloors) {
  const floorsHtml = buildingFloors.length
    ? buildingFloors
        .map(
          (f) => `
        <div class="floor-row">
          <span class="floor-name"><span class="floor-level-dot"></span>${escapeHtml(f.name)}</span>
          <div class="floor-row-actions">
            <button class="edit-floor-btn" data-id="${f.id}" title="Redaktə et"><i class="bi bi-pencil"></i></button>
            <button class="danger delete-floor-btn" data-id="${f.id}" title="Sil"><i class="bi bi-trash"></i></button>
          </div>
        </div>`
        )
        .join("")
    : `<div class="floors-empty">Mərtəbə yoxdur</div>`;

  return `
    <div class="building-card">
      <div class="building-roof"></div>
      <div class="building-header">
        <div>
          <h3>${escapeHtml(building.name)}</h3>
          <p>${escapeHtml(building.address)}</p>
        </div>
        <div class="building-header-actions">
          <button class="icon-action-btn edit-building-btn" data-id="${building.id}" title="Redaktə et"><i class="bi bi-pencil"></i></button>
          <button class="icon-action-btn delete-building-btn" data-id="${building.id}" title="Sil"><i class="bi bi-trash"></i></button>
        </div>
      </div>
      <div class="floors-list">${floorsHtml}</div>
      <div class="building-base"><span class="window"></span><span class="window"></span></div>
    </div>`;
}

function renderBuildings() {
  if (!buildings.length) {
    buildingsGrid.innerHTML = `<div class="empty-state" style="width:100%;"><i class="bi bi-building"></i>Bina tapılmadı.</div>`;
    return;
  }

  const searchTerm = buildingSearchInput.value.trim().toLocaleLowerCase("az");
   const matchedBuildings = searchTerm
    ? buildings.filter(
        (b) =>
          b.name.toLocaleLowerCase("az").includes(searchTerm) ||
          b.address.toLocaleLowerCase("az").includes(searchTerm)
      )
    : buildings;

  if (!matchedBuildings.length) {
    buildingsGrid.innerHTML = `<div class="empty-state" style="width:100%;"><i class="bi bi-search"></i>Axtarışa uyğun bina tapılmadı.</div>`;
    return;
  }

  const sortedBuildings = [...matchedBuildings].sort((a, b) => {
    const countA = floors.filter((f) => f.buildingId === a.id).length;
    const countB = floors.filter((f) => f.buildingId === b.id).length;
    return countB - countA;
  });

  buildingsGrid.innerHTML = sortedBuildings
    .map((b) => {
      const bFloors = floors.filter((f) => f.buildingId === b.id).sort((a, c) => c.levelNumber - a.levelNumber);
      return createBuildingCardHtml(b, bFloors);
    })
    .join("");

  buildingsGrid.querySelectorAll(".edit-building-btn").forEach((btn) => {
    btn.addEventListener("click", () => {
      const b = buildings.find((x) => x.id === btn.dataset.id);
      if (b) openBuildingModal(b);
    });
  });

  buildingsGrid.querySelectorAll(".delete-building-btn").forEach((btn) => {
    btn.addEventListener("click", async () => {
      if (!confirm("Bu binanı silmək istədiyinizə əminsiniz?")) return;
      clearAlert(alertBox);
      try {
        await deleteBuilding(btn.dataset.id);
        await loadData();
      } catch (err) {
        showAlert(alertBox, err.message);
      }
    });
  });

  buildingsGrid.querySelectorAll(".edit-floor-btn").forEach((btn) => {
    btn.addEventListener("click", () => {
      const f = floors.find((x) => x.id === btn.dataset.id);
      if (f) openFloorModal(f);
    });
  });

  buildingsGrid.querySelectorAll(".delete-floor-btn").forEach((btn) => {
    btn.addEventListener("click", async () => {
      if (!confirm("Bu mərtəbəni silmək istədiyinizə əminsiniz?")) return;
      clearAlert(alertBox);
      try {
        await deleteFloor(btn.dataset.id);
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
    const [buildingList, floorList] = await Promise.all([getAllBuildings(), getAllFloors()]);
    buildings = buildingList;
    floors = floorList;

    floorBuildingSelect.innerHTML = buildings.map((b) => `<option value="${b.id}">${escapeHtml(b.name)}</option>`).join("");

    renderBuildings();
  } catch (err) {
    showAlert(alertBox, err.message);
  }
}

logoutBtn.addEventListener("click", async () => {
  await logout();
  window.location.href = "adminLogin.html";
});

loadData();