# Backend API

The API runs at `http://localhost:5080` in development. Swagger is available at `/swagger` and health is available at `/health`.

## Form templates

```text
GET    /api/form-templates
GET    /api/form-templates/{id}
POST   /api/form-templates
POST   /api/form-templates/{id}/publish
```

Creating a template creates a draft. The `POST` body may include a nested `workflow` with all of its ordered steps; the form, fields, options, workflow, and steps are then persisted atomically by one `SaveChanges` operation. `GET /api/form-templates/{id}` returns the creator and timestamps as well as the complete field and workflow definitions. Publishing requires a name and at least one field. Published templates are immutable.

## Workflow templates

```text
GET    /api/workflow-templates
GET    /api/workflow-templates/{id}
POST   /api/workflow-templates
POST   /api/workflow-templates/{id}/publish
```

Each step requires one user approver or role approver, a unique `stepOrder`, and at least one allowed action. Role-assigned steps require identity/role resolution before a submission can start them.

## Statuses

```text
GET /api/statuses?statusType=FormSubmission
```

Statuses are seeded editable records and are resolved by status type and value code internally.

## Submissions

```text
GET  /api/form-templates/{formTemplateId}/submissions/{id}
POST /api/form-templates/{formTemplateId}/submissions
```

A submission requires a published form template. Values are validated against field definitions. If the template has a published workflow, the submission response includes `workflowInstanceId`.

## Workflow execution

```text
GET  /api/workflows/{id}
POST /api/workflows/{id}/steps/{stepInstanceId}/actions
```

Only the assigned approver may act, and the action must be listed in the step's allowed actions. Every action is appended to workflow history. Approval advances to the next step; terminal actions update workflow and submission status.

The current development database uses EF Core InMemory. It resets when the application restarts and does not validate relational constraints or SQL translation like SQL Server/PostgreSQL would.
