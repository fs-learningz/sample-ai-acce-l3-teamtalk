import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

export const TeamTalkPreset = definePreset(Aura, {
  semantic: {
    primary: {
      color: 'var(--color-primary)',
      contrastColor: 'var(--color-primary-content)',
      hoverColor: 'var(--color-primary)',
      activeColor: 'var(--color-primary)',
    },
    colorScheme: {
      light: {
        surface: {
          0: 'var(--color-base-100)',
          50: 'var(--color-base-200)',
          100: 'var(--color-base-300)',
        },
      },
      dark: {
        surface: {
          0: 'var(--color-base-100)',
          50: 'var(--color-base-200)',
          100: 'var(--color-base-300)',
        },
      },
    },
  },
});
