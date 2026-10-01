import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject, Subject } from 'rxjs';
import { vi } from 'vitest';
import { AuthService } from '../../../core/auth/auth.service';
import { SignalRService } from '../../../core/realtime/signalr.service';
import { MessageResponse } from '../../../core/models/message.model';
import { ChannelViewComponent } from './channel-view.component';

describe('ChannelViewComponent', () => {
  let fixture: ComponentFixture<ChannelViewComponent>;
  let httpMock: HttpTestingController;
  let paramMap$: BehaviorSubject<ReturnType<typeof convertToParamMap>>;

  let fakeSignalRService: {
    connectAsync: ReturnType<typeof vi.fn>;
    joinChannelAsync: ReturnType<typeof vi.fn>;
    leaveChannelAsync: ReturnType<typeof vi.fn>;
    messageReceived$: Subject<MessageResponse>;
    messageEdited$: Subject<MessageResponse>;
    messageDeleted$: Subject<MessageResponse>;
  };

  const messagesUrl = '/api/workspaces/w1/channels/c1/messages';

  function message(overrides: Partial<MessageResponse> = {}): MessageResponse {
    return {
      id: 'm1',
      channelId: 'c1',
      senderId: 'u1',
      senderDisplayName: 'Ada',
      content: 'hello',
      sentAtUtc: new Date().toISOString(),
      editedAtUtc: null,
      isDeleted: false,
      ...overrides,
    };
  }

  function flushHistory(items: MessageResponse[] = []): void {
    httpMock.expectOne((r) => r.url === messagesUrl && r.method === 'GET').flush({
      items,
      nextCursor: null,
    });
  }

  function flushPromises(): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve));
  }

  beforeEach(async () => {
    fakeSignalRService = {
      connectAsync: vi.fn(async () => undefined),
      joinChannelAsync: vi.fn(async () => undefined),
      leaveChannelAsync: vi.fn(async () => undefined),
      messageReceived$: new Subject<MessageResponse>(),
      messageEdited$: new Subject<MessageResponse>(),
      messageDeleted$: new Subject<MessageResponse>(),
    };
    paramMap$ = new BehaviorSubject(convertToParamMap({ workspaceId: 'w1', channelId: 'c1' }));

    await TestBed.configureTestingModule({
      imports: [ChannelViewComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: SignalRService, useValue: fakeSignalRService },
        {
          provide: AuthService,
          useValue: { currentUser: () => ({ userId: 'u1', email: '', displayName: '' }) },
        },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: convertToParamMap({ workspaceId: 'w1' }) },
            paramMap: paramMap$,
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ChannelViewComponent);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should load message history for the active channel', () => {
    // Arrange
    fixture.detectChanges();

    // Act
    const req = httpMock.expectOne((r) => r.url === '/api/workspaces/w1/channels/c1/messages');
    req.flush({ items: [], nextCursor: null });

    // Assert
    expect(fixture.componentInstance.messages().length).toBe(0);
  });

  it('should send a message, append it and clear the form', () => {
    // Arrange
    fixture.detectChanges();
    flushHistory();
    fixture.componentInstance.sendForm.setValue({ content: 'hello' });

    // Act
    fixture.componentInstance.onSend();
    const req = httpMock.expectOne(messagesUrl);
    req.flush(message({ id: 'm1', content: 'hello' }));

    // Assert
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ content: 'hello' });
    expect(fixture.componentInstance.messages().map((m) => m.id)).toEqual(['m1']);
    expect(fixture.componentInstance.sendForm.getRawValue().content).toBe('');
  });

  it('should not send a blank message', () => {
    // Arrange
    fixture.detectChanges();
    flushHistory();
    fixture.componentInstance.sendForm.setValue({ content: '' });

    // Act
    fixture.componentInstance.onSend();

    // Assert
    httpMock.expectNone(messagesUrl);
  });

  it('should show an error and keep the typed text when sending fails', () => {
    // Arrange
    fixture.detectChanges();
    flushHistory();
    fixture.componentInstance.sendForm.setValue({ content: 'draft' });

    // Act
    fixture.componentInstance.onSend();
    httpMock.expectOne(messagesUrl).flush(null, { status: 500, statusText: 'Server Error' });

    // Assert
    expect(fixture.componentInstance.errorMessage()).toBe(
      'Could not send the message. Please try again.',
    );
    expect(fixture.componentInstance.sendForm.getRawValue().content).toBe('draft');
    expect(fixture.componentInstance.messages().length).toBe(0);
  });

  it('should append an incoming realtime message for the active channel', () => {
    // Arrange
    fixture.detectChanges();
    flushHistory();

    // Act
    fakeSignalRService.messageReceived$.next(message({ id: 'm2', content: 'live' }));

    // Assert
    expect(fixture.componentInstance.messages().map((m) => m.id)).toEqual(['m2']);
  });

  it('should replace an incoming message that already exists instead of duplicating it', () => {
    // Arrange
    fixture.detectChanges();
    flushHistory([message({ id: 'm1', content: 'before' })]);

    // Act
    fakeSignalRService.messageEdited$.next(message({ id: 'm1', content: 'after' }));

    // Assert
    expect(fixture.componentInstance.messages().length).toBe(1);
    expect(fixture.componentInstance.messages()[0].content).toBe('after');
  });

  it('should apply a realtime delete to an existing message', () => {
    // Arrange
    fixture.detectChanges();
    flushHistory([message({ id: 'm1', content: 'bye' })]);

    // Act
    fakeSignalRService.messageDeleted$.next(
      message({ id: 'm1', content: '[deleted]', isDeleted: true }),
    );

    // Assert
    expect(fixture.componentInstance.messages()[0].isDeleted).toBe(true);
  });

  it('should ignore an incoming message for a different channel', () => {
    // Arrange
    fixture.detectChanges();
    flushHistory();

    // Act
    fakeSignalRService.messageReceived$.next(message({ id: 'm9', channelId: 'other' }));

    // Assert
    expect(fixture.componentInstance.messages().length).toBe(0);
  });

  it('should display history from oldest to newest', () => {
    // Arrange
    fixture.detectChanges();

    // Act
    flushHistory([message({ id: 'm3' }), message({ id: 'm2' }), message({ id: 'm1' })]);

    // Assert
    expect(fixture.componentInstance.messages().map((m) => m.id)).toEqual(['m1', 'm2', 'm3']);
  });

  it('should show an error when loading history fails', () => {
    // Arrange
    fixture.detectChanges();

    // Act
    httpMock
      .expectOne((r) => r.url === messagesUrl)
      .flush(null, { status: 500, statusText: 'Server Error' });

    // Assert
    expect(fixture.componentInstance.errorMessage()).toBe(
      'Could not load messages for this channel.',
    );
  });

  it('should leave the old channel, clear messages, load the new history and join the new channel when switching', async () => {
    // Arrange
    fixture.detectChanges();
    flushHistory([message({ id: 'm1' })]);
    await flushPromises();

    // Act
    paramMap$.next(convertToParamMap({ workspaceId: 'w1', channelId: 'c2' }));
    await flushPromises();
    const req = httpMock.expectOne('/api/workspaces/w1/channels/c2/messages?pageSize=50');
    req.flush({ items: [message({ id: 'n1', channelId: 'c2' })], nextCursor: null });
    await flushPromises();

    // Assert
    expect(fakeSignalRService.leaveChannelAsync).toHaveBeenCalledWith('c1');
    expect(fixture.componentInstance.channelId).toBe('c2');
    expect(fixture.componentInstance.messages().map((m) => m.id)).toEqual(['n1']);
    expect(fakeSignalRService.joinChannelAsync).toHaveBeenLastCalledWith('c2');
  });

  it('should not reload when the same channel id is emitted again', async () => {
    // Arrange
    fixture.detectChanges();
    flushHistory();
    await flushPromises();
    fakeSignalRService.joinChannelAsync.mockClear();

    // Act
    paramMap$.next(convertToParamMap({ workspaceId: 'w1', channelId: 'c1' }));
    await flushPromises();

    // Assert
    httpMock.expectNone((r) => r.url === messagesUrl);
    expect(fakeSignalRService.joinChannelAsync).not.toHaveBeenCalled();
  });

  it('should pre-fill the edit form when an edit is started', () => {
    // Arrange
    fixture.detectChanges();
    flushHistory();

    // Act
    fixture.componentInstance.startEdit(message({ id: 'm1', content: 'original' }));

    // Assert
    expect(fixture.componentInstance.editingMessageId()).toBe('m1');
    expect(fixture.componentInstance.editForm.getRawValue().content).toBe('original');
  });

  it('should save an edit, update the message and leave edit mode', () => {
    // Arrange
    const original = message({ id: 'm1', content: 'original' });
    fixture.detectChanges();
    flushHistory([original]);
    fixture.componentInstance.startEdit(original);
    fixture.componentInstance.editForm.setValue({ content: 'changed' });

    // Act
    fixture.componentInstance.onEditSubmit(original);
    const req = httpMock.expectOne(`${messagesUrl}/m1`);
    req.flush(message({ id: 'm1', content: 'changed', editedAtUtc: new Date().toISOString() }));

    // Assert
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ content: 'changed' });
    expect(fixture.componentInstance.messages()[0].content).toBe('changed');
    expect(fixture.componentInstance.editingMessageId()).toBeNull();
  });

  it('should show an error and stay in edit mode when saving an edit fails', () => {
    // Arrange
    const original = message({ id: 'm1', content: 'original' });
    fixture.detectChanges();
    flushHistory([original]);
    fixture.componentInstance.startEdit(original);
    fixture.componentInstance.editForm.setValue({ content: 'changed' });

    // Act
    fixture.componentInstance.onEditSubmit(original);
    httpMock
      .expectOne(`${messagesUrl}/m1`)
      .flush(null, { status: 500, statusText: 'Server Error' });

    // Assert
    expect(fixture.componentInstance.errorMessage()).toBe(
      'Could not save the edit. Please try again.',
    );
    expect(fixture.componentInstance.editingMessageId()).toBe('m1');
  });

  it('should not submit a blank edit', () => {
    // Arrange
    const original = message({ id: 'm1', content: 'original' });
    fixture.detectChanges();
    flushHistory([original]);
    fixture.componentInstance.startEdit(original);
    fixture.componentInstance.editForm.setValue({ content: '' });

    // Act
    fixture.componentInstance.onEditSubmit(original);

    // Assert
    httpMock.expectNone(`${messagesUrl}/m1`);
  });

  it('should leave edit mode when the edit is cancelled', () => {
    // Arrange
    const original = message({ id: 'm1' });
    fixture.detectChanges();
    flushHistory([original]);
    fixture.componentInstance.startEdit(original);

    // Act
    fixture.componentInstance.cancelEdit();

    // Assert
    expect(fixture.componentInstance.editingMessageId()).toBeNull();
  });

  it('should mark a message as deleted after a successful delete', () => {
    // Arrange
    const original = message({ id: 'm1', content: 'bye' });
    fixture.detectChanges();
    flushHistory([original]);

    // Act
    fixture.componentInstance.deleteMessage(original);
    const req = httpMock.expectOne(`${messagesUrl}/m1`);
    req.flush(null);

    // Assert
    expect(req.request.method).toBe('DELETE');
    expect(fixture.componentInstance.messages()[0].isDeleted).toBe(true);
    expect(fixture.componentInstance.messages()[0].content).toBe('[deleted]');
  });

  it('should show an error and keep the message when deleting fails', () => {
    // Arrange
    const original = message({ id: 'm1', content: 'keep' });
    fixture.detectChanges();
    flushHistory([original]);

    // Act
    fixture.componentInstance.deleteMessage(original);
    httpMock
      .expectOne(`${messagesUrl}/m1`)
      .flush(null, { status: 500, statusText: 'Server Error' });

    // Assert
    expect(fixture.componentInstance.errorMessage()).toBe(
      'Could not delete the message. Please try again.',
    );
    expect(fixture.componentInstance.messages()[0].isDeleted).toBe(false);
  });

  it('should only allow the sender to modify a message', () => {
    // Arrange
    const component = fixture.componentInstance;

    // Act
    const own = component.canModify(message({ senderId: 'u1' }));
    const others = component.canModify(message({ senderId: 'u2' }));

    // Assert
    expect(own).toBe(true);
    expect(others).toBe(false);
  });

  it('should stop listening and leave the channel when destroyed', async () => {
    // Arrange
    fixture.detectChanges();
    flushHistory();
    await flushPromises();

    // Act
    fixture.destroy();
    fakeSignalRService.messageReceived$.next(message({ id: 'late' }));

    // Assert
    expect(fakeSignalRService.leaveChannelAsync).toHaveBeenCalledWith('c1');
    expect(fixture.componentInstance.messages().length).toBe(0);
  });
});
