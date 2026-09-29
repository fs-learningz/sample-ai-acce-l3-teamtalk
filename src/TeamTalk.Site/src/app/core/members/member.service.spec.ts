import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { WorkspaceMemberResponse } from '../models/member.model';
import { MemberService } from './member.service';

const member: WorkspaceMemberResponse = {
  id: 'm1',
  workspaceId: 'w1',
  userId: 'u1',
  displayName: 'Jane',
  email: 'jane@example.com',
  role: 'Admin',
  joinedAtUtc: '2026-01-01T00:00:00Z',
};

describe('MemberService', () => {
  let service: MemberService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(MemberService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('gets the members of a workspace', () => {
    let result: WorkspaceMemberResponse[] | undefined;
    service.getForWorkspaceAsync('w1').subscribe((r) => (result = r));

    const request = httpMock.expectOne('/api/workspaces/w1/members');
    expect(request.request.method).toBe('GET');
    request.flush([member]);

    expect(result).toEqual([member]);
  });

  it('removes a member from a workspace', () => {
    service.removeAsync('w1', 'u2').subscribe();

    const request = httpMock.expectOne('/api/workspaces/w1/members/u2');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });

  it('leaves a workspace', () => {
    service.leaveAsync('w1').subscribe();

    const request = httpMock.expectOne('/api/workspaces/w1/members/leave');
    expect(request.request.method).toBe('POST');
    request.flush(null);
  });

  it('changes a member role with the role in the body', () => {
    service.changeRoleAsync('w1', 'u2', 'Admin').subscribe();

    const request = httpMock.expectOne('/api/workspaces/w1/members/u2/role');
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({ role: 'Admin' });
    request.flush(null);
  });

  it('propagates a conflict when the last admin cannot leave', () => {
    let status: number | undefined;
    service.leaveAsync('w1').subscribe({ error: (e) => (status = e.status) });

    httpMock.expectOne('/api/workspaces/w1/members/leave').flush(null, { status: 409, statusText: 'Conflict' });

    expect(status).toBe(409);
  });
});
