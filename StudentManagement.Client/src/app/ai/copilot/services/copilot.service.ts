import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment.development';
import {
  CopilotChatRequest,
  CopilotChatResponse
} from '../models/copilot.model';
@Injectable({
  providedIn: 'root'
})
export class CopilotService {
  private readonly baseUrl =
    `${environment.apiUrl}/Copilot`;
  constructor(
    private readonly http: HttpClient
  ) {}
  sendMessage(
    request: CopilotChatRequest
  ): Observable<CopilotChatResponse> {
    return this.http.post<CopilotChatResponse>(
      `${this.baseUrl}/chat`,
      request,
      {
        withCredentials: true
      }
    );
  }
}
