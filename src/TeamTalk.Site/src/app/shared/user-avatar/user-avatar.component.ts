import { Component, computed, input } from '@angular/core';
import { PresenceStatus } from '../../core/presence/presence.model';
import { getAvatarPalette, getAvatarSeed, getInitials } from './avatar-color.util';

export type UserAvatarSize = 'sm' | 'lg';

/**
 * Renders a profile image when `imageUrl` is set; otherwise falls back to
 * initials on a deterministic background color. Callers don't need to change
 * when a real profile picture becomes available.
 */
@Component({
  selector: 'app-user-avatar',
  template: `
    <div
      class="relative inline-flex shrink-0 items-center justify-center rounded-full font-semibold select-none"
      [class]="circleSizeClass()"
      [style.backgroundColor]="imageUrl() ? null : palette().background"
      [style.color]="imageUrl() ? null : palette().text"
    >
      @if (imageUrl(); as src) {
        <img [src]="src" [alt]="altText()" class="h-full w-full rounded-full object-cover" />
      } @else {
        <span aria-hidden="true">{{ initials() }}</span>
      }

      @if (presence(); as status) {
        <span
          class="absolute right-0 bottom-0 rounded-full ring-2 ring-base-100"
          [class]="dotSizeClass()"
          [class.bg-green-500]="status === 'online'"
          [class.bg-yellow-400]="status === 'away'"
          [class.bg-gray-400]="status === 'offline'"
        ></span>
      }
    </div>
  `,
})
export class UserAvatarComponent {
  readonly name = input<string | null>(null);
  readonly email = input<string | null>(null);
  readonly imageUrl = input<string | null>(null);
  readonly presence = input<PresenceStatus | null>(null);
  readonly size = input<UserAvatarSize>('sm');

  protected readonly initials = computed(() => getInitials(this.name(), this.email()));
  protected readonly palette = computed(() =>
    getAvatarPalette(getAvatarSeed(this.name(), this.email())),
  );
  protected readonly altText = computed(() => this.name() || this.email() || 'User avatar');

  protected readonly circleSizeClass = computed(() =>
    this.size() === 'lg' ? 'h-14 w-14 text-xl' : 'h-9 w-9 text-sm',
  );

  protected readonly dotSizeClass = computed(() =>
    this.size() === 'lg' ? 'h-3.5 w-3.5' : 'h-2.5 w-2.5',
  );
}
