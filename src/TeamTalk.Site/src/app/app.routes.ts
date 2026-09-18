import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register/register.component').then((m) => m.RegisterComponent),
  },
  {
    path: 'workspaces',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/workspaces/workspace-list/workspace-list.component').then((m) => m.WorkspaceListComponent),
  },
  {
    path: 'invitations',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/invitations/invitation-list/invitation-list.component').then(
        (m) => m.InvitationListComponent,
      ),
  },
  {
    path: 'workspaces/:workspaceId',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/workspaces/workspace-shell/workspace-shell.component').then(
        (m) => m.WorkspaceShellComponent,
      ),
    children: [
      {
        path: 'channels/:channelId',
        loadComponent: () =>
          import('./features/channels/channel-view/channel-view.component').then((m) => m.ChannelViewComponent),
      },
    ],
  },
  { path: '', pathMatch: 'full', redirectTo: 'workspaces' },
  { path: '**', redirectTo: 'workspaces' },
];
