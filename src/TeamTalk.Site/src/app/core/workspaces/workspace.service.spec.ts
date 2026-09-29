import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { WorkspaceResponse } from '../models/workspace.model';
import { WorkspaceService } from './workspace.service';

const workspace: WorkspaceResponse = {
  id: 'w1',
  name: 'Engineering',
  description: null,
  hasIcon: false,
  createdByUserId: 'u1',
  createdByDisplayName: 'Jane',
  createdAtUtc: '2026-01-01T00:00:00Z',
  memberCount: 1,
  currentUserRole: 'Admin',
};

describe('WorkspaceService', () => {
  let service: WorkspaceService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(WorkspaceService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('gets the workspaces of the current user', () => {
    let result: WorkspaceResponse[] | undefined;
    service.getForCurrentUserAsync().subscribe((r) => (result = r));

    const request = httpMock.expectOne('/api/workspaces');
    expect(request.request.method).toBe('GET');
    request.flush([workspace]);

    expect(result).toEqual([workspace]);
  });

  it('gets a workspace by id', () => {
    let result: WorkspaceResponse | undefined;
    service.getByIdAsync('w1').subscribe((r) => (result = r));

    const request = httpMock.expectOne('/api/workspaces/w1');
    expect(request.request.method).toBe('GET');
    request.flush(workspace);

    expect(result).toEqual(workspace);
  });

  it('creates a workspace', () => {
    service.createAsync({ name: 'Engineering' }).subscribe();

    const request = httpMock.expectOne('/api/workspaces');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'Engineering' });
    request.flush(workspace);
  });

  it('updates a workspace', () => {
    service.updateAsync('w1', { name: 'Renamed', description: 'About' }).subscribe();

    const request = httpMock.expectOne('/api/workspaces/w1');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ name: 'Renamed', description: 'About' });
    request.flush(workspace);
  });

  it('uploads the icon as multipart form data with the file under "file"', () => {
    const file = new File([new Uint8Array([1, 2, 3])], 'icon.png', { type: 'image/png' });
    service.updateIconAsync('w1', file).subscribe();

    const request = httpMock.expectOne('/api/workspaces/w1/icon');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toBeInstanceOf(FormData);
    expect((request.request.body as FormData).get('file')).toBeInstanceOf(File);
    expect(((request.request.body as FormData).get('file') as File).name).toBe('icon.png');
    request.flush(workspace);
  });

  it('removes the icon', () => {
    service.removeIconAsync('w1').subscribe();

    const request = httpMock.expectOne('/api/workspaces/w1/icon');
    expect(request.request.method).toBe('DELETE');
    request.flush(workspace);
  });

  it('gets the icon as a blob', () => {
    let result: Blob | undefined;
    service.getIconAsync('w1').subscribe((r) => (result = r));

    const request = httpMock.expectOne('/api/workspaces/w1/icon');
    expect(request.request.method).toBe('GET');
    expect(request.request.responseType).toBe('blob');
    request.flush(new Blob(['x'], { type: 'image/png' }));

    expect(result).toBeInstanceOf(Blob);
  });

  it('deletes a workspace', () => {
    service.deleteAsync('w1').subscribe();

    const request = httpMock.expectOne('/api/workspaces/w1');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });

  it('propagates server errors to the subscriber', () => {
    let status: number | undefined;
    service.getByIdAsync('missing').subscribe({ error: (e) => (status = e.status) });

    httpMock.expectOne('/api/workspaces/missing').flush(null, { status: 404, statusText: 'Not Found' });

    expect(status).toBe(404);
  });
});
