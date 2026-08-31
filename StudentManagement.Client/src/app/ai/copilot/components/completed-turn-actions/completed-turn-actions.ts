import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-completed-turn-actions',
  standalone: true,
  templateUrl: './completed-turn-actions.html',
  styleUrl: './completed-turn-actions.scss'
})
export class CompletedTurnActions {
  readonly disabled = input(false);

  readonly editPrompt = output<void>();
}
