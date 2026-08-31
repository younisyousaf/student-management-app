import { Component, input, output } from '@angular/core';
import { LucideCircleAlert, LucidePencil, LucideRotateCcw } from '@lucide/angular';

@Component({
  selector: 'app-interrupted-turn-actions',
  imports: [LucideCircleAlert, LucidePencil, LucideRotateCcw],
  templateUrl: './interrupted-turn-actions.html',
  styleUrl: './interrupted-turn-actions.scss'
})
export class InterruptedTurnActions {
  readonly disabled = input(false);
  readonly canRetry = input(true);
  readonly canEdit = input(true);
  readonly editPrompt = output<void>();
  readonly tryAgain = output<void>();
}
