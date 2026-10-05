import { TestBed } from '@angular/core/testing';
import { AuthService } from '../auth/auth.service';
import { MessageResponse } from '../models/message.model';
import { SignalRService } from './signalr.service';

const hoisted = vi.hoisted(() => {
  const handlers = new Map<string, (message: unknown) => void>();
  const reconnectedHandlers: Array<() => void> = [];
  const connection = {
    on: vi.fn((event: string, handler: (message: unknown) => void) => {
      handlers.set(event, handler);
    }),
    start: vi.fn(() => Promise.resolve()),
    invoke: vi.fn(() => Promise.resolve()),
    onreconnected: vi.fn((handler: () => void) => {
      reconnectedHandlers.push(handler);
    }),
  };
  const builder = {
    withUrl: vi.fn(),
    withAutomaticReconnect: vi.fn(),
    build: vi.fn(() => connection),
  };
  builder.withUrl.mockReturnValue(builder);
  builder.withAutomaticReconnect.mockReturnValue(builder);

  return { handlers, reconnectedHandlers, connection, builder };
});

vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: vi.fn(function () {
    return hoisted.builder;
  }),
}));

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

describe('SignalRService', () => {
  let service: SignalRService;
  let token: string | null;

  beforeEach(() => {
    token = 'jwt-token';
    hoisted.handlers.clear();
    hoisted.reconnectedHandlers.length = 0;
    hoisted.connection.on.mockClear();
    hoisted.connection.start.mockClear();
    hoisted.connection.invoke.mockClear();
    hoisted.builder.withUrl.mockClear();
    hoisted.builder.withAutomaticReconnect.mockClear();
    hoisted.builder.build.mockClear();

    TestBed.configureTestingModule({
      providers: [{ provide: AuthService, useValue: { getToken: () => token } }],
    });
    service = TestBed.inject(SignalRService);
  });

  it('connects to the channel hub with automatic reconnect and starts the connection', async () => {
    await service.connectAsync();

    expect(hoisted.builder.withUrl).toHaveBeenCalledWith('/hubs/channel', expect.any(Object));
    expect(hoisted.builder.withAutomaticReconnect).toHaveBeenCalled();
    expect(hoisted.connection.start).toHaveBeenCalledTimes(1);
  });

  it('supplies the current auth token through the access token factory', async () => {
    await service.connectAsync();

    const options = hoisted.builder.withUrl.mock.calls[0][1] as { accessTokenFactory: () => string };
    expect(options.accessTokenFactory()).toBe('jwt-token');
  });

  it('supplies an empty string when there is no auth token', async () => {
    token = null;
    await service.connectAsync();

    const options = hoisted.builder.withUrl.mock.calls[0][1] as { accessTokenFactory: () => string };
    expect(options.accessTokenFactory()).toBe('');
  });

  it('does not create a second connection when already connected', async () => {
    await service.connectAsync();
    await service.connectAsync();

    expect(hoisted.builder.build).toHaveBeenCalledTimes(1);
    expect(hoisted.connection.start).toHaveBeenCalledTimes(1);
  });

  it('registers handlers for the message and channel activity events', async () => {
    await service.connectAsync();

    expect([...hoisted.handlers.keys()].sort()).toEqual(
      ['ChannelActivity', 'MessageDeleted', 'MessageEdited', 'ReceiveMessage'].sort(),
    );
  });

  it('emits messageReceived$ when a ReceiveMessage event arrives', async () => {
    const received: MessageResponse[] = [];
    service.messageReceived$.subscribe((m) => received.push(m));
    await service.connectAsync();

    hoisted.handlers.get('ReceiveMessage')!(message);

    expect(received).toEqual([message]);
  });

  it('emits messageEdited$ when a MessageEdited event arrives', async () => {
    const edited: MessageResponse[] = [];
    service.messageEdited$.subscribe((m) => edited.push(m));
    await service.connectAsync();

    hoisted.handlers.get('MessageEdited')!({ ...message, content: 'updated' });

    expect(edited).toHaveLength(1);
    expect(edited[0].content).toBe('updated');
  });

  it('emits messageDeleted$ when a MessageDeleted event arrives', async () => {
    const deleted: MessageResponse[] = [];
    service.messageDeleted$.subscribe((m) => deleted.push(m));
    await service.connectAsync();

    hoisted.handlers.get('MessageDeleted')!({ ...message, isDeleted: true });

    expect(deleted).toHaveLength(1);
    expect(deleted[0].isDeleted).toBe(true);
  });

  it('does not emit on other streams for a single event', async () => {
    const edited: MessageResponse[] = [];
    const deleted: MessageResponse[] = [];
    service.messageEdited$.subscribe((m) => edited.push(m));
    service.messageDeleted$.subscribe((m) => deleted.push(m));
    await service.connectAsync();

    hoisted.handlers.get('ReceiveMessage')!(message);

    expect(edited).toEqual([]);
    expect(deleted).toEqual([]);
  });

  it('invokes JoinChannelAsync on the hub with the channel id', async () => {
    await service.connectAsync();

    await service.joinChannelAsync('c1');

    expect(hoisted.connection.invoke).toHaveBeenCalledWith('JoinChannelAsync', 'c1');
  });

  it('invokes LeaveChannelAsync on the hub with the channel id', async () => {
    await service.connectAsync();

    await service.leaveChannelAsync('c1');

    expect(hoisted.connection.invoke).toHaveBeenCalledWith('LeaveChannelAsync', 'c1');
  });

  it('does nothing when joining a channel before connecting', async () => {
    await service.joinChannelAsync('c1');

    expect(hoisted.connection.invoke).not.toHaveBeenCalled();
  });

  it('does nothing when leaving a channel before connecting', async () => {
    await service.leaveChannelAsync('c1');

    expect(hoisted.connection.invoke).not.toHaveBeenCalled();
  });

  it('propagates a hub error when joining a channel is rejected', async () => {
    await service.connectAsync();
    hoisted.connection.invoke.mockRejectedValueOnce(new Error('You do not have access to this channel.'));

    await expect(service.joinChannelAsync('c1')).rejects.toThrow('You do not have access');
  });

  it('emits channelActivity$ when a ChannelActivity event arrives', async () => {
    const received: unknown[] = [];
    service.channelActivity$.subscribe((a) => received.push(a));
    await service.connectAsync();

    hoisted.handlers.get('ChannelActivity')!({ channelId: 'c1', senderId: 'u2' });

    expect(received).toEqual([{ channelId: 'c1', senderId: 'u2' }]);
  });

  it('invokes JoinWorkspaceAsync on the hub with the workspace id', async () => {
    await service.connectAsync();

    await service.joinWorkspaceAsync('w1');

    expect(hoisted.connection.invoke).toHaveBeenCalledWith('JoinWorkspaceAsync', 'w1');
  });

  it('rejoins the workspace after the connection reconnects', async () => {
    await service.connectAsync();
    await service.joinWorkspaceAsync('w1');
    hoisted.connection.invoke.mockClear();

    hoisted.reconnectedHandlers.forEach((handler) => handler());

    expect(hoisted.connection.invoke).toHaveBeenCalledWith('JoinWorkspaceAsync', 'w1');
  });

  it('does not rejoin a workspace that was left before reconnecting', async () => {
    await service.connectAsync();
    await service.joinWorkspaceAsync('w1');
    await service.leaveWorkspaceAsync('w1');
    hoisted.connection.invoke.mockClear();

    hoisted.reconnectedHandlers.forEach((handler) => handler());

    expect(hoisted.connection.invoke).not.toHaveBeenCalled();
  });
});
