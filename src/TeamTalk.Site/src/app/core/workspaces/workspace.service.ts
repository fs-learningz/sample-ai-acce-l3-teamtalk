import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateWorkspaceRequest, WorkspaceResponse } from '../models/workspace.model';

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
}
