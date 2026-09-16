export interface MessageResponse {
  id: string;
  channelId: string;
  senderId: string;
  senderDisplayName: string;
  content: string;
  sentAtUtc: string;
  editedAtUtc: string | null;
  isDeleted: boolean;
}

export interface ChannelMessagesResponse {
  items: MessageResponse[];
  nextCursor: string | null;
}

export interface SendMessageRequest {
  content: string;
}

export interface EditMessageRequest {
  content: string;
}
