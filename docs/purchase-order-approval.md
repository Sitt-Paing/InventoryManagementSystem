# Purchase order approval and receiving

## Workflow

New POs start with `ApprovalStatus = Draft`. Submit moves Draft to Submitted.
Administrator or SuperAdmin may approve or reject a Submitted PO. Rejection requires
a trimmed reason of 1–500 characters and records the actor ID and `DateTime.Now`.
Existing role and company-query-filter behavior is retained; maker-checker rules
are not introduced by this change.

Approval and receiving are separate states. Only an Approved, non-cancelled,
incomplete PO can receive a new GRN. Repeating an already accepted receipt request
returns its original result through the existing idempotency flow.

Draft POs can be edited. Saving an edit to a Submitted or Rejected PO returns it
to Draft, requiring resubmission. Approved and Cancelled POs cannot be edited.
Edit, approval, rejection and receipt creation use the same PO transaction lock.
Invalid updates roll back rather than persisting the preliminary Draft transition.
The most recent rejection reason and actor/time remain visible after editing and
resubmission; this is current-document metadata, not a full approval-event ledger.

The PO screen offers Submit/Approve/Reject, confirmation, loading/error states,
approval filters and the last rejection reason. GRN creation loads Approved POs
with outstanding receiving status across all result pages. Editing an existing
GRN preserves its original PO, including fully received legacy orders.

## Database

Migration `20261012085910_AddPurchaseOrderRejection` adds nullable `RejectedBy`,
`RejectedOn` and `RejectionReason` (500 characters). It was applied to the local
`InventoryManagement` development database on 12 October 2026. No legacy tenant
assignments were changed. Approval actor IDs remain IDs; the API resolves current
usernames for display.

## Verification on 12 October 2026

API and Angular builds passed. No automated test suite was created.
Manual HTTP requests used a dedicated `PO-VERIFY-*` document and unique GRN keys:

| Scenario | Observed result |
|---|---|
| GRN against Draft / Submitted | 400; no receipt accepted |
| Submit / reject with valid trimmed reason | 200; Rejected with saved reason |
| Blank rejection reason | 422 from the validation pipeline |
| Reject again / approve Rejected | 400 |
| Approve Submitted | 200; approver username `devadmin` and local time returned |
| Edit Approved | 400 |
| Edit Submitted / Rejected with company-owned fixtures | 200 / 200; both return to Draft |
| Invalid supplier during Submitted edit | 400; status remains Submitted (rollback verified) |
| Receive 6 of 10 | 201; PartiallyReceived, received quantity 6 |
| Repeat same receipt request | Same receipt ID; no additional movement |
| Receive 5 when 4 remain | 400 |
| Receive remaining 4 | 201; Completed, received quantity 10 |
| Reverse final receipt | 200; PartiallyReceived, received quantity 6 |
| Two concurrent receipts compete for remaining 4 | One 201, one 400; total 10 |
| Unauthenticated rejection | 401 |
| CompanyAdmin without authority calls approve/reject | 403 / 403 |

Verification receipts were reversed and the dedicated POs were soft-deleted.
The selected product stock was 25 before verification and 25 after cleanup.
Original movements and reversal references are retained. The temporary role-check
user was removed; the dedicated company-owned edit-fixture master records were
soft-deleted. Existing users, master ownership and POs were not edited.

Still to verify manually: browser interactions, cross-company access and more than
100 eligible POs. The existing development masters and legacy POs have null
CompanyId. Existing PO update
validation requires a company and matching company-owned supplier, warehouse,
product and UOM. Those assignments must be resolved deliberately before testing
successful edits of that data; this change does not guess legacy ownership.
