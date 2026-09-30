export type ThemeMode = 'light' | 'dark';

export interface ThemeOption {
  id: string;
  label: string;
  mode: ThemeMode;
  /** Preview swatches: background, surface, primary, accent. */
  swatches: [string, string, string, string];
}

export const DEFAULT_THEME_ID = 'light';

export const THEMES: readonly ThemeOption[] = [
  { id: 'light', label: 'Light', mode: 'light', swatches: ['#ffffff', '#f5f5f5', '#4f46e5', '#0d9488'] },
  { id: 'cupcake', label: 'Cupcake', mode: 'light', swatches: ['#faf7f5', '#efeae6', '#e0669b', '#3aa8a0'] },
  { id: 'nord', label: 'Nord', mode: 'light', swatches: ['#eceff4', '#e5e9f0', '#5e81ac', '#88c0d0'] },
  { id: 'dark', label: 'Dark', mode: 'dark', swatches: ['#1f2937', '#1a222f', '#6366f1', '#2dd4bf'] },
  { id: 'midnight', label: 'Midnight', mode: 'dark', swatches: ['#0b1120', '#070c18', '#38bdf8', '#a78bfa'] },
  { id: 'dracula', label: 'Dracula', mode: 'dark', swatches: ['#282a36', '#21222c', '#bd93f9', '#ff79c6'] },
];

export function findTheme(id: string | null | undefined): ThemeOption | undefined {
  return THEMES.find((theme) => theme.id === id);
}
