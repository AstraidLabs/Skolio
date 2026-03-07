import React, { useEffect, useState } from 'react';
import type { SessionState } from '../shared/auth/session';
import type { createCommunicationApi } from './api';
import { Card, SectionHeader, StatusBadge } from '../shared/ui/primitives';
import { EmptyState, ErrorState, LoadingState } from '../shared/ui/states';

export function CommunicationParityPage({
  api,
  session
}: {
  api: ReturnType<typeof createCommunicationApi>;
  session: SessionState;
}) {
  const schoolId = session.schoolIds[0] ?? '00000000-0000-0000-0000-000000000000';
  const isPlatformAdmin = session.roles.includes('PlatformAdministrator');
  const canPublish = isPlatformAdmin || session.roles.includes('SchoolAdministrator') || session.roles.includes('Teacher');

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [announcements, setAnnouncements] = useState<any[]>([]);
  const [conversations, setConversations] = useState<any[]>([]);
  const [messages, setMessages] = useState<any[]>([]);
  const [notifications, setNotifications] = useState<any[]>([]);
  const [selectedConversationId, setSelectedConversationId] = useState('');
  const [newAnnouncement, setNewAnnouncement] = useState({ title: '', message: '', publishAtUtc: '' });
  const [newMessage, setNewMessage] = useState('');

  const load = () => {
    setLoading(true);
    setError('');
    void Promise.all([
      api.announcements(schoolId),
      api.conversations(schoolId),
      api.notifications(session.subject)
    ])
      .then(([announcementResult, conversationResult, notificationResult]) => {
        setAnnouncements(announcementResult);
        setConversations(conversationResult);
        setNotifications(notificationResult);
      })
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  };

  useEffect(load, [session.accessToken]);

  useEffect(() => {
    if (!selectedConversationId) {
      setMessages([]);
      return;
    }

    void api.messages(selectedConversationId)
      .then(setMessages)
      .catch((e: Error) => setError(e.message));
  }, [selectedConversationId]);

  const publishAnnouncement = () => {
    const payload = { schoolId, title: newAnnouncement.title, message: newAnnouncement.message, publishAtUtc: newAnnouncement.publishAtUtc };
    const action = isPlatformAdmin ? api.publishPlatformAnnouncement(payload) : api.publishAnnouncement(payload);
    void action.then(() => {
      setNewAnnouncement({ title: '', message: '', publishAtUtc: '' });
      load();
    }).catch((e: Error) => setError(e.message));
  };

  const sendMessage = () => {
    if (!selectedConversationId || !newMessage.trim()) return;
    void api.sendMessage({
      conversationId: selectedConversationId,
      senderUserId: session.subject,
      message: newMessage.trim(),
      sentAtUtc: new Date().toISOString(),
      id: ''
    }).then(() => {
      setNewMessage('');
      return api.messages(selectedConversationId);
    }).then(setMessages).catch((e: Error) => setError(e.message));
  };

  if (loading) return <LoadingState text="Loading communication capabilities..." />;
  if (error) return <ErrorState text={error} />;

  return (
    <section className="space-y-3">
      <SectionHeader title="Communication Parity" description="Announcements, conversations, messages and notifications backed by existing endpoints." action={<button className="sk-btn sk-btn-secondary" onClick={load} type="button">Reload</button>} />

      {canPublish ? (
        <Card>
          <p className="font-semibold text-sm">{isPlatformAdmin ? 'Publish platform announcement' : 'Publish school announcement'}</p>
          <div className="mt-2 grid gap-2 md:grid-cols-3">
            <input className="sk-input" placeholder="Title" value={newAnnouncement.title} onChange={(e) => setNewAnnouncement((v) => ({ ...v, title: e.target.value }))} />
            <input className="sk-input" placeholder="Message" value={newAnnouncement.message} onChange={(e) => setNewAnnouncement((v) => ({ ...v, message: e.target.value }))} />
            <input className="sk-input" type="datetime-local" value={newAnnouncement.publishAtUtc} onChange={(e) => setNewAnnouncement((v) => ({ ...v, publishAtUtc: e.target.value }))} />
          </div>
          <div className="mt-2"><button className="sk-btn sk-btn-primary" onClick={publishAnnouncement} type="button">Publish</button></div>
        </Card>
      ) : null}

      <div className="grid gap-3 lg:grid-cols-2">
        <Card>
          <p className="font-semibold text-sm">Announcements</p>
          {announcements.length === 0 ? <EmptyState text="No announcements in current scope." /> : (
            <ul className="sk-list">
              {announcements.map((item) => (
                <li key={item.id} className="sk-list-item">
                  <span>{item.title}</span>
                  <div className="flex gap-2">
                    <StatusBadge label={item.isActive ? 'Active' : 'Inactive'} tone={item.isActive ? 'good' : 'warn'} />
                    {canPublish ? (
                      <button className="sk-btn sk-btn-secondary" type="button" onClick={() => void api.setAnnouncementActivation(item.id, !item.isActive).then(load).catch((e: Error) => setError(e.message))}>
                        {item.isActive ? 'Deactivate' : 'Activate'}
                      </button>
                    ) : null}
                  </div>
                </li>
              ))}
            </ul>
          )}
        </Card>

        <Card>
          <p className="font-semibold text-sm">Notifications panel</p>
          {notifications.length === 0 ? <EmptyState text="No notifications in scope." /> : (
            <ul className="sk-list">
              {notifications.map((notification) => (
                <li key={notification.id} className="sk-list-item">
                  <span>{notification.title}</span>
                  <StatusBadge label={notification.channel} tone="info" />
                </li>
              ))}
            </ul>
          )}
        </Card>
      </div>

      <div className="grid gap-3 lg:grid-cols-2">
        <Card>
          <p className="font-semibold text-sm">Conversations</p>
          {conversations.length === 0 ? <EmptyState text="No conversations in current scope." /> : (
            <ul className="sk-list">
              {conversations.map((conversation) => (
                <li key={conversation.id} className="sk-list-item">
                  <span>{conversation.topic}</span>
                  <button className="sk-btn sk-btn-secondary" type="button" onClick={() => setSelectedConversationId(conversation.id)}>
                    Open
                  </button>
                </li>
              ))}
            </ul>
          )}
        </Card>

        <Card>
          <p className="font-semibold text-sm">Messages</p>
          {!selectedConversationId ? (
            <EmptyState text="Select conversation to load messages." />
          ) : (
            <>
              {messages.length === 0 ? <EmptyState text="No messages in selected conversation." /> : (
                <ul className="sk-list">
                  {messages.map((message) => (
                    <li key={message.id} className="sk-list-item">
                      <span>{message.senderUserId}: {message.message}</span>
                      <span className="text-xs">{message.sentAtUtc}</span>
                    </li>
                  ))}
                </ul>
              )}
              <div className="mt-2 flex gap-2">
                <input className="sk-input" placeholder="Message" value={newMessage} onChange={(e) => setNewMessage(e.target.value)} />
                <button className="sk-btn sk-btn-primary" type="button" onClick={sendMessage}>Send</button>
              </div>
            </>
          )}
        </Card>
      </div>
    </section>
  );
}
