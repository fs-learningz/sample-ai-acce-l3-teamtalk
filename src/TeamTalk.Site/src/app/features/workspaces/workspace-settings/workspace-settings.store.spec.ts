import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { MemberService } from '../../../core/members/member.service';
import { WorkspaceMemberResponse } from '../../../core/models/member.model';
import { WorkspaceResponse, WorkspaceRole } from '../../../core/models/workspace.model';
import { WorkspaceService } from '../../../core/workspaces/workspace.service';
import { WorkspaceSettingsStore } from './workspace-settings.store';

function buildWorkspace(role: WorkspaceRole | null): WorkspaceResponse {
  return {
    id: 'w1',
    name: 'Engineering',
    description: null,
    hasIcon: false,
    createdByUserId: 'u1',
    createdByDisplayName: 'Jane',
    createdAtUtc: '2026-01-01T00:00:00Z',
    memberCount: 2,
    currentUserRole: role,
  };
}

function buildMember(userId: string, role: WorkspaceRole): WorkspaceMemberResponse {
  return {
    id: `m-${userId}`,
    workspaceId: 'w1',
    userId,
    displayName: userId,
    email: `${userId}@example.com`,
    role,
    joinedAtUtc: '2026-01-01T00:00:00Z',
  };
}

describe('WorkspaceSettingsStore', () => {
  const workspaceService = {
    getByIdAsync: vi.fn(),
  };
  const memberService = {
    getForWorkspaceAsync: vi.fn(),
  };

  function createStore(currentUser: { userId: string } | null = { userId: 'u1' }) {
    TestBed.configureTestingModule({
      providers: [
        WorkspaceSettingsStore,
        { provide: WorkspaceService, useValue: workspaceService },
        { provide: MemberService, useValue: memberService },
        { provide: AuthService, useValue: { currentUser: signal(currentUser) } },
      ],
    });

    return TestBed.inject(WorkspaceSettingsStore);
  }

  beforeEach(() => {
    workspaceService.getByIdAsync.mockReset();
    memberService.getForWorkspaceAsync.mockReset();
  });

  it('starts with no workspace, no members and an empty workspace id', () => {
    const store = createStore();

    expect(store.workspaceId).toBe('');
    expect(store.workspace()).toBeNull();
    expect(store.members()).toEqual([]);
  });

  it('exposes the current user id from the auth service', () => {
    expect(createStore({ userId: 'u42' }).currentUserId()).toBe('u42');
  });

  it('exposes a null current user id when nobody is signed in', () => {
    expect(createStore(null).currentUserId()).toBeNull();
  });

  it('is not admin while no workspace is loaded', () => {
    expect(createStore().isAdmin()).toBe(false);
  });

  it('is admin when the loaded workspace says the current user role is Admin', () => {
    const store = createStore();
    store.workspace.set(buildWorkspace('Admin'));

    expect(store.isAdmin()).toBe(true);
  });

  it('is not admin when the current user role is Member', () => {
    const store = createStore();
    store.workspace.set(buildWorkspace('Member'));

    expect(store.isAdmin()).toBe(false);
  });

  it('is not admin when the current user role is unknown', () => {
    const store = createStore();
    store.workspace.set(buildWorkspace(null));

    expect(store.isAdmin()).toBe(false);
  });

  it('counts the admins among the members', () => {
    const store = createStore();
    store.members.set([buildMember('u1', 'Admin'), buildMember('u2', 'Admin'), buildMember('u3', 'Member')]);

    expect(store.adminCount()).toBe(2);
  });

  it('reports zero admins when there are no members', () => {
    expect(createStore().adminCount()).toBe(0);
  });

  it('is the only admin when the user is admin and exactly one admin exists', () => {
    const store = createStore();
    store.workspace.set(buildWorkspace('Admin'));
    store.members.set([buildMember('u1', 'Admin'), buildMember('u2', 'Member')]);

    expect(store.isOnlyAdmin()).toBe(true);
  });

  it('is not the only admin when another admin exists', () => {
    const store = createStore();
    store.workspace.set(buildWorkspace('Admin'));
    store.members.set([buildMember('u1', 'Admin'), buildMember('u2', 'Admin')]);

    expect(store.isOnlyAdmin()).toBe(false);
  });

  it('is not the only admin when the user is a regular member', () => {
    const store = createStore();
    store.workspace.set(buildWorkspace('Member'));
    store.members.set([buildMember('u1', 'Member'), buildMember('u2', 'Admin')]);

    expect(store.isOnlyAdmin()).toBe(false);
  });

  it('recomputes derived state when members change', () => {
    const store = createStore();
    store.workspace.set(buildWorkspace('Admin'));
    store.members.set([buildMember('u1', 'Admin')]);
    expect(store.isOnlyAdmin()).toBe(true);

    store.members.set([buildMember('u1', 'Admin'), buildMember('u2', 'Admin')]);

    expect(store.isOnlyAdmin()).toBe(false);
  });

  it('loads the workspace through the workspace service', () => {
    const workspace = buildWorkspace('Admin');
    workspaceService.getByIdAsync.mockReturnValue(of(workspace));
    const store = createStore();
    let result: WorkspaceResponse | undefined;

    store.loadWorkspace('w1').subscribe((w) => (result = w));

    expect(workspaceService.getByIdAsync).toHaveBeenCalledWith('w1');
    expect(result).toEqual(workspace);
  });

  it('loads the members through the member service', () => {
    const members = [buildMember('u1', 'Admin')];
    memberService.getForWorkspaceAsync.mockReturnValue(of(members));
    const store = createStore();
    let result: WorkspaceMemberResponse[] | undefined;

    store.loadMembers('w1').subscribe((m) => (result = m));

    expect(memberService.getForWorkspaceAsync).toHaveBeenCalledWith('w1');
    expect(result).toEqual(members);
  });
});
