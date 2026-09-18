import { Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { WorkspaceInvitationResponse } from '../../../core/models/invitation.model';
import { InvitationService } from '../../../core/invitations/invitation.service';

@Component({
  selector: 'app-invitation-list',
  imports: [RouterLink, ButtonModule],
  templateUrl: './invitation-list.component.html',
})
export class InvitationListComponent implements OnInit {
  private readonly invitationService = inject(InvitationService);
  private readonly router = inject(Router);

  readonly invitations = signal<WorkspaceInvitationResponse[]>([]);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.loadInvitations();
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
      next: (invitations) => this.invitations.set(invitations),
      error: () => this.errorMessage.set('Could not load your invitations. Please refresh the page.'),
    });
  }
}
