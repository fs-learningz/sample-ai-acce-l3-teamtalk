import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { THEMES } from './theme.model';
import { THEME_STORAGE_KEY, ThemeService } from './theme.service';

describe('ThemeService', () => {
  let httpMock: HttpTestingController;

  function createService(): ThemeService {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    httpMock = TestBed.inject(HttpTestingController);

    return TestBed.inject(ThemeService);
  }

  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
    document.documentElement.removeAttribute('data-color-scheme');
  });

  it('offers at least three light and three dark themes', () => {
    // Arrange
    const service = createService();

    // Act
    const light = service.themes.filter((t) => t.mode === 'light');
    const dark = service.themes.filter((t) => t.mode === 'dark');

    // Assert
    expect(light.length).toBeGreaterThanOrEqual(3);
    expect(dark.length).toBeGreaterThanOrEqual(3);
    expect(THEMES.length).toBe(light.length + dark.length);
  });

  it('applies the theme and color scheme to the document and caches it', () => {
    // Arrange
    const service = createService();

    // Act
    service.apply('dracula');

    // Assert
    expect(document.documentElement.getAttribute('data-theme')).toBe('dracula');
    expect(document.documentElement.getAttribute('data-color-scheme')).toBe('dark');
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('dracula');
    expect(service.theme()).toBe('dracula');
  });

  it('falls back to the default theme for unknown ids', () => {
    // Arrange
    const service = createService();

    // Act
    service.apply('nope');

    // Assert
    expect(service.theme()).toBe('light');
  });

  it('saves the selected theme to the user account', () => {
    // Arrange
    const service = createService();

    // Act
    service.select('nord').subscribe();

    // Assert
    const req = httpMock.expectOne('/api/users/me/theme');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ theme: 'nord' });
    expect(service.theme()).toBe('nord');
    req.flush({ theme: 'nord' });
  });

  it('loads the saved theme from the server', () => {
    // Arrange
    const service = createService();

    // Act
    service.loadFromServer().subscribe();
    httpMock.expectOne('/api/users/me/theme').flush({ theme: 'midnight' });

    // Assert
    expect(service.theme()).toBe('midnight');
  });
});
