import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment.development';
import { ApiResponse } from '../../core/models/api-response.model';
import { Attendance, MarkAttendanceRequest, UpdateAttendanceRequest } from '../../core/models/attendance.model';

@Injectable({ providedIn: 'root' })
export class AttendanceService {
  private readonly baseUrl = `${environment.apiUrl}/Attendance`;

  constructor(private http: HttpClient) {}

  mark(payload: MarkAttendanceRequest) {
    return this.http.post<ApiResponse<null>>(this.baseUrl, payload);
  }

  update(id: number, payload: UpdateAttendanceRequest) {
    return this.http.put<ApiResponse<null>>(`${this.baseUrl}/${id}`, payload);
  }

  getById(id: number) {
    return this.http.get<ApiResponse<Attendance>>(`${this.baseUrl}/${id}`);
  }
}
