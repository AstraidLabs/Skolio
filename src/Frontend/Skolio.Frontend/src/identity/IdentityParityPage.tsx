import React, { useEffect, useState } from 'react';
import type { createIdentityApi } from './api';
import type { SessionState } from '../shared/auth/session';
import { Card, SectionHeader, StatusBadge, WidgetGrid } from '../shared/ui/primitives';
import { EmptyState, ErrorState, LoadingState } from '../shared/ui/states';

export function IdentityParityPage({
  api,
  session
}: {
  api: ReturnType<typeof createIdentityApi>;
  session: SessionState;
}) {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [profile, setProfile] = useState<any>(null);
  const [users, setUsers] = useState<any[]>([]);
  const [roleAssignments, setRoleAssignments] = useState<any[]>([]);
  const [links, setLinks] = useState<any[]>([]);
  const [linkedStudents, setLinkedStudents] = useState<any[]>([]);
  const [profileDraft, setProfileDraft] = useState({ firstName: '', lastName: '', userType: 'Teacher', email: '' });
  const [newRole, setNewRole] = useState({ userProfileId: '', schoolId: '', roleCode: 'Teacher' });
  const [newLink, setNewLink] = useState({ parentUserProfileId: '', studentUserProfileId: '', relationship: 'Parent' });

  const isPlatformAdmin = session.roles.includes('PlatformAdministrator');
  const isSchoolAdmin = session.roles.includes('SchoolAdministrator');
  const isParent = session.roles.includes('Parent');
  const isStudent = session.roles.includes('Student');
  const canAdminIdentity = isPlatformAdmin || isSchoolAdmin;

  const load = () => {
    setLoading(true);
    setError('');

    if (canAdminIdentity) {
      void Promise.all([api.myProfile(), api.userProfiles(), api.roleAssignments(), api.parentStudentLinks()])
        .then(([myProfile, allUsers, roles, parentLinks]) => {
          setProfile(myProfile);
          setUsers(allUsers);
          setRoleAssignments(roles);
          setLinks(parentLinks);
          setProfileDraft({
            firstName: myProfile.firstName ?? '',
            lastName: myProfile.lastName ?? '',
            userType: myProfile.userType ?? 'Teacher',
            email: myProfile.email ?? ''
          });
        })
        .catch((e: Error) => setError(e.message))
        .finally(() => setLoading(false));
      return;
    }

    if (isParent) {
      void Promise.all([api.myProfile(), api.myParentStudentLinks(), api.linkedStudents()])
        .then(([myProfile, parentLinks, students]) => {
          setProfile(myProfile);
          setLinks(parentLinks);
          setLinkedStudents(students);
          setProfileDraft({
            firstName: myProfile.firstName ?? '',
            lastName: myProfile.lastName ?? '',
            userType: myProfile.userType ?? 'Parent',
            email: myProfile.email ?? ''
          });
        })
        .catch((e: Error) => setError(e.message))
        .finally(() => setLoading(false));
      return;
    }

    if (isStudent) {
      void api.studentContext()
        .then((studentContext) => {
          setProfile(studentContext.profile);
          setRoleAssignments(studentContext.roleAssignments);
          setProfileDraft({
            firstName: studentContext.profile.firstName ?? '',
            lastName: studentContext.profile.lastName ?? '',
            userType: studentContext.profile.userType ?? 'Student',
            email: studentContext.profile.email ?? ''
          });
        })
        .catch((e: Error) => setError(e.message))
        .finally(() => setLoading(false));
      return;
    }

    void Promise.all([api.myProfile(), api.myRoleAssignments(session.schoolIds[0])])
      .then(([myProfile, myRoles]) => {
        setProfile(myProfile);
        setRoleAssignments(myRoles);
        setProfileDraft({
          firstName: myProfile.firstName ?? '',
          lastName: myProfile.lastName ?? '',
          userType: myProfile.userType ?? 'Teacher',
          email: myProfile.email ?? ''
        });
      })
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  };

  useEffect(load, [api, session.accessToken]);

  const saveProfile = () => {
    setError('');
    void api.updateMyProfile(profileDraft).then(load).catch((e: Error) => setError(e.message));
  };

  const assignRole = () => {
    setError('');
    void api.assignRole(newRole).then(() => {
      setNewRole({ userProfileId: '', schoolId: '', roleCode: 'Teacher' });
      load();
    }).catch((e: Error) => setError(e.message));
  };

  const createLink = () => {
    setError('');
    void api.createParentStudentLink(newLink).then(() => {
      setNewLink({ parentUserProfileId: '', studentUserProfileId: '', relationship: 'Parent' });
      load();
    }).catch((e: Error) => setError(e.message));
  };

  if (loading) return <LoadingState text="Loading identity capabilities..." />;
  if (error) return <ErrorState text={error} />;

  return (
    <section className="space-y-3">
      <SectionHeader title="Identity Parity" description="Frontend parity for existing identity backend capabilities." />

      {profile ? (
        <Card>
          <p className="font-semibold text-sm">My profile</p>
          <div className="mt-2 grid gap-2 md:grid-cols-2">
            <input className="sk-input" placeholder="First name" value={profileDraft.firstName} onChange={(e) => setProfileDraft((v) => ({ ...v, firstName: e.target.value }))} />
            <input className="sk-input" placeholder="Last name" value={profileDraft.lastName} onChange={(e) => setProfileDraft((v) => ({ ...v, lastName: e.target.value }))} />
            <input className="sk-input" placeholder="Email" value={profileDraft.email} onChange={(e) => setProfileDraft((v) => ({ ...v, email: e.target.value }))} />
            <input className="sk-input" placeholder="User type" value={profileDraft.userType} onChange={(e) => setProfileDraft((v) => ({ ...v, userType: e.target.value }))} />
          </div>
          <div className="mt-2">
            <button className="sk-btn sk-btn-primary" onClick={saveProfile} type="button">Save my profile</button>
          </div>
        </Card>
      ) : (
        <EmptyState text="Identity profile is not available in current role scope." />
      )}

      {canAdminIdentity ? (
        <>
          <WidgetGrid>
            <Card><p className="sk-metric-label">User profiles</p><p className="sk-metric-value">{users.length}</p></Card>
            <Card><p className="sk-metric-label">Role assignments</p><p className="sk-metric-value">{roleAssignments.length}</p></Card>
            <Card><p className="sk-metric-label">Parent-student links</p><p className="sk-metric-value">{links.length}</p></Card>
          </WidgetGrid>

          <Card>
            <p className="font-semibold text-sm">Assign role</p>
            <div className="mt-2 grid gap-2 md:grid-cols-3">
              <input className="sk-input" placeholder="User profile id" value={newRole.userProfileId} onChange={(e) => setNewRole((v) => ({ ...v, userProfileId: e.target.value }))} />
              <input className="sk-input" placeholder="School id" value={newRole.schoolId} onChange={(e) => setNewRole((v) => ({ ...v, schoolId: e.target.value }))} />
              <input className="sk-input" placeholder="Role code" value={newRole.roleCode} onChange={(e) => setNewRole((v) => ({ ...v, roleCode: e.target.value }))} />
            </div>
            <div className="mt-2"><button className="sk-btn sk-btn-primary" onClick={assignRole} type="button">Create role assignment</button></div>
          </Card>

          <Card>
            <p className="font-semibold text-sm">Create parent-student link</p>
            <div className="mt-2 grid gap-2 md:grid-cols-3">
              <input className="sk-input" placeholder="Parent user profile id" value={newLink.parentUserProfileId} onChange={(e) => setNewLink((v) => ({ ...v, parentUserProfileId: e.target.value }))} />
              <input className="sk-input" placeholder="Student user profile id" value={newLink.studentUserProfileId} onChange={(e) => setNewLink((v) => ({ ...v, studentUserProfileId: e.target.value }))} />
              <input className="sk-input" placeholder="Relationship" value={newLink.relationship} onChange={(e) => setNewLink((v) => ({ ...v, relationship: e.target.value }))} />
            </div>
            <div className="mt-2"><button className="sk-btn sk-btn-primary" onClick={createLink} type="button">Create link</button></div>
          </Card>
        </>
      ) : null}

      {roleAssignments.length > 0 ? (
        <Card>
          <p className="font-semibold text-sm">Role assignments</p>
          <ul className="sk-list">
            {roleAssignments.map((assignment) => (
              <li key={assignment.id} className="sk-list-item">
                <span>{assignment.roleCode} | {assignment.schoolId}</span>
                {canAdminIdentity ? (
                  <button className="sk-btn sk-btn-secondary" onClick={() => void api.deleteRoleAssignment(assignment.id).then(load).catch((e: Error) => setError(e.message))} type="button">
                    Remove
                  </button>
                ) : <StatusBadge label="Read" tone="info" />}
              </li>
            ))}
          </ul>
        </Card>
      ) : <EmptyState text="No role assignments in current scope." />}

      {links.length > 0 ? (
        <Card>
          <p className="font-semibold text-sm">Parent-student links</p>
          <ul className="sk-list">
            {links.map((link) => (
              <li key={link.id} className="sk-list-item">
                <span>{link.parentUserProfileId} {'->'} {link.studentUserProfileId} ({link.relationship})</span>
                {canAdminIdentity ? (
                  <div className="flex gap-2">
                    <button
                      className="sk-btn sk-btn-secondary"
                      onClick={() => void api.overrideParentStudentLink(link.id, { relationship: link.relationship, overrideReason: 'Identity parity correction' }).then(load).catch((e: Error) => setError(e.message))}
                      type="button"
                    >
                      Override
                    </button>
                    <button className="sk-btn sk-btn-secondary" onClick={() => void api.deleteParentStudentLink(link.id).then(load).catch((e: Error) => setError(e.message))} type="button">Delete</button>
                  </div>
                ) : <StatusBadge label="Read" tone="info" />}
              </li>
            ))}
          </ul>
        </Card>
      ) : null}

      {isParent ? (
        <Card>
          <p className="font-semibold text-sm">Linked students (parent scope)</p>
          {linkedStudents.length === 0 ? <EmptyState text="No linked students available." /> : (
            <ul className="sk-list">
              {linkedStudents.map((student) => (
                <li key={student.id} className="sk-list-item">{student.firstName} {student.lastName} ({student.email})</li>
              ))}
            </ul>
          )}
        </Card>
      ) : null}

      {canAdminIdentity && users.length > 0 ? (
        <Card>
          <p className="font-semibold text-sm">User profiles list</p>
          <ul className="sk-list">
            {users.slice(0, 20).map((user) => (
              <li key={user.id} className="sk-list-item">
                <span>{user.firstName} {user.lastName} ({user.userType})</span>
                <StatusBadge label={user.isActive ? 'Active' : 'Inactive'} tone={user.isActive ? 'good' : 'warn'} />
              </li>
            ))}
          </ul>
        </Card>
      ) : null}
    </section>
  );
}
