import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { Popover, PopoverModule } from 'primeng/popover';
import { AuthService } from '../../core/auth/auth.service';
import { PRESENCE_LABELS } from '../../core/presence/presence.model';
import { PresenceService } from '../../core/presence/presence.service';
import { UserAvatarComponent } from '../user-avatar/user-avatar.component';

/**
 * Account avatar button + popover, shared by every page that needs the
 * header user menu so the behavior and layout stay identical across pages.
 */
@Component({
  selector: 'app-user-menu',
  imports: [ButtonModule, PopoverModule, UserAvatarComponent],
  templateUrl: './user-menu.component.html',
})
export class UserMenuComponent {
  private readonly authService = inject(AuthService);
  private readonly presenceService = inject(PresenceService);
  private readonly router = inject(Router);

  private readonly popover = viewChild.required(Popover);

  readonly isOpen = signal(false);

  readonly displayName = computed(() => this.authService.currentUser()?.displayName ?? '');
  readonly email = computed(() => this.authService.currentUser()?.email ?? '');
  readonly presence = computed(() => this.presenceService.currentUserPresence());
  readonly presenceLabel = computed(() => PRESENCE_LABELS[this.presence()]);

  toggle(event: Event): void {
    this.popover().toggle(event);
  }

  logout(): void {
    this.popover().hide();
    this.authService.logout();
    void this.router.navigate(['/login']);
  }
}
