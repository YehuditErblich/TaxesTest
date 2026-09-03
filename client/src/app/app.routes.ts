import { Routes } from '@angular/router';

export const routes: Routes = [
	{
		path: 'admin',
		loadComponent: () => import('./features/forms/pages/forms-admin.component').then((module) => module.FormsAdminComponent),
	},
	{
		path: 'forms',
		loadComponent: () => import('./features/forms/pages/forms-dashboard.component').then((module) => module.FormsDashboardComponent),
	},
	{
		path: 'forms/:id',
		loadComponent: () => import('./features/forms/pages/form-submission.component').then((module) => module.FormSubmissionComponent),
	},
	{ path: '', pathMatch: 'full', redirectTo: 'forms' },
	{ path: '**', redirectTo: 'forms' },
];
