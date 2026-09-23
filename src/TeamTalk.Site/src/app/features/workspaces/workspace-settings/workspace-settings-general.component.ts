import { DatePipe } from '@angular/common';
import {
  Component,
  HostListener,
  OnDestroy,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { CanComponentDeactivate } from '../../../core/workspaces/unsaved-workspace-settings.guard';
import { UserAvatarComponent } from '../../../shared/user-avatar/user-avatar.component';
import { WorkspaceIconComponent } from '../../../shared/workspace-icon/workspace-icon.component';
import { WorkspaceService } from '../../../core/workspaces/workspace.service';
import { WorkspaceResponse } from '../../../core/models/workspace.model';
import { WorkspaceSettingsStore } from './workspace-settings.store';

@Component({
  selector: 'app-workspace-settings-general',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    TextareaModule,
    UserAvatarComponent,
    WorkspaceIconComponent,
  ],
  templateUrl: './workspace-settings-general.component.html',
})
export class WorkspaceSettingsGeneralComponent implements OnDestroy, CanComponentDeactivate {
  private readonly formBuilder = inject(FormBuilder);
  private readonly workspaceService = inject(WorkspaceService);
  private readonly router = inject(Router);
  protected readonly store = inject(WorkspaceSettingsStore);

  readonly maxNameLength = 50;
  readonly maxDescriptionLength = 200;

  readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(this.maxNameLength)]],
    description: ['', [Validators.maxLength(this.maxDescriptionLength)]],
  });

  readonly selectedIconFile = signal<File | null>(null);
  readonly iconPreviewUrl = signal<string | null>(null);
  readonly currentIconUrl = signal<string | null>(null);
  readonly removeIconRequested = signal(false);
  readonly isSaving = signal(false);
  readonly successMessage = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly copiedId = signal(false);

  protected readonly displayIconUrl = computed(
    () => this.iconPreviewUrl() ?? (this.removeIconRequested() ? null : this.currentIconUrl()),
  );

  private initialized = false;

  constructor() {
    effect(() => {
      const workspace = this.store.workspace();
      if (workspace && !this.initialized) {
        this.initialized = true;
        this.form.reset({ name: workspace.name, description: workspace.description ?? '' });
        if (!this.store.isAdmin()) {
          this.form.disable();
        }
        if (workspace.hasIcon) {
          this.loadCurrentIcon(workspace.id);
        }
      }
    });
  }

  ngOnDestroy(): void {
    this.revokeIconPreview();
    this.revokeCurrentIconUrl();
  }

  hasUnsavedChanges(): boolean {
    return this.form.dirty || this.selectedIconFile() !== null || this.removeIconRequested();
  }

  @HostListener('window:beforeunload', ['$event'])
  onBeforeUnload(event: BeforeUnloadEvent): void {
    if (this.hasUnsavedChanges()) {
      event.preventDefault();
    }
  }

  onIconSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    input.value = '';
    if (!file) {
      return;
    }

    this.errorMessage.set(null);

    if (file.size > 2 * 1024 * 1024) {
      this.errorMessage.set('The icon must be 2 MB or smaller.');
      return;
    }

    if (!['image/png', 'image/jpeg', 'image/svg+xml'].includes(file.type)) {
      this.errorMessage.set('The icon must be a PNG, JPG, or SVG file.');
      return;
    }

    this.revokeIconPreview();
    this.selectedIconFile.set(file);
    this.iconPreviewUrl.set(URL.createObjectURL(file));
    this.removeIconRequested.set(false);
  }

  removeIcon(): void {
    this.revokeIconPreview();
    this.selectedIconFile.set(null);
    this.iconPreviewUrl.set(null);
    this.removeIconRequested.set(true);
  }

  copyWorkspaceId(): void {
    const id = this.store.workspace()?.id;
    if (!id) {
      return;
    }

    void navigator.clipboard.writeText(id).then(() => {
      this.copiedId.set(true);
      setTimeout(() => this.copiedId.set(false), 2000);
    });
  }

  goToMembers(): void {
    void this.router.navigate(['/workspaces', this.store.workspaceId, 'settings', 'members']);
  }

  save(): void {
    if (this.form.invalid || !this.store.isAdmin()) {
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const { name, description } = this.form.getRawValue();

    this.workspaceService
      .updateAsync(this.store.workspaceId, { name, description: description || null })
      .subscribe({
        next: (updated) => this.applyIconChangeThenFinish(updated),
        error: () => this.onSaveError(),
      });
  }

  private applyIconChangeThenFinish(updated: WorkspaceResponse): void {
    const file = this.selectedIconFile();
    if (file) {
      this.workspaceService.updateIconAsync(this.store.workspaceId, file).subscribe({
        next: (withIcon) => this.onSaveSuccess(withIcon),
        error: () => this.onSaveError(),
      });
      return;
    }

    if (this.removeIconRequested()) {
      this.workspaceService.removeIconAsync(this.store.workspaceId).subscribe({
        next: (withoutIcon) => this.onSaveSuccess(withoutIcon),
        error: () => this.onSaveError(),
      });
      return;
    }

    this.onSaveSuccess(updated);
  }

  private onSaveSuccess(workspace: WorkspaceResponse): void {
    this.store.workspace.set(workspace);
    this.form.markAsPristine();
    this.selectedIconFile.set(null);
    this.removeIconRequested.set(false);
    this.revokeIconPreview();
    this.revokeCurrentIconUrl();
    this.currentIconUrl.set(null);
    if (workspace.hasIcon) {
      this.loadCurrentIcon(workspace.id);
    }
    this.isSaving.set(false);
    this.successMessage.set('Workspace updated.');
  }

  private onSaveError(): void {
    this.isSaving.set(false);
    this.errorMessage.set('Could not save changes. Please try again.');
  }

  private loadCurrentIcon(workspaceId: string): void {
    this.workspaceService.getIconAsync(workspaceId).subscribe({
      next: (blob) => {
        this.revokeCurrentIconUrl();
        this.currentIconUrl.set(URL.createObjectURL(blob));
      },
    });
  }

  private revokeIconPreview(): void {
    const url = this.iconPreviewUrl();
    if (url) {
      URL.revokeObjectURL(url);
    }
  }

  private revokeCurrentIconUrl(): void {
    const url = this.currentIconUrl();
    if (url) {
      URL.revokeObjectURL(url);
    }
  }
}
