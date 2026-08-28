import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-prompt-editor',
  standalone: true,
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
    const textarea = event.target as HTMLTextAreaElement;
    this.valueChange.emit(textarea.value);
  }
}
