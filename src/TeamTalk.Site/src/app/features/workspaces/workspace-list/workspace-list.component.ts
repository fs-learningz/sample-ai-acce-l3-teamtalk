import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { WorkspaceResponse } from '../../../core/models/workspace.model';
import { WorkspaceService } from '../../../core/workspaces/workspace.service';
import { UserMenuComponent } from '../../../shared/user-menu/user-menu.component';
import { WorkspaceIconComponent } from '../../../shared/workspace-icon/workspace-icon.component';

@Component({
  selector: 'app-workspace-list',
  imports: [
    ReactiveFormsModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    UserMenuComponent,
    WorkspaceIconComponent,
  ],
  templateUrl: './workspace-list.component.html',
})
export class WorkspaceListComponent implements OnInit, OnDestroy {
  private readonly formBuilder = inject(FormBuilder);
  private readonly workspaceService = inject(WorkspaceService);
  private readonly router = inject(Router);

  readonly workspaces = signal<WorkspaceResponse[]>([]);
  readonly iconUrls = signal<Record<string, string>>({});
  readonly isCreateDialogVisible = signal(false);
  readonly errorMessage = signal<string | null>(null);

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

  ngOnDestroy(): void {
    Object.values(this.iconUrls()).forEach((url) => URL.revokeObjectURL(url));
  }

  private loadWorkspaces(): void {
    this.workspaceService.getForCurrentUserAsync().subscribe({
      next: (workspaces) => {
        this.workspaces.set(workspaces);
        workspaces.filter((w) => w.hasIcon).forEach((w) => this.loadIcon(w.id));
      },
      error: () =>
        this.errorMessage.set('Could not load your workspaces. Please refresh the page.'),
    });
  }

  private loadIcon(workspaceId: string): void {
    this.workspaceService.getIconAsync(workspaceId).subscribe({
      next: (blob) => {
        this.iconUrls.update((urls) => ({ ...urls, [workspaceId]: URL.createObjectURL(blob) }));
      },
    });
  }
}
