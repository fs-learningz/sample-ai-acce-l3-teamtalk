import { Component } from '@angular/core';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { WorkspaceShellComponent } from './workspace-shell.component';

@Component({ template: '' })
class WorkspaceListStubComponent {}

@Component({ template: '' })
class WorkspaceSettingsStubComponent {}

describe('WorkspaceShellComponent', () => {
  let fixture: ComponentFixture<WorkspaceShellComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WorkspaceShellComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([
          { path: 'workspaces', component: WorkspaceListStubComponent },
          { path: 'workspaces/:workspaceId/settings', component: WorkspaceSettingsStubComponent },
        ]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ workspaceId: 'w1' }) } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(WorkspaceShellComponent);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should load the workspace and its channels on init', () => {
    fixture.detectChanges();

    httpMock.expectOne('/api/workspaces/w1').flush({
      id: 'w1',
      name: 'Engineering',
      createdByUserId: 'u1',
      createdAtUtc: new Date().toISOString(),
      hasIcon: false,
    });
    httpMock.expectOne('/api/workspaces/w1/channels').flush([]);
    httpMock.expectOne('/api/workspaces/w1/members').flush([]);

    expect(fixture.componentInstance.workspace()?.name).toBe('Engineering');
  });

  it('should load pending invitations when the current user is an admin', () => {
    fixture.detectChanges();

    httpMock.expectOne('/api/workspaces/w1').flush({
      id: 'w1',
      name: 'Engineering',
      createdByUserId: 'u1',
      createdAtUtc: new Date().toISOString(),
      hasIcon: false,
      currentUserRole: 'Admin',
    });
    httpMock.expectOne('/api/workspaces/w1/channels').flush([]);
    httpMock.expectOne('/api/workspaces/w1/members').flush([]);
    httpMock.expectOne('/api/workspaces/w1/invitations').flush([]);

    expect(fixture.componentInstance.isAdmin()).toBe(true);
  });

  it('should load workspace members on init', () => {
    fixture.detectChanges();

    httpMock.expectOne('/api/workspaces/w1').flush({
      id: 'w1',
      name: 'Engineering',
      createdByUserId: 'u1',
      createdAtUtc: new Date().toISOString(),
      hasIcon: false,
    });
    httpMock.expectOne('/api/workspaces/w1/channels').flush([]);
    httpMock.expectOne('/api/workspaces/w1/members').flush([
      {
        id: 'm1',
        workspaceId: 'w1',
        userId: 'u1',
        displayName: 'Owner User',
        email: 'owner@example.com',
        role: 'Admin',
        joinedAtUtc: new Date().toISOString(),
      },
    ]);

    expect(fixture.componentInstance.members().length).toBe(1);
  });

  it('should fetch the workspace icon as a blob when the workspace has one', () => {
    fixture.detectChanges();

    httpMock.expectOne('/api/workspaces/w1').flush({
      id: 'w1',
      name: 'Engineering',
      createdByUserId: 'u1',
      createdAtUtc: new Date().toISOString(),
      hasIcon: true,
    });
    httpMock.expectOne('/api/workspaces/w1/channels').flush([]);
    httpMock.expectOne('/api/workspaces/w1/members').flush([]);

    const iconReq = httpMock.expectOne('/api/workspaces/w1/icon');
    expect(iconReq.request.method).toBe('GET');
    iconReq.flush(new Blob(['icon']));

    expect(fixture.componentInstance.workspaceIconUrl()).toBeTruthy();
  });

  it('should navigate to workspace settings when the header is clicked', () => {
    fixture.detectChanges();

    httpMock.expectOne('/api/workspaces/w1').flush({
      id: 'w1',
      name: 'Engineering',
      createdByUserId: 'u1',
      createdAtUtc: new Date().toISOString(),
      hasIcon: false,
    });
    httpMock.expectOne('/api/workspaces/w1/channels').flush([]);
    httpMock.expectOne('/api/workspaces/w1/members').flush([]);

    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate');

    fixture.componentInstance.openWorkspaceSettings();

    expect(navigateSpy).toHaveBeenCalledWith(['/workspaces', 'w1', 'settings']);
  });
});
