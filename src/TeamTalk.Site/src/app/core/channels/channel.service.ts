import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ChannelResponse, CreateChannelRequest } from '../models/channel.model';

@Injectable({ providedIn: 'root' })
export class ChannelService {
  constructor(private readonly http: HttpClient) {}

  getVisibleAsync(workspaceId: string): Observable<ChannelResponse[]> {
    return this.http.get<ChannelResponse[]>(`/api/workspaces/${workspaceId}/channels`);
  }

  createAsync(workspaceId: string, request: CreateChannelRequest): Observable<ChannelResponse> {
    return this.http.post<ChannelResponse>(`/api/workspaces/${workspaceId}/channels`, request);
  }

  joinAsync(workspaceId: string, channelId: string): Observable<void> {
    return this.http.post<void>(`/api/workspaces/${workspaceId}/channels/${channelId}/join`, {});
  }
}
