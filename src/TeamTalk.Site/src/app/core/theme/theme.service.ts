import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { DEFAULT_THEME_ID, THEMES, findTheme } from './theme.model';

export const THEME_STORAGE_KEY = 'teamtalk.theme';

interface ThemeResponse {
  theme: string;
}

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly http = inject(HttpClient);

  private readonly themeSignal = signal<string>(this.readCachedTheme());

  readonly theme = this.themeSignal.asReadonly();
  readonly themes = THEMES;

  constructor() {
    this.applyToDocument(this.themeSignal());
  }

  /** Applies a theme locally (document + cache) without calling the API. */
  apply(themeId: string | null | undefined): void {
    const theme = findTheme(themeId) ? themeId! : DEFAULT_THEME_ID;
    this.themeSignal.set(theme);
    this.applyToDocument(theme);
    this.writeCachedTheme(theme);
  }

  /** Applies the theme immediately, then persists it on the user's account. */
  select(themeId: string): Observable<ThemeResponse> {
    this.apply(themeId);

    return this.http.put<ThemeResponse>('/api/users/me/theme', { theme: themeId });
  }

  /** Loads the saved theme of the signed-in user and applies it. */
  loadFromServer(): Observable<ThemeResponse> {
    return this.http
      .get<ThemeResponse>('/api/users/me/theme')
      .pipe(tap((response) => this.apply(response.theme)));
  }

  reset(): void {
    this.apply(DEFAULT_THEME_ID);
  }

  private applyToDocument(themeId: string): void {
    const root = document.documentElement;
    root.setAttribute('data-theme', themeId);
    root.setAttribute('data-color-scheme', findTheme(themeId)?.mode ?? 'light');
  }

  private readCachedTheme(): string {
    try {
      const cached = localStorage.getItem(THEME_STORAGE_KEY);

      return findTheme(cached) ? cached! : DEFAULT_THEME_ID;
    } catch {
      return DEFAULT_THEME_ID;
    }
  }

  private writeCachedTheme(themeId: string): void {
    try {
      localStorage.setItem(THEME_STORAGE_KEY, themeId);
    } catch {
      // Storage may be unavailable; the theme still applies for this session.
    }
  }
}
