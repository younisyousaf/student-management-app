import { Injectable, signal } from '@angular/core';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface Toast {
  id: string;
  type: ToastType;
  title: string;
  message?: string;
  duration: number;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  readonly toasts = signal<Toast[]>([]);

  success(title: string, message?: string): void { this.show('success', title, message); }
  error(title: string, message?: string): void { this.show('error', title, message, 5500); }
  warning(title: string, message?: string): void { this.show('warning', title, message, 5000); }
  info(title: string, message?: string): void { this.show('info', title, message); }

  dismiss(id: string): void {
    this.toasts.update(toasts => toasts.filter(toast => toast.id !== id));
  }

  private show(type: ToastType, title: string, message?: string, duration = 4200): void {
    const toast: Toast = { id: crypto.randomUUID(), type, title, message, duration };
    this.toasts.update(toasts => [...toasts, toast]);
    window.setTimeout(() => this.dismiss(toast.id), duration);
  }
}
