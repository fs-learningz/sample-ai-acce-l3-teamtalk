import { Component } from '@angular/core';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { WorkspaceShellComponent } from './workspace-shell.component';

@Component({ template: '' })
class WorkspaceListStubComponent {}

@Component({ template: '' })
class WorkspaceSettingsStubComponent {}

describe('WorkspaceShellComponent', () => {
  let fixture: ComponentFixture<WorkspaceShellComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WorkspaceShellComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([
          { path: 'workspaces', component: WorkspaceListStubComponent },
          { path: 'workspaces/:workspaceId/settings', component: WorkspaceSettingsStubComponent },
        ]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ workspaceId: 'w1' }) } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(WorkspaceShellComponent);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should load the workspace and its channels on init', () => {
    fixture.detectChanges();

    httpMock.expectOne('/api/workspaces/w1').flush({
      id: 'w1',
      name: 'Engineering',
      createdByUserId: 'u1',
      createdAtUtc: new Date().toISOString(),
      hasIcon: false,
    });
    httpMock.expectOne('/api/workspaces/w1/channels').flush([]);
    httpMock.expectOne('/api/workspaces/w1/members').flush([]);

    expect(fixture.componentInstance.workspace()?.name).toBe('Engineering');
  });

  it('should load pending invitations when the current user is an admin', () => {
    fixture.detectChanges();

    httpMock.expectOne('/api/workspaces/w1').flush({
      id: 'w1',
      name: 'Engineering',
      createdByUserId: 'u1',
      createdAtUtc: new Date().toISOString(),
      hasIcon: false,
      currentUserRole: 'Admin',
    });
    httpMock.expectOne('/api/workspaces/w1/channels').flush([]);
    httpMock.expectOne('/api/workspaces/w1/members').flush([]);
    httpMock.expectOne('/api/workspaces/w1/invitations').flush([]);

    expect(fixture.componentInstance.isAdmin()).toBe(true);
  });

  it('should load workspace members on init', () => {
    fixture.detectChanges();

    httpMock.expectOne('/api/workspaces/w1').flush({
      id: 'w1',
      name: 'Engineering',
      createdByUserId: 'u1',
      createdAtUtc: new Date().toISOString(),
      hasIcon: false,
    });
    httpMock.expectOne('/api/workspaces/w1/channels').flush([]);
    httpMock.expectOne('/api/workspaces/w1/members').flush([
      {
        id: 'm1',
        workspaceId: 'w1',
        userId: 'u1',
        displayName: 'Owner User',
        email: 'owner@example.com',
        role: 'Admin',
        joinedAtUtc: new Date().toISOString(),
      },
    ]);

    expect(fixture.componentInstance.members().length).toBe(1);
  });

  it('should fetch the workspace icon as a blob when the workspace has one', () => {
    fixture.detectChanges();

    httpMock.expectOne('/api/workspaces/w1').flush({
      id: 'w1',
      name: 'Engineering',
      createdByUserId: 'u1',
      createdAtUtc: new Date().toISOString(),
      hasIcon: true,
    });
    httpMock.expectOne('/api/workspaces/w1/channels').flush([]);
    httpMock.expectOne('/api/workspaces/w1/members').flush([]);

    const iconReq = httpMock.expectOne('/api/workspaces/w1/icon');
    expect(iconReq.request.method).toBe('GET');
    iconReq.flush(new Blob(['icon']));

    expect(fixture.componentInstance.workspaceIconUrl()).toBeTruthy();
  });

  it('should navigate to workspace settings when the header is clicked', () => {
    fixture.detectChanges();

    httpMock.expectOne('/api/workspaces/w1').flush({
      id: 'w1',
      name: 'Engineering',
      createdByUserId: 'u1',
      createdAtUtc: new Date().toISOString(),
      hasIcon: false,
    });
    httpMock.expectOne('/api/workspaces/w1/channels').flush([]);
    httpMock.expectOne('/api/workspaces/w1/members').flush([]);

    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate');

    fixture.componentInstance.openWorkspaceSettings();

    expect(navigateSpy).toHaveBeenCalledWith(['/workspaces', 'w1', 'settings']);
  });

  function workspaceBody(overrides: Record<string, unknown> = {}) {
    return {
      id: 'w1',
      name: 'Engineering',
      createdByUserId: 'u1',
      createdAtUtc: new Date().toISOString(),
      hasIcon: false,
      ...overrides,
    };
  }

  function invitation(id: string, status: string) {
    return {
      id,
      workspaceId: 'w1',
      workspaceName: 'Engineering',
      workspaceHasIcon: false,
      email: `${id}@example.com`,
      role: 'Member',
      status,
      invitedByUserId: 'u1',
      createdAtUtc: new Date().toISOString(),
    };
  }

  /** Runs ngOnInit and answers the initial requests; admin also answers the invitations request. */
  function init(overrides: Record<string, unknown> = {}, invitations: unknown[] = []): void {
    fixture.detectChanges();
    httpMock.expectOne('/api/workspaces/w1').flush(workspaceBody(overrides));
    httpMock.expectOne('/api/workspaces/w1/channels').flush([]);
    httpMock.expectOne('/api/workspaces/w1/members').flush([]);
    if (overrides['currentUserRole'] === 'Admin') {
      httpMock.expectOne('/api/workspaces/w1/invitations').flush(invitations);
    }
  }

  it('should show an error when the workspace fails to load', () => {
    // Arrange
    fixture.detectChanges();

    // Act
    httpMock
      .expectOne('/api/workspaces/w1')
      .flush(null, { status: 500, statusText: 'Server Error' });
    httpMock.expectOne('/api/workspaces/w1/channels').flush([]);
    httpMock.expectOne('/api/workspaces/w1/members').flush([]);

    // Assert
    expect(fixture.componentInstance.errorMessage()).toBe('Could not load this workspace.');
    expect(fixture.componentInstance.workspace()).toBeNull();
  });

  it('should not load pending invitations for a non-admin', () => {
    // Arrange / Act
    init({ currentUserRole: 'Member' });

    // Assert
    httpMock.expectNone('/api/workspaces/w1/invitations');
    expect(fixture.componentInstance.isAdmin()).toBe(false);
  });

  it('should only keep invitations with a Pending status', () => {
    // Arrange / Act
    init({ currentUserRole: 'Admin' }, [
      invitation('i1', 'Pending'),
      invitation('i2', 'Accepted'),
      invitation('i3', 'Revoked'),
      invitation('i4', 'Pending'),
    ]);

    // Assert
    expect(fixture.componentInstance.invitations().map((i) => i.id)).toEqual(['i1', 'i4']);
  });

  it('should not create a channel when the name is empty', () => {
    // Arrange
    init();
    fixture.componentInstance.openCreateDialog();

    // Act
    fixture.componentInstance.onCreateSubmit();

    // Assert
    httpMock.expectNone('/api/workspaces/w1/channels');
  });

  it('should create a channel, close the dialog, reload the channels and open the new channel', () => {
    // Arrange
    init();
    const navigateSpy = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture.componentInstance.openCreateDialog();
    fixture.componentInstance.createForm.setValue({ name: 'general', type: 'Private' });

    // Act
    fixture.componentInstance.onCreateSubmit();
    const req = httpMock.expectOne('/api/workspaces/w1/channels');
    req.flush({
      id: 'c1',
      workspaceId: 'w1',
      name: 'general',
      type: 'Private',
      createdByUserId: 'u1',
      createdAtUtc: new Date().toISOString(),
    });
    httpMock.expectOne('/api/workspaces/w1/channels').flush([]);

    // Assert
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ name: 'general', type: 'Private' });
    expect(fixture.componentInstance.isCreateDialogVisible()).toBe(false);
    expect(navigateSpy).toHaveBeenCalledWith(['/workspaces', 'w1', 'channels', 'c1']);
  });

  it('should show an error and keep the create dialog open when creating a channel fails', () => {
    // Arrange
    init();
    fixture.componentInstance.openCreateDialog();
    fixture.componentInstance.createForm.setValue({ name: 'general', type: 'Public' });

    // Act
    fixture.componentInstance.onCreateSubmit();
    httpMock
      .expectOne('/api/workspaces/w1/channels')
      .flush(null, { status: 409, statusText: 'Conflict' });

    // Assert
    expect(fixture.componentInstance.errorMessage()).toBe(
      'Could not create the channel. Please try again.',
    );
    expect(fixture.componentInstance.isCreateDialogVisible()).toBe(true);
  });

  it('should not send an invitation when the email is invalid', () => {
    // Arrange
    init({ currentUserRole: 'Admin' });
    fixture.componentInstance.openInviteDialog();
    fixture.componentInstance.inviteForm.setValue({ email: 'not-an-email', role: 'Member' });

    // Act
    fixture.componentInstance.onInviteSubmit();

    // Assert
    httpMock.expectNone({ method: 'POST', url: '/api/workspaces/w1/invitations' });
  });

  it('should send an invitation, close the dialog and reload the invitations', () => {
    // Arrange
    init({ currentUserRole: 'Admin' });
    fixture.componentInstance.openInviteDialog();
    fixture.componentInstance.inviteForm.setValue({ email: 'new@example.com', role: 'Admin' });

    // Act
    fixture.componentInstance.onInviteSubmit();
    const req = httpMock.expectOne({ method: 'POST', url: '/api/workspaces/w1/invitations' });
    req.flush(invitation('i9', 'Pending'));
    httpMock
      .expectOne({ method: 'GET', url: '/api/workspaces/w1/invitations' })
      .flush([invitation('i9', 'Pending')]);

    // Assert
    expect(req.request.body).toEqual({ email: 'new@example.com', role: 'Admin' });
    expect(fixture.componentInstance.isInviteDialogVisible()).toBe(false);
    expect(fixture.componentInstance.invitations().length).toBe(1);
  });

  it('should show the already invited or member message when inviting fails', () => {
    // Arrange
    init({ currentUserRole: 'Admin' });
    fixture.componentInstance.openInviteDialog();
    fixture.componentInstance.inviteForm.setValue({ email: 'dup@example.com', role: 'Member' });

    // Act
    fixture.componentInstance.onInviteSubmit();
    httpMock
      .expectOne({ method: 'POST', url: '/api/workspaces/w1/invitations' })
      .flush(null, { status: 409, statusText: 'Conflict' });

    // Assert
    expect(fixture.componentInstance.inviteErrorMessage()).toBe(
      'Could not send the invitation. The user may already be invited or a member.',
    );
    expect(fixture.componentInstance.isInviteDialogVisible()).toBe(true);
  });

  it('should revoke an invitation and reload the invitations', () => {
    // Arrange
    init({ currentUserRole: 'Admin' }, [invitation('i1', 'Pending')]);

    // Act
    fixture.componentInstance.revokeInvitation(fixture.componentInstance.invitations()[0]);
    const req = httpMock.expectOne({ method: 'DELETE', url: '/api/workspaces/w1/invitations/i1' });
    req.flush(null);
    httpMock.expectOne({ method: 'GET', url: '/api/workspaces/w1/invitations' }).flush([]);

    // Assert
    expect(fixture.componentInstance.invitations()).toEqual([]);
  });

  it('should show an error when revoking an invitation fails', () => {
    // Arrange
    init({ currentUserRole: 'Admin' }, [invitation('i1', 'Pending')]);

    // Act
    fixture.componentInstance.revokeInvitation(fixture.componentInstance.invitations()[0]);
    httpMock
      .expectOne({ method: 'DELETE', url: '/api/workspaces/w1/invitations/i1' })
      .flush(null, { status: 500, statusText: 'Server Error' });

    // Assert
    expect(fixture.componentInstance.errorMessage()).toBe('Could not revoke the invitation.');
    expect(fixture.componentInstance.invitations().length).toBe(1);
  });

  it('should reset the forms and errors when the dialogs are reopened', () => {
    // Arrange
    init({ currentUserRole: 'Admin' });
    fixture.componentInstance.createForm.setValue({ name: 'old', type: 'Private' });
    fixture.componentInstance.inviteForm.setValue({ email: 'old@example.com', role: 'Admin' });
    fixture.componentInstance.errorMessage.set('stale');
    fixture.componentInstance.inviteErrorMessage.set('stale');

    // Act
    fixture.componentInstance.openCreateDialog();
    fixture.componentInstance.openInviteDialog();

    // Assert
    expect(fixture.componentInstance.createForm.getRawValue()).toEqual({ name: '', type: 'Public' });
    expect(fixture.componentInstance.inviteForm.getRawValue()).toEqual({ email: '', role: 'Member' });
    expect(fixture.componentInstance.errorMessage()).toBeNull();
    expect(fixture.componentInstance.inviteErrorMessage()).toBeNull();
    expect(fixture.componentInstance.isCreateDialogVisible()).toBe(true);
    expect(fixture.componentInstance.isInviteDialogVisible()).toBe(true);
  });

  it('should revoke the workspace icon blob URL when destroyed', () => {
    // Arrange
    const revokeSpy = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
    fixture.detectChanges();
    httpMock.expectOne('/api/workspaces/w1').flush(workspaceBody({ hasIcon: true }));
    httpMock.expectOne('/api/workspaces/w1/channels').flush([]);
    httpMock.expectOne('/api/workspaces/w1/members').flush([]);
    httpMock.expectOne('/api/workspaces/w1/icon').flush(new Blob(['icon']));
    const iconUrl = fixture.componentInstance.workspaceIconUrl();

    // Act
    fixture.destroy();

    // Assert
    expect(iconUrl).toBeTruthy();
    expect(revokeSpy).toHaveBeenCalledWith(iconUrl);
    revokeSpy.mockRestore();
  });
});
