import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { CanComponentDeactivate, unsavedWorkspaceSettingsGuard } from './unsaved-workspace-settings.guard';

describe('unsavedWorkspaceSettingsGuard', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  function runGuard(hasUnsavedChanges: boolean) {
    const component: CanComponentDeactivate = { hasUnsavedChanges: () => hasUnsavedChanges };

    return unsavedWorkspaceSettingsGuard(
      component,
      {} as ActivatedRouteSnapshot,
      {} as RouterStateSnapshot,
      {} as RouterStateSnapshot,
    );
  }

  it('allows leaving without prompting when there are no unsaved changes', () => {
    const confirmSpy = vi.spyOn(window, 'confirm');

    expect(runGuard(false)).toBe(true);
    expect(confirmSpy).not.toHaveBeenCalled();
  });

  it('allows leaving when the user confirms discarding unsaved changes', () => {
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);

    expect(runGuard(true)).toBe(true);
    expect(confirmSpy).toHaveBeenCalledTimes(1);
  });

  it('blocks leaving when the user cancels the discard prompt', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false);

    expect(runGuard(true)).toBe(false);
  });
});
