import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment.development';
import { ApiResponse } from '../../core/models/api-response.model';
import { Fee, ProcessPaymentRequest } from '../../core/models/fee.model';

@Injectable({ providedIn: 'root' })
export class FeesService {
  private readonly baseUrl = `${environment.apiUrl}/Fees`;

  constructor(private http: HttpClient) {}

  getStatement(studentId: number, courseId: number) {
    return this.http.get<ApiResponse<Fee>>(`${this.baseUrl}/statement`, {
      params: { studentId, courseId }
    });
  }

  pay(payload: ProcessPaymentRequest) {
    return this.http.post<ApiResponse<null>>(`${this.baseUrl}/pay`, payload);
  }
}
