export type ChannelType = 'Public' | 'Private';

export interface ChannelResponse {
  id: string;
  workspaceId: string;
  name: string;
  type: ChannelType;
  createdByUserId: string;
  createdAtUtc: string;
  isPinned: boolean;
  isMuted: boolean;
  hasUnread: boolean;
}

export interface ChannelDetailsResponse {
  id: string;
  workspaceId: string;
  name: string;
  type: ChannelType;
  createdByUserId: string;
  createdByDisplayName: string;
  createdAtUtc: string;
  memberCount: number;
  isMember: boolean;
  isPinned: boolean;
  isMuted: boolean;
}

export interface UpdateChannelPreferencesRequest {
  isPinned: boolean;
  isMuted: boolean;
}

export interface CreateChannelRequest {
  name: string;
  type: ChannelType;
}
