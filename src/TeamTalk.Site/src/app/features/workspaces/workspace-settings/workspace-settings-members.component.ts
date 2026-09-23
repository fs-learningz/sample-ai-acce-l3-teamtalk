import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ConfirmationService } from 'primeng/api';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TooltipModule } from 'primeng/tooltip';
import { WorkspaceMemberResponse } from '../../../core/models/member.model';
import { WorkspaceRole } from '../../../core/models/workspace.model';
import { MemberService } from '../../../core/members/member.service';
import { UserAvatarComponent } from '../../../shared/user-avatar/user-avatar.component';
import { WorkspaceSettingsStore } from './workspace-settings.store';

interface WorkspaceRoleOption {
  label: string;
  value: WorkspaceRole;
}

@Component({
  selector: 'app-workspace-settings-members',
  imports: [
    DatePipe,
    FormsModule,
    ConfirmDialogModule,
    InputTextModule,
    SelectModule,
    TooltipModule,
    UserAvatarComponent,
  ],
  providers: [ConfirmationService],
  templateUrl: './workspace-settings-members.component.html',
})
export class WorkspaceSettingsMembersComponent {
  private readonly memberService = inject(MemberService);
  private readonly confirmationService = inject(ConfirmationService);
  protected readonly store = inject(WorkspaceSettingsStore);

  readonly searchTerm = signal('');
  readonly errorMessage = signal<string | null>(null);

  readonly roleOptions: WorkspaceRoleOption[] = [
    { label: 'Member', value: 'Member' },
    { label: 'Admin', value: 'Admin' },
  ];

  readonly filteredMembers = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    if (!term) {
      return this.store.members();
    }

    return this.store
      .members()
      .filter(
        (m) => m.displayName.toLowerCase().includes(term) || m.email.toLowerCase().includes(term),
      );
  });

  isRoleDropdownDisabled(member: WorkspaceMemberResponse): boolean {
    return member.userId === this.store.currentUserId() && this.store.isOnlyAdmin();
  }

  onRoleChange(member: WorkspaceMemberResponse, role: WorkspaceRole): void {
    if (role === member.role) {
      return;
    }

    this.errorMessage.set(null);
    this.memberService.changeRoleAsync(this.store.workspaceId, member.userId, role).subscribe({
      next: () => this.refreshMembers(),
      error: () => this.errorMessage.set('Could not change this member’s role.'),
    });
  }

  removeMember(member: WorkspaceMemberResponse): void {
    const workspaceName = this.store.workspace()?.name ?? 'this workspace';

    this.confirmationService.confirm({
      header: 'Remove member',
      message: `Remove ${member.displayName} from ${workspaceName}?`,
      acceptLabel: 'Remove',
      rejectLabel: 'Cancel',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', outlined: true },
      accept: () => {
        this.memberService.removeAsync(this.store.workspaceId, member.userId).subscribe({
          next: () => this.refreshMembers(),
          error: () => this.errorMessage.set('Could not remove this member.'),
        });
      },
    });
  }

  private refreshMembers(): void {
    this.store.loadMembers(this.store.workspaceId).subscribe({
      next: (members) => this.store.members.set(members),
      error: () => this.errorMessage.set('Could not refresh the member list.'),
    });
  }
}
