import { Component, computed, signal } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { environment } from '../../../../environments/environment.development';
import { ApiResponse } from '../../../core/models/api-response.model';
import { Fee, PaymentStatus } from '../../../core/models/fee.model';
import { Student } from '../../../core/models/student.model';
import { Course } from '../../../core/models/course.model';
import { CurrencyPipe } from '@angular/common';
import { PaginatedResult } from '../../../shared/models/paginated-result.model';
import { Pagination } from '../../../shared/components/pagination/pagination';

interface FeeRow extends Fee {
  studentName: string;
  courseName: string;
  statusLabel: string;
}

@Component({
  selector: 'app-fee-list',
  standalone: true,
  imports: [RouterLink, CurrencyPipe, Pagination],
  templateUrl: './fee-list.html'
})
export class FeeList {

  readonly pageNumber = signal(1);
  readonly pageSize = 10;

  feesResource =
    httpResource<ApiResponse<PaginatedResult<Fee>>>(
      () =>
        `${environment.apiUrl}/Fees/paged?pageNumber=${this.pageNumber()}&pageSize=${this.pageSize}`
    );
  studentsResource = httpResource<ApiResponse<Student[]>>(() => `${environment.apiUrl}/Students`);
  coursesResource = httpResource<ApiResponse<Course[]>>(() => `${environment.apiUrl}/Courses`);

  isLoading = computed(() =>
    this.feesResource.isLoading() || this.studentsResource.isLoading() || this.coursesResource.isLoading()
  );

  rows = computed<FeeRow[]>(() => {
    const fees = this.feesResource.value()?.data?.items ?? [];
    const students = this.studentsResource.value()?.data ?? [];
    const courses = this.coursesResource.value()?.data ?? [];

    return fees.map(f => ({
      ...f,
      studentName: students.find(s => s.id === f.studentId)?.fullName ?? `Student #${f.studentId}`,
      courseName: courses.find(c => c.id === f.courseId)?.name ?? `Course #${f.courseId}`,
      statusLabel: PaymentStatus[f.status]
    }));
  });

  changePage(pageNumber: number): void {
    this.pageNumber.set(pageNumber);
  }
}
