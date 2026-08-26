# JACO CR – Deferred Change Register

These items should be reviewed and applied together after the main functional testing cycle, unless a change is required to unblock testing.

## Confirmed UI / Functional Changes

1. CR ID in My Change Requests
   - Display the 10-digit CR ID as a hyperlink.
   - Opens the CR Details screen.
   - Implemented in v15 source.

2. Generic action button wording
   - Use generic labels such as `Save`, `Cancel`, `Submit`, `Close`.
   - Avoid product-specific labels such as `Save CR` / `Save Application`.

3. CR create-form field labels
   - `Requested Date` -> `Required By`
   - `Justification` -> `Business Requirements`
   - `Notes` -> `Tangible Benefits`
   - Add `Intangible Benefits`

4. Mandatory creator-entered fields
   - Title
   - Department
   - Priority
   - Impact
   - Required By
   - Change Reason
   - Business Requirements
   - Tangible Benefits
   - Intangible Benefits

5. Controlled dropdown configuration
   - Department, Priority, Impact and Change Reason are controlled values.
   - Values are maintained through the CR Administration screen.

6. Background metadata
   - Created By
   - Created On Date
   - Created On Time
   - Updated By
   - Last Update Date
   - Last Update On

7. Attachments
   - Upload from the CR before submission.
   - Transfer once to the central Approval attachment store on workflow creation.
   - Do not force duplicate uploads.
   - Additional workflow documents can be added from the Approval work item.
   - Transfer status must be visible and retryable.

8. Approval progress visualization
   - Use a Teams-style vertical activity timeline.
   - Show all approvers involved.
   - Show step/status, approver name, timestamp and comments.
   - Show current progress.
   - Support Approved, Action Required, Waiting, Sent Back, Rejected, Not Required, Reassigned and Expired states as applicable.
   - Show final status clearly.

9. CR approval status summary
   - CR should show a compact progress summary.
   - `Open Approval Workflow` opens the authoritative central approval item.

## Architectural rules retained

- JACO CR remains an independent application and database.
- JACO Approval remains the reusable central workflow engine.
- CR communicates with Approval through the API rather than writing to approval tables.
- Sales Discount, Service Discount and future business applications remain independent.
- Approval routing/levels/approvers remain configurable in the central Approval platform.
- Database scripts should be idempotent, schema-aware and end with explicit verification output.

## Deferred until the main test cycle is complete

Apply the items above as a coordinated UI/UX/supportability pass rather than repeatedly changing the source during the core functional testing cycle.
