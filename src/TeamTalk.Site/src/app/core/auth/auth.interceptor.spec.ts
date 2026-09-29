import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let token: string | null;

  beforeEach(() => {
    token = null;

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: { getToken: () => token } },
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('adds a bearer Authorization header when a token exists', () => {
    token = 'abc.def.ghi';

    http.get('/api/workspaces').subscribe();

    const request = httpMock.expectOne('/api/workspaces');
    expect(request.request.headers.get('Authorization')).toBe('Bearer abc.def.ghi');
    request.flush([]);
  });

  it('leaves the request untouched when there is no token', () => {
    token = null;

    http.get('/api/workspaces').subscribe();

    const request = httpMock.expectOne('/api/workspaces');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush([]);
  });

  it('treats an empty token as no token', () => {
    token = '';

    http.get('/api/workspaces').subscribe();

    const request = httpMock.expectOne('/api/workspaces');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush([]);
  });

  it('preserves existing headers and the request method and body', () => {
    token = 'abc';

    http.post('/api/workspaces', { name: 'Eng' }, { headers: { 'X-Custom': '1' } }).subscribe();

    const request = httpMock.expectOne('/api/workspaces');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'Eng' });
    expect(request.request.headers.get('X-Custom')).toBe('1');
    expect(request.request.headers.get('Authorization')).toBe('Bearer abc');
    request.flush({});
  });
});
