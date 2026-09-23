import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
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
});
