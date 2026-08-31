import { CurrencyPipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideBookOpen, LucideClock3, LucidePencil, LucidePlus, LucideTrash2, LucideTriangleAlert, LucideX } from '@lucide/angular';
import { environment } from '../../../../environments/environment.development';
import { ApiResponse } from '../../../core/models/api-response.model';
import { Course } from '../../../core/models/course.model';
import { ToastService } from '../../../core/services/toast.service';
import { Pagination } from '../../../shared/components/pagination/pagination';
import { PaginatedResult } from '../../../shared/models/paginated-result.model';
import { CoursesService } from '../course.service';
import { ConfirmDialog } from '../../../shared/components/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-course-list',
  standalone: true,
  imports: [RouterLink, CurrencyPipe, Pagination, LucideBookOpen, LucideClock3, LucidePencil, LucidePlus, LucideTrash2, LucideTriangleAlert, ConfirmDialog],
  templateUrl: './course-list.html',
  styleUrl: './course-list.scss'
})
export class CourseList {
  private readonly coursesService = inject(CoursesService);
  private readonly toastService = inject(ToastService);

  readonly pageNumber = signal(1);
  readonly pageSize = 10;
  readonly coursePendingDelete = signal<Course | null>(null);
  readonly isDeleting = signal(false);

  readonly coursesResource = httpResource<ApiResponse<PaginatedResult<Course>>>(() =>
    `${environment.apiUrl}/Courses/paged?pageNumber=${this.pageNumber()}&pageSize=${this.pageSize}`
  );

  changePage(pageNumber: number): void {
    this.pageNumber.set(pageNumber);
  }

  requestDelete(course: Course): void {
    this.coursePendingDelete.set(course);
  }

  cancelDelete(): void {
    if (!this.isDeleting()) this.coursePendingDelete.set(null);
  }

  deleteCourse(): void {
    const course = this.coursePendingDelete();
    if (!course || this.isDeleting()) return;

    this.isDeleting.set(true);
    this.coursesService.delete(course.id).subscribe({
      next: () => {
        const items = this.coursesResource.value()?.data?.items ?? [];
        this.coursePendingDelete.set(null);
        this.toastService.success('Course deleted', `${course.name} was removed successfully.`);

        if (items.length === 1 && this.pageNumber() > 1) this.pageNumber.update(page => page - 1);
        else this.coursesResource.reload();
      },
      error: err => {
        this.isDeleting.set(false);
        this.toastService.error('Delete failed', this.extractErrorMessage(err));
      },
      complete: () => this.isDeleting.set(false)
    });
  }

  private extractErrorMessage(err: unknown): string {
    if (err && typeof err === 'object' && 'error' in err) {
      const httpError = (err as { error?: { message?: string } }).error;
      if (httpError?.message) return httpError.message;
    }
    return 'The course could not be deleted.';
  }
}
