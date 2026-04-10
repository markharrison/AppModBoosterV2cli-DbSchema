# Business Requirements

This document describes the business logic and user workflows that must be implemented when modernising the legacy database schema.

---

## User Roles

- **Employee** — can create, edit, and submit their own expenses
- **Manager** — can view submitted expenses from their direct reports and approve or reject them

## User Context

The application uses a simplified user context (no login required). Provide a user selector in the UI header so the tester can switch between users to exercise different roles. The selected user determines what actions are available.

## Expense Lifecycle

```
Draft → Submitted → Approved
                  → Rejected → (edit) → Submitted
```

### Status Transitions

| Current Status | Action         | New Status | Who Can Do It         |
| -------------- | -------------- | ---------- | --------------------- |
| (new)          | Create         | Draft      | Employee              |
| Draft          | Edit           | Draft      | Owner (Employee)      |
| Draft          | Submit         | Submitted  | Owner (Employee)      |
| Submitted      | Approve        | Approved   | Manager of the owner  |
| Submitted      | Reject         | Rejected   | Manager of the owner  |
| Rejected       | Edit & Resubmit| Submitted  | Owner (Employee)      |
| Approved       | (none)         | —          | Final state           |

### Rules

- Employees can only see and manage their own expenses
- Managers see expenses submitted by their direct reports (Users where ManagerId = current manager's UserId)
- Only the expense owner can edit or submit an expense
- Only the manager of the expense owner can approve or reject
- When approved/rejected: set `ReviewedBy` to the manager's UserId and `ReviewedAt` to current UTC time
- `SubmittedAt` is set when status changes to Submitted
- Approved and Rejected expenses cannot be deleted
- Status must not be a free-choice dropdown — transitions must follow the lifecycle above

## UI Workflows

### Employee View

- **My Expenses** — list of the employee's own expenses with status badges
- **Create Expense** — form with category, amount, date, description (status defaults to Draft)
- **Edit Expense** — only available for Draft or Rejected expenses
- **Submit** button — changes status from Draft to Submitted (confirmation required)

### Manager View

- **Pending Approvals** — list of Submitted expenses from direct reports
- **Approve / Reject** buttons on each pending expense (with optional rejection reason)
- Managers can also be employees and submit their own expenses

### Dashboard

- Employees see: summary of their expenses by status
- Managers see: summary of their expenses + count of pending approvals awaiting their review

---
