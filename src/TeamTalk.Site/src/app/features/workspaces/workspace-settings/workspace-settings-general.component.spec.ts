import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { WorkspaceResponse } from '../../../core/models/workspace.model';
import { WorkspaceSettingsGeneralComponent } from './workspace-settings-general.component';
import { WorkspaceSettingsStore } from './workspace-settings.store';

describe('WorkspaceSettingsGeneralComponent', () => {
  let fixture: ComponentFixture<WorkspaceSettingsGeneralComponent>;
  let httpMock: HttpTestingController;
  let store: WorkspaceSettingsStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WorkspaceSettingsGeneralComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), WorkspaceSettingsStore],
    }).compileComponents();

    store = TestBed.inject(WorkspaceSettingsStore);
    store.workspaceId = 'w1';

    fixture = TestBed.createComponent(WorkspaceSettingsGeneralComponent);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  function setWorkspace(overrides: Partial<WorkspaceResponse> = {}): void {
    store.workspace.set({
      id: 'w1',
      name: 'Engineering',
      description: null,
      hasIcon: false,
      createdByUserId: 'u1',
      createdByDisplayName: 'Ada',
      createdAtUtc: new Date().toISOString(),
      memberCount: 2,
      currentUserRole: 'Admin',
      ...overrides,
    });
  }

  it('should populate the form from the loaded workspace', () => {
    setWorkspace({ description: 'A team workspace' });
    fixture.detectChanges();

    expect(fixture.componentInstance.form.value.name).toBe('Engineering');
    expect(fixture.componentInstance.form.value.description).toBe('A team workspace');
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(false);
  });

  it('should disable the form for non-admins', () => {
    setWorkspace({ currentUserRole: 'Member' });
    fixture.detectChanges();

    expect(fixture.componentInstance.form.disabled).toBe(true);
  });

  it('should report unsaved changes once the name is edited', () => {
    setWorkspace();
    fixture.detectChanges();

    fixture.componentInstance.form.controls.name.setValue('Renamed');
    fixture.componentInstance.form.controls.name.markAsDirty();

    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(true);
  });

  it('should save the updated name and description', () => {
    setWorkspace();
    fixture.detectChanges();

    fixture.componentInstance.form.controls.name.setValue('Renamed');
    fixture.componentInstance.save();

    const req = httpMock.expectOne('/api/workspaces/w1');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ name: 'Renamed', description: null });
    req.flush({
      id: 'w1',
      name: 'Renamed',
      description: null,
      hasIcon: false,
      createdByUserId: 'u1',
      createdByDisplayName: 'Ada',
      createdAtUtc: new Date().toISOString(),
      memberCount: 2,
      currentUserRole: 'Admin',
    });

    expect(fixture.componentInstance.successMessage()).toBeTruthy();
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(false);
  });

  function workspaceBody(overrides: Partial<WorkspaceResponse> = {}): WorkspaceResponse {
    return {
      id: 'w1',
      name: 'Engineering',
      description: null,
      hasIcon: false,
      createdByUserId: 'u1',
      createdByDisplayName: 'Ada',
      createdAtUtc: new Date().toISOString(),
      memberCount: 2,
      currentUserRole: 'Admin',
      ...overrides,
    };
  }

  function iconEvent(file: File | null): Event {
    return { target: { files: file ? [file] : [], value: 'C:\\fakepath\\icon' } } as unknown as Event;
  }

  function iconFile(type: string, sizeInBytes = 10): File {
    return new File([new Uint8Array(sizeInBytes)], 'icon', { type });
  }

  it('should reject an icon larger than 2 MB', () => {
    // Arrange
    setWorkspace();
    fixture.detectChanges();

    // Act
    fixture.componentInstance.onIconSelected(iconEvent(iconFile('image/png', 2 * 1024 * 1024 + 1)));

    // Assert
    expect(fixture.componentInstance.errorMessage()).toBe('The icon must be 2 MB or smaller.');
    expect(fixture.componentInstance.selectedIconFile()).toBeNull();
  });

  it('should accept an icon of exactly 2 MB', () => {
    // Arrange
    setWorkspace();
    fixture.detectChanges();

    // Act
    fixture.componentInstance.onIconSelected(iconEvent(iconFile('image/png', 2 * 1024 * 1024)));

    // Assert
    expect(fixture.componentInstance.errorMessage()).toBeNull();
    expect(fixture.componentInstance.selectedIconFile()).not.toBeNull();
  });

  it('should reject unsupported icon file types', () => {
    // Arrange
    setWorkspace();
    fixture.detectChanges();

    for (const type of ['image/gif', 'application/pdf']) {
      // Act
      fixture.componentInstance.onIconSelected(iconEvent(iconFile(type)));

      // Assert
      expect(fixture.componentInstance.errorMessage()).toBe(
        'The icon must be a PNG, JPG, or SVG file.',
      );
      expect(fixture.componentInstance.selectedIconFile()).toBeNull();
    }
  });

  it('should accept PNG, JPG and SVG icons', () => {
    // Arrange
    setWorkspace();
    fixture.detectChanges();

    for (const type of ['image/png', 'image/jpeg', 'image/svg+xml']) {
      // Act
      fixture.componentInstance.onIconSelected(iconEvent(iconFile(type)));

      // Assert
      expect(fixture.componentInstance.errorMessage()).toBeNull();
      expect(fixture.componentInstance.selectedIconFile()?.type).toBe(type);
    }
  });

  it('should do nothing when the file picker is cancelled', () => {
    // Arrange
    setWorkspace();
    fixture.detectChanges();

    // Act
    fixture.componentInstance.onIconSelected(iconEvent(null));

    // Assert
    expect(fixture.componentInstance.selectedIconFile()).toBeNull();
    expect(fixture.componentInstance.errorMessage()).toBeNull();
    expect(fixture.componentInstance.iconPreviewUrl()).toBeNull();
  });

  it('should set a preview for a valid icon and revoke the previous preview', () => {
    // Arrange
    setWorkspace();
    fixture.detectChanges();
    const createSpy = vi
      .spyOn(URL, 'createObjectURL')
      .mockReturnValueOnce('blob:first')
      .mockReturnValueOnce('blob:second');
    const revokeSpy = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);

    // Act
    fixture.componentInstance.onIconSelected(iconEvent(iconFile('image/png')));
    fixture.componentInstance.onIconSelected(iconEvent(iconFile('image/jpeg')));

    // Assert
    expect(createSpy).toHaveBeenCalledTimes(2);
    expect(revokeSpy).toHaveBeenCalledWith('blob:first');
    expect(fixture.componentInstance.iconPreviewUrl()).toBe('blob:second');
  });

  it('should flag the icon for removal and clear any selection', () => {
    // Arrange
    setWorkspace();
    fixture.detectChanges();
    fixture.componentInstance.onIconSelected(iconEvent(iconFile('image/png')));

    // Act
    fixture.componentInstance.removeIcon();

    // Assert
    expect(fixture.componentInstance.removeIconRequested()).toBe(true);
    expect(fixture.componentInstance.selectedIconFile()).toBeNull();
    expect(fixture.componentInstance.iconPreviewUrl()).toBeNull();
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(true);
  });

  it('should clear the removal flag when a new icon is selected afterwards', () => {
    // Arrange
    setWorkspace();
    fixture.detectChanges();
    fixture.componentInstance.removeIcon();

    // Act
    fixture.componentInstance.onIconSelected(iconEvent(iconFile('image/png')));

    // Assert
    expect(fixture.componentInstance.removeIconRequested()).toBe(false);
    expect(fixture.componentInstance.selectedIconFile()).not.toBeNull();
  });

  it('should update the workspace and then upload the icon when a new icon is selected', () => {
    // Arrange
    setWorkspace();
    fixture.detectChanges();
    fixture.componentInstance.onIconSelected(iconEvent(iconFile('image/png')));

    // Act
    fixture.componentInstance.save();
    httpMock.expectOne({ method: 'PUT', url: '/api/workspaces/w1' }).flush(workspaceBody());
    const iconReq = httpMock.expectOne({ method: 'PUT', url: '/api/workspaces/w1/icon' });
    iconReq.flush(workspaceBody({ hasIcon: true }));
    httpMock.expectOne({ method: 'GET', url: '/api/workspaces/w1/icon' }).flush(new Blob(['x']));

    // Assert
    expect(iconReq.request.body instanceof FormData).toBe(true);
    expect(store.workspace()?.hasIcon).toBe(true);
    expect(fixture.componentInstance.selectedIconFile()).toBeNull();
    expect(fixture.componentInstance.successMessage()).toBe('Workspace updated.');
  });

  it('should update the workspace and then remove the icon when removal is requested', () => {
    // Arrange
    setWorkspace({ hasIcon: true });
    fixture.detectChanges();
    httpMock.expectOne({ method: 'GET', url: '/api/workspaces/w1/icon' }).flush(new Blob(['x']));
    fixture.componentInstance.removeIcon();

    // Act
    fixture.componentInstance.save();
    httpMock.expectOne({ method: 'PUT', url: '/api/workspaces/w1' }).flush(workspaceBody());
    httpMock
      .expectOne({ method: 'DELETE', url: '/api/workspaces/w1/icon' })
      .flush(workspaceBody({ hasIcon: false }));

    // Assert
    expect(store.workspace()?.hasIcon).toBe(false);
    expect(fixture.componentInstance.removeIconRequested()).toBe(false);
    expect(fixture.componentInstance.successMessage()).toBe('Workspace updated.');
  });

  it('should show an error and keep the changes unsaved when the icon upload fails', () => {
    // Arrange
    setWorkspace();
    fixture.detectChanges();
    fixture.componentInstance.onIconSelected(iconEvent(iconFile('image/png')));

    // Act
    fixture.componentInstance.save();
    httpMock.expectOne({ method: 'PUT', url: '/api/workspaces/w1' }).flush(workspaceBody());
    httpMock
      .expectOne({ method: 'PUT', url: '/api/workspaces/w1/icon' })
      .flush(null, { status: 500, statusText: 'Server Error' });

    // Assert
    expect(fixture.componentInstance.errorMessage()).toBe(
      'Could not save changes. Please try again.',
    );
    expect(fixture.componentInstance.isSaving()).toBe(false);
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(true);
    expect(fixture.componentInstance.successMessage()).toBeNull();
  });

  it('should show an error when updating the workspace fails', () => {
    // Arrange
    setWorkspace();
    fixture.detectChanges();

    // Act
    fixture.componentInstance.save();
    httpMock
      .expectOne('/api/workspaces/w1')
      .flush(null, { status: 500, statusText: 'Server Error' });

    // Assert
    expect(fixture.componentInstance.errorMessage()).toBe(
      'Could not save changes. Please try again.',
    );
    expect(fixture.componentInstance.isSaving()).toBe(false);
  });

  it('should not save when the user is not an admin', () => {
    // Arrange
    setWorkspace({ currentUserRole: 'Member' });
    fixture.detectChanges();

    // Act
    fixture.componentInstance.save();

    // Assert
    httpMock.expectNone('/api/workspaces/w1');
  });

  it('should not save when the form is invalid', () => {
    // Arrange
    setWorkspace();
    fixture.detectChanges();
    fixture.componentInstance.form.controls.name.setValue('');

    // Act
    fixture.componentInstance.save();

    // Assert
    httpMock.expectNone('/api/workspaces/w1');
  });

  it('should send a blank description as null and a filled one as text', () => {
    // Arrange
    setWorkspace();
    fixture.detectChanges();

    // Act
    fixture.componentInstance.form.controls.description.setValue('');
    fixture.componentInstance.save();
    const blankReq = httpMock.expectOne('/api/workspaces/w1');
    blankReq.flush(workspaceBody());
    fixture.componentInstance.form.controls.description.setValue('About us');
    fixture.componentInstance.save();
    const filledReq = httpMock.expectOne('/api/workspaces/w1');
    filledReq.flush(workspaceBody({ description: 'About us' }));

    // Assert
    expect(blankReq.request.body.description).toBeNull();
    expect(filledReq.request.body.description).toBe('About us');
  });

  it('should be pristine with a success message after a successful save', () => {
    // Arrange
    setWorkspace();
    fixture.detectChanges();
    fixture.componentInstance.form.controls.name.setValue('Renamed');
    fixture.componentInstance.form.controls.name.markAsDirty();

    // Act
    fixture.componentInstance.save();
    httpMock.expectOne('/api/workspaces/w1').flush(workspaceBody({ name: 'Renamed' }));

    // Assert
    expect(fixture.componentInstance.form.pristine).toBe(true);
    expect(fixture.componentInstance.hasUnsavedChanges()).toBe(false);
    expect(fixture.componentInstance.successMessage()).toBe('Workspace updated.');
  });

  it('should copy the workspace id and show the copied state for 2 seconds', async () => {
    // Arrange
    vi.useFakeTimers();
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, 'clipboard', { value: { writeText }, configurable: true });
    setWorkspace();
    fixture.detectChanges();

    // Act
    fixture.componentInstance.copyWorkspaceId();
    await Promise.resolve();
    await Promise.resolve();
    const copiedRightAfter = fixture.componentInstance.copiedId();
    vi.advanceTimersByTime(2000);

    // Assert
    expect(writeText).toHaveBeenCalledWith('w1');
    expect(copiedRightAfter).toBe(true);
    expect(fixture.componentInstance.copiedId()).toBe(false);
  });
});
