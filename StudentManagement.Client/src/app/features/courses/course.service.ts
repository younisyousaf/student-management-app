import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment.development';
import { ApiResponse } from '../../core/models/api-response.model';
import { Course, CourseRequest } from '../../core/models/course.model';

@Injectable({ providedIn: 'root' })
export class CoursesService {
  private readonly baseUrl = `${environment.apiUrl}/Courses`;

  constructor(private http: HttpClient) {}

  getById(id: number) {
    return this.http.get<ApiResponse<Course>>(`${this.baseUrl}/${id}`);
  }

  create(payload: CourseRequest) {
    return this.http.post<ApiResponse<Course>>(this.baseUrl, payload);
  }

  update(id: number, payload: CourseRequest) {
    return this.http.put<ApiResponse<null>>(`${this.baseUrl}/${id}`, payload);
  }

  delete(id: number) {
    return this.http.delete<ApiResponse<null>>(`${this.baseUrl}/${id}`);
  }
}
