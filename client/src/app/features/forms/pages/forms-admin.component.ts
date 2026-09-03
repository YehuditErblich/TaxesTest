import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';

import { FormsApiService } from '../data-access/forms-api.service';
import {
  CreateTemplateRequest,
  CreateTemplateOptionRequest,
  CreateTemplateFieldRequest,
  CreateWorkflowStepRequest,
  CreateWorkflowTemplateRequest,
  FieldType,
  FormTemplateSummary,
  StatusValue,
  WorkflowTemplateSummary,
} from '../models/form.models';

type AdminTab = 'forms' | 'workflows' | 'statuses';

type OptionControls = {
  value: FormControl<string>;
  label: FormControl<string>;
};

type FieldControls = {
  fieldType: FormControl<FieldType>;
  name: FormControl<string>;
  label: FormControl<string>;
  placeholder: FormControl<string>;
  helpText: FormControl<string>;
  isRequired: FormControl<boolean>;
  isReadOnly: FormControl<boolean>;
  options: FormArray<FormGroup<OptionControls>>;
};

type StepControls = {
  name: FormControl<string>;
  description: FormControl<string>;
  approverType: FormControl<'User' | 'Role'>;
  approver: FormControl<string>;
  isRequired: FormControl<boolean>;
  approve: FormControl<boolean>;
  reject: FormControl<boolean>;
  returnForCorrection: FormControl<boolean>;
};

@Component({
  selector: 'app-forms-admin',
  imports: [DatePipe, ReactiveFormsModule, RouterLink],
  templateUrl: './forms-admin.component.html',
  styleUrl: './forms-admin.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FormsAdminComponent implements OnInit {
  private readonly api = inject(FormsApiService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly activeTab = signal<AdminTab>('forms');
  protected readonly testRoles = [
    { id: 1, name: 'מנהל כספים' },
    { id: 2, name: 'מנהל משאבי אנוש' },
    { id: 3, name: 'מנהל תפעול' },
  ] as const;
  protected readonly forms = signal<readonly FormTemplateSummary[]>([]);
  protected readonly workflows = signal<readonly WorkflowTemplateSummary[]>([]);
  protected readonly statuses = signal<readonly StatusValue[]>([]);
  protected readonly isLoading = signal(true);
  protected readonly isSaving = signal(false);
  protected readonly message = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly formForm = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: Validators.required }),
    description: new FormControl('', { nonNullable: true }),
    fields: new FormArray<FormGroup<FieldControls>>([]),
  });
  protected readonly workflowForm = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: Validators.required }),
    description: new FormControl('', { nonNullable: true }),
    steps: new FormArray<FormGroup<StepControls>>([]),
  });

  ngOnInit(): void {
    this.addField();
    this.addWorkflowStep();
    this.loadAll();
  }

  protected get fields(): FormArray<FormGroup<FieldControls>> { return this.formForm.controls.fields; }
  protected get steps(): FormArray<FormGroup<StepControls>> { return this.workflowForm.controls.steps; }

  protected addField(): void {
    this.fields.push(new FormGroup<FieldControls>({
      fieldType: new FormControl<FieldType>('Text', { nonNullable: true }),
      name: new FormControl('', { nonNullable: true, validators: Validators.required }),
      label: new FormControl('', { nonNullable: true, validators: Validators.required }),
      placeholder: new FormControl('', { nonNullable: true }),
      helpText: new FormControl('', { nonNullable: true }),
      isRequired: new FormControl(false, { nonNullable: true }),
      isReadOnly: new FormControl(false, { nonNullable: true }),
      options: new FormArray<FormGroup<OptionControls>>([]),
    }));
  }

  protected removeField(index: number): void {
    if (this.fields.length > 1) this.fields.removeAt(index);
  }

  protected moveField(index: number, direction: -1 | 1): void {
    const next = index + direction;
    if (next < 0 || next >= this.fields.length) return;
    const field = this.fields.at(index);
    this.fields.removeAt(index);
    this.fields.insert(next, field);
  }

  protected options(fieldIndex: number): FormArray<FormGroup<OptionControls>> {
    return this.fields.at(fieldIndex).controls.options;
  }

  protected addOption(fieldIndex: number): void {
    this.options(fieldIndex).push(new FormGroup<OptionControls>({
      value: new FormControl('', { nonNullable: true, validators: Validators.required }),
      label: new FormControl('', { nonNullable: true, validators: Validators.required }),
    }));
  }

  protected removeOption(fieldIndex: number, optionIndex: number): void {
    this.options(fieldIndex).removeAt(optionIndex);
  }

  protected addWorkflowStep(): void {
    this.steps.push(new FormGroup<StepControls>({
      name: new FormControl('', { nonNullable: true, validators: Validators.required }),
      description: new FormControl('', { nonNullable: true }),
      approverType: new FormControl<'User' | 'Role'>('User', { nonNullable: true }),
      approver: new FormControl('', { nonNullable: true, validators: Validators.required }),
      isRequired: new FormControl(true, { nonNullable: true }),
      approve: new FormControl(true, { nonNullable: true }),
      reject: new FormControl(true, { nonNullable: true }),
      returnForCorrection: new FormControl(true, { nonNullable: true }),
    }));
  }

  protected removeWorkflowStep(index: number): void {
    if (this.steps.length > 1) this.steps.removeAt(index);
  }

  protected moveWorkflowStep(index: number, direction: -1 | 1): void {
    const next = index + direction;
    if (next < 0 || next >= this.steps.length) return;
    const step = this.steps.at(index);
    this.steps.removeAt(index);
    this.steps.insert(next, step);
  }

  protected selectTab(tab: AdminTab): void {
    this.activeTab.set(tab);
    this.message.set(null);
    this.error.set(null);
  }

  protected createForm(): void {
    this.error.set(null);
    const formName = this.formForm.controls.name.value.trim();
    if (!this.workflowForm.controls.name.value.trim() && formName) {
      this.workflowForm.controls.name.setValue(`${formName} - מסלול אישורים`);
    }

    if (this.formForm.invalid || this.workflowForm.invalid) {
      this.formForm.markAllAsTouched();
      this.workflowForm.markAllAsTouched();
      this.error.set('לא ניתן לשמור: יש להשלים את שם הטופס, פרטי השדות, שם כל שלב וזהות המאשר.');
      return;
    }

    const request: CreateTemplateRequest = {
      name: this.formForm.controls.name.value,
      description: this.formForm.controls.description.value || null,
      createdByUserId: 'admin-user',
      workflowTemplateId: null,
      workflow: this.buildWorkflowRequest(),
      fields: this.fields.controls.map((control, index): CreateTemplateFieldRequest => ({
        fieldType: control.controls.fieldType.value,
        name: control.controls.name.value,
        label: control.controls.label.value,
        placeholder: control.controls.placeholder.value || null,
        helpText: control.controls.helpText.value || null,
        isRequired: control.controls.isRequired.value,
        isReadOnly: control.controls.isReadOnly.value,
        displayOrder: index + 1,
        options: control.controls.options.controls.map((option, optionIndex): CreateTemplateOptionRequest => ({
          value: option.controls.value.value,
          label: option.controls.label.value,
          displayOrder: optionIndex + 1,
        })),
      })),
    };
    this.isSaving.set(true);
    this.api.createTemplate(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.formForm.reset();
        this.workflowForm.reset();
        this.message.set('הטופס נוצר כטיוטה.');
        this.isSaving.set(false);
        this.loadAll();
      },
      error: (error: HttpErrorResponse) => this.saveFailed(error),
    });
  }

  protected createWorkflow(): void {
    this.error.set(null);
    if (this.workflowForm.invalid) {
      this.workflowForm.markAllAsTouched();
      this.error.set('לא ניתן לשמור: יש להשלים את שם המסלול, שם כל שלב וזהות המאשר.');
      return;
    }

    const request: CreateWorkflowTemplateRequest = {
      name: this.workflowForm.controls.name.value,
      description: null,
      createdByUserId: 'admin-user',
      steps: this.steps.controls.map((control, index): CreateWorkflowStepRequest => ({
        stepOrder: index + 1,
        name: control.controls.name.value,
        description: control.controls.description.value || null,
        approverType: control.controls.approverType.value,
        approverUserId: control.controls.approverType.value === 'User' ? control.controls.approver.value : null,
        approverRoleId: control.controls.approverType.value === 'Role' ? Number(control.controls.approver.value) : null,
        isRequired: control.controls.isRequired.value,
        allowedActions: [
          control.controls.approve.value ? 'Approve' : null,
          control.controls.reject.value ? 'Reject' : null,
          control.controls.returnForCorrection.value ? 'ReturnForCorrection' : null,
        ].filter((action): action is string => action !== null),
      })),
    };
    this.isSaving.set(true);
    this.api.createWorkflowTemplate(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.workflowForm.reset();
        this.message.set('תהליך האישור נוצר כטיוטה.');
        this.isSaving.set(false);
        this.loadAll();
      },
      error: (error: HttpErrorResponse) => this.saveFailed(error),
    });
  }

  private buildWorkflowRequest(): CreateWorkflowTemplateRequest {
    return {
      name: this.workflowForm.controls.name.value,
      description: this.workflowForm.controls.description.value || null,
      createdByUserId: 'admin-user',
      steps: this.steps.controls.map((control, index): CreateWorkflowStepRequest => ({
        stepOrder: index + 1,
        name: control.controls.name.value,
        description: control.controls.description.value || null,
        approverType: control.controls.approverType.value,
        approverUserId: control.controls.approverType.value === 'User' ? control.controls.approver.value : null,
        approverRoleId: control.controls.approverType.value === 'Role' ? Number(control.controls.approver.value) : null,
        isRequired: control.controls.isRequired.value,
        allowedActions: [
          control.controls.approve.value ? 'Approve' : null,
          control.controls.reject.value ? 'Reject' : null,
          control.controls.returnForCorrection.value ? 'ReturnForCorrection' : null,
        ].filter((action): action is string => action !== null),
      })),
    };
  }

  protected updateStatus(status: StatusValue, displayText: string): void {
    const nextText = displayText.trim();
    if (!nextText) {
      return;
    }

    this.api.updateStatus(status.id, {
      displayText: nextText,
      displayOrder: status.displayOrder,
      isActive: true,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.message.set('הסטטוס עודכן.');
        this.loadStatuses();
      },
      error: () => this.error.set('לא ניתן לעדכן את הסטטוס כרגע.'),
    });
  }

  protected publishForm(id: number): void {
    this.api.publishTemplate(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { this.message.set('הטופס פורסם.'); this.loadAll(); },
      error: () => this.error.set('לא ניתן לפרסם את הטופס.'),
    });
  }

  protected publishWorkflow(id: number): void {
    this.api.publishWorkflowTemplate(id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { this.message.set('תהליך האישור פורסם.'); this.loadAll(); },
      error: () => this.error.set('לא ניתן לפרסם את תהליך האישור.'),
    });
  }

  private loadAll(): void {
    this.isLoading.set(true);
    this.error.set(null);
    this.api.listTemplates().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (forms) => { this.forms.set(forms); this.isLoading.set(false); },
      error: () => { this.error.set('לא ניתן לטעון את הטפסים.'); this.isLoading.set(false); },
    });
    this.api.listWorkflowTemplates().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (workflows) => this.workflows.set(workflows),
      error: () => this.error.set('לא ניתן לטעון את תהליכי האישור.'),
    });
    this.loadStatuses();
  }

  private loadStatuses(): void {
    this.api.listStatuses().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (statuses) => this.statuses.set(statuses),
      error: () => this.error.set('לא ניתן לטעון את הסטטוסים.'),
    });
  }

  private saveFailed(error: HttpErrorResponse): void {
    this.isSaving.set(false);
    const serverDetail = typeof error.error?.detail === 'string' ? error.error.detail : null;
    const validationErrors = error.error?.errors && typeof error.error.errors === 'object'
      ? Object.values(error.error.errors as Record<string, string[]>).flat().join(' ')
      : null;
    this.error.set(serverDetail || validationErrors || 'לא ניתן לשמור כרגע. בדקו את הפרטים ונסו שוב.');
  }
}
