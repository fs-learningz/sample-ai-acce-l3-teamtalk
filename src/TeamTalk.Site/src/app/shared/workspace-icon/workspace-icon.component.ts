import { Component, computed, input } from '@angular/core';
import { getAvatarPalette, getAvatarSeed, getInitials } from '../user-avatar/avatar-color.util';

export type WorkspaceIconSize = 'sm' | 'lg';

/**
 * Renders a workspace icon image when `imageUrl` is set; otherwise falls back to
 * initials on a deterministic background color, using the same hashing as
 * `UserAvatarComponent` so workspace and user avatars look like one system.
 */
@Component({
  selector: 'app-workspace-icon',
  template: `
    <div
      class="inline-flex shrink-0 items-center justify-center rounded-field font-semibold select-none"
      [class]="sizeClass()"
      [style.backgroundColor]="imageUrl() ? null : palette().background"
      [style.color]="imageUrl() ? null : palette().text"
    >
      @if (imageUrl(); as src) {
        <img
          [src]="src"
          [alt]="name() || 'Workspace icon'"
          class="h-full w-full rounded-field object-cover"
        />
      } @else {
        <span aria-hidden="true">{{ initials() }}</span>
      }
    </div>
  `,
})
export class WorkspaceIconComponent {
  readonly name = input<string | null>(null);
  readonly imageUrl = input<string | null>(null);
  readonly size = input<WorkspaceIconSize>('sm');

  protected readonly initials = computed(() => getInitials(this.name(), null));
  protected readonly palette = computed(() => getAvatarPalette(getAvatarSeed(this.name(), null)));

  protected readonly sizeClass = computed(() =>
    this.size() === 'lg' ? 'h-14 w-14 text-xl' : 'h-8 w-8 text-sm',
  );
}
