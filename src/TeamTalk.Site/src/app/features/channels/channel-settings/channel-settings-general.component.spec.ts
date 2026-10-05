import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ChannelSettingsGeneralComponent } from './channel-settings-general.component';
import { ChannelSettingsStore } from './channel-settings.store';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';

describe('ChannelSettingsGeneralComponent', () => {
  let fixture: ComponentFixture<ChannelSettingsGeneralComponent>;
  let store: ChannelSettingsStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChannelSettingsGeneralComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), ChannelSettingsStore],
    }).compileComponents();

    store = TestBed.inject(ChannelSettingsStore);
    fixture = TestBed.createComponent(ChannelSettingsGeneralComponent);
  });

  it('should show who created the channel and when', () => {
    store.channel.set({
      id: 'c1',
      workspaceId: 'w1',
      name: 'general',
      type: 'Public',
      createdByUserId: 'u1',
      createdByDisplayName: 'Ada Lovelace',
      createdAtUtc: '2026-03-15T10:00:00Z',
      memberCount: 4,
      isMember: true,
      isPinned: false,
      isMuted: false,
    });

    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('Ada Lovelace');
    expect(text).toContain('March 15, 2026');
    expect(text).toContain('general');
  });

  it('should render nothing until the channel is loaded', () => {
    fixture.detectChanges();

    expect((fixture.nativeElement.textContent as string).trim()).toBe('');
  });
});
