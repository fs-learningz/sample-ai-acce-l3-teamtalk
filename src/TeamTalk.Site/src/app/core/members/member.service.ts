import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { WorkspaceMemberResponse } from '../models/member.model';
import { WorkspaceRole } from '../models/workspace.model';

@Injectable({ providedIn: 'root' })
export class MemberService {
  constructor(private readonly http: HttpClient) {}

  getForWorkspaceAsync(workspaceId: string): Observable<WorkspaceMemberResponse[]> {
    return this.http.get<WorkspaceMemberResponse[]>(`/api/workspaces/${workspaceId}/members`);
  }

  removeAsync(workspaceId: string, memberUserId: string): Observable<void> {
    return this.http.delete<void>(`/api/workspaces/${workspaceId}/members/${memberUserId}`);
  }

  leaveAsync(workspaceId: string): Observable<void> {
    return this.http.post<void>(`/api/workspaces/${workspaceId}/members/leave`, {});
  }

  changeRoleAsync(
    workspaceId: string,
    memberUserId: string,
    role: WorkspaceRole,
  ): Observable<void> {
    return this.http.patch<void>(`/api/workspaces/${workspaceId}/members/${memberUserId}/role`, {
      role,
    });
  }
}
