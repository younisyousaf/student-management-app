import { Component, computed } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { environment } from '../../../../environments/environment.development';
import { ApiResponse } from '../../../core/models/api-response.model';
import { Attendance, AttendanceStatus } from '../../../core/models/attendance.model';
import { Student } from '../../../core/models/student.model';
import { Course } from '../../../core/models/course.model';
import { DatePipe } from '@angular/common';

interface AttendanceRow extends Attendance {
  studentName: string;
  courseName: string;
  statusLabel: string;
}

@Component({
  selector: 'app-attendance-list',
  standalone: true,
  imports: [RouterLink, DatePipe],
  templateUrl: './attendance-list.html'
})
export class AttendanceList {
  attendanceResource = httpResource<ApiResponse<Attendance[]>>(() => `${environment.apiUrl}/Attendance`);
  studentsResource = httpResource<ApiResponse<Student[]>>(() => `${environment.apiUrl}/Students`);
  coursesResource = httpResource<ApiResponse<Course[]>>(() => `${environment.apiUrl}/Courses`);

  isLoading = computed(() =>
    this.attendanceResource.isLoading() || this.studentsResource.isLoading() || this.coursesResource.isLoading()
  );

  rows = computed<AttendanceRow[]>(() => {
    const records = this.attendanceResource.value()?.data ?? [];
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
}
