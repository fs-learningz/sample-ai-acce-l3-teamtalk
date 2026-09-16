import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
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
    req.flush([{ id: 'w1', name: 'Engineering', createdByUserId: 'u1', createdAtUtc: new Date().toISOString() }]);

    expect(fixture.componentInstance.workspaces().length).toBe(1);
  });
});
