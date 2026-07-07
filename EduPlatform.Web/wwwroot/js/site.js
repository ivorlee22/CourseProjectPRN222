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

// Initialize Toasts
document.addEventListener("DOMContentLoaded", function () {
    var toastElList = [].slice.call(document.querySelectorAll('.toast'));
    var toastList = toastElList.map(function (toastEl) {
        return new bootstrap.Toast(toastEl, { delay: 5000 });
    });
    toastList.forEach(toast => toast.show());
});

// Handle Loading State for Forms
document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll("form:not([data-no-loader])").forEach(form => {
        form.addEventListener("submit", function (e) {
            // Check if form is valid (if using unobtrusive validation)
            if ($(this).valid && !$(this).valid()) {
                return; // Stop if form is invalid
            }
            
            const submitBtn = this.querySelector('button[type="submit"]');
            if (submitBtn && !submitBtn.disabled) {
                // Add loading state
                submitBtn.dataset.originalText = submitBtn.innerHTML;
                submitBtn.disabled = true;
                submitBtn.classList.add('btn-loading');
                submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>' + 
                                      '<span class="visually-hidden">Loading...</span>' + 
                                      '<span>' + submitBtn.innerText + '</span>';
                
                // Allow the form to submit but prevent further clicks
                // The disabled attribute might stop submission on some browsers if added synchronously, 
                // but since this is in the submit event, it's usually safe. To be 100% safe, we can defer the disable:
                setTimeout(() => submitBtn.disabled = true, 0);
            }
        });
    });
});
