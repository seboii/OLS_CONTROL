import { definePreset } from "@primevue/themes";
import Aura from "@primevue/themes/aura";
import { activeColors } from "@/config/theme-config.js";

const PrimeVueThemeConfig = definePreset(Aura, {
  primitive: {
    borderRadius: {
      none: "0",
      xs: "2px",
      sm: "6px",
      md: "8px",
      lg: "10px",
      xl: "12px",
    },
    ols: {
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
  semantic: {
    primary: activeColors,
    colorScheme: {
      light: {
        surface: {
          0: "#ffffff",
          50: "{gray.50}",
          100: "{gray.100}",
          200: "{gray.200}",
          300: "{gray.300}",
          400: "{gray.400}",
          500: "{gray.500}",
          600: "{gray.600}",
          700: "{gray.700}",
          800: "{gray.800}",
          900: "{gray.900}",
          950: "{gray.950}",
        },
        formField: {
          color: "{gray.950}",
        },
      },
    },
    semantic: {
      formField: {
        paddingX: "1.25rem",
        paddingY: "0.75rem",
        borderRadius: "{border.radius.md}",
        focusRing: {
          width: "2px",
          style: "solid",
          color: "var(--color-primary-200)",
          offset: "0px",
        },
        transitionDuration: "{transition.duration}",
      },
      list: {
        padding: "0.5rem",
        gap: "2px",
        header: {
          padding: "0.5rem 0.75rem 0.25rem 0.75rem",
        },
        option: {
          padding: "0.75rem 1rem",
          borderRadius: "{border.radius.md}",
        },
        optionGroup: {
          padding: "0.5rem 0.75rem",
          fontWeight: "600",
        },
      },
    },
  },
});

export default PrimeVueThemeConfig;
