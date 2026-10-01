import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
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
      imports: [LoginComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
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
    httpMock.expectNone('/api/auth/login');
  });

  it('should treat a malformed email as invalid and not submit', () => {
    // Arrange
    const component = fixture.componentInstance;
    component.form.setValue({ email: 'not-an-email', password: 'secret' });

    // Act
    component.onSubmit();

    // Assert
    expect(component.form.controls.email.hasError('email')).toBe(true);
    httpMock.expectNone('/api/auth/login');
  });

  it('should log in with valid credentials and navigate to the workspaces', () => {
    // Arrange
    fixture.componentInstance.form.setValue({ email: 'jane@example.com', password: 'secret' });

    // Act
    fixture.componentInstance.onSubmit();
    const req = httpMock.expectOne('/api/auth/login');
    req.flush(authResponse);

    // Assert
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'jane@example.com', password: 'secret' });
    expect(navigateSpy).toHaveBeenCalledWith(['/workspaces']);
  });

  it('should be submitting during the request and not submitting afterwards', () => {
    // Arrange
    fixture.componentInstance.form.setValue({ email: 'jane@example.com', password: 'secret' });

    // Act
    fixture.componentInstance.onSubmit();
    const duringRequest = fixture.componentInstance.isSubmitting();
    httpMock.expectOne('/api/auth/login').flush(authResponse);

    // Assert
    expect(duringRequest).toBe(true);
    expect(fixture.componentInstance.isSubmitting()).toBe(false);
  });

  it('should show an error and stop submitting when the login fails', () => {
    // Arrange
    fixture.componentInstance.form.setValue({ email: 'jane@example.com', password: 'wrong' });

    // Act
    fixture.componentInstance.onSubmit();
    httpMock.expectOne('/api/auth/login').flush(null, { status: 401, statusText: 'Unauthorized' });

    // Assert
    expect(fixture.componentInstance.errorMessage()).toBe('Invalid email or password.');
    expect(fixture.componentInstance.isSubmitting()).toBe(false);
    expect(navigateSpy).not.toHaveBeenCalled();
  });

  it('should clear the previous error when a retry succeeds', () => {
    // Arrange
    fixture.componentInstance.form.setValue({ email: 'jane@example.com', password: 'wrong' });
    fixture.componentInstance.onSubmit();
    httpMock.expectOne('/api/auth/login').flush(null, { status: 401, statusText: 'Unauthorized' });

    // Act
    fixture.componentInstance.form.setValue({ email: 'jane@example.com', password: 'secret' });
    fixture.componentInstance.onSubmit();
    const errorWhileRetrying = fixture.componentInstance.errorMessage();
    httpMock.expectOne('/api/auth/login').flush(authResponse);

    // Assert
    expect(errorWhileRetrying).toBeNull();
    expect(fixture.componentInstance.errorMessage()).toBeNull();
  });
});
