import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ChannelResponse } from '../models/channel.model';
import { ChannelService } from './channel.service';

const channel: ChannelResponse = {
  id: 'c1',
  workspaceId: 'w1',
  name: 'general',
  type: 'Public',
  createdByUserId: 'u1',
  createdAtUtc: '2026-01-01T00:00:00Z',
};

describe('ChannelService', () => {
  let service: ChannelService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(ChannelService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('gets the channels visible to the user in a workspace', () => {
    let result: ChannelResponse[] | undefined;
    service.getVisibleAsync('w1').subscribe((r) => (result = r));

    const request = httpMock.expectOne('/api/workspaces/w1/channels');
    expect(request.request.method).toBe('GET');
    request.flush([channel]);

    expect(result).toEqual([channel]);
  });

  it('creates a channel in a workspace', () => {
    service.createAsync('w1', { name: 'general', type: 'Private' }).subscribe();

    const request = httpMock.expectOne('/api/workspaces/w1/channels');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'general', type: 'Private' });
    request.flush(channel);
  });

  it('joins a channel', () => {
    service.joinAsync('w1', 'c1').subscribe();

    const request = httpMock.expectOne('/api/workspaces/w1/channels/c1/join');
    expect(request.request.method).toBe('POST');
    request.flush(null);
  });

  it('propagates a conflict when the channel name already exists', () => {
    let status: number | undefined;
    service.createAsync('w1', { name: 'general', type: 'Public' }).subscribe({
      error: (e) => (status = e.status),
    });

    httpMock.expectOne('/api/workspaces/w1/channels').flush(null, { status: 409, statusText: 'Conflict' });

    expect(status).toBe(409);
  });
});
