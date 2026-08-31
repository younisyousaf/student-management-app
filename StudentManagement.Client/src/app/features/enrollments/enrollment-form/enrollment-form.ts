import { httpResource } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LucideArrowLeft, LucideBookOpen, LucideGraduationCap, LucideSave, LucideUserRound } from '@lucide/angular';
import { environment } from '../../../../environments/environment.development';
import { ApiResponse } from '../../../core/models/api-response.model';
import { Course } from '../../../core/models/course.model';
import { Student } from '../../../core/models/student.model';
import { ToastService } from '../../../core/services/toast.service';
import { EnrollmentsService } from '../enrollments.service';

@Component({
  selector: 'app-enrollment-form',
  standalone: true,
  imports: [FormsModule, RouterLink, LucideArrowLeft, LucideBookOpen, LucideGraduationCap, LucideSave, LucideUserRound],
  templateUrl: './enrollment-form.html',
  styleUrl: './enrollment-form.scss'
})
export class EnrollmentForm {
  private readonly enrollmentsService = inject(EnrollmentsService);
  private readonly router = inject(Router);
  private readonly toastService = inject(ToastService);

  readonly studentsResource = httpResource<ApiResponse<Student[]>>(() => `${environment.apiUrl}/Students`);
  readonly coursesResource = httpResource<ApiResponse<Course[]>>(() => `${environment.apiUrl}/Courses`);
  readonly selectedStudentId = signal<number | null>(null);
  readonly selectedCourseId = signal<number | null>(null);
  readonly isSaving = signal(false);

  onSubmit(): void {
    const studentId = this.selectedStudentId();
    const courseId = this.selectedCourseId();

    if (!studentId || !courseId) {
      this.toastService.warning('Selection required', 'Select both a student and a course.');
      return;
    }

    this.isSaving.set(true);
    this.enrollmentsService.enroll({ studentId, courseId }).subscribe({
      next: () => {
        this.toastService.success('Student enrolled', 'The student was enrolled successfully.');
        this.router.navigate(['/enrollments']);
      },
      error: err => {
        this.isSaving.set(false);
        this.toastService.error('Enrollment failed', this.extractErrorMessage(err));
      }
    });
  }

  private extractErrorMessage(err: unknown): string {
    if (err && typeof err === 'object' && 'error' in err) {
      const httpError = (err as { error?: { message?: string } }).error;
      if (httpError?.message) return httpError.message;
    }
    return 'The student could not be enrolled.';
  }
}
