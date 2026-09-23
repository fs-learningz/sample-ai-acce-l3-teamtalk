import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { WorkspaceInvitationResponse } from '../../../core/models/invitation.model';
import { InvitationService } from '../../../core/invitations/invitation.service';
import { WorkspaceIconComponent } from '../../../shared/workspace-icon/workspace-icon.component';

@Component({
  selector: 'app-invitation-list',
  imports: [RouterLink, ButtonModule, WorkspaceIconComponent],
  templateUrl: './invitation-list.component.html',
})
export class InvitationListComponent implements OnInit, OnDestroy {
  private readonly invitationService = inject(InvitationService);
  private readonly router = inject(Router);

  readonly invitations = signal<WorkspaceInvitationResponse[]>([]);
  readonly iconUrls = signal<Record<string, string>>({});
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.loadInvitations();
  }

  ngOnDestroy(): void {
    Object.values(this.iconUrls()).forEach((url) => URL.revokeObjectURL(url));
  }

  acceptInvitation(invitation: WorkspaceInvitationResponse): void {
    this.errorMessage.set(null);
    this.invitationService.acceptAsync(invitation.id).subscribe({
      next: () => void this.router.navigate(['/workspaces', invitation.workspaceId]),
      error: () => this.errorMessage.set('Could not accept the invitation. Please try again.'),
    });
  }

  declineInvitation(invitation: WorkspaceInvitationResponse): void {
    this.errorMessage.set(null);
    this.invitationService.declineAsync(invitation.id).subscribe({
      next: () => this.loadInvitations(),
      error: () => this.errorMessage.set('Could not decline the invitation. Please try again.'),
    });
  }

  private loadInvitations(): void {
    this.invitationService.getMyInvitationsAsync().subscribe({
      next: (invitations) => {
        this.invitations.set(invitations);
        invitations.filter((i) => i.workspaceHasIcon).forEach((i) => this.loadIcon(i.id));
      },
      error: () =>
        this.errorMessage.set('Could not load your invitations. Please refresh the page.'),
    });
  }

  private loadIcon(invitationId: string): void {
    this.invitationService.getWorkspaceIconAsync(invitationId).subscribe({
      next: (blob) => {
        this.iconUrls.update((urls) => ({ ...urls, [invitationId]: URL.createObjectURL(blob) }));
      },
    });
  }
}
