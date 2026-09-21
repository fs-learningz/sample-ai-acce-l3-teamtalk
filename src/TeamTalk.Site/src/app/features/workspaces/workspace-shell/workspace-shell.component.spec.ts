import { Component } from '@angular/core';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { WorkspaceShellComponent } from './workspace-shell.component';

@Component({ template: '' })
class WorkspaceListStubComponent {}

describe('WorkspaceShellComponent', () => {
  let fixture: ComponentFixture<WorkspaceShellComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WorkspaceShellComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'workspaces', component: WorkspaceListStubComponent }]),
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

    httpMock
      .expectOne('/api/workspaces/w1')
      .flush({
        id: 'w1',
        name: 'Engineering',
        createdByUserId: 'u1',
        createdAtUtc: new Date().toISOString(),
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
      currentUserRole: 'Admin',
    });
    httpMock.expectOne('/api/workspaces/w1/channels').flush([]);
    httpMock.expectOne('/api/workspaces/w1/members').flush([]);
    httpMock.expectOne('/api/workspaces/w1/invitations').flush([]);

    expect(fixture.componentInstance.isAdmin()).toBe(true);
  });

  it('should load workspace members on init', () => {
    fixture.detectChanges();

    httpMock
      .expectOne('/api/workspaces/w1')
      .flush({
        id: 'w1',
        name: 'Engineering',
        createdByUserId: 'u1',
        createdAtUtc: new Date().toISOString(),
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

  it('should call the remove endpoint when removing a member is confirmed', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    fixture.detectChanges();

    httpMock
      .expectOne('/api/workspaces/w1')
      .flush({
        id: 'w1',
        name: 'Engineering',
        createdByUserId: 'u1',
        createdAtUtc: new Date().toISOString(),
      });
    httpMock.expectOne('/api/workspaces/w1/channels').flush([]);
    httpMock.expectOne('/api/workspaces/w1/members').flush([]);

    fixture.componentInstance.removeMember({
      id: 'm2',
      workspaceId: 'w1',
      userId: 'u2',
      displayName: 'Member User',
      email: 'member@example.com',
      role: 'Member',
      joinedAtUtc: new Date().toISOString(),
    });

    const removeReq = httpMock.expectOne('/api/workspaces/w1/members/u2');
    expect(removeReq.request.method).toBe('DELETE');
    removeReq.flush(null);

    httpMock.expectOne('/api/workspaces/w1/members').flush([]);
  });

  it('should not call the remove endpoint when removing a member is cancelled', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false);
    fixture.detectChanges();

    httpMock
      .expectOne('/api/workspaces/w1')
      .flush({
        id: 'w1',
        name: 'Engineering',
        createdByUserId: 'u1',
        createdAtUtc: new Date().toISOString(),
      });
    httpMock.expectOne('/api/workspaces/w1/channels').flush([]);
    httpMock.expectOne('/api/workspaces/w1/members').flush([]);

    fixture.componentInstance.removeMember({
      id: 'm2',
      workspaceId: 'w1',
      userId: 'u2',
      displayName: 'Member User',
      email: 'member@example.com',
      role: 'Member',
      joinedAtUtc: new Date().toISOString(),
    });

    httpMock.expectNone('/api/workspaces/w1/members/u2');
  });

  it('should call the leave endpoint when leaving is confirmed', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    fixture.detectChanges();

    httpMock
      .expectOne('/api/workspaces/w1')
      .flush({
        id: 'w1',
        name: 'Engineering',
        createdByUserId: 'u1',
        createdAtUtc: new Date().toISOString(),
      });
    httpMock.expectOne('/api/workspaces/w1/channels').flush([]);
    httpMock.expectOne('/api/workspaces/w1/members').flush([]);

    fixture.componentInstance.leaveWorkspace();

    const leaveReq = httpMock.expectOne('/api/workspaces/w1/members/leave');
    expect(leaveReq.request.method).toBe('POST');
    leaveReq.flush(null);
  });

  it('should call the delete endpoint when deleting the workspace is confirmed', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    fixture.detectChanges();

    httpMock
      .expectOne('/api/workspaces/w1')
      .flush({
        id: 'w1',
        name: 'Engineering',
        createdByUserId: 'u1',
        createdAtUtc: new Date().toISOString(),
      });
    httpMock.expectOne('/api/workspaces/w1/channels').flush([]);
    httpMock.expectOne('/api/workspaces/w1/members').flush([]);

    fixture.componentInstance.deleteWorkspace();

    const deleteReq = httpMock.expectOne('/api/workspaces/w1');
    expect(deleteReq.request.method).toBe('DELETE');
    deleteReq.flush(null);
  });
});
