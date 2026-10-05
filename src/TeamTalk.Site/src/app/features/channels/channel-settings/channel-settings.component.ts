import { Component, OnInit, inject, signal } from '@angular/core';
import {
  ActivatedRoute,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';
import { ChannelSettingsStore } from './channel-settings.store';

@Component({
  selector: 'app-channel-settings',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  providers: [ChannelSettingsStore],
  templateUrl: './channel-settings.component.html',
})
export class ChannelSettingsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly store = inject(ChannelSettingsStore);

  readonly workspaceId = this.route.snapshot.paramMap.get('workspaceId')!;
  readonly channelId = this.route.snapshot.paramMap.get('channelId')!;
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.store.workspaceId = this.workspaceId;
    this.store.channelId = this.channelId;

    this.store.loadChannel().subscribe({
      next: (channel) => this.store.channel.set(channel),
      error: () => this.errorMessage.set('Could not load this channel.'),
    });

    this.store.loadWorkspace().subscribe({
      next: (workspace) => this.store.workspace.set(workspace),
      error: () => this.errorMessage.set('Could not load this workspace.'),
    });
  }

  backToChannel(): void {
    void this.router.navigate(['/workspaces', this.workspaceId, 'channels', this.channelId]);
  }
}
