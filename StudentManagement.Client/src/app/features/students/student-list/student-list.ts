import { Component } from '@angular/core';
import { httpResource } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { environment } from '../../../../environments/environment.development';
import { ApiResponse } from '../../../core/models/api-response.model';
import { Student } from '../../../core/models/student.model';
import { StudentsService } from '../students.service';

@Component({
  selector: 'app-student-list',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './student-list.html'
})
export class StudentList {
  studentsResource = httpResource<ApiResponse<Student[]>>(() => `${environment.apiUrl}/Students`);

  constructor(private studentsService: StudentsService) {}

  deleteStudent(id: number): void {
    if (!confirm('Delete this student?')) return;

    this.studentsService.delete(id).subscribe({
      next: () => this.studentsResource.reload(),
      error: (err) => alert(err.error?.message ?? 'Delete failed.')
    });
  }
}
