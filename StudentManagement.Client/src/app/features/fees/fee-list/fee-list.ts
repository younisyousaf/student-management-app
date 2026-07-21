import { Component, computed } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { environment } from '../../../../environments/environment.development';
import { ApiResponse } from '../../../core/models/api-response.model';
import { Fee, PaymentStatus } from '../../../core/models/fee.model';
import { Student } from '../../../core/models/student.model';
import { Course } from '../../../core/models/course.model';
import { CurrencyPipe } from '@angular/common';

interface FeeRow extends Fee {
  studentName: string;
  courseName: string;
  statusLabel: string;
}

@Component({
  selector: 'app-fee-list',
  standalone: true,
  imports: [RouterLink, CurrencyPipe],
  templateUrl: './fee-list.html'
})
export class FeeList {
  feesResource = httpResource<ApiResponse<Fee[]>>(() => `${environment.apiUrl}/Fees`);
  studentsResource = httpResource<ApiResponse<Student[]>>(() => `${environment.apiUrl}/Students`);
  coursesResource = httpResource<ApiResponse<Course[]>>(() => `${environment.apiUrl}/Courses`);

  isLoading = computed(() =>
    this.feesResource.isLoading() || this.studentsResource.isLoading() || this.coursesResource.isLoading()
  );

  rows = computed<FeeRow[]>(() => {
    const fees = this.feesResource.value()?.data ?? [];
    const students = this.studentsResource.value()?.data ?? [];
    const courses = this.coursesResource.value()?.data ?? [];

    return fees.map(f => ({
      ...f,
      studentName: students.find(s => s.id === f.studentId)?.fullName ?? `Student #${f.studentId}`,
      courseName: courses.find(c => c.id === f.courseId)?.name ?? `Course #${f.courseId}`,
      statusLabel: PaymentStatus[f.status]
    }));
  });
}
