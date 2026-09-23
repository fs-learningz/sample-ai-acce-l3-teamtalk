import { Injectable, computed, inject } from '@angular/core';
import { AuthService } from '../auth/auth.service';
import { PresenceStatus } from './presence.model';

/**
 * We don't have real presence tracking yet. Until the backend broadcasts
 * presence, the current user shows as online for the duration of their
 * session; `getPresenceForUser` is the seam other members' presence will
 * plug into later without changing callers.
 */
@Injectable({ providedIn: 'root' })
export class PresenceService {
  private readonly authService = inject(AuthService);

  readonly currentUserPresence = computed<PresenceStatus>(() =>
    this.authService.isAuthenticated() ? 'online' : 'offline',
  );

  getPresenceForUser(userId: string): PresenceStatus {
    const currentUser = this.authService.currentUser();
    if (currentUser?.userId === userId) {
      return this.currentUserPresence();
    }

    return 'offline';
  }
}
