import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { ChannelResponse, ChannelType } from '../../../core/models/channel.model';
import { WorkspaceResponse } from '../../../core/models/workspace.model';
import { ChannelService } from '../../../core/channels/channel.service';
import { WorkspaceService } from '../../../core/workspaces/workspace.service';

interface ChannelTypeOption {
  label: string;
  value: ChannelType;
}

@Component({
  selector: 'app-workspace-shell',
  imports: [ReactiveFormsModule, RouterOutlet, ButtonModule, DialogModule, InputTextModule, SelectModule],
  templateUrl: './workspace-shell.component.html',
})
export class WorkspaceShellComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly workspaceService = inject(WorkspaceService);
  private readonly channelService = inject(ChannelService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly workspaceId = this.route.snapshot.paramMap.get('workspaceId')!;
  readonly workspace = signal<WorkspaceResponse | null>(null);
  readonly channels = signal<ChannelResponse[]>([]);
  readonly isCreateDialogVisible = signal(false);

  readonly channelTypes: ChannelTypeOption[] = [
    { label: 'Public', value: 'Public' },
    { label: 'Private', value: 'Private' },
  ];

  readonly createForm = this.formBuilder.nonNullable.group({
    name: ['', Validators.required],
    type: this.formBuilder.nonNullable.control<ChannelType>('Public', Validators.required),
  });

  ngOnInit(): void {
    this.workspaceService.getByIdAsync(this.workspaceId).subscribe((workspace) => this.workspace.set(workspace));
    this.loadChannels();
  }

  openCreateDialog(): void {
    this.createForm.reset({ name: '', type: 'Public' });
    this.isCreateDialogVisible.set(true);
  }

  onCreateSubmit(): void {
    if (this.createForm.invalid) {
      return;
    }

    this.channelService.createAsync(this.workspaceId, this.createForm.getRawValue()).subscribe((channel) => {
      this.isCreateDialogVisible.set(false);
      this.loadChannels();
      this.openChannel(channel);
    });
  }

  openChannel(channel: ChannelResponse): void {
    void this.router.navigate(['/workspaces', this.workspaceId, 'channels', channel.id]);
  }

  private loadChannels(): void {
    this.channelService.getVisibleAsync(this.workspaceId).subscribe((channels) => this.channels.set(channels));
  }
}
