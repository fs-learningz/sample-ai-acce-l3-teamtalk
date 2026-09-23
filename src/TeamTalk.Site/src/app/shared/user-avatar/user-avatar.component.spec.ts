import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';
import { UserAvatarComponent } from './user-avatar.component';

describe('UserAvatarComponent', () => {
  let fixture: ComponentFixture<UserAvatarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UserAvatarComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(UserAvatarComponent);
  });

  it('renders the initials of a full name', () => {
    fixture.componentRef.setInput('name', 'Jane Doe');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent.trim()).toBe('JD');
  });

  it('falls back to the email initial when there is no name', () => {
    fixture.componentRef.setInput('email', 'jane@example.com');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent.trim()).toBe('J');
  });

  it('renders an image instead of initials when an image URL is provided', () => {
    fixture.componentRef.setInput('name', 'Jane Doe');
    fixture.componentRef.setInput('imageUrl', 'https://example.com/avatar.png');
    fixture.detectChanges();

    const img = fixture.nativeElement.querySelector('img');
    expect(img).toBeTruthy();
    expect(img.getAttribute('src')).toBe('https://example.com/avatar.png');
    expect(fixture.nativeElement.textContent.trim()).toBe('');
  });

  it('does not render a presence dot when presence is not set', () => {
    fixture.componentRef.setInput('name', 'Jane Doe');
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector('.bg-green-500, .bg-yellow-400, .bg-gray-400'),
    ).toBeNull();
  });

  it('renders a presence dot matching the given status', () => {
    fixture.componentRef.setInput('name', 'Jane Doe');
    fixture.componentRef.setInput('presence', 'away');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.bg-yellow-400')).toBeTruthy();
  });
});
