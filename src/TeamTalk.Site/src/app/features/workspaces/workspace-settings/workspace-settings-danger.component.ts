import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ConfirmationService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { InputTextModule } from 'primeng/inputtext';
import { MemberService } from '../../../core/members/member.service';
import { WorkspaceService } from '../../../core/workspaces/workspace.service';
import { WorkspaceSettingsStore } from './workspace-settings.store';

@Component({
  selector: 'app-workspace-settings-danger',
  imports: [FormsModule, ButtonModule, ConfirmDialogModule, InputTextModule],
  providers: [ConfirmationService],
  templateUrl: './workspace-settings-danger.component.html',
})
export class WorkspaceSettingsDangerComponent {
  private readonly workspaceService = inject(WorkspaceService);
  private readonly memberService = inject(MemberService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly router = inject(Router);
  protected readonly store = inject(WorkspaceSettingsStore);

  readonly deleteConfirmationText = signal('');
  readonly isDeleting = signal(false);
  readonly isLeaving = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly canDelete = computed(
    () => this.deleteConfirmationText().trim() === (this.store.workspace()?.name ?? '').trim(),
  );

  readonly canLeave = computed(() => !this.store.isOnlyAdmin());

  deleteWorkspace(): void {
    if (!this.canDelete()) {
      return;
    }

    this.isDeleting.set(true);
    this.errorMessage.set(null);

    this.workspaceService.deleteAsync(this.store.workspaceId).subscribe({
      next: () => void this.router.navigate(['/workspaces']),
      error: () => {
        this.isDeleting.set(false);
        this.errorMessage.set('Could not delete this workspace.');
      },
    });
  }

  leaveWorkspace(): void {
    const workspaceName = this.store.workspace()?.name ?? 'this workspace';

    this.confirmationService.confirm({
      header: 'Leave workspace',
      message: `Are you sure you want to leave ${workspaceName}?`,
      acceptLabel: 'Leave',
      rejectLabel: 'Cancel',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', outlined: true },
      accept: () => {
        this.isLeaving.set(true);
        this.errorMessage.set(null);

        this.memberService.leaveAsync(this.store.workspaceId).subscribe({
          next: () => void this.router.navigate(['/workspaces']),
          error: () => {
            this.isLeaving.set(false);
            this.errorMessage.set('Could not leave this workspace.');
          },
        });
      },
    });
  }
}
