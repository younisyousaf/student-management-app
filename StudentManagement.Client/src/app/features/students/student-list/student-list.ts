import { Component, signal } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { environment } from '../../../../environments/environment.development';
import { ApiResponse } from '../../../core/models/api-response.model';
import { Student } from '../../../core/models/student.model';
import { PaginatedResult } from '../../../shared/models/paginated-result.model';
import { Pagination } from '../../../shared/components/pagination/pagination';
import { StudentsService } from '../students.service';
@Component({
  selector: 'app-student-list',
  standalone: true,
  imports: [
    RouterLink,
    Pagination
  ],
  templateUrl: './student-list.html'
})
export class StudentList {
  readonly pageNumber = signal(1);
  readonly pageSize = 10;
  studentsResource =
    httpResource<
      ApiResponse<
        PaginatedResult<Student>
      >
    >(
      () =>
        `${environment.apiUrl}/Students/paged?pageNumber=${this.pageNumber()}&pageSize=${this.pageSize}`
    );
  constructor(
    private studentsService:
      StudentsService
  ) { }
  changePage(
    pageNumber: number
  ): void {
    this.pageNumber.set(
      pageNumber
    );
  }
  deleteStudent(
    id: number
  ): void {
    if (!confirm('Delete this student?')) {
      return;
    }
    this.studentsService.delete(id).subscribe({
        next: () => {
          const items =
            this.studentsResource
              .value()
              ?.data
              ?.items ?? [];
          if (
            items.length === 1 &&
            this.pageNumber() > 1
          ) {
            this.pageNumber.update(
              page => page - 1
            );
          } else {
            this.studentsResource
              .reload();
          }
        },
        error: err =>
          alert(
            err.error?.message ??
            'Delete failed.'
          )
      });
  }
}
