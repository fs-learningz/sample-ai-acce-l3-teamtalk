import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { ConfirmationService } from 'primeng/api';
import { vi } from 'vitest';
import { ChannelDetailsResponse } from '../../../core/models/channel.model';
import { WorkspaceResponse } from '../../../core/models/workspace.model';
import { ChannelSettingsDangerComponent } from './channel-settings-danger.component';
import { ChannelSettingsStore } from './channel-settings.store';

describe('ChannelSettingsDangerComponent', () => {
  let fixture: ComponentFixture<ChannelSettingsDangerComponent>;
  let httpMock: HttpTestingController;
  let store: ChannelSettingsStore;

  const channel: ChannelDetailsResponse = {
    id: 'c1',
    workspaceId: 'w1',
    name: 'general',
    type: 'Public',
    createdByUserId: 'u1',
    createdByDisplayName: 'Ada',
    createdAtUtc: new Date().toISOString(),
    memberCount: 3,
    isMember: true,
    isPinned: false,
    isMuted: false,
  };

  const workspace: WorkspaceResponse = {
    id: 'w1',
    name: 'Engineering',
    description: null,
    hasIcon: false,
    createdByUserId: 'u1',
    createdByDisplayName: 'Ada',
    createdAtUtc: new Date().toISOString(),
    memberCount: 3,
    currentUserRole: 'Member',
  };

  function setup(
    channelOverrides: Partial<ChannelDetailsResponse> = {},
    role: 'Admin' | 'Member' = 'Member',
  ): void {
    store.channel.set({ ...channel, ...channelOverrides });
    store.workspace.set({ ...workspace, currentUserRole: role });
    fixture.detectChanges();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChannelSettingsDangerComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        ChannelSettingsStore,
      ],
    }).compileComponents();

    store = TestBed.inject(ChannelSettingsStore);
    store.workspaceId = 'w1';
    store.channelId = 'c1';

    fixture = TestBed.createComponent(ChannelSettingsDangerComponent);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should show Leave but not Delete for a member who is not an admin', () => {
    setup({}, 'Member');

    const text = fixture.nativeElement.textContent as string;

    expect(text).toContain('Leave channel');
    expect(text).not.toContain('Delete channel');
  });

  it('should show Delete for a workspace admin', () => {
    setup({}, 'Admin');

    expect(fixture.nativeElement.textContent).toContain('Delete channel');
  });

  it('should hide Leave when the user is not a member of the channel', () => {
    setup({ isMember: false }, 'Admin');

    expect(fixture.nativeElement.textContent).not.toContain('Leave channel');
  });

  it('should not allow delete until the channel name is typed exactly', () => {
    setup({}, 'Admin');

    expect(fixture.componentInstance.canDelete()).toBe(false);

    fixture.componentInstance.deleteConfirmationText.set('general');

    expect(fixture.componentInstance.canDelete()).toBe(true);
  });

  it('should not call the API when deleting without the confirmation text', () => {
    setup({}, 'Admin');

    fixture.componentInstance.deleteChannel();

    httpMock.expectNone('/api/workspaces/w1/channels/c1');
  });

  it('should delete the channel and navigate back to the workspace', () => {
    setup({}, 'Admin');
    const navigateSpy = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture.componentInstance.deleteConfirmationText.set('general');

    fixture.componentInstance.deleteChannel();
    const req = httpMock.expectOne('/api/workspaces/w1/channels/c1');
    req.flush(null);

    expect(req.request.method).toBe('DELETE');
    expect(navigateSpy).toHaveBeenCalledWith(['/workspaces', 'w1']);
  });

  it('should show an error when deleting fails', () => {
    setup({}, 'Admin');
    fixture.componentInstance.deleteConfirmationText.set('general');

    fixture.componentInstance.deleteChannel();
    httpMock
      .expectOne('/api/workspaces/w1/channels/c1')
      .flush(null, { status: 500, statusText: 'Server Error' });

    expect(fixture.componentInstance.errorMessage()).toBe('Could not delete this channel.');
    expect(fixture.componentInstance.isDeleting()).toBe(false);
  });

  it('should leave the channel after confirmation and navigate back to the workspace', () => {
    setup({}, 'Member');
    const navigateSpy = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    const confirmation = fixture.debugElement.injector.get(ConfirmationService);
    vi.spyOn(confirmation, 'confirm').mockImplementation((c) => {
      c.accept?.();
      return confirmation;
    });

    fixture.componentInstance.leaveChannel();
    const req = httpMock.expectOne('/api/workspaces/w1/channels/c1/leave');
    req.flush(null);

    expect(req.request.method).toBe('POST');
    expect(navigateSpy).toHaveBeenCalledWith(['/workspaces', 'w1']);
  });
});
