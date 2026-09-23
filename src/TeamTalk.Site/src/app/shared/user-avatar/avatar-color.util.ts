export interface AvatarPaletteColor {
  background: string;
  text: string;
}

/**
 * Fixed palette of background/text pairs. Every pair meets WCAG AA contrast
 * (>= 4.5:1) so any name/email hash produces an accessible avatar.
 */
export const AVATAR_PALETTE: readonly AvatarPaletteColor[] = [
  { background: '#C0392B', text: '#FFFFFF' },
  { background: '#B7420E', text: '#FFFFFF' },
  { background: '#7D6608', text: '#FFFFFF' },
  { background: '#1E8449', text: '#FFFFFF' },
  { background: '#117864', text: '#FFFFFF' },
  { background: '#1F618D', text: '#FFFFFF' },
  { background: '#3D5A80', text: '#FFFFFF' },
  { background: '#6C3483', text: '#FFFFFF' },
  { background: '#99154A', text: '#FFFFFF' },
  { background: '#5C4033', text: '#FFFFFF' },
];

/** Deterministic string hash (djb2). Same input always yields the same output. */
export function hashString(value: string): number {
  let hash = 5381;
  for (let i = 0; i < value.length; i++) {
    hash = (hash * 33) ^ value.charCodeAt(i);
  }
  return Math.abs(hash);
}

/** The seed used to pick a color: the name when present, otherwise the email. */
export function getAvatarSeed(
  name: string | null | undefined,
  email: string | null | undefined,
): string {
  return name?.trim() || email?.trim() || '';
}

export function getAvatarPalette(seed: string): AvatarPaletteColor {
  if (!seed) {
    return AVATAR_PALETTE[0];
  }

  const index = hashString(seed) % AVATAR_PALETTE.length;
  return AVATAR_PALETTE[index];
}

/**
 * "Jane Doe" -> "JD", "Jane" -> "J", no name -> first letter of email, else "?".
 */
export function getInitials(
  name: string | null | undefined,
  email: string | null | undefined,
): string {
  const parts = name?.trim().split(/\s+/).filter(Boolean) ?? [];

  if (parts.length >= 2) {
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  }

  if (parts.length === 1) {
    return parts[0][0].toUpperCase();
  }

  const trimmedEmail = email?.trim();
  if (trimmedEmail) {
    return trimmedEmail[0].toUpperCase();
  }

  return '?';
}
