export type WorkspaceRole = 'Member' | 'Admin';

export interface WorkspaceResponse {
  id: string;
  name: string;
  description: string | null;
  hasIcon: boolean;
  createdByUserId: string;
  createdByDisplayName: string | null;
  createdAtUtc: string;
  memberCount: number | null;
  currentUserRole: WorkspaceRole | null;
}

export interface CreateWorkspaceRequest {
  name: string;
}

export interface UpdateWorkspaceRequest {
  name: string;
  description: string | null;
}
