import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { ThemeService } from '../theme/theme.service';
import { AuthResponse, LoginRequest, RegisterRequest } from '../models/auth.model';

const TOKEN_STORAGE_KEY = 'teamtalk.auth.token';
const USER_STORAGE_KEY = 'teamtalk.auth.user';

interface StoredUser {
  userId: string;
  email: string;
  displayName: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly themeService = inject(ThemeService);
  private readonly currentUserSignal = signal<StoredUser | null>(this.readStoredUser());

  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.currentUserSignal() !== null);

  constructor(private readonly http: HttpClient) {}

  registerAsync(request: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>('/api/auth/register', request)
      .pipe(tap((response) => this.storeSession(response)));
  }

  loginAsync(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>('/api/auth/login', request)
      .pipe(tap((response) => this.storeSession(response)));
  }

  logout(): void {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
    localStorage.removeItem(USER_STORAGE_KEY);
    this.currentUserSignal.set(null);
    this.themeService.reset();
  }

  getToken(): string | null {
    try {
      return localStorage.getItem(TOKEN_STORAGE_KEY);
    } catch {
      return null;
    }
  }

  private storeSession(response: AuthResponse): void {
    try {
      localStorage.setItem(TOKEN_STORAGE_KEY, response.token);
      const user: StoredUser = {
        userId: response.userId,
        email: response.email,
        displayName: response.displayName,
      };
      localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(user));
      this.currentUserSignal.set(user);
      this.themeService.apply(response.theme);
    } catch {
      this.currentUserSignal.set(null);
    }
  }

  private readStoredUser(): StoredUser | null {
    try {
      const raw = localStorage.getItem(USER_STORAGE_KEY);
      const token = localStorage.getItem(TOKEN_STORAGE_KEY);
      if (!raw || !token) {
        return null;
      }

      return JSON.parse(raw) as StoredUser;
    } catch {
      return null;
    }
  }
}
