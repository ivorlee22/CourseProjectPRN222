document.querySelectorAll("form[data-confirm]").forEach((form) => {
  form.addEventListener("submit", (event) => {
    const message = form.dataset.confirm;
    if (message && !window.confirm(message)) {
      event.preventDefault();
    }
  });
});

// Password visibility toggle
document.addEventListener('click', function (e) {
  if (e.target.closest('.toggle-password')) {
    const btn = e.target.closest('.toggle-password');
    const input = document.querySelector(btn.dataset.target);
    if (input) {
      if (input.type === 'password') {
        input.type = 'text';
        btn.innerHTML = '<i class="bi bi-eye-slash"></i>';
      } else {
        input.type = 'password';
        btn.innerHTML = '<i class="bi bi-eye"></i>';
      }
    }
  }
});

const interactiveSelector = "a, button, input, select, textarea, label, summary, [role='button'], [data-card-ignore]";

document.addEventListener("click", (event) => {
  const target = event.target instanceof Element ? event.target : event.target.parentElement;
  const card = target?.closest("[data-clickable-card]");
  if (!card || target.closest(interactiveSelector)) {
    return;
  }

  const link = card.querySelector("[data-card-link]") || card.querySelector("a[href]");
  if (link) {
    link.click();
  }
});

document.addEventListener("keydown", (event) => {
  if (event.key !== "Enter" && event.key !== " ") {
    return;
  }

  const target = event.target instanceof Element ? event.target : event.target.parentElement;
  const card = target?.closest("[data-clickable-card]");
  if (!card || target.closest(interactiveSelector)) {
    return;
  }

  const link = card.querySelector("[data-card-link]") || card.querySelector("a[href]");
  if (link) {
    event.preventDefault();
    link.click();
  }
});

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
