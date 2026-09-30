import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { unsavedWorkspaceSettingsGuard } from './core/workspaces/unsaved-workspace-settings.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./features/auth/register/register.component').then((m) => m.RegisterComponent),
  },
  {
    path: 'workspaces',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/workspaces/workspace-list/workspace-list.component').then(
        (m) => m.WorkspaceListComponent,
      ),
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
          import('./features/channels/channel-view/channel-view.component').then(
            (m) => m.ChannelViewComponent,
          ),
      },
    ],
  },
  {
    path: 'workspaces/:workspaceId/settings',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/workspaces/workspace-settings/workspace-settings.component').then(
        (m) => m.WorkspaceSettingsComponent,
      ),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'general' },
      {
        path: 'general',
        loadComponent: () =>
          import('./features/workspaces/workspace-settings/workspace-settings-general.component').then(
            (m) => m.WorkspaceSettingsGeneralComponent,
          ),
        canDeactivate: [unsavedWorkspaceSettingsGuard],
      },
      {
        path: 'appearance',
        loadComponent: () =>
          import('./features/workspaces/workspace-settings/workspace-settings-appearance.component').then(
            (m) => m.WorkspaceSettingsAppearanceComponent,
          ),
      },
      {
        path: 'members',
        loadComponent: () =>
          import('./features/workspaces/workspace-settings/workspace-settings-members.component').then(
            (m) => m.WorkspaceSettingsMembersComponent,
          ),
      },
      {
        path: 'danger',
        loadComponent: () =>
          import('./features/workspaces/workspace-settings/workspace-settings-danger.component').then(
            (m) => m.WorkspaceSettingsDangerComponent,
          ),
      },
    ],
  },
  { path: '', pathMatch: 'full', redirectTo: 'workspaces' },
  { path: '**', redirectTo: 'workspaces' },
];
