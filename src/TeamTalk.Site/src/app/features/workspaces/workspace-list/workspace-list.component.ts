import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { ConfirmationService } from 'primeng/api';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { WorkspaceResponse } from '../../../core/models/workspace.model';
import { WorkspaceService } from '../../../core/workspaces/workspace.service';
import { AuthService } from '../../../core/auth/auth.service';
import { MemberService } from '../../../core/members/member.service';
import { UserMenuComponent } from '../../../shared/user-menu/user-menu.component';

@Component({
  selector: 'app-workspace-list',
  imports: [
    ReactiveFormsModule,
    ButtonModule,
    ConfirmDialogModule,
    DialogModule,
    InputTextModule,
    UserMenuComponent,
  ],
  providers: [ConfirmationService],
  templateUrl: './workspace-list.component.html',
})
export class WorkspaceListComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly workspaceService = inject(WorkspaceService);
  private readonly memberService = inject(MemberService);
  private readonly authService = inject(AuthService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly router = inject(Router);

  readonly workspaces = signal<WorkspaceResponse[]>([]);
  readonly isCreateDialogVisible = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly currentUserId = computed(() => this.authService.currentUser()?.userId ?? null);

  readonly createForm = this.formBuilder.nonNullable.group({
    name: ['', Validators.required],
  });

  ngOnInit(): void {
    this.loadWorkspaces();
  }

  openCreateDialog(): void {
    this.createForm.reset();
    this.errorMessage.set(null);
    this.isCreateDialogVisible.set(true);
  }

  onCreateSubmit(): void {
    if (this.createForm.invalid) {
      return;
    }

    this.errorMessage.set(null);
    this.workspaceService.createAsync(this.createForm.getRawValue()).subscribe({
      next: (workspace) => {
        this.isCreateDialogVisible.set(false);
        this.loadWorkspaces();
        void this.router.navigate(['/workspaces', workspace.id]);
      },
      error: () => {
        this.errorMessage.set('Could not create the workspace. Please try again.');
      },
    });
  }

  openWorkspace(workspace: WorkspaceResponse): void {
    void this.router.navigate(['/workspaces', workspace.id]);
  }

  openInvitations(): void {
    void this.router.navigate(['/invitations']);
  }

  isOwner(workspace: WorkspaceResponse): boolean {
    const currentUserId = this.currentUserId();
    return !!currentUserId && workspace.createdByUserId === currentUserId;
  }

  deleteWorkspace(workspace: WorkspaceResponse): void {
    this.confirmationService.confirm({
      header: 'Delete workspace',
      message: `Delete "${workspace.name}"? This will permanently remove all its channels and messages.`,
      acceptLabel: 'Delete',
      rejectLabel: 'Cancel',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', outlined: true },
      accept: () => {
        this.workspaceService.deleteAsync(workspace.id).subscribe({
          next: () => this.loadWorkspaces(),
          error: () => this.errorMessage.set('Could not delete this workspace.'),
        });
      },
    });
  }

  leaveWorkspace(workspace: WorkspaceResponse): void {
    this.confirmationService.confirm({
      header: 'Leave workspace',
      message: `Are you sure you want to leave "${workspace.name}"?`,
      acceptLabel: 'Leave',
      rejectLabel: 'Cancel',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', outlined: true },
      accept: () => {
        this.memberService.leaveAsync(workspace.id).subscribe({
          next: () => this.loadWorkspaces(),
          error: () => this.errorMessage.set('Could not leave this workspace.'),
        });
      },
    });
  }

  private loadWorkspaces(): void {
    this.workspaceService.getForCurrentUserAsync().subscribe({
      next: (workspaces) => this.workspaces.set(workspaces),
      error: () =>
        this.errorMessage.set('Could not load your workspaces. Please refresh the page.'),
    });
  }
}
