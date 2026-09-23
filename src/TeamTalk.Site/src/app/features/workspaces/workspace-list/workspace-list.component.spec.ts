import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ConfirmationService } from 'primeng/api';
import { vi } from 'vitest';
import { AuthService } from '../../../core/auth/auth.service';
import { WorkspaceListComponent } from './workspace-list.component';

describe('WorkspaceListComponent', () => {
  let fixture: ComponentFixture<WorkspaceListComponent>;
  let httpMock: HttpTestingController;

  async function setup(currentUserId: string | null): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [WorkspaceListComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            currentUser: () =>
              currentUserId ? { userId: currentUserId, email: '', displayName: '' } : null,
            isAuthenticated: () => currentUserId !== null,
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(WorkspaceListComponent);
    httpMock = TestBed.inject(HttpTestingController);
  }

  afterEach(() => {
    httpMock.verify();
  });

  it('should load the current user workspaces on init', async () => {
    await setup('u1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/workspaces');
    req.flush([
      { id: 'w1', name: 'Engineering', createdByUserId: 'u1', createdAtUtc: new Date().toISOString() },
    ]);

    expect(fixture.componentInstance.workspaces().length).toBe(1);
  });

  it('should call the delete endpoint when the owner confirms deleting a workspace', async () => {
    await setup('u1');
    fixture.detectChanges();

    httpMock.expectOne('/api/workspaces').flush([
      { id: 'w1', name: 'Engineering', createdByUserId: 'u1', createdAtUtc: new Date().toISOString() },
    ]);

    const workspace = fixture.componentInstance.workspaces()[0];
    expect(fixture.componentInstance.isOwner(workspace)).toBe(true);

    const confirmationService = fixture.debugElement.injector.get(ConfirmationService);
    vi.spyOn(confirmationService, 'confirm').mockImplementation((confirmation) => {
      confirmation.accept?.();
      return confirmationService;
    });

    fixture.componentInstance.deleteWorkspace(workspace);

    const deleteReq = httpMock.expectOne('/api/workspaces/w1');
    expect(deleteReq.request.method).toBe('DELETE');
    deleteReq.flush(null);

    httpMock.expectOne('/api/workspaces').flush([]);
  });

  it('should not call the delete endpoint when deleting a workspace is cancelled', async () => {
    await setup('u1');
    fixture.detectChanges();

    httpMock.expectOne('/api/workspaces').flush([
      { id: 'w1', name: 'Engineering', createdByUserId: 'u1', createdAtUtc: new Date().toISOString() },
    ]);

    const workspace = fixture.componentInstance.workspaces()[0];

    const confirmationService = fixture.debugElement.injector.get(ConfirmationService);
    vi.spyOn(confirmationService, 'confirm').mockImplementation((confirmation) => {
      confirmation.reject?.();
      return confirmationService;
    });

    fixture.componentInstance.deleteWorkspace(workspace);

    httpMock.expectNone('/api/workspaces/w1');
  });

  it('should call the leave endpoint when a non-owner confirms leaving a workspace', async () => {
    await setup('u2');
    fixture.detectChanges();

    httpMock.expectOne('/api/workspaces').flush([
      { id: 'w1', name: 'Engineering', createdByUserId: 'u1', createdAtUtc: new Date().toISOString() },
    ]);

    const workspace = fixture.componentInstance.workspaces()[0];
    expect(fixture.componentInstance.isOwner(workspace)).toBe(false);

    const confirmationService = fixture.debugElement.injector.get(ConfirmationService);
    vi.spyOn(confirmationService, 'confirm').mockImplementation((confirmation) => {
      confirmation.accept?.();
      return confirmationService;
    });

    fixture.componentInstance.leaveWorkspace(workspace);

    const leaveReq = httpMock.expectOne('/api/workspaces/w1/members/leave');
    expect(leaveReq.request.method).toBe('POST');
    leaveReq.flush(null);

    httpMock.expectOne('/api/workspaces').flush([]);
  });

  it('should not call the leave endpoint when leaving a workspace is cancelled', async () => {
    await setup('u2');
    fixture.detectChanges();

    httpMock.expectOne('/api/workspaces').flush([
      { id: 'w1', name: 'Engineering', createdByUserId: 'u1', createdAtUtc: new Date().toISOString() },
    ]);

    const workspace = fixture.componentInstance.workspaces()[0];

    const confirmationService = fixture.debugElement.injector.get(ConfirmationService);
    vi.spyOn(confirmationService, 'confirm').mockImplementation((confirmation) => {
      confirmation.reject?.();
      return confirmationService;
    });

    fixture.componentInstance.leaveWorkspace(workspace);

    httpMock.expectNone('/api/workspaces/w1/members/leave');
  });
});
