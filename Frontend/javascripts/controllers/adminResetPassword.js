import { resetPassword } from "../api/authApi.js";
import { showAlert, clearAlert, showFieldErrors, clearFieldErrors } from "../components/alert.js";

const form = document.getElementById("resetForm");
const alertBox = document.getElementById("alertBox");
const submitBtn = document.getElementById("submitBtn");
const emailInput = document.getElementById("email");
const otpRow = document.getElementById("otpRow");
const otpBoxes = Array.from(otpRow.querySelectorAll(".otp-box"));

const passwordSection = document.getElementById("passwordSection");
const newPasswordInput = document.getElementById("newPassword");
const confirmNewPasswordInput = document.getElementById("confirmNewPassword");

const params = new URLSearchParams(window.location.search);
const prefillEmail = params.get("email");
if (prefillEmail) emailInput.value = prefillEmail;

function getCode() {
  return otpBoxes.map((b) => b.value).join("");
}

function shakeOtp() {
  otpRow.classList.remove("shake");
  void otpRow.offsetWidth;
  otpRow.classList.add("shake");
}

function updateLockState() {
  const isComplete = getCode().length === 6;
  passwordSection.classList.toggle("is-locked", !isComplete);
  newPasswordInput.disabled = !isComplete;
  confirmNewPasswordInput.disabled = !isComplete;
  submitBtn.disabled = !isComplete;
}

otpBoxes.forEach((box, idx) => {
  box.addEventListener("input", () => {
    box.value = box.value.replace(/\D/g, "");
    if (box.value && otpBoxes[idx + 1]) otpBoxes[idx + 1].focus();
    updateLockState();
  });

  box.addEventListener("keydown", (e) => {
    if (e.key === "Backspace" && !box.value && otpBoxes[idx - 1]) {
      otpBoxes[idx - 1].focus();
    }
    updateLockState();
  });

  box.addEventListener("paste", (e) => {
    const text = (e.clipboardData || window.clipboardData).getData("text").replace(/\D/g, "");
    if (!text) return;
    e.preventDefault();
    text.split("").slice(0, otpBoxes.length).forEach((digit, i) => {
      if (otpBoxes[i]) otpBoxes[i].value = digit;
    });
    const next = otpBoxes[Math.min(text.length, otpBoxes.length - 1)];
    if (next) next.focus();
    updateLockState();
  });
});

form.addEventListener("submit", async (e) => {
  e.preventDefault();
  clearAlert(alertBox);
  clearFieldErrors(form);

  const email = emailInput.value.trim();
  const code = getCode();

  if (!email) {
    showAlert(alertBox, "Email daxil edin.", "warning");
    return;
  }

  if (code.length !== 6) {
    showAlert(alertBox, "6 rəqəmli kodu tam daxil edin.", "warning");
    shakeOtp();
    return;
  }

  const dto = {
    Email: email,
    Token: code,
    NewPassword: form.NewPassword.value,
    ConfirmNewPassword: form.ConfirmNewPassword.value,
  };

  submitBtn.disabled = true;
  submitBtn.innerHTML = "Yenilənir...";

  try {
    await resetPassword(dto);
    window.location.href = "adminLogin.html?reset=1";
  } catch (err) {
    if (err.fieldErrors) showFieldErrors(form, err.fieldErrors);
    showAlert(alertBox, err.message);
    shakeOtp();
  } finally {
    submitBtn.disabled = false;
    submitBtn.innerHTML = 'Şifrəni yenilə <i class="bi bi-check2"></i>';
  }
});
