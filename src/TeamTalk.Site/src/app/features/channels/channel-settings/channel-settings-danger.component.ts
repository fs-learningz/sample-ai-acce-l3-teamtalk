import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ConfirmationService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { InputTextModule } from 'primeng/inputtext';
import { ChannelService } from '../../../core/channels/channel.service';
import { ChannelSettingsStore } from './channel-settings.store';

@Component({
  selector: 'app-channel-settings-danger',
  imports: [FormsModule, ButtonModule, ConfirmDialogModule, InputTextModule],
  providers: [ConfirmationService],
  templateUrl: './channel-settings-danger.component.html',
})
export class ChannelSettingsDangerComponent {
  private readonly channelService = inject(ChannelService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly router = inject(Router);
  protected readonly store = inject(ChannelSettingsStore);

  readonly deleteConfirmationText = signal('');
  readonly isDeleting = signal(false);
  readonly isLeaving = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly canDelete = computed(
    () => this.deleteConfirmationText().trim() === (this.store.channel()?.name ?? '').trim(),
  );

  deleteChannel(): void {
    if (!this.canDelete()) {
      return;
    }

    this.isDeleting.set(true);
    this.errorMessage.set(null);

    this.channelService.deleteAsync(this.store.workspaceId, this.store.channelId).subscribe({
      next: () => void this.router.navigate(['/workspaces', this.store.workspaceId]),
      error: () => {
        this.isDeleting.set(false);
        this.errorMessage.set('Could not delete this channel.');
      },
    });
  }

  leaveChannel(): void {
    const channelName = this.store.channel()?.name ?? 'this channel';

    this.confirmationService.confirm({
      header: 'Leave channel',
      message: `Are you sure you want to leave ${channelName}?`,
      acceptLabel: 'Leave',
      rejectLabel: 'Cancel',
      acceptButtonProps: { severity: 'danger' },
      rejectButtonProps: { severity: 'secondary', outlined: true },
      accept: () => {
        this.isLeaving.set(true);
        this.errorMessage.set(null);

        this.channelService.leaveAsync(this.store.workspaceId, this.store.channelId).subscribe({
          next: () => void this.router.navigate(['/workspaces', this.store.workspaceId]),
          error: () => {
            this.isLeaving.set(false);
            this.errorMessage.set('Could not leave this channel.');
          },
        });
      },
    });
  }
}
