import { forgotPassword } from "../api/authApi.js";
import { showAlert, clearAlert, showFieldErrors, clearFieldErrors } from "../components/alert.js";

const form = document.getElementById("forgotForm");
const alertBox = document.getElementById("alertBox");
const submitBtn = document.getElementById("submitBtn");

form.addEventListener("submit", async (e) => {
  e.preventDefault();
  clearAlert(alertBox);
  clearFieldErrors(form);

  const email = form.Email.value.trim();

  submitBtn.disabled = true;
  submitBtn.innerHTML = "Göndərilir...";

  try {
    await forgotPassword({ Email: email });
    showAlert(alertBox, "Əgər bu email mövcuddursa, bərpa kodu göndərildi. Yönləndirilirsiniz...", "success");
    setTimeout(() => {
      window.location.href = `adminResetPassword.html?email=${encodeURIComponent(email)}`;
    }, 1400);
  } catch (err) {
    if (err.fieldErrors) showFieldErrors(form, err.fieldErrors);
    showAlert(alertBox, err.message);
    submitBtn.disabled = false;
    submitBtn.innerHTML = 'Bərpa kodu göndər <i class="bi bi-arrow-right"></i>';
  }
});
