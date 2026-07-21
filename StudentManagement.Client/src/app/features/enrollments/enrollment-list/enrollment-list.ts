import { Component, computed } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { environment } from '../../../../environments/environment.development';
import { ApiResponse } from '../../../core/models/api-response.model';
import { Enrollment } from '../../../core/models/enrollment.model';
import { Student } from '../../../core/models/student.model';
import { Course } from '../../../core/models/course.model';
import { EnrollmentsService } from '../enrollments.service';
import { DatePipe } from '@angular/common';

interface EnrollmentRow {
  id: number;
  studentName: string;
  courseName: string;
  enrollDate: string;
  status: string;
}

@Component({
  selector: 'app-enrollment-list',
  standalone: true,
  imports: [RouterLink, DatePipe],
  templateUrl: './enrollment-list.html'
})
export class EnrollmentList {
  enrollmentsResource = httpResource<ApiResponse<Enrollment[]>>(() => `${environment.apiUrl}/Enrollments`);
  studentsResource = httpResource<ApiResponse<Student[]>>(() => `${environment.apiUrl}/Students`);
  coursesResource = httpResource<ApiResponse<Course[]>>(() => `${environment.apiUrl}/Courses`);

  isLoading = computed(() =>
    this.enrollmentsResource.isLoading() || this.studentsResource.isLoading() || this.coursesResource.isLoading()
  );

  rows = computed<EnrollmentRow[]>(() => {
    const enrollments = this.enrollmentsResource.value()?.data ?? [];
    const students = this.studentsResource.value()?.data ?? [];
    const courses = this.coursesResource.value()?.data ?? [];

    return enrollments.map(e => ({
      id: e.id,
      studentName: students.find(s => s.id === e.studentId)?.fullName ?? `Student #${e.studentId}`,
      courseName: courses.find(c => c.id === e.courseId)?.name ?? `Course #${e.courseId}`,
      enrollDate: e.enrollDate,
      status: e.status
    }));
  });

  constructor(private enrollmentsService: EnrollmentsService) {}

  dropEnrollment(id: number): void {
    if (!confirm('Drop this enrollment?')) return;
    this.enrollmentsService.drop(id).subscribe({
      next: () => this.enrollmentsResource.reload(),
      error: (err: unknown) => alert(this.extractErrorMessage(err))
    });
  }

  completeEnrollment(id: number): void {
    this.enrollmentsService.complete(id).subscribe({
      next: () => this.enrollmentsResource.reload(),
      error: (err: unknown) => alert(this.extractErrorMessage(err))
    });
  }

  private extractErrorMessage(err: unknown): string {
    if (err && typeof err === 'object' && 'error' in err) {
      const httpError = (err as { error?: { message?: string } }).error;
      if (httpError?.message) return httpError.message;
    }
    return 'Action failed.';
  }
}
