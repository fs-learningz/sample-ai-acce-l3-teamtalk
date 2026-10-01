import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { RegisterComponent } from './register.component';

describe('RegisterComponent', () => {
  let fixture: ComponentFixture<RegisterComponent>;
  let httpMock: HttpTestingController;
  let navigateSpy: ReturnType<typeof vi.spyOn>;

  const authResponse = {
    userId: 'u1',
    email: 'jane@example.com',
    displayName: 'Jane',
    token: 'jwt',
    theme: 'light',
  };

  beforeEach(async () => {
    localStorage.clear();

    await TestBed.configureTestingModule({
      imports: [RegisterComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(RegisterComponent);
    httpMock = TestBed.inject(HttpTestingController);
    navigateSpy = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should create', () => {
    // Arrange / Act
    const component = fixture.componentInstance;

    // Assert
    expect(component).toBeTruthy();
  });

  it('should not submit when the form is invalid', () => {
    // Arrange
    const component = fixture.componentInstance;

    // Act
    component.onSubmit();

    // Assert
    httpMock.expectNone('/api/auth/register');
  });

  it('should reject a password shorter than 8 characters', () => {
    // Arrange
    const component = fixture.componentInstance;

    // Act
    component.form.setValue({ displayName: 'Jane', email: 'jane@example.com', password: '1234567' });

    // Assert
    expect(component.form.controls.password.hasError('minlength')).toBe(true);
    expect(component.form.invalid).toBe(true);
  });

  it('should accept a password of exactly 8 characters', () => {
    // Arrange
    const component = fixture.componentInstance;

    // Act
    component.form.setValue({ displayName: 'Jane', email: 'jane@example.com', password: '12345678' });

    // Assert
    expect(component.form.controls.password.valid).toBe(true);
    expect(component.form.valid).toBe(true);
  });

  it('should reject an empty display name', () => {
    // Arrange
    const component = fixture.componentInstance;

    // Act
    component.form.setValue({ displayName: '', email: 'jane@example.com', password: '12345678' });

    // Assert
    expect(component.form.controls.displayName.hasError('required')).toBe(true);
  });

  it('should reject a malformed email', () => {
    // Arrange
    const component = fixture.componentInstance;

    // Act
    component.form.setValue({ displayName: 'Jane', email: 'nope', password: '12345678' });

    // Assert
    expect(component.form.controls.email.hasError('email')).toBe(true);
  });

  it('should register with valid details and navigate to the workspaces', () => {
    // Arrange
    fixture.componentInstance.form.setValue({
      displayName: 'Jane',
      email: 'jane@example.com',
      password: '12345678',
    });

    // Act
    fixture.componentInstance.onSubmit();
    const req = httpMock.expectOne('/api/auth/register');
    req.flush(authResponse);

    // Assert
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      displayName: 'Jane',
      email: 'jane@example.com',
      password: '12345678',
    });
    expect(fixture.componentInstance.isSubmitting()).toBe(false);
    expect(navigateSpy).toHaveBeenCalledWith(['/workspaces']);
  });

  it('should show the duplicate-email error and stop submitting when registration fails', () => {
    // Arrange
    fixture.componentInstance.form.setValue({
      displayName: 'Jane',
      email: 'jane@example.com',
      password: '12345678',
    });

    // Act
    fixture.componentInstance.onSubmit();
    httpMock.expectOne('/api/auth/register').flush(null, { status: 409, statusText: 'Conflict' });

    // Assert
    expect(fixture.componentInstance.errorMessage()).toBe(
      'Could not create the account. The email may already be registered.',
    );
    expect(fixture.componentInstance.isSubmitting()).toBe(false);
    expect(navigateSpy).not.toHaveBeenCalled();
  });
});
