import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

const mixContent = (percent: number): string =>
  `color-mix(in oklab, var(--color-base-content) ${percent}%, var(--color-base-100))`;

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
      // Aura's dark scheme uses surface.0 as the text color and 900/950 as
      // backgrounds, so the scale runs from base-content down to base-300.
      dark: {
        surface: {
          0: 'var(--color-base-content)',
          50: mixContent(92),
          100: mixContent(85),
          200: mixContent(72),
          300: mixContent(58),
          400: mixContent(46),
          500: mixContent(36),
          600: mixContent(26),
          700: mixContent(18),
          800: 'var(--color-base-100)',
          900: 'var(--color-base-200)',
          950: 'var(--color-base-300)',
        },
      },
    },
  },
});
