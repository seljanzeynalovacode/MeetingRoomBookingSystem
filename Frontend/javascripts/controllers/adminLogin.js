import { adminLogin, verifyAdmin2FA } from "../api/authApi.js";
import { showAlert, clearAlert, showFieldErrors, clearFieldErrors } from "../components/alert.js";

const alertBox = document.getElementById("alertBox");

const step1 = document.getElementById("step1");
const step2 = document.getElementById("step2");
const step1Form = document.getElementById("adminLoginForm");
const step1SubmitBtn = document.getElementById("step1SubmitBtn");
const toggleEye = document.getElementById("toggleEye");
const passwordInput = document.getElementById("password");

const step2Form = document.getElementById("admin2faForm");
const step2SubmitBtn = document.getElementById("step2SubmitBtn");
const step2EmailLabel = document.getElementById("step2EmailLabel");
const otpRow = document.getElementById("otpRow");
const otpBoxes = Array.from(otpRow.querySelectorAll(".otp-box"));
const backToStep1 = document.getElementById("backToStep1");

let pendingEmail = "";
let pendingRememberMe = false;

toggleEye.addEventListener("click", () => {
  const isPassword = passwordInput.type === "password";
  passwordInput.type = isPassword ? "text" : "password";
  toggleEye.innerHTML = `<i class="bi bi-${isPassword ? "eye-slash" : "eye"}"></i>`;
});

function goToStep2(email) {
  pendingEmail = email;
  step2EmailLabel.textContent = email;
  step1.classList.remove("active");
  step2.classList.add("active");
  otpBoxes.forEach((b) => (b.value = ""));
  otpBoxes[0].focus();
}

function goToStep1() {
  step2.classList.remove("active");
  step1.classList.add("active");
  clearAlert(alertBox);
}

step1Form.addEventListener("submit", async (e) => {
  e.preventDefault();
  clearAlert(alertBox);
  clearFieldErrors(step1Form);

  const dto = {
    Email: step1Form.Email.value.trim(),
    Password: step1Form.Password.value,
    RememberMe: step1Form.RememberMe.checked,
  };
  pendingRememberMe = dto.RememberMe;

  step1SubmitBtn.disabled = true;
  step1SubmitBtn.innerHTML = "Yoxlanılır...";

  try {
    const result = await adminLogin(dto);
    if (result.requires2FA) {
      goToStep2(dto.Email);
    } else if (result.tokens) {
      window.location.href = "adminDashboard.html";
    } else {
      showAlert(alertBox, result.message || "Giriş uğursuz oldu.");
    }
  } catch (err) {
    if (err.fieldErrors) showFieldErrors(step1Form, err.fieldErrors);
    showAlert(alertBox, err.message);
  } finally {
    step1SubmitBtn.disabled = false;
    step1SubmitBtn.innerHTML = 'Davam et <i class="bi bi-arrow-right"></i>';
  }
});

otpBoxes.forEach((box, idx) => {
  box.addEventListener("input", () => {
    box.value = box.value.replace(/\D/g, "");
    if (box.value && otpBoxes[idx + 1]) otpBoxes[idx + 1].focus();
  });

  box.addEventListener("keydown", (e) => {
    if (e.key === "Backspace" && !box.value && otpBoxes[idx - 1]) {
      otpBoxes[idx - 1].focus();
    }
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
  });
});

function getCode() {
  return otpBoxes.map((b) => b.value).join("");
}

function shakeOtp() {
  otpRow.classList.remove("shake");
  void otpRow.offsetWidth;
  otpRow.classList.add("shake");
}

step2Form.addEventListener("submit", async (e) => {
  e.preventDefault();
  clearAlert(alertBox);

  const code = getCode();
  if (code.length !== 6) {
    showAlert(alertBox, "6 rəqəmli kodu tam daxil edin.", "warning");
    shakeOtp();
    return;
  }

  step2SubmitBtn.disabled = true;
  step2SubmitBtn.innerHTML = "Yoxlanılır...";

  try {
    await verifyAdmin2FA({ Email: pendingEmail, Code: code, RememberMe: pendingRememberMe });
    window.location.href = "adminDashboard.html";
  } catch (err) {
    showAlert(alertBox, err.message);
    shakeOtp();
    otpBoxes.forEach((b) => (b.value = ""));
    otpBoxes[0].focus();
  } finally {
    step2SubmitBtn.disabled = false;
    step2SubmitBtn.innerHTML = 'Təsdiqlə <i class="bi bi-check2"></i>';
  }
});

backToStep1.addEventListener("click", (e) => {
  e.preventDefault();
  goToStep1();
});