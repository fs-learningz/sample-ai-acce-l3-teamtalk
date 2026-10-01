import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ConfirmationService } from 'primeng/api';
import { vi } from 'vitest';
import { AuthService } from '../../../core/auth/auth.service';
import { WorkspaceMemberResponse } from '../../../core/models/member.model';
import { WorkspaceResponse } from '../../../core/models/workspace.model';
import { WorkspaceSettingsMembersComponent } from './workspace-settings-members.component';
import { WorkspaceSettingsStore } from './workspace-settings.store';

describe('WorkspaceSettingsMembersComponent', () => {
  let fixture: ComponentFixture<WorkspaceSettingsMembersComponent>;
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
    memberCount: 2,
    currentUserRole: 'Admin',
  };

  function member(overrides: Partial<WorkspaceMemberResponse>): WorkspaceMemberResponse {
    return {
      id: overrides.userId ?? 'm',
      workspaceId: 'w1',
      userId: 'u1',
      displayName: 'User',
      email: 'user@example.com',
      role: 'Member',
      joinedAtUtc: new Date().toISOString(),
      ...overrides,
    };
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WorkspaceSettingsMembersComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        WorkspaceSettingsStore,
        {
          provide: AuthService,
          useValue: { currentUser: () => ({ userId: 'u1', email: '', displayName: '' }) },
        },
      ],
    }).compileComponents();

    store = TestBed.inject(WorkspaceSettingsStore);
    store.workspaceId = 'w1';
    store.workspace.set(workspace);

    fixture = TestBed.createComponent(WorkspaceSettingsMembersComponent);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should filter members by name or email', () => {
    store.members.set([
      member({ userId: 'u1', displayName: 'Ada Lovelace', email: 'ada@example.com' }),
      member({ userId: 'u2', displayName: 'Bob Smith', email: 'bob@example.com' }),
    ]);
    fixture.detectChanges();

    fixture.componentInstance.searchTerm.set('bob');

    expect(fixture.componentInstance.filteredMembers().length).toBe(1);
    expect(fixture.componentInstance.filteredMembers()[0].displayName).toBe('Bob Smith');
  });

  it('should disable the current user’s own role dropdown when they are the only admin', () => {
    store.members.set([
      member({ userId: 'u1', displayName: 'Ada Lovelace', role: 'Admin' }),
      member({ userId: 'u2', displayName: 'Bob Smith', role: 'Member' }),
    ]);
    fixture.detectChanges();

    const adminMember = store.members()[0];
    expect(fixture.componentInstance.isRoleDropdownDisabled(adminMember)).toBe(true);
  });

  it('should call the change-role endpoint when a role is changed', () => {
    store.members.set([member({ userId: 'u2', displayName: 'Bob Smith', role: 'Member' })]);
    fixture.detectChanges();

    fixture.componentInstance.onRoleChange(store.members()[0], 'Admin');

    const req = httpMock.expectOne('/api/workspaces/w1/members/u2/role');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ role: 'Admin' });
    req.flush(null);

    httpMock.expectOne('/api/workspaces/w1/members').flush([]);
  });

  function mockConfirm(decision: 'accept' | 'reject'): ReturnType<typeof vi.spyOn> {
    const confirmationService = fixture.debugElement.injector.get(ConfirmationService);

    return vi.spyOn(confirmationService, 'confirm').mockImplementation((confirmation) => {
      if (decision === 'accept') {
        confirmation.accept?.();
      } else {
        confirmation.reject?.();
      }
      return confirmationService;
    });
  }

  it('should not call the API when the role is unchanged', () => {
    // Arrange
    store.members.set([member({ userId: 'u2', role: 'Member' })]);
    fixture.detectChanges();

    // Act
    fixture.componentInstance.onRoleChange(store.members()[0], 'Member');

    // Assert
    httpMock.expectNone('/api/workspaces/w1/members/u2/role');
  });

  it('should refresh the members in the store after a role change', () => {
    // Arrange
    store.members.set([member({ userId: 'u2', role: 'Member' })]);
    fixture.detectChanges();

    // Act
    fixture.componentInstance.onRoleChange(store.members()[0], 'Admin');
    httpMock.expectOne('/api/workspaces/w1/members/u2/role').flush(null);
    httpMock.expectOne('/api/workspaces/w1/members').flush([member({ userId: 'u2', role: 'Admin' })]);

    // Assert
    expect(store.members()[0].role).toBe('Admin');
  });

  it('should show an error when changing a role fails', () => {
    // Arrange
    store.members.set([member({ userId: 'u2', role: 'Member' })]);
    fixture.detectChanges();

    // Act
    fixture.componentInstance.onRoleChange(store.members()[0], 'Admin');
    httpMock
      .expectOne('/api/workspaces/w1/members/u2/role')
      .flush(null, { status: 400, statusText: 'Bad Request' });

    // Assert
    expect(fixture.componentInstance.errorMessage()).toBe('Could not change this member’s role.');
    expect(store.members()[0].role).toBe('Member');
  });

  it('should remove a member and refresh the list once the removal is confirmed', () => {
    // Arrange
    store.members.set([member({ userId: 'u2', displayName: 'Bob' })]);
    fixture.detectChanges();
    const confirmSpy = mockConfirm('accept');

    // Act
    fixture.componentInstance.removeMember(store.members()[0]);
    const req = httpMock.expectOne('/api/workspaces/w1/members/u2');
    req.flush(null);
    httpMock.expectOne('/api/workspaces/w1/members').flush([]);

    // Assert
    expect(req.request.method).toBe('DELETE');
    expect(confirmSpy.mock.calls[0][0].message).toBe('Remove Bob from Engineering?');
    expect(store.members()).toEqual([]);
  });

  it('should not remove a member when the confirmation is rejected', () => {
    // Arrange
    store.members.set([member({ userId: 'u2' })]);
    fixture.detectChanges();
    mockConfirm('reject');

    // Act
    fixture.componentInstance.removeMember(store.members()[0]);

    // Assert
    httpMock.expectNone('/api/workspaces/w1/members/u2');
    expect(store.members().length).toBe(1);
  });

  it('should show an error when removing a member fails', () => {
    // Arrange
    store.members.set([member({ userId: 'u2' })]);
    fixture.detectChanges();
    mockConfirm('accept');

    // Act
    fixture.componentInstance.removeMember(store.members()[0]);
    httpMock
      .expectOne('/api/workspaces/w1/members/u2')
      .flush(null, { status: 500, statusText: 'Server Error' });

    // Assert
    expect(fixture.componentInstance.errorMessage()).toBe('Could not remove this member.');
  });

  it('should show all members for an empty or whitespace search', () => {
    // Arrange
    store.members.set([member({ userId: 'u1' }), member({ userId: 'u2' })]);
    fixture.detectChanges();

    // Act
    fixture.componentInstance.searchTerm.set('   ');

    // Assert
    expect(fixture.componentInstance.filteredMembers().length).toBe(2);
  });

  it('should return no members when the search matches nothing', () => {
    // Arrange
    store.members.set([member({ userId: 'u1', displayName: 'Ada', email: 'ada@example.com' })]);
    fixture.detectChanges();

    // Act
    fixture.componentInstance.searchTerm.set('zzz');

    // Assert
    expect(fixture.componentInstance.filteredMembers()).toEqual([]);
  });

  it('should search case-insensitively', () => {
    // Arrange
    store.members.set([member({ userId: 'u1', displayName: 'Ada Lovelace', email: 'ada@example.com' })]);
    fixture.detectChanges();

    // Act
    fixture.componentInstance.searchTerm.set('LOVELACE');

    // Assert
    expect(fixture.componentInstance.filteredMembers().length).toBe(1);
  });

  it('should keep the role dropdown enabled for other members even when the user is the only admin', () => {
    // Arrange
    store.members.set([
      member({ userId: 'u1', role: 'Admin' }),
      member({ userId: 'u2', role: 'Member' }),
    ]);
    fixture.detectChanges();

    // Act
    const disabledForOther = fixture.componentInstance.isRoleDropdownDisabled(store.members()[1]);

    // Assert
    expect(disabledForOther).toBe(false);
  });
});
