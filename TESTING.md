# Manual acceptance checklist

Use a disposable database or backup. Prepare a Manager, an Agent with a team,
two customer companies, portal users for each, and accounts with restricted
permissions/inactive status. Record date, tester, actual result, and screenshot
for each row. These manual checks are not automatically marked complete by the
separate automated HTTP checks in SUBMISSION_REVIEW.md.

| Done | Workflow | Steps and expected result |
| --- | --- | --- |
| [ ] | Manager login | Dashboard and customer/team/user management are available. |
| [ ] | Agent login | Queues work; Manager-only actions/customer-detail links are hidden; customer/team lists are read-only. |
| [ ] | Customer login | Portal displays only the linked company's tickets. |
| [ ] | Role restrictions | Customer direct URLs to Tickets/MyTickets/Dashboard/Customers/Teams/Users are denied. Agent direct management/delete URLs are denied. |
| [ ] | Ticket create | Valid ticket gets an INC number, customer, severity, assignment, and SLA. Missing title/description and invalid assignee/group are rejected. |
| [ ] | Ticket edit | Change fields/status and reload; values and relevant activity history persist. |
| [ ] | Ticket delete/restore | Manager deletes a disposable ticket; it appears under All Tickets → Deleted Tickets / Restore. Restore it and confirm its comments, attachments, and history return. Agents cannot access restore. |
| [ ] | Comments/internal notes | Add visible staff comment, internal note, and customer reply. Attach a file to a public reply and to an internal note; each appears under its comment, only the public file appears in the portal, and the activity timeline records both. Confirm edited comments record an event. |
| [ ] | Reply counts | Customer reply followed by internal note stays flagged in table, personal/team count, and filtered tabs. A visible staff reply clears it. |
| [ ] | Last activity | Edit a comment and log/edit time; staff last-activity ordering changes. Portal excludes internal-note/activity timestamps. |
| [ ] | Time logging | Log 1h15m and edit it; check ticket total and customer hours/revenue. Zero/negative time is rejected. |
| [ ] | SLA pause/resume | Enter WaitingOnCustomer/WaitingOnVendor; display says Paused. Resume InProgress manually or post a customer reply; effective due date includes the pause and timeline records resume/status. Resolve/close and inspect deadline/completion display. |
| [ ] | Customer SLA | Save custom durations, create a new ticket, check deadline; invalid durations fail validation. |
| [ ] | Customers | Create/edit/deactivate/reactivate; filters and saved values agree. Deactivation blocks an already signed-in portal user on ticket access. |
| [ ] | Portal create/link | New account requires password; existing non-staff account links without password. Staff, duplicate, and links to soft-deleted companies are rejected. |
| [ ] | Portal deactivate/unlink | Active flag controls ticket access. Unlinked users receive access denied without a redirect loop. |
| [ ] | Portal permissions | No view permission blocks list/detail GET and comment POST. No comment permission permits viewing but hides form and blocks crafted POST. |
| [ ] | Portal ticket creation | Manager enables Can Create for an active portal user with Can View. Button appears; submitted ticket is linked to that user's company, starts New, and displays a success alert. Disabling permission hides the button and direct GET/POST are denied. |
| [ ] | Cross-customer safety | Other-company ticket ID in GET/comment POST returns not found and inserts no comment. |
| [ ] | User management | Manager creates an Agent from Users, optionally assigns an active team, then edits/searches the account. Public Register creates no staff role; Managers can provision its roleless account. Self-deactivation/self-removal of Manager fail. Portal users remain in customer management. |
| [ ] | Bulk ticket claim | Assign an Agent to an active team. Select multiple unassigned tickets and claim them; they become assigned to the Agent and unrouted tickets adopt that team’s group. Tickets in other groups and already assigned tickets cannot be claimed. |
| [ ] | Team management | Create/edit a team using a real support group (Unassigned is rejected); assign staff; deactivate and confirm unassignment. Empty team deletion succeeds; active-member deletion fails. |
| [ ] | Dashboard | Compare counts/resolution averages/open-ticket SLA percentage with known data. Customer filter persists; All Customers clears it. |
| [ ] | Search/filter/sort | Combine text with status/severity/group/category. Check both directions for title, creation date, and last activity. Select a terminal status explicitly to see closed tickets. |
| [ ] | Persistence | Restart with same database; users/customers/teams/tickets remain. |
| [ ] | Offline recovery | Exercise accepted password reset using a disposable account, then sign in with its new password. |
| [ ] | Presentation | Check narrow/wide layouts, validation messages, empty states, and report screenshots. |

Record failures with page, role, steps, expected/actual result, and demonstration
impact. Use fictional data and omit passwords from screenshots.


## Logic-fix regression checklist

- Register a lab account: it has no staff role/profile; Manager provisions it from Users
  or links it to a customer. Create a staff account directly from Users.
- User list includes unprovisioned and portal accounts; portal account management routes
  to its customer. Public registration never grants Manager access.
- Demote another signed-in Manager: the old session loses management access.
- Create portal accounts with all checkboxes checked, then unchecked; edit an existing
  account's three permissions. Verify immediate GET/POST permission revocation.
- Change ticket group: the assignee dropdown refreshes. Test two teams sharing a group.
- Reject duplicate team names, invalid customers and invalid team selection visibly.
- Preserve an inactive current customer when saving an existing ticket.
- Change status to Waiting, Resolved, Closed and Cancelled: correct notes fields appear;
  saved notes remain. Reopen: current resolution disappears but audit history remains.
- Severity/customer changes recalculate an unchanged deadline unless Keep deadline is
  checked. A manually changed deadline wins; clearing it recalculates.
- SLA due text and sorting include accumulated pauses. Pause an overdue ticket: it
  remains breached. Healthy paused tickets use the Paused filter and badge.
- Unassigned queue includes non-New open work without a named assignee.
- Agent claims several tickets from Unassigned; each is assigned to the current Agent,
  ungrouped tickets adopt that Agent's team group, and other-group tickets are rejected.
- Delete and restore a ticket as Manager; confirm dependent history reappears.
- Add a comment, internal note, portal reply, and attachment; staff timeline records each
  event and internal-only events remain hidden from the customer.
- Attach a file while posting each reply. It appears under the matching comment, not in
  the ticket-level file list. A file on an internal note stays inaccessible to portal
  users even when its attachment ID is requested directly.
- Set a ticket to WaitingOnCustomer, then reply as the linked customer. It moves to
  InProgress, clears the SLA pause, accumulates the pause duration, and records both
  transitions. A Resolved ticket reopens on a customer reply; Closed/Cancelled tickets
  reject new portal replies. A deactivated company cannot post from an existing session.
- Team group changes with active members or existing open group work are rejected.
- Check server-time label, portal edited-comment marker and mobile/desktop layout.

## Pagination and attachment checks

- Create enough rows to span three pages. Verify 10/25/50/100 page sizes, first/last/
  previous/next controls, invalid page-size fallback, and out-of-range page clamping.
- Page through Tickets with search, status, severity, SLA, sort, and group filters;
  confirm the selected rows remain in the correct order on later pages.
- Page through My Tickets personal/team lists, Team Queue, Unassigned, Customers,
  Teams, Users, and Portal My Tickets. Confirm the filter and page size persist.
- As Agent, attach a small allowed text or PDF file to a public reply, then attach one
  to an internal note. Verify staff can download both, the portal sees only the public
  file beneath the correct reply, and the ticket-level list does not duplicate them.
- As a portal user with Can View and Can Comment, attach an allowed file to a reply on
  their own ticket. Confirm it appears beneath the reply to their company and staff. A
  user without CanAddComments cannot post/upload; a user without CanViewTickets cannot
  view or download.
- Try another company's ticket ID and attachment ID in the portal; access must fail.
- Reject unsupported extensions, mismatched signatures, empty files, and files larger
  than 10 MB. Confirm validation feedback and that rejected files leave no metadata or
  file behind.
- As Agent, try deleting another staff member's file (must be denied); Managers may
  delete any attachment and the stored file should be removed. Agents may remove their
  own files.
- Confirm uploads are stored outside `wwwroot` under the configured `FileStorage` path.
  Back up the attachment folder together with app.db.

Automated assertions and remaining visual checks are recorded in LOGIC_FIXES.md.
