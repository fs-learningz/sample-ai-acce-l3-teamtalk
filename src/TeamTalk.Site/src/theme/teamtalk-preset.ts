import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

export const TeamTalkPreset = definePreset(Aura, {
  semantic: {
    primary: {
      50: '{color-primary}',
      100: '{color-primary}',
      200: '{color-primary}',
      300: '{color-primary}',
      400: '{color-primary}',
      500: '{color-primary}',
      600: '{color-primary}',
      700: '{color-primary}',
      800: '{color-primary}',
      900: '{color-primary}',
      950: '{color-primary}',
    },
    colorScheme: {
      light: {
        surface: {
          0: '{color-base-100}',
          50: '{color-base-200}',
          100: '{color-base-300}',
        },
      },
      dark: {
        surface: {
          0: '{color-base-100}',
          50: '{color-base-200}',
          100: '{color-base-300}',
        },
      },
    },
  },
});
