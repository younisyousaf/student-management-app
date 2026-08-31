import { Component, input, output } from '@angular/core';
import { LucideCheck, LucideShieldAlert, LucideTriangleAlert, LucideX } from '@lucide/angular';
import { CopilotApprovalRequest } from '../../models/copilot.model';

@Component({
  selector: 'app-approval-card',
  imports: [LucideCheck, LucideShieldAlert, LucideTriangleAlert, LucideX],
  templateUrl: './approval-card.html',
  styleUrl: './approval-card.scss'
})
export class ApprovalCard {
  readonly approval = input.required<CopilotApprovalRequest>();
  readonly isSending = input.required<boolean>();
  readonly approve = output<void>();
  readonly reject = output<void>();
}
