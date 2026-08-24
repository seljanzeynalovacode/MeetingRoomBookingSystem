import { isLoggedIn, getAccessToken, refreshAccessToken } from "../api/apiClient.js";
import { updateProfile, changePassword, logout } from "../api/authApi.js";
import { showAlert, clearAlert } from "../components/alert.js";

if (!isLoggedIn()) {
  window.location.href = "login.html";
}

const alertBox = document.getElementById("alertBox");

const profileForm = document.getElementById("profileForm");
const firstNameInput = document.getElementById("firstNameInput");
const lastNameInput = document.getElementById("lastNameInput");
const userNameInput = document.getElementById("userNameInput");
const profileSubmitBtn = document.getElementById("profileSubmitBtn");

const passwordForm = document.getElementById("passwordForm");
const currentPasswordInput = document.getElementById("currentPasswordInput");
const newPasswordInput = document.getElementById("newPasswordInput");
const confirmNewPasswordInput = document.getElementById("confirmNewPasswordInput");
const passwordSubmitBtn = document.getElementById("passwordSubmitBtn");

const avatarChip = document.getElementById("avatarChip");
const avatarMenu = document.getElementById("avatarMenu");
const avatarInitials = document.getElementById("avatarInitials");
const avatarName = document.getElementById("avatarName");
const logoutBtn = document.getElementById("logoutBtn");

const profileHeroAvatar = document.getElementById("profileHeroAvatar");
const profileHeroName = document.getElementById("profileHeroName");
const profileHeroEmail = document.getElementById("profileHeroEmail");

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

function updateAvatarDisplay(firstName, lastName) {
  const name = `${firstName} ${lastName}`.trim();
  const initials = firstName && lastName ? (firstName[0] + lastName[0]).toUpperCase() : name.slice(0, 2).toUpperCase();

  avatarName.textContent = name || "İstifadəçi";
  avatarInitials.textContent = initials;

  profileHeroAvatar.textContent = initials;
  profileHeroName.textContent = name || "İstifadəçi";
}

function prefillFromToken() {
  const claims = parseJwt(getAccessToken()) || {};
  const firstName = claims["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname"] || "";
  const lastName = claims["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname"] || "";
  const userName = claims["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] || "";
  const email = claims["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] || "";
  firstNameInput.value = firstName;
  lastNameInput.value = lastName;
  userNameInput.value = userName;
  profileHeroEmail.textContent = email;
  updateAvatarDisplay(firstName, lastName);
}

profileForm.addEventListener("submit", async (e) => {
  e.preventDefault();
  clearAlert(alertBox);

  const firstName = firstNameInput.value.trim();
  const lastName = lastNameInput.value.trim();

  profileSubmitBtn.disabled = true;
  profileSubmitBtn.textContent = "Yadda saxlanılır...";

  try {
    await updateProfile({ FirstName: firstName, LastName: lastName, UserName: userNameInput.value.trim() });
    await refreshAccessToken();
    updateAvatarDisplay(firstName, lastName);
    showAlert(alertBox, "Profil məlumatları yeniləndi.", "success");
  } catch (err) {
    showAlert(alertBox, err.message);
  } finally {
    profileSubmitBtn.disabled = false;
    profileSubmitBtn.textContent = "Yadda saxla";
  }
});

passwordForm.addEventListener("submit", async (e) => {
  e.preventDefault();
  clearAlert(alertBox);

  if (newPasswordInput.value !== confirmNewPasswordInput.value) {
    showAlert(alertBox, "Yeni şifrələr uyğun gəlmir.", "warning");
    return;
  }

  passwordSubmitBtn.disabled = true;
  passwordSubmitBtn.textContent = "Yadda saxlanılır...";

  try {
    await changePassword({
      CurrentPassword: currentPasswordInput.value,
      NewPassword: newPasswordInput.value,
      ConfirmNewPassword: confirmNewPasswordInput.value,
    });
    passwordForm.reset();
    showAlert(alertBox, "Şifrə uğurla dəyişdirildi.", "success");
  } catch (err) {
    showAlert(alertBox, err.message);
  } finally {
    passwordSubmitBtn.disabled = false;
    passwordSubmitBtn.textContent = "Şifrəni dəyiş";
  }
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

prefillFromToken();