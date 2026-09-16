import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, Subject } from 'rxjs';
import { SignalRService } from '../../../core/realtime/signalr.service';
import { MessageResponse } from '../../../core/models/message.model';
import { ChannelViewComponent } from './channel-view.component';

describe('ChannelViewComponent', () => {
  let fixture: ComponentFixture<ChannelViewComponent>;
  let httpMock: HttpTestingController;

  const fakeSignalRService = {
    connectAsync: async () => undefined,
    joinChannelAsync: async () => undefined,
    leaveChannelAsync: async () => undefined,
    messageReceived$: new Subject<MessageResponse>(),
    messageEdited$: new Subject<MessageResponse>(),
    messageDeleted$: new Subject<MessageResponse>(),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChannelViewComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: SignalRService, useValue: fakeSignalRService },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: convertToParamMap({ workspaceId: 'w1' }) },
            paramMap: of(convertToParamMap({ workspaceId: 'w1', channelId: 'c1' })),
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
    fixture.detectChanges();

    const req = httpMock.expectOne((r) => r.url === '/api/workspaces/w1/channels/c1/messages');
    req.flush({ items: [], nextCursor: null });

    expect(fixture.componentInstance.messages().length).toBe(0);
  });
});
