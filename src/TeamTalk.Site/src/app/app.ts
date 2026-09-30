import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth/auth.service';
import { ThemeService } from './core/theme/theme.service';

@Component({
  imports: [RouterOutlet],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  protected readonly title = signal('TeamTalk.Site');

  constructor() {
    const themeService = inject(ThemeService);

    if (inject(AuthService).isAuthenticated()) {
      themeService.loadFromServer().subscribe({ error: () => undefined });
    }
  }
}
