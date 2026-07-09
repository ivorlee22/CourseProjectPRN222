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

// Initialize Teacher Statistics and Report Chart Heights/Widths (CSP compliance)
document.addEventListener("DOMContentLoaded", function () {
    const bars = document.querySelectorAll(".teacher-stat-chart__bar, .report-column-chart__bar");
    bars.forEach(function (bar) {
        const height = bar.getAttribute("data-height");
        if (height) {
            bar.style.height = height;
        }
    });

    const distributions = document.querySelectorAll(".report-distribution__fill, .usage-progress__bar span");
    distributions.forEach(function (fill) {
        const width = fill.getAttribute("data-width");
        if (width) {
            fill.style.width = width;
        }
    });

    const pieCharts = document.querySelectorAll(".report-pie-chart");
    pieCharts.forEach(function (chart) {
        const segments = chart.getAttribute("data-segments");
        if (segments) {
            chart.style.background = segments;
        }
    });
});
