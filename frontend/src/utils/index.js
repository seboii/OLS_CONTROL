export const SET_POSITION = ({ target, active_area_bg }) => {
  let active_area_element = document.getElementById(target);
  let active_area_bg_element = document.getElementById(active_area_bg);
  active_area_bg_element.style.width = `${active_area_element.offsetWidth}px`;
  active_area_bg_element.style.left = `${active_area_element.offsetLeft}px`;
  active_area_bg_element.style.top = `${active_area_element.offsetTop}px`;
  active_area_bg_element.style.left = `${active_area_element.offsetLeft}px`;

  window.addEventListener("resize", () => {
    active_area_bg_element.style.width = `${active_area_element.offsetWidth}px`;
    active_area_bg_element.style.left = `${active_area_element.offsetLeft}px`;
    active_area_bg_element.style.top = `${active_area_element.offsetTop}px`;
    active_area_bg_element.style.left = `${active_area_element.offsetLeft}px`;
  });
};

export const formatExtendedTime = (minutes) => {
  const MINUTES_IN_YEAR = 525600; // 365 gün
  const MINUTES_IN_MONTH = 43200; // 30 gün
  const MINUTES_IN_WEEK = 10080; // 7 gün
  const MINUTES_IN_DAY = 1440; // 24 saat
  const MINUTES_IN_HOUR = 60;

  const years = Math.floor(minutes / MINUTES_IN_YEAR);
  minutes %= MINUTES_IN_YEAR;

  const months = Math.floor(minutes / MINUTES_IN_MONTH);
  minutes %= MINUTES_IN_MONTH;

  const weeks = Math.floor(minutes / MINUTES_IN_WEEK);
  minutes %= MINUTES_IN_WEEK;

  const days = Math.floor(minutes / MINUTES_IN_DAY);
  minutes %= MINUTES_IN_DAY;

  const hours = Math.floor(minutes / MINUTES_IN_HOUR);
  const remainingMinutes = minutes % MINUTES_IN_HOUR;

  let result = [];
  if (years) result.push(`${years} yıl`);
  if (months) result.push(`${months} ay`);
  if (weeks) result.push(`${weeks} hafta`);
  if (days) result.push(`${days} gün`);
  if (hours) result.push(`${hours} saat`);
  if (remainingMinutes) result.push(`${remainingMinutes} dakika`);

  return result.join(" ");
};
