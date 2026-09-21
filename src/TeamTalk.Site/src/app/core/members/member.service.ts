import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { WorkspaceMemberResponse } from '../models/member.model';

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
}
