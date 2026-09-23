import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  CreateWorkspaceRequest,
  UpdateWorkspaceRequest,
  WorkspaceResponse,
} from '../models/workspace.model';

@Injectable({ providedIn: 'root' })
export class WorkspaceService {
  constructor(private readonly http: HttpClient) {}

  getForCurrentUserAsync(): Observable<WorkspaceResponse[]> {
    return this.http.get<WorkspaceResponse[]>('/api/workspaces');
  }

  getByIdAsync(workspaceId: string): Observable<WorkspaceResponse> {
    return this.http.get<WorkspaceResponse>(`/api/workspaces/${workspaceId}`);
  }

  createAsync(request: CreateWorkspaceRequest): Observable<WorkspaceResponse> {
    return this.http.post<WorkspaceResponse>('/api/workspaces', request);
  }

  updateAsync(workspaceId: string, request: UpdateWorkspaceRequest): Observable<WorkspaceResponse> {
    return this.http.put<WorkspaceResponse>(`/api/workspaces/${workspaceId}`, request);
  }

  updateIconAsync(workspaceId: string, file: File): Observable<WorkspaceResponse> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.put<WorkspaceResponse>(`/api/workspaces/${workspaceId}/icon`, formData);
  }

  removeIconAsync(workspaceId: string): Observable<WorkspaceResponse> {
    return this.http.delete<WorkspaceResponse>(`/api/workspaces/${workspaceId}/icon`);
  }

  getIconAsync(workspaceId: string): Observable<Blob> {
    return this.http.get(`/api/workspaces/${workspaceId}/icon`, { responseType: 'blob' });
  }

  deleteAsync(workspaceId: string): Observable<void> {
    return this.http.delete<void>(`/api/workspaces/${workspaceId}`);
  }
}
