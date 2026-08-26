import { Component, computed, signal } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { environment } from '../../../../environments/environment.development';
import { ApiResponse } from '../../../core/models/api-response.model';
import { Enrollment } from '../../../core/models/enrollment.model';
import { Student } from '../../../core/models/student.model';
import { Course } from '../../../core/models/course.model';
import { EnrollmentsService } from '../enrollments.service';
import { DatePipe } from '@angular/common';
import { PaginatedResult } from '../../../shared/models/paginated-result.model';
import { Pagination } from '../../../shared/components/pagination/pagination';
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
  imports: [RouterLink, DatePipe, Pagination],
  templateUrl: './enrollment-list.html'
})
export class EnrollmentList {
  readonly pageNumber = signal(1);
  readonly pageSize = 10;
  enrollmentsResource =
    httpResource<
      ApiResponse<
        PaginatedResult<Enrollment>
      >
    >(
      () =>
        `${environment.apiUrl}/Enrollments/paged?pageNumber=${this.pageNumber()}&pageSize=${this.pageSize}`
    );
  studentsResource = httpResource<ApiResponse<Student[]>>(() => `${environment.apiUrl}/Students`);
  coursesResource = httpResource<ApiResponse<Course[]>>(() => `${environment.apiUrl}/Courses`);
  isLoading = computed(() =>
    this.enrollmentsResource.isLoading() || this.studentsResource.isLoading() || this.coursesResource.isLoading()
  );
  rows = computed<EnrollmentRow[]>(() => {
    const enrollments = this.enrollmentsResource.value()?.data?.items ?? [];
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
  changePage(pageNumber: number): void {
    this.pageNumber.set(pageNumber);
  }
  constructor(private enrollmentsService: EnrollmentsService) { }
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
