export type ChannelType = 'Public' | 'Private';

export interface ChannelResponse {
  id: string;
  workspaceId: string;
  name: string;
  type: ChannelType;
  createdByUserId: string;
  createdAtUtc: string;
}

export interface CreateChannelRequest {
  name: string;
  type: ChannelType;
}
