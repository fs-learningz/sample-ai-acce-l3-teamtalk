import { WorkspaceRole } from './workspace.model';

export interface WorkspaceMemberResponse {
  id: string;
  workspaceId: string;
  userId: string;
  displayName: string;
  email: string;
  role: WorkspaceRole;
  joinedAtUtc: string;
}
