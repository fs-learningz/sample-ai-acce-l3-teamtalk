export interface WorkspaceResponse {
  id: string;
  name: string;
  createdByUserId: string;
  createdAtUtc: string;
}

export interface CreateWorkspaceRequest {
  name: string;
}
