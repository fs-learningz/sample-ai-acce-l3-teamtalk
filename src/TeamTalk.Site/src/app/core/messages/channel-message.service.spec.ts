import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ChannelMessagesResponse, MessageResponse } from '../models/message.model';
import { ChannelMessageService } from './channel-message.service';

const message: MessageResponse = {
  id: 'm1',
  channelId: 'c1',
  senderId: 'u1',
  senderDisplayName: 'Jane',
  content: 'hello',
  sentAtUtc: '2026-01-01T00:00:00Z',
  editedAtUtc: null,
  isDeleted: false,
};

describe('ChannelMessageService', () => {
  let service: ChannelMessageService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(ChannelMessageService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('gets the first page of history with only the page size', () => {
    let result: ChannelMessagesResponse | undefined;
    service.getHistoryAsync('w1', 'c1', null, 50).subscribe((r) => (result = r));

    const request = httpMock.expectOne(
      (r) => r.url === '/api/workspaces/w1/channels/c1/messages',
    );
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('pageSize')).toBe('50');
    expect(request.request.params.has('cursor')).toBe(false);
    request.flush({ items: [message], nextCursor: null });

    expect(result?.items).toEqual([message]);
  });

  it('includes the cursor when requesting an older page', () => {
    service.getHistoryAsync('w1', 'c1', '2026-01-01T00:00:00Z', 20).subscribe();

    const request = httpMock.expectOne(
      (r) => r.url === '/api/workspaces/w1/channels/c1/messages',
    );
    expect(request.request.params.get('cursor')).toBe('2026-01-01T00:00:00Z');
    expect(request.request.params.get('pageSize')).toBe('20');
    request.flush({ items: [], nextCursor: null });
  });

  it('sends a message', () => {
    service.sendAsync('w1', 'c1', { content: 'hello' }).subscribe();

    const request = httpMock.expectOne('/api/workspaces/w1/channels/c1/messages');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ content: 'hello' });
    request.flush(message);
  });

  it('edits a message', () => {
    service.editAsync('w1', 'c1', 'm1', { content: 'updated' }).subscribe();

    const request = httpMock.expectOne('/api/workspaces/w1/channels/c1/messages/m1');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ content: 'updated' });
    request.flush(message);
  });

  it('deletes a message', () => {
    service.deleteAsync('w1', 'c1', 'm1').subscribe();

    const request = httpMock.expectOne('/api/workspaces/w1/channels/c1/messages/m1');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });

  it('propagates a forbidden response when the user cannot post', () => {
    let status: number | undefined;
    service.sendAsync('w1', 'c1', { content: 'hello' }).subscribe({ error: (e) => (status = e.status) });

    httpMock
      .expectOne('/api/workspaces/w1/channels/c1/messages')
      .flush(null, { status: 403, statusText: 'Forbidden' });

    expect(status).toBe(403);
  });
});
