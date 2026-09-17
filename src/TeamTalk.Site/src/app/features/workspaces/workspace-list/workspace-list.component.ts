import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { WorkspaceResponse } from '../../../core/models/workspace.model';
import { WorkspaceService } from '../../../core/workspaces/workspace.service';

@Component({
  selector: 'app-workspace-list',
  imports: [ReactiveFormsModule, ButtonModule, DialogModule, InputTextModule],
  templateUrl: './workspace-list.component.html',
})
export class WorkspaceListComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly workspaceService = inject(WorkspaceService);
  private readonly router = inject(Router);

  readonly workspaces = signal<WorkspaceResponse[]>([]);
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

  private loadWorkspaces(): void {
    this.workspaceService.getForCurrentUserAsync().subscribe({
      next: (workspaces) => this.workspaces.set(workspaces),
      error: () => this.errorMessage.set('Could not load your workspaces. Please refresh the page.'),
    });
  }
}
