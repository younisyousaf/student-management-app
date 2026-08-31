import { Component, input, output } from '@angular/core';
import { LucidePencil } from '@lucide/angular';

@Component({
  selector: 'app-completed-turn-actions',
  imports: [LucidePencil],
  templateUrl: './completed-turn-actions.html',
  styleUrl: './completed-turn-actions.scss'
})
export class CompletedTurnActions {
  readonly disabled = input(false);
  readonly editPrompt = output<void>();
}
