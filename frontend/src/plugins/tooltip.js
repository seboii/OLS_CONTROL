export default {
  mounted(el, binding) {
    let tooltipEl; // Tooltip elementi
    let showTimeout; // Açılma gecikmesi için zamanlayıcı
    let hideTimeout; // Kapanma zamanlayıcısı

    const position = binding.modifiers.top
      ? "top"
      : binding.modifiers.bottom
      ? "bottom"
      : binding.modifiers.left
      ? "left"
      : binding.modifiers.right
      ? "right"
      : "top";

    let text = binding.value;
    let delay = 400;

    if (typeof text === "object") {
      text = text.content;
      if ("delay" in binding.value) {
        delay = binding.value.delay;
      }
    }

    // Tooltip oluşturma ve gösterme fonksiyonu
    const showTooltip = () => {
      clearTimeout(hideTimeout); // Kapanma zamanlayıcısını temizle

      if (!tooltipEl) {
        // Tooltip elementi oluştur
        tooltipEl = document.createElement("div");
        tooltipEl.className = `v-tooltip ${position}`;
        tooltipEl.textContent = text;

        // Tooltip'e mouseenter/mouseleave olayları ekle
        tooltipEl.addEventListener("mouseenter", () => {
          clearTimeout(hideTimeout); // Kapanmayı iptal et
        });
        tooltipEl.addEventListener("mouseleave", hideTooltip);

        // Tooltip DOM'a ekleniyor
        document.body.appendChild(tooltipEl);

        // Animasyon tamamlandığında DOM'dan silmek için olay ekleniyor
        tooltipEl.addEventListener("transitionend", () => {
          if (tooltipEl && !tooltipEl.classList.contains("visible")) {
            tooltipEl.remove(); // DOM'dan kaldır
            tooltipEl = null;
          }
        });
      }

      // Pozisyonu hesapla
      const rect = el.getBoundingClientRect();
      const tooltipRect = tooltipEl.getBoundingClientRect();

      let top, left;

      if (position === "top") {
        top = rect.top - tooltipRect.height - 8; // 8px boşluk
        left = rect.left + rect.width / 2 - tooltipRect.width / 2;

        // Ekran dışına taşmayı önle
        if (top < 0) top = rect.bottom + 8;
        if (left < 0) left = 8;
        if (left + tooltipRect.width > window.innerWidth) left = window.innerWidth - tooltipRect.width - 8;
      } else if (position === "bottom") {
        top = rect.bottom + 8; // 8px boşluk
        left = rect.left + rect.width / 2 - tooltipRect.width / 2;

        if (top + tooltipRect.height > window.innerHeight) top = rect.top - tooltipRect.height - 8;
        if (left < 0) left = 8;
        if (left + tooltipRect.width > window.innerWidth) left = window.innerWidth - tooltipRect.width - 8;
      } else if (position === "left") {
        top = rect.top + rect.height / 2 - tooltipRect.height / 2;
        left = rect.left - tooltipRect.width - 8; // 8px boşluk

        if (left < 0) left = rect.right + 8;
        if (top < 0) top = 8;
        if (top + tooltipRect.height > window.innerHeight) top = window.innerHeight - tooltipRect.height - 8;
      } else if (position === "right") {
        top = rect.top + rect.height / 2 - tooltipRect.height / 2;
        left = rect.right + 8; // 8px boşluk

        if (left + tooltipRect.width > window.innerWidth) left = rect.left - tooltipRect.width - 8;
        if (top < 0) top = 8;
        if (top + tooltipRect.height > window.innerHeight) top = window.innerHeight - tooltipRect.height - 8;
      }

      tooltipEl.style.top = `${top}px`;
      tooltipEl.style.left = `${left}px`;

      // Tooltip'i görünür yap
      setTimeout(() => {
        tooltipEl.classList.add("visible");
      }, 0);
    };

    // Tooltip gizleme fonksiyonu
    const hideTooltip = () => {
      clearTimeout(showTimeout); // Açılma zamanlayıcısını temizle
      if (tooltipEl) {
        hideTimeout = setTimeout(() => {
          tooltipEl.classList.remove("visible");
        }, 100); // Kapanma animasyonu için kısa bir gecikme
      }
    };

    // Element olaylarını bağla
    el.addEventListener("mouseenter", () => {
      showTimeout = setTimeout(showTooltip, delay);
    });
    el.addEventListener("mouseleave", hideTooltip);
    // el.addEventListener("click", hideTooltip);
  },
};
