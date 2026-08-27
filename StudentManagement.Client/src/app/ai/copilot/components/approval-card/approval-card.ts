import { Component, input, output } from '@angular/core';
import { CopilotApprovalRequest } from '../../models/copilot.model';
@Component({
  selector: 'app-approval-card',
  standalone: true,
  templateUrl: './approval-card.html',
  styleUrl: './approval-card.scss'
})
export class ApprovalCard {
  readonly approval = input.required<CopilotApprovalRequest>();
  readonly isSending = input.required<boolean>();
  readonly approve = output<void>();
  readonly reject = output<void>();
}
