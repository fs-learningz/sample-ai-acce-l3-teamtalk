import { Component, OnInit, inject, signal } from '@angular/core';
import {
  ActivatedRoute,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';
import { WorkspaceSettingsStore } from './workspace-settings.store';

@Component({
  selector: 'app-workspace-settings',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  providers: [WorkspaceSettingsStore],
  templateUrl: './workspace-settings.component.html',
})
export class WorkspaceSettingsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly store = inject(WorkspaceSettingsStore);

  readonly workspaceId = this.route.snapshot.paramMap.get('workspaceId')!;
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.store.workspaceId = this.workspaceId;

    this.store.loadWorkspace(this.workspaceId).subscribe({
      next: (workspace) => this.store.workspace.set(workspace),
      error: () => this.errorMessage.set('Could not load this workspace.'),
    });

    this.store.loadMembers(this.workspaceId).subscribe({
      next: (members) => this.store.members.set(members),
      error: () => this.errorMessage.set('Could not load workspace members.'),
    });
  }

  backToWorkspace(): void {
    void this.router.navigate(['/workspaces', this.workspaceId]);
  }
}
