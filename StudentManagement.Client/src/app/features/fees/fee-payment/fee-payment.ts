import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FeesService } from '../fees.service';
import { Fee } from '../../../core/models/fee.model';
import { CurrencyPipe } from '@angular/common';

@Component({
  selector: 'app-fee-payment',
  standalone: true,
  imports: [FormsModule, RouterLink, CurrencyPipe],
  templateUrl: './fee-payment.html'
})
export class FeePayment implements OnInit {
  studentId = signal<number | null>(null);
  courseId = signal<number | null>(null);
  statement = signal<Fee | null>(null);

  amountPaid = signal<number>(0);
  remarks = signal('');

  errorMessage = signal<string | null>(null);
  isLoadingStatement = signal(true);
  isSaving = signal(false);

  constructor(
    private feesService: FeesService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    const studentIdParam = this.route.snapshot.queryParamMap.get('studentId');
    const courseIdParam = this.route.snapshot.queryParamMap.get('courseId');

    if (!studentIdParam || !courseIdParam) {
      this.errorMessage.set('Missing student or course reference.');
      this.isLoadingStatement.set(false);
      return;
    }

    const sId = Number(studentIdParam);
    const cId = Number(courseIdParam);
    this.studentId.set(sId);
    this.courseId.set(cId);

    this.feesService.getStatement(sId, cId).subscribe({
      next: (res) => {
        this.statement.set(res.data);
        this.isLoadingStatement.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load fee statement.');
        this.isLoadingStatement.set(false);
      }
    });
  }

  onSubmit(): void {
    this.errorMessage.set(null);

    const remaining = this.statement()?.remainingBalance ?? 0;
    if (this.amountPaid() <= 0 || this.amountPaid() > remaining) {
      this.errorMessage.set(`Enter an amount between 0.01 and ${remaining}.`);
      return;
    }

    this.isSaving.set(true);

    this.feesService.pay({
      studentId: this.studentId()!,
      courseId: this.courseId()!,
      amountPaid: this.amountPaid(),
      remarks: this.remarks() || undefined
    }).subscribe({
      next: () => this.router.navigate(['/fees']),
      error: (err: unknown) => {
        this.errorMessage.set(this.extractErrorMessage(err));
        this.isSaving.set(false);
      }
    });
  }

  private extractErrorMessage(err: unknown): string {
    if (err && typeof err === 'object' && 'error' in err) {
      const httpError = (err as { error?: { message?: string } }).error;
      if (httpError?.message) return httpError.message;
    }
    return 'Payment failed.';
  }
}
