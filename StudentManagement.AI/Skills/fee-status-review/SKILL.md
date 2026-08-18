---
name: fee-status-review
description: Review a student's course fee status using live fee data and explain outstanding or completed payment status without inventing institutional consequences.
---

# Fee Status Review

Use this skill when a user asks about a student's fee status, outstanding balance, or payment state.

## Process

1. Identify the exact student.

2. Identify the relevant course or enrollment.

3. Retrieve the student's live fee statement using the available fee tools.

4. Use the returned fee values directly:
   - amount due,
   - amount paid,
   - remaining balance,
   - payment status.

5. Do not recalculate or replace authoritative values returned by the fee tool.

6. If the user asks whether a fee status affects eligibility, penalties, enrollment, or another consequence:
   - retrieve the relevant institutional policy,
   - do not infer the consequence from the fee status alone.
   - Do not conclude that a fee status has no consequence merely because the retrieved policy does not mention one.
   - If the retrieved policy does not explicitly establish the consequence, say that the effect cannot be determined from the available policy.

7. If live fee data cannot be retrieved, explain that the fee status cannot currently be determined.

8. Never process or modify a payment while performing a fee-status review.