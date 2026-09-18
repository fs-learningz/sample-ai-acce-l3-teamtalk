export type WorkspaceRole = 'Member' | 'Admin';

export interface WorkspaceResponse {
  id: string;
  name: string;
  createdByUserId: string;
  createdAtUtc: string;
  currentUserRole: WorkspaceRole | null;
}

export interface CreateWorkspaceRequest {
  name: string;
}
