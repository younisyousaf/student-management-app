import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment.development';
import { ApiResponse } from '../../core/models/api-response.model';
import { Enrollment, EnrollStudentRequest } from '../../core/models/enrollment.model';

@Injectable({ providedIn: 'root' })
export class EnrollmentsService {
  private readonly baseUrl = `${environment.apiUrl}/Enrollments`;

  constructor(private http: HttpClient) {}

  enroll(payload: EnrollStudentRequest) {
    return this.http.post<ApiResponse<null>>(this.baseUrl, payload);
  }

  drop(enrollmentId: number) {
    return this.http.delete<ApiResponse<null>>(`${this.baseUrl}/${enrollmentId}`);
  }

  complete(enrollmentId: number) {
    return this.http.post<ApiResponse<null>>(`${this.baseUrl}/complete`, { enrollmentId });
  }
}
