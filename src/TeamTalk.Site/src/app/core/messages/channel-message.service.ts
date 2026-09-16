import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  ChannelMessagesResponse,
  EditMessageRequest,
  MessageResponse,
  SendMessageRequest,
} from '../models/message.model';

@Injectable({ providedIn: 'root' })
export class ChannelMessageService {
  constructor(private readonly http: HttpClient) {}

  getHistoryAsync(
    workspaceId: string,
    channelId: string,
    cursor: string | null,
    pageSize: number,
  ): Observable<ChannelMessagesResponse> {
    let params = new HttpParams().set('pageSize', pageSize);
    if (cursor) {
      params = params.set('cursor', cursor);
    }

    return this.http.get<ChannelMessagesResponse>(
      `/api/workspaces/${workspaceId}/channels/${channelId}/messages`,
      { params },
    );
  }

  sendAsync(workspaceId: string, channelId: string, request: SendMessageRequest): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(
      `/api/workspaces/${workspaceId}/channels/${channelId}/messages`,
      request,
    );
  }

  editAsync(
    workspaceId: string,
    channelId: string,
    messageId: string,
    request: EditMessageRequest,
  ): Observable<MessageResponse> {
    return this.http.put<MessageResponse>(
      `/api/workspaces/${workspaceId}/channels/${channelId}/messages/${messageId}`,
      request,
    );
  }

  deleteAsync(workspaceId: string, channelId: string, messageId: string): Observable<void> {
    return this.http.delete<void>(
      `/api/workspaces/${workspaceId}/channels/${channelId}/messages/${messageId}`,
    );
  }
}
