import { httpResource } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideMail, LucidePencil, LucidePhone, LucidePlus, LucideTrash2, LucideTriangleAlert, LucideUserRound, LucideUsers, LucideX } from '@lucide/angular';
import { environment } from '../../../../environments/environment.development';
import { ApiResponse } from '../../../core/models/api-response.model';
import { Student } from '../../../core/models/student.model';
import { ToastService } from '../../../core/services/toast.service';
import { Pagination } from '../../../shared/components/pagination/pagination';
import { PaginatedResult } from '../../../shared/models/paginated-result.model';
import { StudentsService } from '../students.service';

@Component({
  selector: 'app-student-list',
  standalone: true,
  imports: [RouterLink, Pagination, LucideMail, LucidePencil, LucidePhone, LucidePlus, LucideTrash2, LucideTriangleAlert, LucideUserRound, LucideUsers, LucideX],
  templateUrl: './student-list.html',
  styleUrl: './student-list.scss'
})
export class StudentList {
  private readonly studentsService = inject(StudentsService);
  private readonly toastService = inject(ToastService);

  readonly pageNumber = signal(1);
  readonly pageSize = 10;
  readonly studentPendingDelete = signal<Student | null>(null);
  readonly isDeleting = signal(false);

  readonly studentsResource = httpResource<ApiResponse<PaginatedResult<Student>>>(() =>
    `${environment.apiUrl}/Students/paged?pageNumber=${this.pageNumber()}&pageSize=${this.pageSize}`
  );

  changePage(pageNumber: number): void {
    this.pageNumber.set(pageNumber);
  }

  requestDelete(student: Student): void {
    this.studentPendingDelete.set(student);
  }

  cancelDelete(): void {
    if (!this.isDeleting()) this.studentPendingDelete.set(null);
  }

  deleteStudent(): void {
    const student = this.studentPendingDelete();
    if (!student || this.isDeleting()) return;

    this.isDeleting.set(true);
    this.studentsService.delete(student.id).subscribe({
      next: () => {
        const items = this.studentsResource.value()?.data?.items ?? [];
        this.studentPendingDelete.set(null);
        this.toastService.success('Student deleted', `${student.fullName} was removed successfully.`);

        if (items.length === 1 && this.pageNumber() > 1) this.pageNumber.update(page => page - 1);
        else this.studentsResource.reload();
      },
      error: err => this.toastService.error('Delete failed', err.error?.message ?? 'The student could not be deleted.'),
      complete: () => this.isDeleting.set(false)
    });
  }
}
