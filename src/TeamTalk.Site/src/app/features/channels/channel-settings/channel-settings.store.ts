import { Injectable, computed, inject, signal } from '@angular/core';
import { ChannelService } from '../../../core/channels/channel.service';
import { ChannelDetailsResponse } from '../../../core/models/channel.model';
import { WorkspaceResponse } from '../../../core/models/workspace.model';
import { WorkspaceService } from '../../../core/workspaces/workspace.service';

/**
 * Shared, route-scoped state for the Channel Settings page so the
 * General and Danger sections don't each re-fetch the channel and workspace.
 */
@Injectable()
export class ChannelSettingsStore {
  private readonly channelService = inject(ChannelService);
  private readonly workspaceService = inject(WorkspaceService);

  workspaceId = '';
  channelId = '';

  readonly channel = signal<ChannelDetailsResponse | null>(null);
  readonly workspace = signal<WorkspaceResponse | null>(null);

  readonly isWorkspaceAdmin = computed(() => this.workspace()?.currentUserRole === 'Admin');
  readonly isMember = computed(() => this.channel()?.isMember ?? false);

  loadChannel() {
    return this.channelService.getDetailsAsync(this.workspaceId, this.channelId);
  }

  loadWorkspace() {
    return this.workspaceService.getByIdAsync(this.workspaceId);
  }
}
