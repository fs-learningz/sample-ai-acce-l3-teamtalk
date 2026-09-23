import { WorkspaceRole } from './workspace.model';

export type InvitationStatus = 'Pending' | 'Accepted' | 'Declined' | 'Revoked';

export interface WorkspaceInvitationResponse {
  id: string;
  workspaceId: string;
  workspaceName: string;
  workspaceHasIcon: boolean;
  email: string;
  role: WorkspaceRole;
  status: InvitationStatus;
  invitedByUserId: string;
  createdAtUtc: string;
}

export interface InviteMemberRequest {
  email: string;
  role: WorkspaceRole;
}
