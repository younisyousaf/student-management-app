import { Component, computed, signal } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { environment } from '../../../../environments/environment.development';
import { ApiResponse } from '../../../core/models/api-response.model';
import { Attendance, AttendanceStatus } from '../../../core/models/attendance.model';
import { Student } from '../../../core/models/student.model';
import { Course } from '../../../core/models/course.model';
import { DatePipe } from '@angular/common';
import { PaginatedResult } from '../../../shared/models/paginated-result.model';
import { Pagination } from '../../../shared/components/pagination/pagination';
interface AttendanceRow extends Attendance {
  studentName: string;
  courseName: string;
  statusLabel: string;
}
@Component({
  selector: 'app-attendance-list',
  standalone: true,
  imports: [RouterLink, DatePipe, Pagination],
  templateUrl: './attendance-list.html'
})
export class AttendanceList {
  readonly pageNumber = signal(1);
  readonly pageSize = 10;
  attendanceResource =
    httpResource<
      ApiResponse<
        PaginatedResult<Attendance>
      >
    >(
      () =>
        `${environment.apiUrl}/Attendance/paged?pageNumber=${this.pageNumber()}&pageSize=${this.pageSize}`
    );
  studentsResource = httpResource<ApiResponse<Student[]>>(() => `${environment.apiUrl}/Students`);
  coursesResource = httpResource<ApiResponse<Course[]>>(() => `${environment.apiUrl}/Courses`);
  isLoading = computed(() =>
    this.attendanceResource.isLoading() || this.studentsResource.isLoading() || this.coursesResource.isLoading()
  );
  rows = computed<AttendanceRow[]>(() => {
    const records = this.attendanceResource.value()?.data?.items ?? [];
    const students = this.studentsResource.value()?.data ?? [];
    const courses = this.coursesResource.value()?.data ?? [];
    return records
      .map(a => ({
        ...a,
        studentName: students.find(s => s.id === a.studentId)?.fullName ?? `Student #${a.studentId}`,
        courseName: courses.find(c => c.id === a.courseId)?.name ?? `Course #${a.courseId}`,
        statusLabel: AttendanceStatus[a.status]
      }))
      .sort((a, b) => b.date.localeCompare(a.date));
  });
  changePage(pageNumber: number): void {
    this.pageNumber.set( pageNumber);
  }
}
