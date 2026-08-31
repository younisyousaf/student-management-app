import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LucideArrowRight, LucideBrainCircuit, LucideEye, LucideEyeOff, LucideLockKeyhole, LucideMail, LucideShieldCheck, LucideSparkles, LucideUserPlus, LucideUserRound } from '@lucide/angular';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, RouterLink, LucideArrowRight, LucideBrainCircuit, LucideEye, LucideEyeOff, LucideLockKeyhole, LucideMail, LucideShieldCheck, LucideSparkles, LucideUserPlus, LucideUserRound],
  templateUrl: './register.html',
  styleUrl: './register.scss'
})
export class Register {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toastService = inject(ToastService);

  readonly username = signal('');
  readonly email = signal('');
  readonly password = signal('');
  readonly confirmPassword = signal('');
  readonly showPassword = signal(false);
  readonly isLoading = signal(false);

  onSubmit(): void {
    if (!this.username().trim() || !this.email().trim() || !this.password() || !this.confirmPassword()) {
      this.toastService.warning('Details required', 'Complete all account fields.');
      return;
    }

    if (this.password() !== this.confirmPassword()) {
      this.toastService.warning('Passwords do not match', 'Enter the same password in both fields.');
      return;
    }

    this.isLoading.set(true);
    this.authService.register({
      username: this.username().trim(),
      email: this.email().trim(),
      password: this.password()
    }).subscribe({
      next: () => {
        this.toastService.success('Account created', 'Your SmartCampus account is ready. Sign in to continue.');
        this.router.navigate(['/login']);
      },
      error: (err: unknown) => {
        this.isLoading.set(false);
        this.toastService.error('Registration failed', this.extractErrorMessage(err));
      }
    });
  }

  private extractErrorMessage(err: unknown): string {
    if (err && typeof err === 'object' && 'error' in err) {
      const error = (err as { error?: { message?: string } }).error;
      if (error?.message) return error.message;
    }
    return 'The account could not be created.';
  }
}
