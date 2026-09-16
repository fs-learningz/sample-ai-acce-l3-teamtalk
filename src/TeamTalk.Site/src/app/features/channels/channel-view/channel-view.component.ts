import { DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subscription } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { AuthService } from '../../../core/auth/auth.service';
import { ChannelMessageService } from '../../../core/messages/channel-message.service';
import { MessageResponse } from '../../../core/models/message.model';
import { SignalRService } from '../../../core/realtime/signalr.service';

@Component({
  selector: 'app-channel-view',
  imports: [DatePipe, ReactiveFormsModule, ButtonModule, InputTextModule],
  templateUrl: './channel-view.component.html',
})
export class ChannelViewComponent implements OnInit, OnDestroy {
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly channelMessageService = inject(ChannelMessageService);
  private readonly signalRService = inject(SignalRService);
  readonly authService = inject(AuthService);

  readonly workspaceId = this.route.snapshot.paramMap.get('workspaceId')!;
  channelId = '';

  readonly messages = signal<MessageResponse[]>([]);
  readonly editingMessageId = signal<string | null>(null);

  readonly sendForm = this.formBuilder.nonNullable.group({
    content: ['', Validators.required],
  });

  readonly editForm = this.formBuilder.nonNullable.group({
    content: ['', Validators.required],
  });

  private readonly subscriptions = new Subscription();

  ngOnInit(): void {
    this.subscriptions.add(
      this.route.paramMap.subscribe((params) => {
        const nextChannelId = params.get('channelId');
        if (!nextChannelId || nextChannelId === this.channelId) {
          return;
        }

        void this.switchChannelAsync(nextChannelId);
      }),
    );

    this.subscriptions.add(this.signalRService.messageReceived$.subscribe((message) => this.applyIncoming(message)));
    this.subscriptions.add(this.signalRService.messageEdited$.subscribe((message) => this.applyIncoming(message)));
    this.subscriptions.add(this.signalRService.messageDeleted$.subscribe((message) => this.applyIncoming(message)));
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    if (this.channelId) {
      void this.signalRService.leaveChannelAsync(this.channelId);
    }
  }

  onSend(): void {
    if (this.sendForm.invalid) {
      return;
    }

    const content = this.sendForm.getRawValue().content;
    this.channelMessageService.sendAsync(this.workspaceId, this.channelId, { content }).subscribe((message) => {
      this.applyIncoming(message);
      this.sendForm.reset({ content: '' });
    });
  }

  startEdit(message: MessageResponse): void {
    this.editingMessageId.set(message.id);
    this.editForm.setValue({ content: message.content });
  }

  cancelEdit(): void {
    this.editingMessageId.set(null);
  }

  onEditSubmit(message: MessageResponse): void {
    if (this.editForm.invalid) {
      return;
    }

    const content = this.editForm.getRawValue().content;
    this.channelMessageService.editAsync(this.workspaceId, this.channelId, message.id, { content }).subscribe(
      (updated) => {
        this.applyIncoming(updated);
        this.editingMessageId.set(null);
      },
    );
  }

  deleteMessage(message: MessageResponse): void {
    this.channelMessageService.deleteAsync(this.workspaceId, this.channelId, message.id).subscribe(() => {
      this.messages.update((current) =>
        current.map((m) => (m.id === message.id ? { ...m, isDeleted: true, content: '[deleted]' } : m)),
      );
    });
  }

  canModify(message: MessageResponse): boolean {
    return message.senderId === this.authService.currentUser()?.userId;
  }

  private async switchChannelAsync(nextChannelId: string): Promise<void> {
    if (this.channelId) {
      await this.signalRService.leaveChannelAsync(this.channelId);
    }

    this.channelId = nextChannelId;
    this.messages.set([]);
    this.loadHistory();

    await this.signalRService.connectAsync();
    await this.signalRService.joinChannelAsync(this.channelId);
  }

  private loadHistory(): void {
    this.channelMessageService.getHistoryAsync(this.workspaceId, this.channelId, null, 50).subscribe((page) => {
      this.messages.set([...page.items].reverse());
    });
  }

  private applyIncoming(message: MessageResponse): void {
    if (message.channelId !== this.channelId) {
      return;
    }

    this.messages.update((current) => {
      const index = current.findIndex((m) => m.id === message.id);
      if (index === -1) {
        return [...current, message];
      }

      const next = [...current];
      next[index] = message;
      return next;
    });
  }
}
