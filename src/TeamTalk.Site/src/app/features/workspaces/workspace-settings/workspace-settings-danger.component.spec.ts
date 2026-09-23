import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { ConfirmationService } from 'primeng/api';
import { vi } from 'vitest';
import { WorkspaceMemberResponse } from '../../../core/models/member.model';
import { WorkspaceResponse } from '../../../core/models/workspace.model';
import { WorkspaceSettingsDangerComponent } from './workspace-settings-danger.component';
import { WorkspaceSettingsStore } from './workspace-settings.store';

describe('WorkspaceSettingsDangerComponent', () => {
  let fixture: ComponentFixture<WorkspaceSettingsDangerComponent>;
  let httpMock: HttpTestingController;
  let store: WorkspaceSettingsStore;

  const workspace: WorkspaceResponse = {
    id: 'w1',
    name: 'Engineering',
    description: null,
    hasIcon: false,
    createdByUserId: 'u1',
    createdByDisplayName: 'Ada',
    createdAtUtc: new Date().toISOString(),
    memberCount: 1,
    currentUserRole: 'Admin',
  };

  function admin(userId: string): WorkspaceMemberResponse {
    return {
      id: userId,
      workspaceId: 'w1',
      userId,
      displayName: 'Admin',
      email: 'admin@example.com',
      role: 'Admin',
      joinedAtUtc: new Date().toISOString(),
    };
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WorkspaceSettingsDangerComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        WorkspaceSettingsStore,
      ],
    }).compileComponents();

    store = TestBed.inject(WorkspaceSettingsStore);
    store.workspaceId = 'w1';
    store.workspace.set(workspace);

    fixture = TestBed.createComponent(WorkspaceSettingsDangerComponent);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should not allow delete until the workspace name is typed exactly', () => {
    fixture.detectChanges();

    expect(fixture.componentInstance.canDelete()).toBe(false);

    fixture.componentInstance.deleteConfirmationText.set('Engineering');

    expect(fixture.componentInstance.canDelete()).toBe(true);
  });

  it('should delete the workspace and navigate to the workspace list', () => {
    fixture.detectChanges();
    fixture.componentInstance.deleteConfirmationText.set('Engineering');

    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate');

    fixture.componentInstance.deleteWorkspace();

    const req = httpMock.expectOne('/api/workspaces/w1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);

    expect(navigateSpy).toHaveBeenCalledWith(['/workspaces']);
  });

  it('should hide leave as an option when the current user is the only admin', () => {
    store.members.set([admin('u1')]);
    fixture.detectChanges();

    expect(fixture.componentInstance.canLeave()).toBe(false);
  });

  it('should leave the workspace on confirmation and navigate to the workspace list', () => {
    store.members.set([admin('u1'), admin('u2')]);
    fixture.detectChanges();

    expect(fixture.componentInstance.canLeave()).toBe(true);

    const confirmationService = fixture.debugElement.injector.get(ConfirmationService);
    vi.spyOn(confirmationService, 'confirm').mockImplementation((confirmation) => {
      confirmation.accept?.();
      return confirmationService;
    });

    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate');

    fixture.componentInstance.leaveWorkspace();

    const req = httpMock.expectOne('/api/workspaces/w1/members/leave');
    expect(req.request.method).toBe('POST');
    req.flush(null);

    expect(navigateSpy).toHaveBeenCalledWith(['/workspaces']);
  });
});
