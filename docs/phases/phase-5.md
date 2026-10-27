# Phase 5 · FlowDesk notifications

[Documentation](../README.md) · [Roadmap](../roadmap.md) · [Verification](../verification.md)

**Status:** Implemented within the app-event scope, introduced in v0.3.0.

## Delivered scope

Persist up to 100 FlowDesk event cards; show unread state; filter unread; mark read; dismiss; clear; opt into Windows banners and optional sound. Cards record in-app, quieted, submitted, or failed delivery. Strict notification links route to one app instance.

## Use the feature

Open Notifications to review cards and preferences. Windows banners are opt-in and sound defaults to silent. Use Test notification to exercise delivery. Dismissing or clearing removes corresponding FlowDesk entries from Windows notification history. Activating a valid notification link opens the center and marks its card read.

## Verification evidence

[v0.3.0 verification](../verification-v0.3.0.md) records notification state/native history/UI workflows and cold/warm single-instance protocol activation. [v0.4.0](../verification-v0.4.0.md) reran the earlier 23 desktop workflows and activation tests successfully.

## Remaining work and boundaries

This center contains FlowDesk's own events, not other apps' notifications. Windows controls banner visibility. A physical banner click was not automated; stored payload and complete protocol activation were tested separately. Global Do Not Disturb is not controlled.

## Implementation map

`NotificationCenter.cs`, `NotificationsView.cs`, `NativeNotifications.cs`, and `NotificationProtocol.cs`.

---

[← Phase 4](phase-4.md) · [All phases](../roadmap.md) · [Phase 6 →](phase-6.md)
