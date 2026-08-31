import { CurrencyPipe, DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideBookOpen, LucideCalendarDays, LucideCircleDollarSign, LucideCreditCard, LucideReceiptText, LucideTriangleAlert, LucideUserRound, LucideWalletCards } from '@lucide/angular';
import { environment } from '../../../../environments/environment.development';
import { ApiResponse } from '../../../core/models/api-response.model';
import { Course } from '../../../core/models/course.model';
import { Fee, PaymentStatus } from '../../../core/models/fee.model';
import { Student } from '../../../core/models/student.model';
import { Pagination } from '../../../shared/components/pagination/pagination';
import { PaginatedResult } from '../../../shared/models/paginated-result.model';

interface FeeRow extends Fee {
  studentName: string;
  studentRollNumber: string;
  courseName: string;
  courseCode: string;
  statusLabel: string;
}

@Component({
  selector: 'app-fee-list',
  standalone: true,
  imports: [RouterLink, CurrencyPipe, DatePipe, Pagination, LucideBookOpen, LucideCalendarDays, LucideCircleDollarSign, LucideCreditCard, LucideReceiptText, LucideTriangleAlert, LucideUserRound, LucideWalletCards],
  templateUrl: './fee-list.html',
  styleUrl: './fee-list.scss'
})
export class FeeList {
  readonly pageNumber = signal(1);
  readonly pageSize = 10;

  readonly feesResource = httpResource<ApiResponse<PaginatedResult<Fee>>>(() =>
    `${environment.apiUrl}/Fees/paged?pageNumber=${this.pageNumber()}&pageSize=${this.pageSize}`
  );

  readonly studentsResource = httpResource<ApiResponse<Student[]>>(() => `${environment.apiUrl}/Students`);
  readonly coursesResource = httpResource<ApiResponse<Course[]>>(() => `${environment.apiUrl}/Courses`);

  readonly isLoading = computed(() =>
    this.feesResource.isLoading() || this.studentsResource.isLoading() || this.coursesResource.isLoading()
  );

  readonly hasError = computed(() =>
    !!this.feesResource.error() || !!this.studentsResource.error() || !!this.coursesResource.error()
  );

  readonly rows = computed<FeeRow[]>(() => {
    const students = this.studentsResource.value()?.data ?? [];
    const courses = this.coursesResource.value()?.data ?? [];

    return (this.feesResource.value()?.data?.items ?? []).map(fee => {
      const student = students.find(item => item.id === fee.studentId);
      const course = courses.find(item => item.id === fee.courseId);

      return {
        ...fee,
        studentName: student?.fullName ?? `Student #${fee.studentId}`,
        studentRollNumber: student?.rollNumber ?? `#${fee.studentId}`,
        courseName: course?.name ?? `Course #${fee.courseId}`,
        courseCode: course?.code ?? `#${fee.courseId}`,
        statusLabel: PaymentStatus[fee.status]
      };
    });
  });

  changePage(pageNumber: number): void {
    this.pageNumber.set(pageNumber);
  }
}
