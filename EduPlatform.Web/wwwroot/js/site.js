document.querySelectorAll("form[data-confirm]").forEach((form) => {
  form.addEventListener("submit", (event) => {
    const message = form.dataset.confirm;
    if (message && !window.confirm(message)) {
      event.preventDefault();
    }
  });
});

const courseType = document.querySelector("#courseType");
const passwordField = document.querySelector("#coursePasswordField");

function updatePasswordVisibility() {
  if (!courseType || !passwordField) {
    return;
  }

  passwordField.hidden = courseType.value !== "Private";
}

courseType?.addEventListener("change", updatePasswordVisibility);
updatePasswordVisibility();
