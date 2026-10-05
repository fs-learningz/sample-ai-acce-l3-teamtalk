import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { ChannelService } from '../../../core/channels/channel.service';
import { ChannelDetailsResponse } from '../../../core/models/channel.model';
import { WorkspaceResponse } from '../../../core/models/workspace.model';
import { WorkspaceService } from '../../../core/workspaces/workspace.service';
import { ChannelSettingsComponent } from './channel-settings.component';

@Component({ template: '' })
class ChannelViewStubComponent {}

const channel: ChannelDetailsResponse = {
  id: 'c1',
  workspaceId: 'w1',
  name: 'general',
  type: 'Public',
  createdByUserId: 'u1',
  createdByDisplayName: 'Jane',
  createdAtUtc: '2026-01-01T00:00:00Z',
  memberCount: 2,
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
  createdByDisplayName: 'Jane',
  createdAtUtc: '2026-01-01T00:00:00Z',
  memberCount: 2,
  currentUserRole: 'Admin',
};

describe('ChannelSettingsComponent', () => {
  let fixture: ComponentFixture<ChannelSettingsComponent>;
  const channelService = { getDetailsAsync: vi.fn() };
  const workspaceService = { getByIdAsync: vi.fn() };

  function createComponent() {
    fixture = TestBed.createComponent(ChannelSettingsComponent);
    fixture.detectChanges();

    return fixture;
  }

  beforeEach(async () => {
    channelService.getDetailsAsync.mockReset().mockReturnValue(of(channel));
    workspaceService.getByIdAsync.mockReset().mockReturnValue(of(workspace));

    await TestBed.configureTestingModule({
      imports: [ChannelSettingsComponent],
      providers: [
        provideRouter([
          { path: 'workspaces/:workspaceId/channels/:channelId', component: ChannelViewStubComponent },
        ]),
        { provide: ChannelService, useValue: channelService },
        { provide: WorkspaceService, useValue: workspaceService },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: convertToParamMap({ workspaceId: 'w1', channelId: 'c1' }) },
          },
        },
      ],
    }).compileComponents();
  });

  it('reads the workspace and channel ids from the route into the store', () => {
    const { componentInstance } = createComponent();

    expect(componentInstance.workspaceId).toBe('w1');
    expect(componentInstance.channelId).toBe('c1');
    expect(componentInstance.store.workspaceId).toBe('w1');
    expect(componentInstance.store.channelId).toBe('c1');
  });

  it('loads the channel and workspace into the shared store on init', () => {
    const { componentInstance } = createComponent();

    expect(channelService.getDetailsAsync).toHaveBeenCalledWith('w1', 'c1');
    expect(workspaceService.getByIdAsync).toHaveBeenCalledWith('w1');
    expect(componentInstance.store.channel()).toBe(channel);
    expect(componentInstance.store.workspace()).toBe(workspace);
    expect(componentInstance.errorMessage()).toBeNull();
  });

  it('renders the channel name in the header and links to the General and Danger sections', () => {
    createComponent();
    const element: HTMLElement = fixture.nativeElement;

    expect(element.querySelector('h1')?.textContent).toContain('general settings');
    const links = Array.from(element.querySelectorAll('aside a')).map((a) => a.textContent?.trim());
    expect(links).toEqual(['General', 'Danger']);
  });

  it('shows an error message when the channel fails to load', () => {
    channelService.getDetailsAsync.mockReturnValue(throwError(() => new Error('boom')));

    const { componentInstance } = createComponent();

    expect(componentInstance.errorMessage()).toBe('Could not load this channel.');
    expect(componentInstance.store.channel()).toBeNull();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Could not load this channel.');
  });

  it('shows an error message when the workspace fails to load', () => {
    workspaceService.getByIdAsync.mockReturnValue(throwError(() => new Error('boom')));

    const { componentInstance } = createComponent();

    expect(componentInstance.errorMessage()).toBe('Could not load this workspace.');
    expect(componentInstance.store.workspace()).toBeNull();
  });

  it('navigates back to the channel when requested', () => {
    const { componentInstance } = createComponent();
    const navigateSpy = vi.spyOn(TestBed.inject(Router), 'navigate');

    componentInstance.backToChannel();

    expect(navigateSpy).toHaveBeenCalledWith(['/workspaces', 'w1', 'channels', 'c1']);
  });
});
