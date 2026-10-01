import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { THEMES } from '../../../core/theme/theme.model';
import { THEME_STORAGE_KEY, ThemeService } from '../../../core/theme/theme.service';
import { WorkspaceSettingsAppearanceComponent } from './workspace-settings-appearance.component';

describe('WorkspaceSettingsAppearanceComponent', () => {
  let fixture: ComponentFixture<WorkspaceSettingsAppearanceComponent>;
  let httpMock: HttpTestingController;
  let themeService: ThemeService;

  const dark = THEMES.find((t) => t.id === 'dracula')!;

  beforeEach(async () => {
    localStorage.clear();

    await TestBed.configureTestingModule({
      imports: [WorkspaceSettingsAppearanceComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    themeService = TestBed.inject(ThemeService);
    fixture = TestBed.createComponent(WorkspaceSettingsAppearanceComponent);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    vi.restoreAllMocks();
    localStorage.removeItem(THEME_STORAGE_KEY);
    document.documentElement.removeAttribute('data-theme');
    document.documentElement.removeAttribute('data-color-scheme');
  });

  it('should group the themes into light and dark lists', () => {
    // Arrange
    const groups = fixture.componentInstance.groups;

    // Act
    const light = groups.find((g) => g.mode === 'light')!;
    const darkGroup = groups.find((g) => g.mode === 'dark')!;

    // Assert
    expect(light.themes.every((t) => t.mode === 'light')).toBe(true);
    expect(darkGroup.themes.every((t) => t.mode === 'dark')).toBe(true);
    expect(light.themes.length + darkGroup.themes.length).toBe(THEMES.length);
  });

  it('should save a newly selected theme', () => {
    // Arrange
    fixture.detectChanges();

    // Act
    fixture.componentInstance.select(dark);
    const req = httpMock.expectOne('/api/users/me/theme');
    req.flush({ theme: dark.id });

    // Assert
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ theme: 'dracula' });
    expect(fixture.componentInstance.currentTheme()).toBe('dracula');
    expect(fixture.componentInstance.errorMessage()).toBeNull();
  });

  it('should not call the API when the current theme is selected again', () => {
    // Arrange
    fixture.detectChanges();
    const current = THEMES.find((t) => t.id === fixture.componentInstance.currentTheme())!;
    const selectSpy = vi.spyOn(themeService, 'select');

    // Act
    fixture.componentInstance.select(current);

    // Assert
    expect(selectSpy).not.toHaveBeenCalled();
    httpMock.expectNone('/api/users/me/theme');
  });

  it('should revert to the previous theme and show an error when saving fails', () => {
    // Arrange
    fixture.detectChanges();
    const previous = fixture.componentInstance.currentTheme();

    // Act
    fixture.componentInstance.select(dark);
    httpMock
      .expectOne('/api/users/me/theme')
      .flush(null, { status: 500, statusText: 'Server Error' });

    // Assert
    expect(fixture.componentInstance.currentTheme()).toBe(previous);
    expect(fixture.componentInstance.errorMessage()).toBe(
      'Could not save your theme. Please try again.',
    );
  });
});
