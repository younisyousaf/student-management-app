import { httpResource } from '@angular/common/http';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LucideArrowLeft, LucideBookOpen, LucideCalendarCheck, LucideCalendarDays, LucideSave, LucideUserRound } from '@lucide/angular';
import { environment } from '../../../../environments/environment.development';
import { ApiResponse } from '../../../core/models/api-response.model';
import { AttendanceStatus } from '../../../core/models/attendance.model';
import { Course } from '../../../core/models/course.model';
import { Student } from '../../../core/models/student.model';
import { ToastService } from '../../../core/services/toast.service';
import { AttendanceService } from '../attendance.service';

@Component({
  selector: 'app-attendance-form',
  standalone: true,
  imports: [FormsModule, RouterLink, LucideArrowLeft, LucideBookOpen, LucideCalendarCheck, LucideCalendarDays, LucideSave, LucideUserRound],
  templateUrl: './attendance-form.html',
  styleUrl: './attendance-form.scss'
})
export class AttendanceForm implements OnInit {
  private readonly attendanceService = inject(AttendanceService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toastService = inject(ToastService);

  readonly isEditMode = signal(false);
  readonly attendanceId = signal<number | null>(null);
  readonly studentsResource = httpResource<ApiResponse<Student[]>>(() => `${environment.apiUrl}/Students`);
  readonly coursesResource = httpResource<ApiResponse<Course[]>>(() => `${environment.apiUrl}/Courses`);

  readonly selectedStudentId = signal<number | null>(null);
  readonly selectedCourseId = signal<number | null>(null);
  readonly date = signal(this.todayInCampusTimeZone());
  readonly status = signal<AttendanceStatus>(AttendanceStatus.Present);
  readonly remarks = signal('');
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);

  readonly selectedStudent = computed(() =>
    (this.studentsResource.value()?.data ?? []).find(student => student.id === this.selectedStudentId()) ?? null
  );

  readonly selectedCourse = computed(() =>
    (this.coursesResource.value()?.data ?? []).find(course => course.id === this.selectedCourseId()) ?? null
  );

  readonly statusOptions = [
    { value: AttendanceStatus.Present, label: 'Present' },
    { value: AttendanceStatus.Absent, label: 'Absent' },
    { value: AttendanceStatus.Late, label: 'Late' },
    { value: AttendanceStatus.Excused, label: 'Excused' }
  ];

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (!idParam) return;

    const id = Number(idParam);
    this.isEditMode.set(true);
    this.attendanceId.set(id);
    this.isLoading.set(true);

    this.attendanceService.getById(id).subscribe({
      next: res => {
        const record = res.data;
        this.selectedStudentId.set(record.studentId);
        this.selectedCourseId.set(record.courseId);
        this.date.set(record.date.substring(0, 10));
        this.status.set(record.status);
        this.remarks.set(record.remarks ?? '');
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.toastService.error('Unable to load attendance', 'The attendance record could not be loaded.');
      }
    });
  }

  onSubmit(): void {
    if (this.isEditMode()) {
      this.updateAttendance();
      return;
    }

    const studentId = this.selectedStudentId();
    const courseId = this.selectedCourseId();

    if (!studentId || !courseId) {
      this.toastService.warning('Selection required', 'Select both a student and a course.');
      return;
    }

    this.isSaving.set(true);
    this.attendanceService.mark({
      studentId,
      courseId,
      date: this.date(),
      status: this.status(),
      remarks: this.remarks() || undefined
    }).subscribe({
      next: () => {
        this.toastService.success('Attendance marked', 'The attendance record was saved successfully.');
        this.router.navigate(['/attendance']);
      },
      error: err => {
        this.isSaving.set(false);
        this.toastService.error('Save failed', this.extractErrorMessage(err));
      }
    });
  }

  private updateAttendance(): void {
    this.isSaving.set(true);

    this.attendanceService.update(this.attendanceId()!, {
      status: this.status(),
      remarks: this.remarks() || undefined
    }).subscribe({
      next: () => {
        this.toastService.success('Attendance updated', 'The attendance record was updated successfully.');
        this.router.navigate(['/attendance']);
      },
      error: err => {
        this.isSaving.set(false);
        this.toastService.error('Save failed', this.extractErrorMessage(err));
      }
    });
  }

  private todayInCampusTimeZone(): string {
    const parts = new Intl.DateTimeFormat('en-US', {
      timeZone: environment.appTimeZone,
      year: 'numeric',
      month: '2-digit',
      day: '2-digit'
    }).formatToParts(new Date());

    const value = (type: string) => parts.find(part => part.type === type)?.value ?? '';
    return `${value('year')}-${value('month')}-${value('day')}`;
  }

  private extractErrorMessage(err: unknown): string {
    if (err && typeof err === 'object' && 'error' in err) {
      const httpError = (err as { error?: { message?: string } }).error;
      if (httpError?.message) return httpError.message;
    }
    return 'The attendance record could not be saved.';
  }
}
