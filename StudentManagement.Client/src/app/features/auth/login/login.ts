import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LucideArrowRight, LucideBrainCircuit, LucideEye, LucideEyeOff, LucideLockKeyhole, LucideShieldCheck, LucideSparkles, LucideUserRound } from '@lucide/angular';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink, LucideArrowRight, LucideBrainCircuit, LucideEye, LucideEyeOff, LucideLockKeyhole, LucideShieldCheck, LucideSparkles, LucideUserRound],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class Login {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly toastService = inject(ToastService);

  readonly username = signal('');
  readonly password = signal('');
  readonly showPassword = signal(false);
  readonly isLoading = signal(false);

  onSubmit(): void {
    if (!this.username().trim() || !this.password()) {
      this.toastService.warning('Credentials required', 'Enter your username and password.');
      return;
    }

    this.isLoading.set(true);
    this.authService.login({ username: this.username().trim(), password: this.password() }).subscribe({
      next: () => {
        this.authService.setAuthenticated(true);
        this.toastService.success('Welcome back', 'You signed in successfully.');

        const returnUrl = this.route.snapshot.queryParams['returnUrl'];
        const lastRoute = sessionStorage.getItem('lastRoute');
        this.router.navigateByUrl(returnUrl || lastRoute || '/students');
      },
      error: (err: unknown) => {
        this.isLoading.set(false);
        this.toastService.error('Sign in failed', this.extractErrorMessage(err));
      }
    });
  }

  private extractErrorMessage(err: unknown): string {
    if (err && typeof err === 'object' && 'error' in err) {
      const error = (err as { error?: { message?: string } }).error;
      if (error?.message) return error.message;
    }
    return 'Invalid username or password.';
  }
}
