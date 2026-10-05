import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ChannelService } from '../../../core/channels/channel.service';
import { ChannelDetailsResponse } from '../../../core/models/channel.model';
import { WorkspaceResponse, WorkspaceRole } from '../../../core/models/workspace.model';
import { WorkspaceService } from '../../../core/workspaces/workspace.service';
import { ChannelSettingsStore } from './channel-settings.store';

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

function buildChannel(isMember: boolean): ChannelDetailsResponse {
  return {
    id: 'c1',
    workspaceId: 'w1',
    name: 'general',
    type: 'Public',
    createdByUserId: 'u1',
    createdByDisplayName: 'Jane',
    createdAtUtc: '2026-01-01T00:00:00Z',
    memberCount: 2,
    isMember,
    isPinned: false,
    isMuted: false,
  };
}

describe('ChannelSettingsStore', () => {
  const channelService = {
    getDetailsAsync: vi.fn(),
  };
  const workspaceService = {
    getByIdAsync: vi.fn(),
  };

  function createStore() {
    TestBed.configureTestingModule({
      providers: [
        ChannelSettingsStore,
        { provide: ChannelService, useValue: channelService },
        { provide: WorkspaceService, useValue: workspaceService },
      ],
    });

    return TestBed.inject(ChannelSettingsStore);
  }

  beforeEach(() => {
    channelService.getDetailsAsync.mockReset();
    workspaceService.getByIdAsync.mockReset();
  });

  it('starts with no channel, no workspace and empty ids', () => {
    const store = createStore();

    expect(store.workspaceId).toBe('');
    expect(store.channelId).toBe('');
    expect(store.channel()).toBeNull();
    expect(store.workspace()).toBeNull();
  });

  it('is not workspace admin while no workspace is loaded', () => {
    expect(createStore().isWorkspaceAdmin()).toBe(false);
  });

  it('is workspace admin when the current user role is Admin', () => {
    const store = createStore();
    store.workspace.set(buildWorkspace('Admin'));

    expect(store.isWorkspaceAdmin()).toBe(true);
  });

  it('is not workspace admin when the current user role is Member', () => {
    const store = createStore();
    store.workspace.set(buildWorkspace('Member'));

    expect(store.isWorkspaceAdmin()).toBe(false);
  });

  it('is not workspace admin when the current user role is unknown', () => {
    const store = createStore();
    store.workspace.set(buildWorkspace(null));

    expect(store.isWorkspaceAdmin()).toBe(false);
  });

  it('is not a member while no channel is loaded', () => {
    expect(createStore().isMember()).toBe(false);
  });

  it('is a member when the loaded channel says the user is a member', () => {
    const store = createStore();
    store.channel.set(buildChannel(true));

    expect(store.isMember()).toBe(true);
  });

  it('is not a member when the loaded channel says the user is not a member', () => {
    const store = createStore();
    store.channel.set(buildChannel(false));

    expect(store.isMember()).toBe(false);
  });

  it('loads the channel details using the stored workspace and channel ids', () => {
    const channel = buildChannel(true);
    channelService.getDetailsAsync.mockReturnValue(of(channel));
    const store = createStore();
    store.workspaceId = 'w1';
    store.channelId = 'c1';
    let loaded: ChannelDetailsResponse | undefined;

    store.loadChannel().subscribe((result) => (loaded = result));

    expect(channelService.getDetailsAsync).toHaveBeenCalledWith('w1', 'c1');
    expect(loaded).toBe(channel);
  });

  it('loads the workspace using the stored workspace id', () => {
    const workspace = buildWorkspace('Admin');
    workspaceService.getByIdAsync.mockReturnValue(of(workspace));
    const store = createStore();
    store.workspaceId = 'w1';
    let loaded: WorkspaceResponse | undefined;

    store.loadWorkspace().subscribe((result) => (loaded = result));

    expect(workspaceService.getByIdAsync).toHaveBeenCalledWith('w1');
    expect(loaded).toBe(workspace);
  });
});
