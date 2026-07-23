import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { httpResource } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { environment } from '../../../../environments/environment.development';
import { ApiResponse } from '../../../core/models/api-response.model';
import { Student } from '../../../core/models/student.model';
import { Course } from '../../../core/models/course.model';
import { AttendanceStatus } from '../../../core/models/attendance.model';
import { AttendanceService } from '../attendance.service';

@Component({
  selector: 'app-attendance-form',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './attendance-form.html'
})
export class AttendanceForm implements OnInit {
  isEditMode = signal(false);
  attendanceId = signal<number | null>(null);

  studentsResource = httpResource<ApiResponse<Student[]>>(() => `${environment.apiUrl}/Students`);
  coursesResource = httpResource<ApiResponse<Course[]>>(() => `${environment.apiUrl}/Courses`);

  // Mark-mode fields
  selectedStudentId = signal<number | null>(null);
  selectedCourseId = signal<number | null>(null);
  date = signal<string>(new Date().toISOString().substring(0, 10));

  // Shared / edit-mode fields
  status = signal<AttendanceStatus>(AttendanceStatus.Present);
  remarks = signal('');

  // Edit-mode display-only context (can't be changed once recorded)
  studentName = signal('');
  courseName = signal('');

  errorMessage = signal<string | null>(null);
  isLoading = signal(false);
  isSaving = signal(false);

  readonly statusOptions = [
    { value: AttendanceStatus.Present, label: 'Present' },
    { value: AttendanceStatus.Absent, label: 'Absent' },
    { value: AttendanceStatus.Late, label: 'Late' },
    { value: AttendanceStatus.Excused, label: 'Excused' }
  ];

  constructor(
    private attendanceService: AttendanceService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (!idParam) return;

    this.isEditMode.set(true);
    const id = Number(idParam);
    this.attendanceId.set(id);
    this.isLoading.set(true);

    this.attendanceService.getById(id).subscribe({
      next: (res) => {
        const record = res.data;
        this.status.set(record.status);
        this.remarks.set(record.remarks ?? '');
        this.date.set(record.date.substring(0, 10));
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load attendance record.');
        this.isLoading.set(false);
      }
    });
  }

  onSubmit(): void {
    this.errorMessage.set(null);

    if (this.isEditMode()) {
      this.isSaving.set(true);
      this.attendanceService.update(this.attendanceId()!, {
        status: this.status(),
        remarks: this.remarks() || undefined
      }).subscribe({
        next: () => this.router.navigate(['/attendance']),
        error: (err: unknown) => {
          this.errorMessage.set(this.extractErrorMessage(err));
          this.isSaving.set(false);
        }
      });
      return;
    }

    if (!this.selectedStudentId() || !this.selectedCourseId()) {
      this.errorMessage.set('Select a student and a course.');
      return;
    }

    this.isSaving.set(true);
    this.attendanceService.mark({
      studentId: this.selectedStudentId()!,
      courseId: this.selectedCourseId()!,
      date: this.date(),
      status: this.status(),
      remarks: this.remarks() || undefined
    }).subscribe({
      next: () => this.router.navigate(['/attendance']),
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
    return 'Save failed.';
  }
}
