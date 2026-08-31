import { Component, inject } from '@angular/core';
import { LucideCircleCheck, LucideCircleX, LucideInfo, LucideTriangleAlert, LucideX } from '@lucide/angular';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [LucideCircleCheck, LucideCircleX, LucideInfo, LucideTriangleAlert, LucideX],
  templateUrl: './toast-container.html',
  styleUrl: './toast-container.scss'
})
export class ToastContainer {
  readonly toastService = inject(ToastService);
}
