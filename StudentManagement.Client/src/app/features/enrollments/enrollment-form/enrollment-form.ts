import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { httpResource } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { environment } from '../../../../environments/environment.development';
import { ApiResponse } from '../../../core/models/api-response.model';
import { Student } from '../../../core/models/student.model';
import { Course } from '../../../core/models/course.model';
import { EnrollmentsService } from '../enrollments.service';

@Component({
  selector: 'app-enrollment-form',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './enrollment-form.html'
})
export class EnrollmentForm {
  studentsResource = httpResource<ApiResponse<Student[]>>(() => `${environment.apiUrl}/Students`);
  coursesResource = httpResource<ApiResponse<Course[]>>(() => `${environment.apiUrl}/Courses`);

  selectedStudentId = signal<number | null>(null);
  selectedCourseId = signal<number | null>(null);
  errorMessage = signal<string | null>(null);
  isSaving = signal(false);

  constructor(private enrollmentsService: EnrollmentsService, private router: Router) {}

  onSubmit(): void {
    if (!this.selectedStudentId() || !this.selectedCourseId()) {
      this.errorMessage.set('Select a student and a course.');
      return;
    }

    this.errorMessage.set(null);
    this.isSaving.set(true);

    this.enrollmentsService.enroll({
      studentId: this.selectedStudentId()!,
      courseId: this.selectedCourseId()!
    }).subscribe({
      next: () => this.router.navigate(['/enrollments']),
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
    return 'Enrollment failed.';
  }
}
