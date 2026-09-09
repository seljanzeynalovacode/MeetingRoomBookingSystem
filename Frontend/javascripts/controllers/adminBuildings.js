import { requireAdmin, getDisplayIdentity } from "../components/adminGuard.js";
import { initAdminMobileNav, initAdminAccountMenu } from "../components/mobileNav.js";
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
import { initDropdown } from "../components/customDropdown.js";
import { escapeHtml } from "../utils/escapeHtml.js";
import { initThemeToggle } from "../components/theme.js";
import { t } from "../components/i18n.js";
import { floorLabel } from "../utils/floorLabel.js";

requireAdmin();
initAdminMobileNav();
initThemeToggle(document.getElementById("themeToggleBtn"));
initAdminAccountMenu();

{
  const { initials, name } = getDisplayIdentity();
  const adminAccountInitials = document.getElementById("adminAccountInitials");
  const adminAccountName = document.getElementById("adminAccountName");
  if (adminAccountInitials) adminAccountInitials.textContent = initials;
  if (adminAccountName) adminAccountName.textContent = name;
}

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
const floorLevelInput = document.getElementById("floorLevel");
const floorNamePreview = document.getElementById("floorNamePreview");
const floorSubmitBtn = document.getElementById("floorModalSubmitBtn");

const floorBuildingDropdown = initDropdown({
  btn: document.getElementById("floorBuildingBtn"),
  panel: document.getElementById("floorBuildingPanel"),
  options: [],
  placeholder: t("common.select"),
  onChange: () => applySuggestedFloorDefaults(),
});

let buildings = [];
let floors = [];
let editingBuildingId = null;
let editingFloorId = null;

function updateFloorNamePreview() {
  const level = floorLevelInput.value;
  floorNamePreview.textContent = level !== "" ? floorLabel(level) : "";
}

function getNextLevelForBuilding(buildingId) {
  const buildingFloors = floors.filter((f) => f.buildingId === buildingId);
  if (!buildingFloors.length) return 1;
  return Math.max(...buildingFloors.map((f) => f.levelNumber)) + 1;
}

function applySuggestedFloorDefaults() {
  if (editingFloorId) return;
  const buildingId = floorBuildingDropdown.getValue();
  if (!buildingId) return;
  floorLevelInput.value = getNextLevelForBuilding(buildingId);
  updateFloorNamePreview();
}

floorLevelInput.addEventListener("input", updateFloorNamePreview);
/* ---------- Building modal ---------- */

function openBuildingModal(building) {
  editingBuildingId = building ? building.id : null;
  buildingModalTitle.textContent = building ? t("adminbuildings.edit_building") : t("adminbuildings.add_building");
  buildingSubmitBtn.textContent = building ? t("profile.save") : t("common.create");
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
  buildingSubmitBtn.textContent = t("common.saving");

  try {
    if (editingBuildingId) {
      await updateBuilding(editingBuildingId, dto);
    } else {
      await createBuilding(dto);
    }
    closeBuildingModal();
    await loadData();
  } catch (err) {
    showAlert(buildingModalAlert, err.message);
  } finally {
    buildingSubmitBtn.disabled = false;
    buildingSubmitBtn.textContent = editingBuildingId ? t("profile.save") : t("common.create");
  }
});

/* ---------- Floor modal ---------- */

function openFloorModal(floor) {
  editingFloorId = floor ? floor.id : null;
  floorModalTitle.textContent = floor ? t("adminbuildings.edit_floor") : t("adminbuildings.add_floor");
  floorSubmitBtn.textContent = floor ? t("profile.save") : t("common.create");

  if (floor) {
    floorBuildingDropdown.setValue(floor.buildingId);
    floorLevelInput.value = floor.levelNumber;
    updateFloorNamePreview();
  } else {
    floorBuildingDropdown.setValue(buildings[0]?.id || "");
    applySuggestedFloorDefaults();
  }

  floorModalAlert.innerHTML = "";
  floorModal.classList.add("open");
  floorLevelInput.focus();
  floorLevelInput.select();
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

  const level = Number(floorLevelInput.value);
  const dto = {
    Name: floorLabel(level),
    LevelNumber: level,
    BuildingId: floorBuildingDropdown.getValue(),
  };

  floorSubmitBtn.disabled = true;
  floorSubmitBtn.textContent = t("common.saving");

  try {
    if (editingFloorId) {
      await updateFloor(editingFloorId, dto);
    } else {
      await createFloor(dto);
    }
    closeFloorModal();
    await loadData();
  } catch (err) {
    showAlert(floorModalAlert, err.message);
  } finally {
    floorSubmitBtn.disabled = false;
    floorSubmitBtn.textContent = editingFloorId ? t("profile.save") : t("common.create");
  }
});

/* ---------- Rendering ---------- */

function createBuildingCardHtml(building, buildingFloors) {
  const floorsHtml = buildingFloors.length
    ? buildingFloors
        .map(
          (f) => `
        <div class="floor-row">
          <span class="floor-name"><span class="floor-level-dot"></span>${escapeHtml(floorLabel(f.levelNumber))}</span>
          <div class="floor-row-actions">
            <button class="edit-floor-btn" data-id="${f.id}" title="${t("common.edit")}"><i class="bi bi-pencil"></i></button>
            <button class="danger delete-floor-btn" data-id="${f.id}" title="${t("common.delete")}"><i class="bi bi-trash"></i></button>
          </div>
        </div>`
        )
        .join("")
    : `<div class="floors-empty">${t("adminbuildings.no_floors")}</div>`;

  return `
    <div class="building-card">
      <div class="building-roof"></div>
      <div class="building-header">
        <div>
          <h3>${escapeHtml(building.name)}</h3>
          <p>${escapeHtml(building.address)}</p>
        </div>
        <div class="building-header-actions">
          <button class="icon-action-btn edit-building-btn" data-id="${building.id}" title="${t("common.edit")}"><i class="bi bi-pencil"></i></button>
          <button class="icon-action-btn delete-building-btn" data-id="${building.id}" title="${t("common.delete")}"><i class="bi bi-trash"></i></button>
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
      if (!confirm(t("adminbuildings.confirm_delete_building"))) return;
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
      if (!confirm(t("adminbuildings.confirm_delete_floor"))) return;
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

    floorBuildingDropdown.setOptions(buildings.map((b) => ({ value: b.id, label: b.name })));

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