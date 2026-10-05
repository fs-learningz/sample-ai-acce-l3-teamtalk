import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { MessageResponse } from '../models/message.model';

export interface ChannelActivity {
  channelId: string;
  senderId: string;
}

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private connection: signalR.HubConnection | null = null;

  private readonly messageReceivedSubject = new Subject<MessageResponse>();
  private readonly messageEditedSubject = new Subject<MessageResponse>();
  private readonly messageDeletedSubject = new Subject<MessageResponse>();
  private readonly channelActivitySubject = new Subject<ChannelActivity>();
  private joinedWorkspaceId: string | null = null;

  readonly messageReceived$ = this.messageReceivedSubject.asObservable();
  readonly messageEdited$ = this.messageEditedSubject.asObservable();
  readonly messageDeleted$ = this.messageDeletedSubject.asObservable();
  readonly channelActivity$ = this.channelActivitySubject.asObservable();

  constructor(private readonly authService: AuthService) {}

  async connectAsync(): Promise<void> {
    if (this.connection) {
      return;
    }

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/channel', {
        accessTokenFactory: () => this.authService.getToken() ?? '',
      })
      .withAutomaticReconnect()
      .build();

    connection.on('ReceiveMessage', (message: MessageResponse) => this.messageReceivedSubject.next(message));
    connection.on('MessageEdited', (message: MessageResponse) => this.messageEditedSubject.next(message));
    connection.on('MessageDeleted', (message: MessageResponse) => this.messageDeletedSubject.next(message));

    connection.on('ChannelActivity', (activity: ChannelActivity) => this.channelActivitySubject.next(activity));
    // Group membership is lost when the connection drops, so rejoin the workspace once it is back.
    connection.onreconnected(() => {
      if (this.joinedWorkspaceId) {
        void connection.invoke('JoinWorkspaceAsync', this.joinedWorkspaceId);
      }
    });

    this.connection = connection;
    await connection.start();
  }

  async joinWorkspaceAsync(workspaceId: string): Promise<void> {
    if (!this.connection) {
      return;
    }

    this.joinedWorkspaceId = workspaceId;
    await this.connection.invoke('JoinWorkspaceAsync', workspaceId);
  }

  async leaveWorkspaceAsync(workspaceId: string): Promise<void> {
    if (!this.connection) {
      return;
    }

    this.joinedWorkspaceId = null;
    await this.connection.invoke('LeaveWorkspaceAsync', workspaceId);
  }

  async joinChannelAsync(channelId: string): Promise<void> {
    if (!this.connection) {
      return;
    }

    await this.connection.invoke('JoinChannelAsync', channelId);
  }

  async leaveChannelAsync(channelId: string): Promise<void> {
    if (!this.connection) {
      return;
    }

    await this.connection.invoke('LeaveChannelAsync', channelId);
  }
}
