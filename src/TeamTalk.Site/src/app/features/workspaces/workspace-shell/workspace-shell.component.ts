import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { ChannelResponse, ChannelType } from '../../../core/models/channel.model';
import { WorkspaceResponse, WorkspaceRole } from '../../../core/models/workspace.model';
import { WorkspaceInvitationResponse } from '../../../core/models/invitation.model';
import { WorkspaceMemberResponse } from '../../../core/models/member.model';
import { ChannelService } from '../../../core/channels/channel.service';
import { WorkspaceService } from '../../../core/workspaces/workspace.service';
import { InvitationService } from '../../../core/invitations/invitation.service';
import { MemberService } from '../../../core/members/member.service';
import { UserMenuComponent } from '../../../shared/user-menu/user-menu.component';
import { WorkspaceIconComponent } from '../../../shared/workspace-icon/workspace-icon.component';

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

  readonly workspaceId = this.route.snapshot.paramMap.get('workspaceId')!;
  readonly workspace = signal<WorkspaceResponse | null>(null);
  readonly workspaceIconUrl = signal<string | null>(null);
  readonly channels = signal<ChannelResponse[]>([]);
  readonly invitations = signal<WorkspaceInvitationResponse[]>([]);
  readonly members = signal<WorkspaceMemberResponse[]>([]);
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
  }

  ngOnDestroy(): void {
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
