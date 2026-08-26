import { Component, signal } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { environment } from '../../../../environments/environment.development';
import { ApiResponse } from '../../../core/models/api-response.model';
import { PaginatedResult } from '../../../shared/models/paginated-result.model';
import { Pagination } from '../../../shared/components/pagination/pagination';
import { Course } from '../../../core/models/course.model';
import { CoursesService } from '../course.service';
import { CurrencyPipe } from '@angular/common';
@Component({
  selector: 'app-course-list',
  standalone: true,
  imports: [RouterLink, CurrencyPipe, Pagination],
  templateUrl: './course-list.html'
})
export class CourseList {
  readonly pageNumber = signal(1);
  readonly pageSize = 10;
  coursesResource =
    httpResource<
      ApiResponse<
        PaginatedResult<Course>
      >
    >(
      () =>
        `${environment.apiUrl}/Courses/paged?pageNumber=${this.pageNumber()}&pageSize=${this.pageSize}`
    );
  constructor(private coursesService: CoursesService) { }
  changePage(pageNumber: number): void {
    this.pageNumber.set(pageNumber);
  }
  deleteCourse(id: number): void {
    if (!confirm('Delete this course?')) return;
    this.coursesService.delete(id).subscribe({
      next: () => {
        const items = this.coursesResource.value()?.data?.items ?? [];
        if (items.length === 1 && this.pageNumber() > 1) {
          this.pageNumber.update(page => page - 1);
        } else {
          this.coursesResource.reload();
        }
      },
      error: (err: unknown) => alert(this.extractErrorMessage(err))
    });
  }
  private extractErrorMessage(err: unknown): string {
    if (err && typeof err === 'object' && 'error' in err) {
      const httpError = (err as { error?: { message?: string } }).error;
      if (httpError?.message) return httpError.message;
    }
    return 'Delete failed.';
  }
}
