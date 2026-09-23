import { Component } from '@angular/core';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { WorkspaceSettingsComponent } from './workspace-settings.component';

@Component({ template: '' })
class WorkspaceShellStubComponent {}

describe('WorkspaceSettingsComponent', () => {
  let fixture: ComponentFixture<WorkspaceSettingsComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WorkspaceSettingsComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([
          { path: 'workspaces/:workspaceId', component: WorkspaceShellStubComponent },
        ]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ workspaceId: 'w1' }) } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(WorkspaceSettingsComponent);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should load the workspace and members into the shared store on init', () => {
    fixture.detectChanges();

    httpMock.expectOne('/api/workspaces/w1').flush({
      id: 'w1',
      name: 'Engineering',
      createdByUserId: 'u1',
      createdAtUtc: new Date().toISOString(),
      hasIcon: false,
    });
    httpMock.expectOne('/api/workspaces/w1/members').flush([]);

    expect(fixture.componentInstance.store.workspace()?.name).toBe('Engineering');
    expect(fixture.componentInstance.store.workspaceId).toBe('w1');
  });

  it('should navigate back to the workspace when requested', () => {
    fixture.detectChanges();
    httpMock.expectOne('/api/workspaces/w1').flush({
      id: 'w1',
      name: 'Engineering',
      createdByUserId: 'u1',
      createdAtUtc: new Date().toISOString(),
      hasIcon: false,
    });
    httpMock.expectOne('/api/workspaces/w1/members').flush([]);

    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate');

    fixture.componentInstance.backToWorkspace();

    expect(navigateSpy).toHaveBeenCalledWith(['/workspaces', 'w1']);
  });
});
