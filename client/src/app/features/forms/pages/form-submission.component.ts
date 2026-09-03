import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { FormsApiService } from '../data-access/forms-api.service';
import {
  CreateSubmissionRequest,
  FormField,
  FormTemplateDetails,
  SubmissionResponse,
} from '../models/form.models';

interface DynamicFormControls {
  [fieldName: string]: FormControl<string | boolean | readonly number[]>;
}

@Component({
  selector: 'app-form-submission',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './form-submission.component.html',
  styleUrl: './form-submission.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FormSubmissionComponent implements OnInit {
  private readonly api = inject(FormsApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly template = signal<FormTemplateDetails | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly isSubmitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly success = signal<SubmissionResponse | null>(null);
  protected readonly submitted = signal(false);
  protected readonly form = new FormGroup<DynamicFormControls>({});

  private templateId = 0;

  ngOnInit(): void {
    this.templateId = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isInteger(this.templateId) || this.templateId <= 0) {
      this.errorMessage.set('The requested form could not be found.');
      this.isLoading.set(false);
      return;
    }

    this.api
      .getTemplate(this.templateId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (template) => {
          this.template.set(template);
          this.buildForm(template.fields);
          this.isLoading.set(false);
        },
        error: () => {
          this.errorMessage.set('We could not load this form. Try returning to the library.');
          this.isLoading.set(false);
        },
      });
  }

  protected submit(): void {
    this.submitted.set(true);
    if (this.form.invalid || this.template() === null) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    const request: CreateSubmissionRequest = {
      submittedByUserId: 'demo-user',
      values: this.template()!.fields
        .filter((field) => !this.isSelectionField(field))
        .map((field) => ({ fieldName: field.name, value: this.valueAsString(field) })),
      selectedOptions: this.template()!.fields
        .filter((field) => this.isSelectionField(field))
        .map((field) => ({
          fieldName: field.name,
          fieldOptionIds: this.selectedOptionIds(field),
        }))
        .filter((selection) => selection.fieldOptionIds.length > 0),
    };

    this.api
      .submitForm(this.templateId, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.success.set(response);
          this.isSubmitting.set(false);
        },
        error: () => {
          this.errorMessage.set('Your submission could not be sent. Please check the fields and try again.');
          this.isSubmitting.set(false);
        },
      });
  }

  protected isSelectionField(field: FormField): boolean {
    return field.fieldType === 'Select' || field.fieldType === 'Radio' || field.fieldType === 'MultiSelect';
  }

  protected control(field: FormField): FormControl<string | boolean | readonly number[]> {
    return this.form.controls[field.name];
  }

  protected hasError(field: FormField): boolean {
    const control = this.control(field);
    return control.invalid && (control.touched || this.submitted());
  }

  protected valueAsString(field: FormField): string | null {
    const value = this.control(field).value;
    return typeof value === 'string' ? value : String(value ?? '');
  }

  protected isChecked(event: Event): boolean {
    return (event.target as HTMLInputElement).checked;
  }

  protected selectedOptionIds(field: FormField): readonly number[] {
    const value = this.control(field).value;
    return Array.isArray(value) ? value : value ? [Number(value)] : [];
  }

  protected toggleOption(field: FormField, optionId: number, checked: boolean): void {
    const selected = new Set(this.selectedOptionIds(field));
    checked ? selected.add(optionId) : selected.delete(optionId);
    this.control(field).setValue([...selected]);
    this.control(field).markAsTouched();
  }

  private buildForm(fields: readonly FormField[]): void {
    for (const field of fields) {
      const initialValue = this.isSelectionField(field)
        ? field.fieldType === 'MultiSelect' ? [] : ''
        : field.fieldType === 'Checkbox' ? false : '';
      this.form.addControl(
        field.name,
        new FormControl(initialValue, {
          nonNullable: true,
          validators: field.isRequired ? [Validators.required] : [],
        }),
      );
    }
  }
}
