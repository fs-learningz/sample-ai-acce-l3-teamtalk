import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { WorkspaceShellComponent } from './workspace-shell.component';

describe('WorkspaceShellComponent', () => {
  let fixture: ComponentFixture<WorkspaceShellComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WorkspaceShellComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
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
      .flush({ id: 'w1', name: 'Engineering', createdByUserId: 'u1', createdAtUtc: new Date().toISOString() });
    httpMock.expectOne('/api/workspaces/w1/channels').flush([]);

    expect(fixture.componentInstance.workspace()?.name).toBe('Engineering');
  });
});
