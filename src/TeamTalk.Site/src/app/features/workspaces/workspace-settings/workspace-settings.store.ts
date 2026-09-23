import { Injectable, computed, inject, signal } from '@angular/core';
import { AuthService } from '../../../core/auth/auth.service';
import { MemberService } from '../../../core/members/member.service';
import { WorkspaceMemberResponse } from '../../../core/models/member.model';
import { WorkspaceResponse } from '../../../core/models/workspace.model';
import { WorkspaceService } from '../../../core/workspaces/workspace.service';

/**
 * Shared, route-scoped state for the Workspace Settings page so the
 * General/Members/Danger zone sections don't each re-fetch the workspace
 * and member list.
 */
@Injectable()
export class WorkspaceSettingsStore {
  private readonly workspaceService = inject(WorkspaceService);
  private readonly memberService = inject(MemberService);
  private readonly authService = inject(AuthService);

  workspaceId = '';

  readonly workspace = signal<WorkspaceResponse | null>(null);
  readonly members = signal<WorkspaceMemberResponse[]>([]);

  readonly currentUserId = computed(() => this.authService.currentUser()?.userId ?? null);
  readonly isAdmin = computed(() => this.workspace()?.currentUserRole === 'Admin');
  readonly adminCount = computed(() => this.members().filter((m) => m.role === 'Admin').length);
  readonly isOnlyAdmin = computed(() => this.isAdmin() && this.adminCount() === 1);

  loadWorkspace(workspaceId: string) {
    return this.workspaceService.getByIdAsync(workspaceId);
  }

  loadMembers(workspaceId: string) {
    return this.memberService.getForWorkspaceAsync(workspaceId);
  }
}
