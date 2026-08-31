import { DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideBookOpen, LucideCalendarDays, LucideCircleCheck, LucideGraduationCap, LucidePlus, LucideTriangleAlert, LucideUserMinus, LucideUserRound, LucideX } from '@lucide/angular';
import { environment } from '../../../../environments/environment.development';
import { ApiResponse } from '../../../core/models/api-response.model';
import { Course } from '../../../core/models/course.model';
import { Enrollment } from '../../../core/models/enrollment.model';
import { Student } from '../../../core/models/student.model';
import { ToastService } from '../../../core/services/toast.service';
import { Pagination } from '../../../shared/components/pagination/pagination';
import { PaginatedResult } from '../../../shared/models/paginated-result.model';
import { EnrollmentsService } from '../enrollments.service';
import { ConfirmDialog } from '../../../shared/components/confirm-dialog/confirm-dialog';

interface EnrollmentRow {
  id: number;
  studentName: string;
  studentRollNumber: string;
  courseName: string;
  courseCode: string;
  enrollDate: string;
  status: Enrollment['status'];
}

@Component({
  selector: 'app-enrollment-list',
  standalone: true,
  imports: [RouterLink, DatePipe, Pagination, LucideBookOpen, LucideCalendarDays, LucideCircleCheck, LucideGraduationCap, LucidePlus, LucideTriangleAlert, LucideUserMinus, LucideUserRound, ConfirmDialog],
  templateUrl: './enrollment-list.html',
  styleUrl: './enrollment-list.scss'
})
export class EnrollmentList {
  private readonly enrollmentsService = inject(EnrollmentsService);
  private readonly toastService = inject(ToastService);

  readonly pageNumber = signal(1);
  readonly pageSize = 10;
  readonly enrollmentPendingDrop = signal<EnrollmentRow | null>(null);
  readonly updatingEnrollmentId = signal<number | null>(null);

  readonly enrollmentsResource = httpResource<ApiResponse<PaginatedResult<Enrollment>>>(() =>
    `${environment.apiUrl}/Enrollments/paged?pageNumber=${this.pageNumber()}&pageSize=${this.pageSize}`
  );

  readonly studentsResource = httpResource<ApiResponse<Student[]>>(() => `${environment.apiUrl}/Students`);
  readonly coursesResource = httpResource<ApiResponse<Course[]>>(() => `${environment.apiUrl}/Courses`);

  readonly isLoading = computed(() =>
    this.enrollmentsResource.isLoading() || this.studentsResource.isLoading() || this.coursesResource.isLoading()
  );

  readonly hasError = computed(() =>
    !!this.enrollmentsResource.error() || !!this.studentsResource.error() || !!this.coursesResource.error()
  );

  readonly rows = computed<EnrollmentRow[]>(() => {
    const students = this.studentsResource.value()?.data ?? [];
    const courses = this.coursesResource.value()?.data ?? [];

    return (this.enrollmentsResource.value()?.data?.items ?? []).map(enrollment => {
      const student = students.find(item => item.id === enrollment.studentId);
      const course = courses.find(item => item.id === enrollment.courseId);

      return {
        id: enrollment.id,
        studentName: student?.fullName ?? `Student #${enrollment.studentId}`,
        studentRollNumber: student?.rollNumber ?? `#${enrollment.studentId}`,
        courseName: course?.name ?? `Course #${enrollment.courseId}`,
        courseCode: course?.code ?? `#${enrollment.courseId}`,
        enrollDate: enrollment.enrollDate,
        status: enrollment.status
      };
    });
  });

  changePage(pageNumber: number): void {
    this.pageNumber.set(pageNumber);
  }

  requestDrop(row: EnrollmentRow): void {
    this.enrollmentPendingDrop.set(row);
  }

  cancelDrop(): void {
    if (!this.updatingEnrollmentId()) this.enrollmentPendingDrop.set(null);
  }

  completeEnrollment(row: EnrollmentRow): void {
    if (this.updatingEnrollmentId()) return;

    this.updatingEnrollmentId.set(row.id);
    this.enrollmentsService.complete(row.id).subscribe({
      next: () => {
        this.toastService.success('Enrollment completed', `${row.studentName} completed ${row.courseName}.`);
        this.enrollmentsResource.reload();
      },
      error: err => {
        this.updatingEnrollmentId.set(null);
        this.toastService.error('Action failed', this.extractErrorMessage(err));
      },
      complete: () => this.updatingEnrollmentId.set(null)
    });
  }

  dropEnrollment(): void {
    const row = this.enrollmentPendingDrop();
    if (!row || this.updatingEnrollmentId()) return;

    this.updatingEnrollmentId.set(row.id);
    this.enrollmentsService.drop(row.id).subscribe({
      next: () => {
        this.enrollmentPendingDrop.set(null);
        this.toastService.success('Enrollment dropped', `${row.studentName} was dropped from ${row.courseName}.`);
        this.enrollmentsResource.reload();
      },
      error: err => {
        this.updatingEnrollmentId.set(null);
        this.toastService.error('Action failed', this.extractErrorMessage(err));
      },
      complete: () => this.updatingEnrollmentId.set(null)
    });
  }

  private extractErrorMessage(err: unknown): string {
    if (err && typeof err === 'object' && 'error' in err) {
      const httpError = (err as { error?: { message?: string } }).error;
      if (httpError?.message) return httpError.message;
    }
    return 'The enrollment action could not be completed.';
  }
}
