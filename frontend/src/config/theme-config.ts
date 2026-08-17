import Cookies from "js-cookie";

// olsold: resources/js/config/theme-config.js — birebir taşındı. Kaynakta
// PrimeVue preset'inin "primary" semantic rengini besliyordu; burada Tailwind
// v4'ün `blue-*` paletini aynı `--color-primary-*` değişkenlerine yönlendiren
// @theme bloğu (index.css) besleniyor (bkz. o dosyadaki yorum).
export const THEME_COOKIE = "theme-color";

export interface ThemeColorShades {
  default: string;
  50: string;
  100: string;
  200: string;
  300: string;
  400: string;
  500: string;
  600: string;
  700: string;
  800: string;
  900: string;
  950: string;
}

export interface ThemeColorEntry {
  name: string;
  colors: ThemeColorShades;
}

export const primary_colors: ThemeColorEntry[] = [
  {
    name: "primary",
    colors: {
      default: "#4241d5",
      50: "#edf1fc",
      100: "#dde3fa",
      200: "#c3ccf5",
      300: "#a0abef",
      400: "#7980e8",
      500: "#4241d5",
      600: "#4237be",
      700: "#51499a",
      800: "#3f3a72",
      900: "#383659",
      950: "#212033",
    },
  },
  {
    name: "black",
    colors: {
      default: "#000000",
      50: "#f6f6f6",
      100: "#e7e7e7",
      200: "#d1d1d1",
      300: "#b0b0b0",
      400: "#888888",
      500: "#0d0d0d",
      600: "#5d5d5d",
      700: "#4f4f4f",
      800: "#454545",
      900: "#3d3d3d",
      950: "#0d0d0d",
    },
  },
  {
    name: "slate",
    colors: {
      default: "#18181B",
      50: "#f7f7f8",
      100: "#eeeef0",
      200: "#d9d9de",
      300: "#b8b9c1",
      400: "#91939f",
      500: "#18181B",
      600: "#5d5e6c",
      700: "#4c4d58",
      800: "#41414b",
      900: "#393941",
      950: "#18181b",
    },
  },
  {
    name: "orange",
    colors: {
      default: "#eb5e41",
      50: "#fef4f2",
      100: "#fde7e3",
      200: "#fdd4cb",
      300: "#fab5a7",
      400: "#f58a74",
      500: "#eb5e41",
      600: "#d8482a",
      700: "#b6391f",
      800: "#97321d",
      900: "#7d2f1f",
      950: "#44150b",
    },
  },
  {
    name: "green",
    colors: {
      default: "#55871c",
      50: "#f5fbea",
      100: "#e8f6d1",
      200: "#d2eea8",
      300: "#b4e274",
      400: "#97d249",
      500: "#78b72b",
      600: "#55871c",
      700: "#476f1c",
      800: "#3b591b",
      900: "#324c1b",
      950: "#18290a",
    },
  },
  {
    name: "blue",
    colors: {
      default: "#3459ef",
      50: "#f0f3fe",
      100: "#dce4fd",
      200: "#c1d0fc",
      300: "#96b3fa",
      400: "#658cf5",
      500: "#3459ef",
      600: "#2b43e5",
      700: "#2331d2",
      800: "#2329aa",
      900: "#212987",
      950: "#191c52",
    },
  },
  {
    name: "red",
    colors: {
      default: "#ff2323",
      50: "#fff0f0",
      100: "#ffdddd",
      200: "#ffc0c0",
      300: "#ff9494",
      400: "#ff5757",
      500: "#ff2323",
      600: "#ff0000",
      700: "#d70000",
      800: "#b10303",
      900: "#920a0a",
      950: "#500000",
    },
  },
  {
    name: "rose",
    colors: {
      default: "#ef3475",
      50: "#fdf2f6",
      100: "#fde6f0",
      200: "#fccee1",
      300: "#fba6c8",
      400: "#f76fa2",
      500: "#ef3475",
      600: "#e0225a",
      700: "#c21443",
      800: "#a01438",
      900: "#861531",
      950: "#520519",
    },
  },
  {
    name: "magenta",
    colors: {
      default: "#ff21df",
      50: "#fff2fd",
      100: "#ffe3fc",
      200: "#ffc6f8",
      300: "#ff99ed",
      400: "#ff5de2",
      500: "#ff21df",
      600: "#ff00ea",
      700: "#cf00b9",
      800: "#a90096",
      900: "#890677",
      950: "#5e0052",
    },
  },
];

const themeCookie = Cookies.get(THEME_COOKIE);
const activeColorObject = primary_colors.find((color) => color.name === themeCookie);

export const activeColorName = activeColorObject ? activeColorObject.name : primary_colors[0].name;
export const activeColors = activeColorObject ? activeColorObject.colors : primary_colors[0].colors;

(Object.keys(activeColors) as Array<keyof ThemeColorShades>).forEach((key) => {
  document.documentElement.style.setProperty(`--color-primary-${key}`, activeColors[key]);
});

// olsold: setThemeColor — çerezi yazar ve tam sayfa yeniler (yeni renk
// module-load sırasında yeniden uygulanır).
export function setThemeColor(name: string) {
  Cookies.set(THEME_COOKIE, name);
  window.location.reload();
}
