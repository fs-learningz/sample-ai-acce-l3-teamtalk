import { Component, inject, signal } from '@angular/core';
import { ThemeMode, ThemeOption } from '../../../core/theme/theme.model';
import { ThemeService } from '../../../core/theme/theme.service';

@Component({
  selector: 'app-workspace-settings-appearance',
  templateUrl: './workspace-settings-appearance.component.html',
})
export class WorkspaceSettingsAppearanceComponent {
  private readonly themeService = inject(ThemeService);

  readonly currentTheme = this.themeService.theme;
  readonly errorMessage = signal<string | null>(null);

  readonly groups: { mode: ThemeMode; title: string; themes: ThemeOption[] }[] = [
    { mode: 'light', title: 'Light themes', themes: this.filter('light') },
    { mode: 'dark', title: 'Dark themes', themes: this.filter('dark') },
  ];

  select(theme: ThemeOption): void {
    if (theme.id === this.currentTheme()) {
      return;
    }

    const previous = this.currentTheme();
    this.errorMessage.set(null);

    this.themeService.select(theme.id).subscribe({
      error: () => {
        this.themeService.apply(previous);
        this.errorMessage.set('Could not save your theme. Please try again.');
      },
    });
  }

  private filter(mode: ThemeMode): ThemeOption[] {
    return this.themeService.themes.filter((theme) => theme.mode === mode);
  }
}
