import type { createHttpClient } from '../shared/http/httpClient';

export type Announcement = { id: string; schoolId: string; title: string; message: string; publishAtUtc: string };
export type Conversation = { id: string; schoolId: string; topic: string; participantUserIds: string[] };
export type Message = { id: string; conversationId: string; senderUserId: string; message: string; sentAtUtc: string };
export type Notification = { id: string; recipientUserId: string; title: string; body: string; channel: string };

export function createCommunicationApi(http: ReturnType<typeof createHttpClient>) {
  return {
    announcements: (schoolId: string) => http<Announcement[]>('communication', `/api/communication/announcements?schoolId=${schoolId}`),
    announcement: (id: string) => http<Announcement>('communication', `/api/communication/announcements/${id}`),
    publishAnnouncement: (payload: Omit<Announcement, 'id'>) => http<Announcement>('communication', '/api/communication/announcements', { method: 'POST', body: JSON.stringify(payload) }),
    conversations: (schoolId: string) => http<Conversation[]>('communication', `/api/communication/conversations?schoolId=${schoolId}`),
    conversation: (id: string) => http<Conversation>('communication', `/api/communication/conversations/${id}`),
    messages: (conversationId: string) => http<Message[]>('communication', `/api/communication/conversations/${conversationId}/messages`),
    sendMessage: (payload: Omit<Message, 'id' | 'sentAtUtc'>) => http<Message>('communication', '/api/communication/conversations/messages', { method: 'POST', body: JSON.stringify(payload) }),
    notifications: (recipientUserId: string) => http<Notification[]>('communication', `/api/communication/notifications?recipientUserId=${recipientUserId}`)
  };
}
