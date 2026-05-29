document.addEventListener('DOMContentLoaded', () => {
  const btn = document.getElementById('sidebar-toggle');
  const sidebar = document.getElementById('sidebar');
  if (btn && sidebar) {
    btn.addEventListener('click', () => sidebar.classList.toggle('open'));
  }
  // lazy-load images as they scroll into view
  if ('IntersectionObserver' in window) {
    const imgs = document.querySelectorAll('img[loading="lazy"]');
    const obs = new IntersectionObserver((entries) => {
      entries.forEach(e => { if (e.isIntersecting) { e.target.src = e.target.src; } });
    });
    imgs.forEach(img => obs.observe(img));
  }
});
