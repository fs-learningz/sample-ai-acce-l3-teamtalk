import { describe, expect, it } from 'vitest';
import {
  AVATAR_PALETTE,
  getAvatarPalette,
  getAvatarSeed,
  getInitials,
  hashString,
} from './avatar-color.util';

describe('getInitials', () => {
  it('uses the first and last name initials for a full name', () => {
    expect(getInitials('Jane Doe', 'jane@example.com')).toBe('JD');
  });

  it('handles more than two name parts by using the first and last', () => {
    expect(getInitials('Jane Middle Doe', null)).toBe('JD');
  });

  it('uses a single initial for a single name', () => {
    expect(getInitials('Jane', 'jane@example.com')).toBe('J');
  });

  it('falls back to the first letter of the email when there is no name', () => {
    expect(getInitials(null, 'jane@example.com')).toBe('J');
    expect(getInitials('   ', 'jane@example.com')).toBe('J');
  });

  it('falls back to a placeholder when neither name nor email is available', () => {
    expect(getInitials(null, null)).toBe('?');
  });

  it('always returns uppercase initials', () => {
    expect(getInitials('jane doe', null)).toBe('JD');
    expect(getInitials(null, 'jane@example.com')).toBe('J');
  });
});

describe('getAvatarSeed', () => {
  it('prefers the name over the email', () => {
    expect(getAvatarSeed('Jane Doe', 'jane@example.com')).toBe('Jane Doe');
  });

  it('falls back to the email when there is no name', () => {
    expect(getAvatarSeed(null, 'jane@example.com')).toBe('jane@example.com');
  });

  it('returns an empty string when neither is available', () => {
    expect(getAvatarSeed(null, null)).toBe('');
  });
});

describe('getAvatarPalette', () => {
  it('is deterministic for the same seed', () => {
    expect(getAvatarPalette('Jane Doe')).toEqual(getAvatarPalette('Jane Doe'));
  });

  it('picks colors only from the fixed palette', () => {
    const result = getAvatarPalette('Some Random Name');
    expect(AVATAR_PALETTE).toContainEqual(result);
  });

  it('can produce different colors for different seeds', () => {
    const seeds = ['Jane Doe', 'John Smith', 'Alex Kim', 'Priya Patel', 'Chen Wei'];
    const colors = new Set(seeds.map((seed) => getAvatarPalette(seed).background));
    expect(colors.size).toBeGreaterThan(1);
  });
});

describe('hashString', () => {
  it('returns the same hash for the same input', () => {
    expect(hashString('Jane Doe')).toBe(hashString('Jane Doe'));
  });

  it('is non-negative', () => {
    expect(hashString('anything')).toBeGreaterThanOrEqual(0);
  });
});
