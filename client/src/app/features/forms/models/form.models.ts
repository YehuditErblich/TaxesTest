export type FieldType =
  | 'Text'
  | 'TextArea'
  | 'Number'
  | 'Date'
  | 'Checkbox'
  | 'Radio'
  | 'Select'
  | 'MultiSelect'
  | 'File';

export interface FormTemplateSummary {
  readonly id: number;
  readonly name: string;
  readonly status: string;
  readonly createdAt: string;
  readonly publishedAt: string | null;
}

export interface FieldOption {
  readonly id: number;
  readonly value: string;
  readonly label: string;
  readonly displayOrder: number;
}

export interface FormField {
  readonly id: number;
  readonly fieldType: FieldType;
  readonly name: string;
  readonly label: string;
  readonly isRequired: boolean;
  readonly displayOrder: number;
  readonly options: readonly FieldOption[];
}

export interface FormTemplateDetails {
  readonly id: number;
  readonly name: string;
  readonly description: string | null;
  readonly createdByUserId: string;
  readonly createdAt: string;
  readonly publishedAt: string | null;
  readonly status: string;
  readonly workflowTemplateId: number | null;
  readonly workflow: WorkflowTemplateDetails | null;
  readonly fields: readonly FormField[];
}

export interface CreateTemplateFieldRequest {
  readonly fieldType: FieldType;
  readonly name: string;
  readonly label: string;
  readonly placeholder: string | null;
  readonly helpText: string | null;
  readonly isRequired: boolean;
  readonly isReadOnly: boolean;
  readonly displayOrder: number;
  readonly options: readonly CreateTemplateOptionRequest[];
}

export interface CreateTemplateOptionRequest {
  readonly value: string;
  readonly label: string;
  readonly displayOrder: number;
}

export interface CreateTemplateRequest {
  readonly name: string;
  readonly description: string | null;
  readonly createdByUserId: string;
  readonly workflowTemplateId: number | null;
  readonly workflow: CreateWorkflowTemplateRequest | null;
  readonly fields: readonly CreateTemplateFieldRequest[];
}

export interface SubmissionResponse {
  readonly id: number;
  readonly formTemplateId: number;
  readonly workflowInstanceId: number | null;
  readonly status: string;
  readonly createdAt: string;
  readonly submittedAt: string | null;
}

export interface WorkflowTemplateSummary {
  readonly id: number;
  readonly name: string;
  readonly status: string;
  readonly createdAt: string;
  readonly publishedAt: string | null;
}

export interface WorkflowTemplateDetails {
  readonly id: number;
  readonly name: string;
  readonly description: string | null;
  readonly status: string;
  readonly steps: readonly WorkflowStepDetails[];
}

export interface WorkflowStepDetails {
  readonly id: number;
  readonly stepOrder: number;
  readonly name: string;
  readonly description: string | null;
  readonly approverType: 'User' | 'Role';
  readonly approverUserId: string | null;
  readonly approverRoleId: number | null;
  readonly isRequired: boolean;
  readonly allowedActions: readonly string[];
}

export interface StatusValue {
  readonly id: number;
  readonly statusTypeCode: string;
  readonly valueCode: string;
  readonly displayText: string;
  readonly displayOrder: number;
}

export interface CreateWorkflowTemplateRequest {
  readonly name: string;
  readonly description: string | null;
  readonly createdByUserId: string;
  readonly steps: readonly CreateWorkflowStepRequest[];
}

export interface CreateWorkflowStepRequest {
  readonly stepOrder: number;
  readonly name: string;
  readonly description: string | null;
  readonly approverType: 'User' | 'Role';
  readonly approverUserId: string | null;
  readonly approverRoleId: number | null;
  readonly isRequired: boolean;
  readonly allowedActions: readonly string[];
}

export interface SubmissionValueRequest {
  readonly fieldName: string;
  readonly value: string | null;
}

export interface SelectedOptionRequest {
  readonly fieldName: string;
  readonly fieldOptionIds: readonly number[];
}

export interface CreateSubmissionRequest {
  readonly submittedByUserId: string;
  readonly values: readonly SubmissionValueRequest[];
  readonly selectedOptions: readonly SelectedOptionRequest[];
}

export interface WorkflowStepInstance {
  readonly id: number;
  readonly workflowStepId: number;
  readonly assignedToUserId: string;
  readonly status: string;
  readonly stepOrder: number;
  readonly startedAt: string;
  readonly completedAt: string | null;
}

export interface WorkflowAction {
  readonly id: number;
  readonly workflowStepInstanceId: number;
  readonly actionType: string;
  readonly performedByUserId: string;
  readonly performedAt: string;
  readonly comment: string | null;
}

export interface WorkflowInstance {
  readonly id: number;
  readonly formSubmissionId: number;
  readonly status: string;
  readonly currentStepOrder: number;
  readonly startedAt: string;
  readonly completedAt: string | null;
  readonly steps: readonly WorkflowStepInstance[];
  readonly actions: readonly WorkflowAction[];
}
