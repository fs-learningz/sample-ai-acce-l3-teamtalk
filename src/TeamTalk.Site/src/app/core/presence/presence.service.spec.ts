import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { describe, expect, it } from 'vitest';
import { AuthService } from '../auth/auth.service';
import { PresenceService } from './presence.service';

describe('PresenceService', () => {
  function setup(currentUser: { userId: string; email: string; displayName: string } | null) {
    TestBed.configureTestingModule({
      providers: [
        {
          provide: AuthService,
          useValue: {
            currentUser: signal(currentUser),
            isAuthenticated: () => currentUser !== null,
          },
        },
      ],
    });

    return TestBed.inject(PresenceService);
  }

  it('shows the current user as online while there is an active session', () => {
    const service = setup({ userId: 'u1', email: 'jane@example.com', displayName: 'Jane Doe' });
    expect(service.currentUserPresence()).toBe('online');
    expect(service.getPresenceForUser('u1')).toBe('online');
  });

  it('shows offline when there is no active session', () => {
    const service = setup(null);
    expect(service.currentUserPresence()).toBe('offline');
  });

  it('shows offline for a user that is not the current session user', () => {
    const service = setup({ userId: 'u1', email: 'jane@example.com', displayName: 'Jane Doe' });
    expect(service.getPresenceForUser('someone-else')).toBe('offline');
  });
});
