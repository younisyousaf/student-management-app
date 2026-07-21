import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment.development';
import { ApiResponse } from '../../core/models/api-response.model';
import { Student, CreateStudentRequest, UpdateStudentRequest } from '../../core/models/student.model';

@Injectable({ providedIn: 'root' })
export class StudentsService {
  private readonly baseUrl = `${environment.apiUrl}/Students`;

  constructor(private http: HttpClient) {}

  getById(id: number) {
    return this.http.get<ApiResponse<Student>>(`${this.baseUrl}/${id}`);
  }

  create(payload: CreateStudentRequest) {
    return this.http.post<ApiResponse<Student>>(this.baseUrl, payload);
  }

  update(id: number, payload: UpdateStudentRequest) {
  return this.http.put<ApiResponse<null>>(`${this.baseUrl}/${id}`, payload);
}

  delete(id: number) {
    return this.http.delete<ApiResponse<null>>(`${this.baseUrl}/${id}`);
  }
}
