import { DatePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { ChannelSettingsStore } from './channel-settings.store';

@Component({
  selector: 'app-channel-settings-general',
  imports: [DatePipe],
  templateUrl: './channel-settings-general.component.html',
})
export class ChannelSettingsGeneralComponent {
  protected readonly store = inject(ChannelSettingsStore);
}
