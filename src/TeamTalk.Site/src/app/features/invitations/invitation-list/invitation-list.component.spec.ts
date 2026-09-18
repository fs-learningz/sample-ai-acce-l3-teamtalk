import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
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
});
