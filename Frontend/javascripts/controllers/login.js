import { userLogin } from "../api/authApi.js";
import { showAlert, clearAlert, showFieldErrors, clearFieldErrors } from "../components/alert.js";

const form = document.getElementById("loginForm");
const alertBox = document.getElementById("alertBox");
const submitBtn = document.getElementById("submitBtn");
const toggleEye = document.getElementById("toggleEye");
const passwordInput = document.getElementById("password");

toggleEye.addEventListener("click", () => {
  const isPassword = passwordInput.type === "password";
  passwordInput.type = isPassword ? "text" : "password";
  toggleEye.innerHTML = `<i class="bi bi-${isPassword ? "eye-slash" : "eye"}"></i>`;
});

const params = new URLSearchParams(window.location.search);
if (params.get("confirmed") === "1") {
  showAlert(alertBox, "Email təsdiqləndi. İndi daxil ola bilərsiniz.", "success");
} else if (params.get("reset") === "1") {
  showAlert(alertBox, "Şifrəniz yeniləndi. İndi daxil ola bilərsiniz.", "success");
}

form.addEventListener("submit", async (e) => {
  e.preventDefault();
  clearAlert(alertBox);
  clearFieldErrors(form);

  const dto = {
    Email: form.Email.value.trim(),
    Password: form.Password.value,
    RememberMe: form.RememberMe.checked,
  };

  submitBtn.disabled = true;
  submitBtn.innerHTML = "Daxil olunur...";

  try {
    await userLogin(dto);
    window.location.href = "dashboard.html";
  } catch (err) {
    if (err.fieldErrors) {
      showFieldErrors(form, err.fieldErrors);
    }
    showAlert(alertBox, err.message);
  } finally {
    submitBtn.disabled = false;
    submitBtn.innerHTML = 'Daxil ol <i class="bi bi-arrow-right"></i>';
  }
});