import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { describe, expect, it, vi } from 'vitest';
import { AuthService } from '../../core/auth/auth.service';
import { UserMenuComponent } from './user-menu.component';

describe('UserMenuComponent', () => {
  let fixture: ComponentFixture<UserMenuComponent>;
  let authService: {
    currentUser: () => unknown;
    isAuthenticated: () => boolean;
    logout: () => void;
  };

  async function setup(): Promise<void> {
    authService = {
      currentUser: () => ({ userId: 'u1', email: 'jane.doe@example.com', displayName: 'Jane Doe' }),
      isAuthenticated: () => true,
      logout: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [UserMenuComponent],
      providers: [provideRouter([]), { provide: AuthService, useValue: authService }],
    }).compileComponents();

    fixture = TestBed.createComponent(UserMenuComponent);
    fixture.detectChanges();
  }

  it('exposes the current user display name and email', async () => {
    await setup();

    expect(fixture.componentInstance.displayName()).toBe('Jane Doe');
    expect(fixture.componentInstance.email()).toBe('jane.doe@example.com');
  });

  it('shows the current session as online with a matching label', async () => {
    await setup();

    expect(fixture.componentInstance.presence()).toBe('online');
    expect(fixture.componentInstance.presenceLabel()).toBe('Online');
  });

  it('marks the trigger button as a collapsed popup by default', async () => {
    await setup();

    const button: HTMLButtonElement = fixture.nativeElement.querySelector('button');
    expect(button.getAttribute('aria-haspopup')).toBe('true');
    expect(button.getAttribute('aria-expanded')).toBe('false');
  });

  it('logs out, clears the session, and redirects to login', async () => {
    await setup();
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    fixture.componentInstance.logout();

    expect(authService.logout).toHaveBeenCalled();
    expect(navigateSpy).toHaveBeenCalledWith(['/login']);
  });
});
