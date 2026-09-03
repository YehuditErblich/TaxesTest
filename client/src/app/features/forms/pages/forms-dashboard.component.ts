import { ChangeDetectionStrategy, Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';

import { FormsApiService } from '../data-access/forms-api.service';
import { FormTemplateSummary } from '../models/form.models';

@Component({
  selector: 'app-forms-dashboard',
  imports: [RouterLink, DatePipe, DecimalPipe],
  templateUrl: './forms-dashboard.component.html',
  styleUrl: './forms-dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FormsDashboardComponent implements OnInit {
  private readonly api = inject(FormsApiService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly templates = signal<readonly FormTemplateSummary[]>([]);
  protected readonly isLoading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.loadTemplates();
  }

  protected reload(): void {
    this.loadTemplates();
  }

  private loadTemplates(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.api
      .listTemplates()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (templates) => {
          this.templates.set(templates);
          this.isLoading.set(false);
        },
        error: () => {
          this.errorMessage.set('We could not load the form templates. Try again.');
          this.isLoading.set(false);
        },
      });
  }
}
