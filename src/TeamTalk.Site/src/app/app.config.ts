import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter } from '@angular/router';
import { providePrimeNG } from 'primeng/config';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { TeamTalkPreset } from '../theme/teamtalk-preset';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAnimationsAsync(),
    providePrimeNG({
      theme: {
        preset: TeamTalkPreset,
        options: {
          darkModeSelector: '[data-theme="dark"]',
        },
      },
      license:
        'eyJpZCI6Ijk5NzYwYzhkLTMwOTItNGQ0MS1hZjM5LWQ4ZjIzOWEzNjZhMSIsInByb2R1Y3QiOiJwcmltZXVpIiwidGllciI6ImNvbW11bml0eSIsInR5cGUiOiJkZXYiLCJpYXQiOjE3ODk1NzEyMTQsImV4cCI6MTgyMTEwNzIxNH0.hnArnLrEAGlNC17dYuT_5TVMe1w2L0TOYoRfz64eBqoJl0wWzNZdbr1YnTJJQZvf6BJopgJHPePvSijhoavdAw',
    }),
  ],
};
