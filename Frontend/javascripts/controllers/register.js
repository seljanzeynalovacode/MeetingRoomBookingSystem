import { register } from "../api/authApi.js";
import { showAlert, clearAlert, showFieldErrors, clearFieldErrors } from "../components/alert.js";

const form = document.getElementById("registerForm");
const alertBox = document.getElementById("alertBox");
const submitBtn = document.getElementById("submitBtn");

form.addEventListener("submit", async (e) => {
  e.preventDefault();
  clearAlert(alertBox);
  clearFieldErrors(form);

  const dto = {
    FirstName: form.FirstName.value.trim(),
    LastName: form.LastName.value.trim(),
    UserName: form.Username.value.trim(),
    Email: form.Email.value.trim(),
    Password: form.Password.value,
    ConfirmPassword: form.ConfirmPassword.value,
  };

  submitBtn.disabled = true;
  submitBtn.innerHTML = "Göndərilir...";

  try {
    await register(dto);
    window.location.href = `confirmEmail.html?email=${encodeURIComponent(dto.Email)}`;
  } catch (err) {
    if (err.fieldErrors) {
      showFieldErrors(form, err.fieldErrors);
    }
    showAlert(alertBox, err.message);
  } finally {
    submitBtn.disabled = false;
    submitBtn.innerHTML = 'Qeydiyyatdan keç <i class="bi bi-arrow-right"></i>';
  }
});