import { DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideBookOpen, LucideCalendarCheck, LucideCalendarDays, LucidePencil, LucidePlus, LucideTriangleAlert, LucideUserRound } from '@lucide/angular';
import { environment } from '../../../../environments/environment.development';
import { ApiResponse } from '../../../core/models/api-response.model';
import { Attendance, AttendanceStatus } from '../../../core/models/attendance.model';
import { Course } from '../../../core/models/course.model';
import { Student } from '../../../core/models/student.model';
import { Pagination } from '../../../shared/components/pagination/pagination';
import { PaginatedResult } from '../../../shared/models/paginated-result.model';

interface AttendanceRow extends Attendance {
  studentName: string;
  studentRollNumber: string;
  courseName: string;
  courseCode: string;
  statusLabel: string;
}

@Component({
  selector: 'app-attendance-list',
  standalone: true,
  imports: [RouterLink, DatePipe, Pagination, LucideBookOpen, LucideCalendarCheck, LucideCalendarDays, LucidePencil, LucidePlus, LucideTriangleAlert, LucideUserRound],
  templateUrl: './attendance-list.html',
  styleUrl: './attendance-list.scss'
})
export class AttendanceList {
  readonly pageNumber = signal(1);
  readonly pageSize = 10;

  readonly attendanceResource = httpResource<ApiResponse<PaginatedResult<Attendance>>>(() =>
    `${environment.apiUrl}/Attendance/paged?pageNumber=${this.pageNumber()}&pageSize=${this.pageSize}`
  );

  readonly studentsResource = httpResource<ApiResponse<Student[]>>(() => `${environment.apiUrl}/Students`);
  readonly coursesResource = httpResource<ApiResponse<Course[]>>(() => `${environment.apiUrl}/Courses`);

  readonly isLoading = computed(() =>
    this.attendanceResource.isLoading() || this.studentsResource.isLoading() || this.coursesResource.isLoading()
  );

  readonly hasError = computed(() =>
    !!this.attendanceResource.error() || !!this.studentsResource.error() || !!this.coursesResource.error()
  );

  readonly rows = computed<AttendanceRow[]>(() => {
    const students = this.studentsResource.value()?.data ?? [];
    const courses = this.coursesResource.value()?.data ?? [];

    return (this.attendanceResource.value()?.data?.items ?? [])
      .map(record => {
        const student = students.find(item => item.id === record.studentId);
        const course = courses.find(item => item.id === record.courseId);

        return {
          ...record,
          studentName: student?.fullName ?? `Student #${record.studentId}`,
          studentRollNumber: student?.rollNumber ?? `#${record.studentId}`,
          courseName: course?.name ?? `Course #${record.courseId}`,
          courseCode: course?.code ?? `#${record.courseId}`,
          statusLabel: AttendanceStatus[record.status]
        };
      })
      .sort((a, b) => b.date.localeCompare(a.date));
  });

  changePage(pageNumber: number): void {
    this.pageNumber.set(pageNumber);
  }
}
