import { Component, OnDestroy, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterOutlet } from '@angular/router';
import { Subscription } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { Popover, PopoverModule } from 'primeng/popover';
import { SelectModule } from 'primeng/select';
import {
  ChannelResponse,
  ChannelType,
  UpdateChannelPreferencesRequest,
} from '../../../core/models/channel.model';
import { WorkspaceResponse, WorkspaceRole } from '../../../core/models/workspace.model';
import { WorkspaceInvitationResponse } from '../../../core/models/invitation.model';
import { WorkspaceMemberResponse } from '../../../core/models/member.model';
import { ChannelService } from '../../../core/channels/channel.service';
import { WorkspaceService } from '../../../core/workspaces/workspace.service';
import { InvitationService } from '../../../core/invitations/invitation.service';
import { AuthService } from '../../../core/auth/auth.service';
import { ChannelActivity, SignalRService } from '../../../core/realtime/signalr.service';
import { MemberService } from '../../../core/members/member.service';
import { UserMenuComponent } from '../../../shared/user-menu/user-menu.component';
import { WorkspaceIconComponent } from '../../../shared/workspace-icon/workspace-icon.component';

function compareOrdinal(a: string, b: string): number {
  return a < b ? -1 : a > b ? 1 : 0;
}

interface ChannelTypeOption {
  label: string;
  value: ChannelType;
}

interface WorkspaceRoleOption {
  label: string;
  value: WorkspaceRole;
}

@Component({
  selector: 'app-workspace-shell',
  imports: [
    ReactiveFormsModule,
    RouterOutlet,
    ButtonModule,
    DialogModule,
    InputTextModule,
    PopoverModule,
    SelectModule,
    UserMenuComponent,
    WorkspaceIconComponent,
  ],
  templateUrl: './workspace-shell.component.html',
})
export class WorkspaceShellComponent implements OnInit, OnDestroy {
  private readonly formBuilder = inject(FormBuilder);
  private readonly workspaceService = inject(WorkspaceService);
  private readonly channelService = inject(ChannelService);
  private readonly invitationService = inject(InvitationService);
  private readonly memberService = inject(MemberService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly signalRService = inject(SignalRService);
  private readonly popover = viewChild.required(Popover);
  private readonly subscriptions = new Subscription();

  readonly workspaceId = this.route.snapshot.paramMap.get('workspaceId')!;
  readonly workspace = signal<WorkspaceResponse | null>(null);
  readonly workspaceIconUrl = signal<string | null>(null);
  readonly channels = signal<ChannelResponse[]>([]);
  readonly invitations = signal<WorkspaceInvitationResponse[]>([]);
  readonly members = signal<WorkspaceMemberResponse[]>([]);
  readonly menuChannel = signal<ChannelResponse | null>(null);
  readonly isCreateDialogVisible = signal(false);
  readonly isInviteDialogVisible = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly inviteErrorMessage = signal<string | null>(null);

  readonly isAdmin = computed(() => this.workspace()?.currentUserRole === 'Admin');

  readonly channelTypes: ChannelTypeOption[] = [
    { label: 'Public', value: 'Public' },
    { label: 'Private', value: 'Private' },
  ];

  readonly workspaceRoles: WorkspaceRoleOption[] = [
    { label: 'Member', value: 'Member' },
    { label: 'Admin', value: 'Admin' },
  ];

  readonly createForm = this.formBuilder.nonNullable.group({
    name: ['', Validators.required],
    type: this.formBuilder.nonNullable.control<ChannelType>('Public', Validators.required),
  });

  readonly inviteForm = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    role: this.formBuilder.nonNullable.control<WorkspaceRole>('Member', Validators.required),
  });

  ngOnInit(): void {
    this.workspaceService.getByIdAsync(this.workspaceId).subscribe({
      next: (workspace) => {
        this.workspace.set(workspace);
        if (workspace.currentUserRole === 'Admin') {
          this.loadInvitations();
        }
        if (workspace.hasIcon) {
          this.loadWorkspaceIcon();
        }
      },
      error: () => this.errorMessage.set('Could not load this workspace.'),
    });
    this.loadChannels();
    this.loadMembers();

    this.subscriptions.add(
      this.channelService.channelRead$.subscribe((channelId) =>
        this.patchChannel(channelId, { hasUnread: false }),
      ),
    );
    this.subscriptions.add(
      this.signalRService.channelActivity$.subscribe((activity) => this.onChannelActivity(activity)),
    );
    void this.joinWorkspaceRealtime();
    this.subscriptions.add(
      this.channelService.channelRemoved$.subscribe(() => this.loadChannels()),
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    void this.signalRService.leaveWorkspaceAsync(this.workspaceId).catch(() => undefined);
    this.revokeWorkspaceIconUrl();
  }

  openWorkspaceSettings(): void {
    void this.router.navigate(['/workspaces', this.workspaceId, 'settings']);
  }

  private loadWorkspaceIcon(): void {
    this.workspaceService.getIconAsync(this.workspaceId).subscribe({
      next: (blob) => {
        this.revokeWorkspaceIconUrl();
        this.workspaceIconUrl.set(URL.createObjectURL(blob));
      },
      error: () => this.workspaceIconUrl.set(null),
    });
  }

  private revokeWorkspaceIconUrl(): void {
    const url = this.workspaceIconUrl();
    if (url) {
      URL.revokeObjectURL(url);
    }
  }

  private loadMembers(): void {
    this.memberService.getForWorkspaceAsync(this.workspaceId).subscribe({
      next: (members) => this.members.set(members),
      error: () => this.errorMessage.set('Could not load workspace members.'),
    });
  }

  openInviteDialog(): void {
    this.inviteForm.reset({ email: '', role: 'Member' });
    this.inviteErrorMessage.set(null);
    this.isInviteDialogVisible.set(true);
  }

  onInviteSubmit(): void {
    if (this.inviteForm.invalid) {
      return;
    }

    this.inviteErrorMessage.set(null);
    this.invitationService.inviteAsync(this.workspaceId, this.inviteForm.getRawValue()).subscribe({
      next: () => {
        this.isInviteDialogVisible.set(false);
        this.loadInvitations();
      },
      error: () => {
        this.inviteErrorMessage.set(
          'Could not send the invitation. The user may already be invited or a member.',
        );
      },
    });
  }

  revokeInvitation(invitation: WorkspaceInvitationResponse): void {
    this.invitationService.revokeAsync(this.workspaceId, invitation.id).subscribe({
      next: () => this.loadInvitations(),
      error: () => this.errorMessage.set('Could not revoke the invitation.'),
    });
  }

  private loadInvitations(): void {
    this.invitationService.getForWorkspaceAsync(this.workspaceId).subscribe({
      next: (invitations) =>
        this.invitations.set(invitations.filter((i) => i.status === 'Pending')),
      error: () => this.errorMessage.set('Could not load pending invitations.'),
    });
  }

  openCreateDialog(): void {
    this.createForm.reset({ name: '', type: 'Public' });
    this.errorMessage.set(null);
    this.isCreateDialogVisible.set(true);
  }

  onCreateSubmit(): void {
    if (this.createForm.invalid) {
      return;
    }

    this.errorMessage.set(null);
    this.channelService.createAsync(this.workspaceId, this.createForm.getRawValue()).subscribe({
      next: (channel) => {
        this.isCreateDialogVisible.set(false);
        this.loadChannels();
        this.openChannel(channel);
      },
      error: () => {
        this.errorMessage.set('Could not create the channel. Please try again.');
      },
    });
  }

  private async joinWorkspaceRealtime(): Promise<void> {
    try {
      await this.signalRService.connectAsync();
      await this.signalRService.joinWorkspaceAsync(this.workspaceId);
    } catch {
      // Live unread indicators are a nicety; the list is still correct after the next reload.
    }
  }

  private onChannelActivity(activity: ChannelActivity): void {
    if (activity.senderId === this.authService.currentUser()?.userId) {
      return;
    }

    const channel = this.channels().find((c) => c.id === activity.channelId);

    if (!channel) {
      // A channel we have not seen yet (for example one created after this page loaded).
      this.loadChannels();
      return;
    }

    if (this.router.url.includes(`/channels/${activity.channelId}`)) {
      // The user is looking at this channel right now, so the message is read on arrival.
      this.channelService.markReadAsync(this.workspaceId, activity.channelId).subscribe({
        error: () => this.patchChannel(activity.channelId, { hasUnread: true }),
      });
      return;
    }

    this.patchChannel(activity.channelId, { hasUnread: true });
  }

  toggleChannelMenu(event: Event, channel: ChannelResponse): void {
    this.menuChannel.set(channel);
    this.popover().toggle(event);
  }

  markAsRead(channel: ChannelResponse): void {
    this.popover().hide();
    this.channelService.markReadAsync(this.workspaceId, channel.id).subscribe({
      error: () => this.errorMessage.set('Could not mark the channel as read.'),
    });
  }

  toggleMute(channel: ChannelResponse): void {
    this.updatePreferences(channel, { isPinned: channel.isPinned, isMuted: !channel.isMuted });
  }

  togglePin(channel: ChannelResponse): void {
    this.updatePreferences(channel, { isPinned: !channel.isPinned, isMuted: channel.isMuted });
  }

  openChannelSettings(channel: ChannelResponse): void {
    this.popover().hide();
    void this.router.navigate(['/workspaces', this.workspaceId, 'channels', channel.id, 'settings']);
  }

  private updatePreferences(channel: ChannelResponse, preferences: UpdateChannelPreferencesRequest): void {
    this.popover().hide();
    this.errorMessage.set(null);
    this.channelService.updatePreferencesAsync(this.workspaceId, channel.id, preferences).subscribe({
      next: () => this.patchChannel(channel.id, preferences),
      error: () => this.errorMessage.set('Could not update the channel. Please try again.'),
    });
  }

  private patchChannel(channelId: string, changes: Partial<ChannelResponse>): void {
    this.channels.update((current) =>
      current
        .map((c) => (c.id === channelId ? { ...c, ...changes } : c))
        // Same order as the server: pinned first, then by name (ordinal).
        .sort((a, b) => Number(b.isPinned) - Number(a.isPinned) || compareOrdinal(a.name, b.name)),
    );
  }

  openChannel(channel: ChannelResponse): void {
    void this.router.navigate(['/workspaces', this.workspaceId, 'channels', channel.id]);
  }

  private loadChannels(): void {
    this.channelService.getVisibleAsync(this.workspaceId).subscribe({
      next: (channels) => this.channels.set(channels),
      error: () => this.errorMessage.set('Could not load channels for this workspace.'),
    });
  }
}
