import { Component } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { environment } from '../../../../environments/environment.development';
import { ApiResponse } from '../../../core/models/api-response.model';
import { Course } from '../../../core/models/course.model';
import { CoursesService } from '../course.service';
import { CurrencyPipe } from '@angular/common';

@Component({
  selector: 'app-course-list',
  standalone: true,
  imports: [RouterLink, CurrencyPipe],
  templateUrl: './course-list.html'
})
export class CourseList {
  coursesResource = httpResource<ApiResponse<Course[]>>(() => `${environment.apiUrl}/Courses`);

  constructor(private coursesService: CoursesService) {}

  deleteCourse(id: number): void {
    if (!confirm('Delete this course?')) return;

    this.coursesService.delete(id).subscribe({
      next: () => this.coursesResource.reload(),
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
