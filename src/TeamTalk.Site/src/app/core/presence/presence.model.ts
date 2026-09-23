export type PresenceStatus = 'online' | 'away' | 'offline';

export const PRESENCE_LABELS: Record<PresenceStatus, string> = {
  online: 'Online',
  away: 'Away',
  offline: 'Offline',
};
