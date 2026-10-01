import { Route } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { unsavedWorkspaceSettingsGuard } from './core/workspaces/unsaved-workspace-settings.guard';
import { routes } from './app.routes';

describe('app routes', () => {
  function find(path: string): Route {
    return routes.find((r) => r.path === path)!;
  }

  it('should protect every workspace, invitation and settings route with the auth guard', () => {
    // Arrange
    const protectedPaths = [
      'workspaces',
      'invitations',
      'workspaces/:workspaceId',
      'workspaces/:workspaceId/settings',
    ];

    // Act
    const guards = protectedPaths.map((path) => find(path).canActivate);

    // Assert
    for (const canActivate of guards) {
      expect(canActivate).toEqual([authGuard]);
    }
  });

  it('should leave the login and register routes public', () => {
    // Arrange / Act
    const login = find('login');
    const register = find('register');

    // Assert
    expect(login.canActivate).toBeUndefined();
    expect(register.canActivate).toBeUndefined();
  });

  it('should redirect the empty and unknown paths to the workspaces', () => {
    // Arrange / Act
    const empty = find('');
    const wildcard = find('**');

    // Assert
    expect(empty.redirectTo).toBe('workspaces');
    expect(empty.pathMatch).toBe('full');
    expect(wildcard.redirectTo).toBe('workspaces');
  });

  it('should redirect the settings root to the general tab', () => {
    // Arrange
    const settings = find('workspaces/:workspaceId/settings');

    // Act
    const root = settings.children!.find((r) => r.path === '')!;

    // Assert
    expect(root.redirectTo).toBe('general');
    expect(root.pathMatch).toBe('full');
  });

  it('should guard leaving the general settings tab when there are unsaved changes', () => {
    // Arrange
    const settings = find('workspaces/:workspaceId/settings');

    // Act
    const general = settings.children!.find((r) => r.path === 'general')!;

    // Assert
    expect(general.canDeactivate).toEqual([unsavedWorkspaceSettingsGuard]);
  });

  it('should expose the channel route as a child of the workspace shell', () => {
    // Arrange
    const shell = find('workspaces/:workspaceId');

    // Act
    const channel = shell.children!.find((r) => r.path === 'channels/:channelId');

    // Assert
    expect(channel).toBeDefined();
  });
});
