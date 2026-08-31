import { Component, HostListener, input, output } from '@angular/core';
import { LucideTriangleAlert, LucideX } from '@lucide/angular';

@Component({
  selector: 'app-confirm-dialog',
  imports: [LucideTriangleAlert, LucideX],
  templateUrl: './confirm-dialog.html',
  styleUrl: './confirm-dialog.scss'
})
export class ConfirmDialog {
  readonly title = input.required<string>();
  readonly subject = input.required<string>();
  readonly message = input.required<string>();
  readonly warning = input('This action cannot be undone.');
  readonly confirmLabel = input('Confirm');
  readonly processingLabel = input('Processing...');
  readonly processing = input(false);

  readonly cancel = output<void>();
  readonly confirm = output<void>();

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (!this.processing()) this.cancel.emit();
  }
}
