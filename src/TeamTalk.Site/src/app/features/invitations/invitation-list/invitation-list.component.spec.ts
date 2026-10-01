import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { InvitationListComponent } from './invitation-list.component';

describe('InvitationListComponent', () => {
  let fixture: ComponentFixture<InvitationListComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InvitationListComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    fixture = TestBed.createComponent(InvitationListComponent);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should load the pending invitations for the current user on init', () => {
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/invitations');
    req.flush([
      {
        id: 'i1',
        workspaceId: 'w1',
        workspaceName: 'Engineering',
        email: 'user@example.com',
        role: 'Member',
        status: 'Pending',
        invitedByUserId: 'u1',
        createdAtUtc: new Date().toISOString(),
      },
    ]);

    expect(fixture.componentInstance.invitations().length).toBe(1);
  });

  it('should fetch the workspace icon as a blob when the invitation has one', () => {
    fixture.detectChanges();

    httpMock.expectOne('/api/invitations').flush([
      {
        id: 'i1',
        workspaceId: 'w1',
        workspaceName: 'Engineering',
        workspaceHasIcon: true,
        email: 'user@example.com',
        role: 'Member',
        status: 'Pending',
        invitedByUserId: 'u1',
        createdAtUtc: new Date().toISOString(),
      },
    ]);

    const iconReq = httpMock.expectOne('/api/invitations/i1/workspace-icon');
    expect(iconReq.request.method).toBe('GET');
    iconReq.flush(new Blob(['icon']));

    expect(fixture.componentInstance.iconUrls()['i1']).toBeTruthy();
  });

  function invitation(id: string, workspaceHasIcon = false) {
    return {
      id,
      workspaceId: `w-${id}`,
      workspaceName: 'Engineering',
      workspaceHasIcon,
      email: 'user@example.com',
      role: 'Member',
      status: 'Pending',
      invitedByUserId: 'u1',
      createdAtUtc: new Date().toISOString(),
    };
  }

  it('should accept an invitation and navigate into the workspace', () => {
    // Arrange
    const navigateSpy = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture.detectChanges();
    httpMock.expectOne('/api/invitations').flush([invitation('i1')]);

    // Act
    fixture.componentInstance.acceptInvitation(fixture.componentInstance.invitations()[0]);
    const req = httpMock.expectOne('/api/invitations/i1/accept');
    req.flush(null);

    // Assert
    expect(req.request.method).toBe('POST');
    expect(navigateSpy).toHaveBeenCalledWith(['/workspaces', 'w-i1']);
  });

  it('should show an error and not navigate when accepting fails', () => {
    // Arrange
    const navigateSpy = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture.detectChanges();
    httpMock.expectOne('/api/invitations').flush([invitation('i1')]);

    // Act
    fixture.componentInstance.acceptInvitation(fixture.componentInstance.invitations()[0]);
    httpMock
      .expectOne('/api/invitations/i1/accept')
      .flush(null, { status: 409, statusText: 'Conflict' });

    // Assert
    expect(fixture.componentInstance.errorMessage()).toBe(
      'Could not accept the invitation. Please try again.',
    );
    expect(navigateSpy).not.toHaveBeenCalled();
  });

  it('should decline an invitation and reload the list', () => {
    // Arrange
    fixture.detectChanges();
    httpMock.expectOne('/api/invitations').flush([invitation('i1'), invitation('i2')]);

    // Act
    fixture.componentInstance.declineInvitation(fixture.componentInstance.invitations()[0]);
    const req = httpMock.expectOne('/api/invitations/i1/decline');
    req.flush(null);
    httpMock.expectOne('/api/invitations').flush([invitation('i2')]);

    // Assert
    expect(req.request.method).toBe('POST');
    expect(fixture.componentInstance.invitations().map((i) => i.id)).toEqual(['i2']);
  });

  it('should show an error when declining fails', () => {
    // Arrange
    fixture.detectChanges();
    httpMock.expectOne('/api/invitations').flush([invitation('i1')]);

    // Act
    fixture.componentInstance.declineInvitation(fixture.componentInstance.invitations()[0]);
    httpMock
      .expectOne('/api/invitations/i1/decline')
      .flush(null, { status: 500, statusText: 'Server Error' });

    // Assert
    expect(fixture.componentInstance.errorMessage()).toBe(
      'Could not decline the invitation. Please try again.',
    );
    expect(fixture.componentInstance.invitations().length).toBe(1);
  });

  it('should show an error when the invitations fail to load', () => {
    // Arrange
    fixture.detectChanges();

    // Act
    httpMock
      .expectOne('/api/invitations')
      .flush(null, { status: 500, statusText: 'Server Error' });

    // Assert
    expect(fixture.componentInstance.errorMessage()).toBe(
      'Could not load your invitations. Please refresh the page.',
    );
  });

  it('should fetch icons only for invitations whose workspace has one and revoke them on destroy', () => {
    // Arrange
    const revokeSpy = vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
    fixture.detectChanges();
    httpMock.expectOne('/api/invitations').flush([invitation('i1', true), invitation('i2', false)]);
    httpMock.expectOne('/api/invitations/i1/workspace-icon').flush(new Blob(['icon']));
    const iconUrl = fixture.componentInstance.iconUrls()['i1'];

    // Act
    fixture.destroy();

    // Assert
    httpMock.expectNone('/api/invitations/i2/workspace-icon');
    expect(fixture.componentInstance.iconUrls()['i2']).toBeUndefined();
    expect(revokeSpy).toHaveBeenCalledWith(iconUrl);
    revokeSpy.mockRestore();
  });
});
