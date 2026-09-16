import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { MessageResponse } from '../models/message.model';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private connection: signalR.HubConnection | null = null;

  private readonly messageReceivedSubject = new Subject<MessageResponse>();
  private readonly messageEditedSubject = new Subject<MessageResponse>();
  private readonly messageDeletedSubject = new Subject<MessageResponse>();

  readonly messageReceived$ = this.messageReceivedSubject.asObservable();
  readonly messageEdited$ = this.messageEditedSubject.asObservable();
  readonly messageDeleted$ = this.messageDeletedSubject.asObservable();

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

    this.connection = connection;
    await connection.start();
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
