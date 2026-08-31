import { Component, input, output } from '@angular/core';
import { LucideSend, LucideX } from '@lucide/angular';

@Component({
  selector: 'app-prompt-editor',
  imports: [LucideSend, LucideX],
  templateUrl: './prompt-editor.html',
  styleUrl: './prompt-editor.scss'
})
export class PromptEditor {
  readonly value = input.required<string>();
  readonly disabled = input(false);
  readonly valueChange = output<string>();
  readonly cancel = output<void>();
  readonly submit = output<void>();

  onInput(event: Event): void {
    this.valueChange.emit((event.target as HTMLTextAreaElement).value);
  }
}
