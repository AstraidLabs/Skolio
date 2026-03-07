import type { createHttpClient } from '../shared/http/httpClient';

export type Announcement = { id: string; schoolId: string; title: string; message: string; publishAtUtc: string; isActive: boolean };
export type Conversation = { id: string; schoolId: string; topic: string; participantUserIds: string[] };
export type Message = { id: string; conversationId: string; senderUserId: string; message: string; sentAtUtc: string };
export type Notification = { id: string; recipientUserId: string; title: string; body: string; channel: string };

export function createCommunicationApi(http: ReturnType<typeof createHttpClient>) {
  return {
    announcements: (schoolId: string, isActive?: boolean) => http<Announcement[]>('communication', `/api/communication/announcements?schoolId=${schoolId}${typeof isActive === 'boolean' ? `&isActive=${isActive}` : ''}`),
    announcement: (id: string) => http<Announcement>('communication', `/api/communication/announcements/${id}`),
    publishAnnouncement: (payload: { schoolId: string; title: string; message: string; publishAtUtc: string }) => http<Announcement>('communication', '/api/communication/announcements', { method: 'POST', body: JSON.stringify(payload) }),
    publishPlatformAnnouncement: (payload: { schoolId: string; title: string; message: string; publishAtUtc: string }) => http<Announcement>('communication', '/api/communication/announcements/platform', { method: 'POST', body: JSON.stringify(payload) }),
    overrideAnnouncement: (id: string, payload: { title: string; message: string; publishAtUtc: string; overrideReason: string }) => http<Announcement>('communication', `/api/communication/announcements/${id}/override`, { method: 'PUT', body: JSON.stringify(payload) }),
    setAnnouncementActivation: (id: string, isActive: boolean) => http<Announcement>('communication', `/api/communication/announcements/${id}/deactivation`, { method: 'PUT', body: JSON.stringify({ isActive }) }),
    conversations: (schoolId: string) => http<Conversation[]>('communication', `/api/communication/conversations?schoolId=${schoolId}`),
    conversation: (id: string) => http<Conversation>('communication', `/api/communication/conversations/${id}`),
    messages: (conversationId: string) => http<Message[]>('communication', `/api/communication/conversations/${conversationId}/messages`),
    sendMessage: (payload: Omit<Message, 'id' | 'sentAtUtc'>) => http<Message>('communication', '/api/communication/conversations/messages', { method: 'POST', body: JSON.stringify(payload) }),
    notifications: (recipientUserId: string) => http<Notification[]>('communication', `/api/communication/notifications?recipientUserId=${recipientUserId}`)
  };
}