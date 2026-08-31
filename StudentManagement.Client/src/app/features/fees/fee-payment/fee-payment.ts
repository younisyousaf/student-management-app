import { CurrencyPipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LucideArrowLeft, LucideBookOpen, LucideCreditCard, LucideReceiptText, LucideUserRound, LucideWalletCards } from '@lucide/angular';
import { environment } from '../../../../environments/environment.development';
import { ApiResponse } from '../../../core/models/api-response.model';
import { Course } from '../../../core/models/course.model';
import { Fee } from '../../../core/models/fee.model';
import { Student } from '../../../core/models/student.model';
import { ToastService } from '../../../core/services/toast.service';
import { FeesService } from '../fees.service';

@Component({
  selector: 'app-fee-payment',
  standalone: true,
  imports: [FormsModule, RouterLink, CurrencyPipe, LucideArrowLeft, LucideBookOpen, LucideCreditCard, LucideReceiptText, LucideUserRound, LucideWalletCards],
  templateUrl: './fee-payment.html',
  styleUrl: './fee-payment.scss'
})
export class FeePayment implements OnInit {
  private readonly feesService = inject(FeesService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toastService = inject(ToastService);

  readonly studentId = signal<number | null>(null);
  readonly courseId = signal<number | null>(null);
  readonly statement = signal<Fee | null>(null);
  readonly amountPaid = signal(0);
  readonly remarks = signal('');
  readonly isLoadingStatement = signal(true);
  readonly isSaving = signal(false);

  readonly studentsResource = httpResource<ApiResponse<Student[]>>(() => `${environment.apiUrl}/Students`);
  readonly coursesResource = httpResource<ApiResponse<Course[]>>(() => `${environment.apiUrl}/Courses`);

  readonly student = computed(() =>
    (this.studentsResource.value()?.data ?? []).find(item => item.id === this.studentId()) ?? null
  );

  readonly course = computed(() =>
    (this.coursesResource.value()?.data ?? []).find(item => item.id === this.courseId()) ?? null
  );

  ngOnInit(): void {
    const studentId = Number(this.route.snapshot.queryParamMap.get('studentId'));
    const courseId = Number(this.route.snapshot.queryParamMap.get('courseId'));

    if (!studentId || !courseId) {
      this.isLoadingStatement.set(false);
      this.toastService.error('Invalid payment request', 'Student or course information is missing.');
      return;
    }

    this.studentId.set(studentId);
    this.courseId.set(courseId);

    this.feesService.getStatement(studentId, courseId).subscribe({
      next: res => {
        this.statement.set(res.data);
        this.isLoadingStatement.set(false);
      },
      error: () => {
        this.isLoadingStatement.set(false);
        this.toastService.error('Unable to load statement', 'The fee statement could not be loaded.');
      }
    });
  }

  payFullBalance(): void {
    this.amountPaid.set(this.statement()?.remainingBalance ?? 0);
  }

  onSubmit(): void {
    const statement = this.statement();
    if (!statement) return;

    if (this.amountPaid() <= 0 || this.amountPaid() > statement.remainingBalance) {
      this.toastService.warning('Invalid payment amount', `Enter an amount between 0.01 and ${statement.remainingBalance}.`);
      return;
    }

    this.isSaving.set(true);
    this.feesService.pay({
      studentId: this.studentId()!,
      courseId: this.courseId()!,
      amountPaid: this.amountPaid(),
      remarks: this.remarks() || undefined
    }).subscribe({
      next: () => {
        this.toastService.success('Payment recorded', 'The payment was processed successfully.');
        this.router.navigate(['/fees']);
      },
      error: err => {
        this.isSaving.set(false);
        this.toastService.error('Payment failed', this.extractErrorMessage(err));
      }
    });
  }

  private extractErrorMessage(err: unknown): string {
    if (err && typeof err === 'object' && 'error' in err) {
      const httpError = (err as { error?: { message?: string } }).error;
      if (httpError?.message) return httpError.message;
    }
    return 'The payment could not be processed.';
  }
}
