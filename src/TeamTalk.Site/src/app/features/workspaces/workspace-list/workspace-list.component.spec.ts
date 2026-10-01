import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { WorkspaceListComponent } from './workspace-list.component';

describe('WorkspaceListComponent', () => {
  let fixture: ComponentFixture<WorkspaceListComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WorkspaceListComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(WorkspaceListComponent);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should load the current user workspaces on init', () => {
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/workspaces');
    req.flush([
      {
        id: 'w1',
        name: 'Engineering',
        createdByUserId: 'u1',
        createdAtUtc: new Date().toISOString(),
      },
    ]);

    expect(fixture.componentInstance.workspaces().length).toBe(1);
  });

  it('should navigate into a workspace when it is opened', () => {
    fixture.detectChanges();

    httpMock.expectOne('/api/workspaces').flush([
      {
        id: 'w1',
        name: 'Engineering',
        createdByUserId: 'u1',
        createdAtUtc: new Date().toISOString(),
      },
    ]);

    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate');

    fixture.componentInstance.openWorkspace(fixture.componentInstance.workspaces()[0]);

    expect(navigateSpy).toHaveBeenCalledWith(['/workspaces', 'w1']);
  });

  it('should create a workspace and navigate into it', () => {
    fixture.detectChanges();

    httpMock.expectOne('/api/workspaces').flush([]);

    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate');

    fixture.componentInstance.openCreateDialog();
    fixture.componentInstance.createForm.setValue({ name: 'Design' });
    fixture.componentInstance.onCreateSubmit();

    const createReq = httpMock.expectOne('/api/workspaces');
    expect(createReq.request.method).toBe('POST');
    createReq.flush({
      id: 'w2',
      name: 'Design',
      createdByUserId: 'u1',
      createdAtUtc: new Date().toISOString(),
    });

    httpMock.expectOne('/api/workspaces').flush([]);

    expect(navigateSpy).toHaveBeenCalledWith(['/workspaces', 'w2']);
  });

  it('should fetch the workspace icon as a blob when the workspace has one', () => {
    fixture.detectChanges();

    httpMock.expectOne('/api/workspaces').flush([
      {
        id: 'w1',
        name: 'Engineering',
        createdByUserId: 'u1',
        createdAtUtc: new Date().toISOString(),
        hasIcon: true,
      },
    ]);

    const iconReq = httpMock.expectOne('/api/workspaces/w1/icon');
    expect(iconReq.request.method).toBe('GET');
    iconReq.flush(new Blob(['icon']));

    expect(fixture.componentInstance.iconUrls()['w1']).toBeTruthy();
  });

  function workspace(id: string, hasIcon = false) {
    return {
      id,
      name: `Workspace ${id}`,
      createdByUserId: 'u1',
      createdAtUtc: new Date().toISOString(),
      hasIcon,
    };
  }

  it('should not create a workspace when the name is empty', () => {
    // Arrange
    fixture.detectChanges();
    httpMock.expectOne('/api/workspaces').flush([]);
    fixture.componentInstance.openCreateDialog();

    // Act
    fixture.componentInstance.onCreateSubmit();

    // Assert
    httpMock.expectNone({ method: 'POST', url: '/api/workspaces' });
  });

  it('should show an error and keep the dialog open when creating a workspace fails', () => {
    // Arrange
    fixture.detectChanges();
    httpMock.expectOne('/api/workspaces').flush([]);
    fixture.componentInstance.openCreateDialog();
    fixture.componentInstance.createForm.setValue({ name: 'Design' });

    // Act
    fixture.componentInstance.onCreateSubmit();
    httpMock
      .expectOne({ method: 'POST', url: '/api/workspaces' })
      .flush(null, { status: 500, statusText: 'Server Error' });

    // Assert
    expect(fixture.componentInstance.errorMessage()).toBe(
      'Could not create the workspace. Please try again.',
    );
    expect(fixture.componentInstance.isCreateDialogVisible()).toBe(true);
  });

  it('should show an error when the workspaces fail to load', () => {
    // Arrange
    fixture.detectChanges();

    // Act
    httpMock
      .expectOne('/api/workspaces')
      .flush(null, { status: 500, statusText: 'Server Error' });

    // Assert
    expect(fixture.componentInstance.errorMessage()).toBe(
      'Could not load your workspaces. Please refresh the page.',
    );
    expect(fixture.componentInstance.workspaces()).toEqual([]);
  });

  it('should navigate to the invitations page', () => {
    // Arrange
    const navigateSpy = vi.spyOn(TestBed.inject(Router), 'navigate');
    fixture.detectChanges();
    httpMock.expectOne('/api/workspaces').flush([]);

    // Act
    fixture.componentInstance.openInvitations();

    // Assert
    expect(navigateSpy).toHaveBeenCalledWith(['/invitations']);
  });

  it('should fetch icons only for workspaces that have one and revoke them on destroy', () => {
    // Arrange
    const revokeSpy = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
    fixture.detectChanges();
    httpMock.expectOne('/api/workspaces').flush([workspace('w1', true), workspace('w2', false)]);
    httpMock.expectOne('/api/workspaces/w1/icon').flush(new Blob(['icon']));
    const iconUrl = fixture.componentInstance.iconUrls()['w1'];

    // Act
    fixture.destroy();

    // Assert
    httpMock.expectNone('/api/workspaces/w2/icon');
    expect(fixture.componentInstance.iconUrls()['w2']).toBeUndefined();
    expect(revokeSpy).toHaveBeenCalledWith(iconUrl);
    revokeSpy.mockRestore();
  });
});
