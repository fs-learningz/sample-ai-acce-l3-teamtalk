import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthResponse } from '../models/auth.model';
import { AuthService } from './auth.service';

const TOKEN_KEY = 'teamtalk.auth.token';
const USER_KEY = 'teamtalk.auth.user';

const authResponse: AuthResponse = {
  userId: 'u1',
  email: 'jane@example.com',
  displayName: 'Jane Doe',
  token: 'jwt-token',
};

describe('AuthService', () => {
  let httpMock: HttpTestingController;

  function createService(): AuthService {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    httpMock = TestBed.inject(HttpTestingController);

    return TestBed.inject(AuthService);
  }

  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
    vi.restoreAllMocks();
    localStorage.clear();
  });

  it('starts unauthenticated when nothing is stored', () => {
    const service = createService();

    expect(service.isAuthenticated()).toBe(false);
    expect(service.currentUser()).toBeNull();
    expect(service.getToken()).toBeNull();
  });

  it('restores the session from storage when both token and user are stored', () => {
    localStorage.setItem(TOKEN_KEY, 'jwt-token');
    localStorage.setItem(
      USER_KEY,
      JSON.stringify({ userId: 'u1', email: 'jane@example.com', displayName: 'Jane Doe' }),
    );

    const service = createService();

    expect(service.isAuthenticated()).toBe(true);
    expect(service.currentUser()?.email).toBe('jane@example.com');
    expect(service.getToken()).toBe('jwt-token');
  });

  it('does not restore a session when the token is missing', () => {
    localStorage.setItem(USER_KEY, JSON.stringify({ userId: 'u1', email: 'a@b.c', displayName: 'A' }));

    const service = createService();

    expect(service.isAuthenticated()).toBe(false);
  });

  it('does not restore a session when the stored user is not valid JSON', () => {
    localStorage.setItem(TOKEN_KEY, 'jwt-token');
    localStorage.setItem(USER_KEY, '{not json');

    const service = createService();

    expect(service.isAuthenticated()).toBe(false);
  });

  it('posts credentials to the login endpoint and stores the session', () => {
    const service = createService();
    let response: AuthResponse | undefined;

    service.loginAsync({ email: 'jane@example.com', password: 'Passw0rd!' }).subscribe((r) => (response = r));

    const request = httpMock.expectOne('/api/auth/login');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ email: 'jane@example.com', password: 'Passw0rd!' });
    request.flush(authResponse);

    expect(response).toEqual(authResponse);
    expect(service.isAuthenticated()).toBe(true);
    expect(service.currentUser()).toEqual({
      userId: 'u1',
      email: 'jane@example.com',
      displayName: 'Jane Doe',
    });
    expect(localStorage.getItem(TOKEN_KEY)).toBe('jwt-token');
    expect(service.getToken()).toBe('jwt-token');
  });

  it('posts the registration to the register endpoint and stores the session', () => {
    const service = createService();

    service
      .registerAsync({ email: 'jane@example.com', displayName: 'Jane Doe', password: 'Passw0rd!' })
      .subscribe();

    const request = httpMock.expectOne('/api/auth/register');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      email: 'jane@example.com',
      displayName: 'Jane Doe',
      password: 'Passw0rd!',
    });
    request.flush(authResponse);

    expect(service.isAuthenticated()).toBe(true);
    expect(localStorage.getItem(USER_KEY)).toContain('jane@example.com');
  });

  it('does not store a session when login fails', () => {
    const service = createService();
    let error: unknown;

    service.loginAsync({ email: 'jane@example.com', password: 'wrong' }).subscribe({
      error: (e) => (error = e),
    });

    httpMock
      .expectOne('/api/auth/login')
      .flush({ title: 'Auth.InvalidCredentials' }, { status: 401, statusText: 'Unauthorized' });

    expect(error).toBeDefined();
    expect(service.isAuthenticated()).toBe(false);
    expect(localStorage.getItem(TOKEN_KEY)).toBeNull();
  });

  it('clears the session on logout', () => {
    const service = createService();
    service.loginAsync({ email: 'jane@example.com', password: 'Passw0rd!' }).subscribe();
    httpMock.expectOne('/api/auth/login').flush(authResponse);

    service.logout();

    expect(service.isAuthenticated()).toBe(false);
    expect(service.currentUser()).toBeNull();
    expect(localStorage.getItem(TOKEN_KEY)).toBeNull();
    expect(localStorage.getItem(USER_KEY)).toBeNull();
  });

  it('returns null from getToken when storage access throws', () => {
    const service = createService();
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('storage blocked');
    });

    expect(service.getToken()).toBeNull();
  });

  it('stays unauthenticated when storing the session throws', () => {
    const service = createService();
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('quota exceeded');
    });

    service.loginAsync({ email: 'jane@example.com', password: 'Passw0rd!' }).subscribe();
    httpMock.expectOne('/api/auth/login').flush(authResponse);

    expect(service.isAuthenticated()).toBe(false);
  });
});
