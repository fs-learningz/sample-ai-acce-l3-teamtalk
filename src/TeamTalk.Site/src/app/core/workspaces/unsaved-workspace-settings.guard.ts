import { CanDeactivateFn } from '@angular/router';

export interface CanComponentDeactivate {
  hasUnsavedChanges(): boolean;
}

export const unsavedWorkspaceSettingsGuard: CanDeactivateFn<CanComponentDeactivate> = (
  component,
) => {
  if (!component.hasUnsavedChanges()) {
    return true;
  }

  return window.confirm('You have unsaved changes. Leave this page and discard them?');
};
