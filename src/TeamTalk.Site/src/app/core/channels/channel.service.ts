import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, Subject, tap } from 'rxjs';
import {
  ChannelDetailsResponse,
  ChannelResponse,
  CreateChannelRequest,
  UpdateChannelPreferencesRequest,
} from '../models/channel.model';

@Injectable({ providedIn: 'root' })
export class ChannelService {
  private readonly channelRead = new Subject<string>();

  /** Emits the id of a channel whenever the current user marks it as read. */
  readonly channelRead$ = this.channelRead.asObservable();

  /** Emits the id of a channel after it was deleted or the user left it. */
  private readonly channelRemoved = new Subject<string>();
  readonly channelRemoved$ = this.channelRemoved.asObservable();

  constructor(private readonly http: HttpClient) {}

  getVisibleAsync(workspaceId: string): Observable<ChannelResponse[]> {
    return this.http.get<ChannelResponse[]>(`/api/workspaces/${workspaceId}/channels`);
  }

  getDetailsAsync(workspaceId: string, channelId: string): Observable<ChannelDetailsResponse> {
    return this.http.get<ChannelDetailsResponse>(
      `/api/workspaces/${workspaceId}/channels/${channelId}`,
    );
  }

  createAsync(workspaceId: string, request: CreateChannelRequest): Observable<ChannelResponse> {
    return this.http.post<ChannelResponse>(`/api/workspaces/${workspaceId}/channels`, request);
  }

  joinAsync(workspaceId: string, channelId: string): Observable<void> {
    return this.http.post<void>(`/api/workspaces/${workspaceId}/channels/${channelId}/join`, {});
  }

  markReadAsync(workspaceId: string, channelId: string): Observable<void> {
    return this.http
      .post<void>(`/api/workspaces/${workspaceId}/channels/${channelId}/read`, {})
      .pipe(tap(() => this.channelRead.next(channelId)));
  }

  updatePreferencesAsync(
    workspaceId: string,
    channelId: string,
    request: UpdateChannelPreferencesRequest,
  ): Observable<void> {
    return this.http.put<void>(
      `/api/workspaces/${workspaceId}/channels/${channelId}/preferences`,
      request,
    );
  }

  leaveAsync(workspaceId: string, channelId: string): Observable<void> {
    return this.http
      .post<void>(`/api/workspaces/${workspaceId}/channels/${channelId}/leave`, {})
      .pipe(tap(() => this.channelRemoved.next(channelId)));
  }

  deleteAsync(workspaceId: string, channelId: string): Observable<void> {
    return this.http
      .delete<void>(`/api/workspaces/${workspaceId}/channels/${channelId}`)
      .pipe(tap(() => this.channelRemoved.next(channelId)));
  }
}
