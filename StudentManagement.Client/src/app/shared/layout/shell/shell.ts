import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { LucideBookOpen, LucideBot, LucideCalendarDays, LucideCreditCard, LucideGraduationCap, LucideLogOut, LucideMenu, LucidePanelLeft, LucideSparkles, LucideUsers, LucideX } from '@lucide/angular';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';


@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet, LucideUsers, LucideBookOpen, LucideGraduationCap, LucideCreditCard, LucideCalendarDays, LucideBot, LucideMenu, LucideX, LucideLogOut, LucidePanelLeft, LucideSparkles],
  templateUrl: './shell.html',
  styleUrl: './shell.scss'
})
export class Shell {
  private readonly authService = inject(AuthService);
  private readonly toastService = inject(ToastService);
  readonly isMobileNavOpen = signal(false);
  readonly isSidebarCollapsed = signal(false);

  toggleMobileNav(): void {
    this.isMobileNavOpen.update(value => !value);
  }

  toggleSidebar(): void {
    this.isSidebarCollapsed.update(value => !value);
  }

  closeMobileNav(): void {
    this.isMobileNavOpen.set(false);
  }

  logout(): void {
    this.authService.logout();
  }

  testToast(): void {
    this.toastService.success('SmartCampus ready', 'Toast notifications are working.');
  }
}
