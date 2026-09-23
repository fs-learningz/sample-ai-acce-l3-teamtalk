import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { InviteMemberRequest, WorkspaceInvitationResponse } from '../models/invitation.model';

@Injectable({ providedIn: 'root' })
export class InvitationService {
  constructor(private readonly http: HttpClient) {}

  inviteAsync(
    workspaceId: string,
    request: InviteMemberRequest,
  ): Observable<WorkspaceInvitationResponse> {
    return this.http.post<WorkspaceInvitationResponse>(
      `/api/workspaces/${workspaceId}/invitations`,
      request,
    );
  }

  getForWorkspaceAsync(workspaceId: string): Observable<WorkspaceInvitationResponse[]> {
    return this.http.get<WorkspaceInvitationResponse[]>(
      `/api/workspaces/${workspaceId}/invitations`,
    );
  }

  revokeAsync(workspaceId: string, invitationId: string): Observable<void> {
    return this.http.delete<void>(`/api/workspaces/${workspaceId}/invitations/${invitationId}`);
  }

  getMyInvitationsAsync(): Observable<WorkspaceInvitationResponse[]> {
    return this.http.get<WorkspaceInvitationResponse[]>('/api/invitations');
  }

  acceptAsync(invitationId: string): Observable<void> {
    return this.http.post<void>(`/api/invitations/${invitationId}/accept`, {});
  }

  declineAsync(invitationId: string): Observable<void> {
    return this.http.post<void>(`/api/invitations/${invitationId}/decline`, {});
  }

  getWorkspaceIconAsync(invitationId: string): Observable<Blob> {
    return this.http.get(`/api/invitations/${invitationId}/workspace-icon`, {
      responseType: 'blob',
    });
  }
}
