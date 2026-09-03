import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  CreateSubmissionRequest,
  CreateTemplateRequest,
  FormTemplateDetails,
  FormTemplateSummary,
  SubmissionResponse,
  WorkflowInstance,
  CreateWorkflowTemplateRequest,
  StatusValue,
  WorkflowTemplateSummary,
} from '../models/form.models';

@Injectable({ providedIn: 'root' })
export class FormsApiService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5080/api';

  listTemplates(): Observable<readonly FormTemplateSummary[]> {
    return this.http.get<readonly FormTemplateSummary[]>(`${this.apiUrl}/form-templates`);
  }

  getTemplate(id: number): Observable<FormTemplateDetails> {
    return this.http.get<FormTemplateDetails>(`${this.apiUrl}/form-templates/${id}`);
  }

  createTemplate(request: CreateTemplateRequest): Observable<FormTemplateDetails> {
    return this.http.post<FormTemplateDetails>(`${this.apiUrl}/form-templates`, request);
  }

  publishTemplate(id: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/form-templates/${id}/publish`, {});
  }

  submitForm(templateId: number, request: CreateSubmissionRequest): Observable<SubmissionResponse> {
    return this.http.post<SubmissionResponse>(
      `${this.apiUrl}/form-templates/${templateId}/submissions`,
      request,
    );
  }

  getWorkflow(id: number): Observable<WorkflowInstance> {
    return this.http.get<WorkflowInstance>(`${this.apiUrl}/workflows/${id}`);
  }

  listWorkflowTemplates(): Observable<readonly WorkflowTemplateSummary[]> {
    return this.http.get<readonly WorkflowTemplateSummary[]>(`${this.apiUrl}/workflow-templates`);
  }

  createWorkflowTemplate(request: CreateWorkflowTemplateRequest): Observable<WorkflowTemplateSummary> {
    return this.http.post<WorkflowTemplateSummary>(`${this.apiUrl}/workflow-templates`, request);
  }

  publishWorkflowTemplate(id: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/workflow-templates/${id}/publish`, {});
  }

  listStatuses(statusType?: string): Observable<readonly StatusValue[]> {
    const suffix = statusType ? `?statusType=${encodeURIComponent(statusType)}` : '';
    return this.http.get<readonly StatusValue[]>(`${this.apiUrl}/statuses${suffix}`);
  }

  updateStatus(id: number, request: Pick<StatusValue, 'displayText' | 'displayOrder'> & { isActive: boolean }): Observable<StatusValue> {
    return this.http.put<StatusValue>(`${this.apiUrl}/statuses/${id}`, request);
  }
}
