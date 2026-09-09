import { requireAdmin, getDisplayIdentity } from "../components/adminGuard.js";
import { initAdminMobileNav, initAdminAccountMenu } from "../components/mobileNav.js";
import { getAllEquipment, createEquipment, updateEquipment, deleteEquipment } from "../api/roomsApi.js";
import { logout } from "../api/authApi.js";
import { showAlert, clearAlert } from "../components/alert.js";
import { escapeHtml } from "../utils/escapeHtml.js";
import { initThemeToggle } from "../components/theme.js";
import { t } from "../components/i18n.js";
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
const equipmentGrid = document.getElementById("equipmentGrid");
const addEquipmentBtn = document.getElementById("addEquipmentBtn");
const equipmentSearchInput = document.getElementById("equipmentSearchInput");
const resetSearchBtn = document.getElementById("resetSearchBtn");

equipmentSearchInput.addEventListener("input", renderEquipment);
resetSearchBtn.addEventListener("click", () => {
  equipmentSearchInput.value = "";
  renderEquipment();
});

const modal = document.getElementById("equipmentModal");
const modalTitle = document.getElementById("equipmentModalTitle");
const modalAlert = document.getElementById("equipmentModalAlert");
const modalClose = document.getElementById("equipmentModalClose");
const modalCancelBtn = document.getElementById("equipmentModalCancelBtn");
const form = document.getElementById("equipmentForm");
const nameInput = document.getElementById("equipmentName");
const submitBtn = document.getElementById("equipmentModalSubmitBtn");

let equipment = [];
const EQUIPMENT_ICON_RULES = [
  { keywords: ["konfrans telefonu", "konfrans telefon"], icon: "bi-mic" },
  { keywords: ["ağıllı lövhə", "agilli lovhe", "smart board", "smartboard"], icon: "bi-easel2" },
  { keywords: ["proyektor", "projector"], icon: "bi-projector" },
  { keywords: ["lövhə", "lovhe", "whiteboard", "flipchart"], icon: "bi-easel" },
  { keywords: ["video", "kamera", "camera"], icon: "bi-camera-video" },
  { keywords: ["tv", "ekran", "monitor", "display"], icon: "bi-tv" },
  { keywords: ["mikrofon", "microphone", "mic"], icon: "bi-mic" },
  { keywords: ["səsucaldan", "sesucaldan", "hoparlör", "hoparlor", "speaker"], icon: "bi-volume-up" },
  { keywords: ["noutbuk", "laptop", "kompüter", "komputer", "computer"], icon: "bi-laptop" },
  { keywords: ["telefon", "phone"], icon: "bi-telephone" },
  { keywords: ["printer", "printr", "çap"], icon: "bi-printer" },
  { keywords: ["wifi", "internet", "router"], icon: "bi-wifi" },
  { keywords: ["kondisioner", "isitme", "isitmə"], icon: "bi-snow" },
  { keywords: ["qulaqlıq", "qulaqliq", "headset", "headphone"], icon: "bi-headset" },
  { keywords: ["kabel", "hdmi", "usb"], icon: "bi-usb-plug" },
];

function getEquipmentIcon(name) {
  const lower = name.toLocaleLowerCase("az");
  for (const rule of EQUIPMENT_ICON_RULES) {
    if (rule.keywords.some((kw) => lower.includes(kw))) {
      return rule.icon;
    }
  }
  return "bi-box-seam";
}
let editingId = null;
function openModal(item) {
  editingId = item ? item.id : null;
  modalTitle.textContent = item ? t("adminequipment.edit") : t("adminequipment.add");
  submitBtn.textContent = item ? t("profile.save") : t("common.create");
  nameInput.value = item ? item.name : "";
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

addEquipmentBtn.addEventListener("click", () => openModal(null));

form.addEventListener("submit", async (e) => {
  e.preventDefault();
  modalAlert.innerHTML = "";

  const dto = { Name: nameInput.value.trim() };

  submitBtn.disabled = true;
  submitBtn.textContent = t("common.saving");

  try {
    if (editingId) {
      await updateEquipment(editingId, dto);
    } else {
      await createEquipment(dto);
    }
    closeModal();
    await loadEquipment();
  } catch (err) {
    showAlert(modalAlert, err.message);
  } finally {
    submitBtn.disabled = false;
    submitBtn.textContent = editingId ? t("profile.save") : t("common.create");
  }
});

function createEquipmentCardHtml(item) {
  const icon = getEquipmentIcon(item.name);
  return `
    <div class="equipment-card">
      <div class="equipment-icon"><i class="bi ${icon}"></i></div>
      <div class="equipment-name">${escapeHtml(translateEquipmentName(item.name))}</div>
      <div class="equipment-actions">
        <button class="icon-action-btn edit-equipment-btn" data-id="${item.id}" title="${t("common.edit")}"><i class="bi bi-pencil"></i></button>
        <button class="icon-action-btn delete-equipment-btn" data-id="${item.id}" title="${t("common.delete")}"><i class="bi bi-trash"></i></button>
      </div>
    </div>`;
}

function renderEquipment() {
  if (!equipment.length) {
    equipmentGrid.innerHTML = `<div class="empty-state" style="width:100%;"><i class="bi bi-cpu"></i>${t("adminequipment.not_found")}</div>`;
    return;
  }

  const searchTerm = equipmentSearchInput.value.trim().toLocaleLowerCase("az");
  const matched = searchTerm
    ? equipment.filter((eq) => eq.name.toLocaleLowerCase("az").includes(searchTerm))
    : equipment;

  if (!matched.length) {
    equipmentGrid.innerHTML = `<div class="empty-state" style="width:100%;"><i class="bi bi-search"></i>${t("adminequipment.no_search_results")}</div>`;
    return;
  }

 equipmentGrid.innerHTML = matched.map(createEquipmentCardHtml).join("");

  equipmentGrid.querySelectorAll(".edit-equipment-btn").forEach((btn) => {
    btn.addEventListener("click", () => {
      const item = equipment.find((x) => x.id === btn.dataset.id);
      if (item) openModal(item);
    });
  });

  equipmentGrid.querySelectorAll(".delete-equipment-btn").forEach((btn) => {
    btn.addEventListener("click", async () => {
      if (!confirm(t("adminequipment.confirm_delete"))) return;
      clearAlert(alertBox);
      try {
        await deleteEquipment(btn.dataset.id);
        await loadEquipment();
      } catch (err) {
        showAlert(alertBox, err.message);
      }
    });
  });
}

async function loadEquipment() {
  clearAlert(alertBox);
  try {
    equipment = await getAllEquipment();
    renderEquipment();
  } catch (err) {
    showAlert(alertBox, err.message);
  }
}

logoutBtn.addEventListener("click", async () => {
  await logout();
  window.location.href = "adminLogin.html";
});

loadEquipment();
