import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
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
});
