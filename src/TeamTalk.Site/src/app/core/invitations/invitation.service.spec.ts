import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { WorkspaceInvitationResponse } from '../models/invitation.model';
import { InvitationService } from './invitation.service';

const invitation: WorkspaceInvitationResponse = {
  id: 'i1',
  workspaceId: 'w1',
  workspaceName: 'Engineering',
  workspaceHasIcon: false,
  email: 'bob@example.com',
  role: 'Member',
  status: 'Pending',
  invitedByUserId: 'u1',
  createdAtUtc: '2026-01-01T00:00:00Z',
};

describe('InvitationService', () => {
  let service: InvitationService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(InvitationService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('invites a member to a workspace', () => {
    service.inviteAsync('w1', { email: 'bob@example.com', role: 'Member' }).subscribe();

    const request = httpMock.expectOne('/api/workspaces/w1/invitations');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ email: 'bob@example.com', role: 'Member' });
    request.flush(invitation);
  });

  it('gets the invitations of a workspace', () => {
    let result: WorkspaceInvitationResponse[] | undefined;
    service.getForWorkspaceAsync('w1').subscribe((r) => (result = r));

    const request = httpMock.expectOne('/api/workspaces/w1/invitations');
    expect(request.request.method).toBe('GET');
    request.flush([invitation]);

    expect(result).toEqual([invitation]);
  });

  it('revokes an invitation', () => {
    service.revokeAsync('w1', 'i1').subscribe();

    const request = httpMock.expectOne('/api/workspaces/w1/invitations/i1');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });

  it('gets the invitations addressed to the current user', () => {
    let result: WorkspaceInvitationResponse[] | undefined;
    service.getMyInvitationsAsync().subscribe((r) => (result = r));

    const request = httpMock.expectOne('/api/invitations');
    expect(request.request.method).toBe('GET');
    request.flush([invitation]);

    expect(result).toEqual([invitation]);
  });

  it('accepts an invitation', () => {
    service.acceptAsync('i1').subscribe();

    const request = httpMock.expectOne('/api/invitations/i1/accept');
    expect(request.request.method).toBe('POST');
    request.flush(null);
  });

  it('declines an invitation', () => {
    service.declineAsync('i1').subscribe();

    const request = httpMock.expectOne('/api/invitations/i1/decline');
    expect(request.request.method).toBe('POST');
    request.flush(null);
  });

  it('gets the workspace icon of an invitation as a blob', () => {
    let result: Blob | undefined;
    service.getWorkspaceIconAsync('i1').subscribe((r) => (result = r));

    const request = httpMock.expectOne('/api/invitations/i1/workspace-icon');
    expect(request.request.method).toBe('GET');
    expect(request.request.responseType).toBe('blob');
    request.flush(new Blob(['x'], { type: 'image/png' }));

    expect(result).toBeInstanceOf(Blob);
  });

  it('propagates a conflict when accepting an already answered invitation', () => {
    let status: number | undefined;
    service.acceptAsync('i1').subscribe({ error: (e) => (status = e.status) });

    httpMock.expectOne('/api/invitations/i1/accept').flush(null, { status: 409, statusText: 'Conflict' });

    expect(status).toBe(409);
  });
});
