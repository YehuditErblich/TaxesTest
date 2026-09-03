using Microsoft.EntityFrameworkCore;
using server.Domain.Entities;
using server.Domain.Enums;

namespace server.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
	public DbSet<FormTemplate> FormTemplates => Set<FormTemplate>();
	public DbSet<FormField> FormFields => Set<FormField>();
	public DbSet<FieldOption> FieldOptions => Set<FieldOption>();
	public DbSet<WorkflowTemplate> WorkflowTemplates => Set<WorkflowTemplate>();
	public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();
	public DbSet<FormSubmission> FormSubmissions => Set<FormSubmission>();
	public DbSet<FormSubmissionValue> FormSubmissionValues => Set<FormSubmissionValue>();
	public DbSet<SubmissionSelectedOption> SubmissionSelectedOptions => Set<SubmissionSelectedOption>();
	public DbSet<Upload> Uploads => Set<Upload>();
	public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
	public DbSet<WorkflowStepInstance> WorkflowStepInstances => Set<WorkflowStepInstance>();
	public DbSet<WorkflowAction> WorkflowActions => Set<WorkflowAction>();
	public DbSet<StatusType> StatusTypes => Set<StatusType>();
	public DbSet<StatusValue> StatusValues => Set<StatusValue>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<FormTemplate>(entity =>
		{
			entity.ToTable("FormTemplates");
			entity.HasKey(template => template.Id);
			entity.Property(template => template.Name).HasMaxLength(200).IsRequired();
			entity.Property(template => template.Description).HasMaxLength(2000);
			entity.Property(template => template.CreatedByUserId).HasMaxLength(100).IsRequired();
			entity.HasOne(template => template.Status).WithMany(status => status.FormTemplates)
				.HasForeignKey(template => template.StatusId).OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(template => template.WorkflowTemplate).WithMany(workflow => workflow.FormTemplates)
				.HasForeignKey(template => template.WorkflowTemplateId).OnDelete(DeleteBehavior.Restrict);
		});

		modelBuilder.Entity<FormField>(entity =>
		{
			entity.ToTable("FormFields");
			entity.HasKey(field => field.Id);
			entity.Property(field => field.Name).HasMaxLength(100).IsRequired();
			entity.Property(field => field.Label).HasMaxLength(200).IsRequired();
			entity.Property(field => field.Placeholder).HasMaxLength(500);
			entity.Property(field => field.HelpText).HasMaxLength(2000);
			entity.Property(field => field.DefaultValue).HasMaxLength(4000);
			entity.Property(field => field.ValidationSettingsJson).HasMaxLength(4000);
			entity.Property(field => field.DisplaySettingsJson).HasMaxLength(4000);
			entity.HasIndex(field => new { field.FormTemplateId, field.Name }).IsUnique();
			entity.HasIndex(field => new { field.FormTemplateId, field.DisplayOrder }).IsUnique();
			entity.HasOne(field => field.FormTemplate).WithMany(template => template.Fields)
				.HasForeignKey(field => field.FormTemplateId).OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<FieldOption>(entity =>
		{
			entity.ToTable("FieldOptions");
			entity.HasKey(option => option.Id);
			entity.Property(option => option.Value).HasMaxLength(200).IsRequired();
			entity.Property(option => option.Label).HasMaxLength(200).IsRequired();
			entity.HasIndex(option => new { option.FormFieldId, option.Value }).IsUnique();
			entity.HasOne(option => option.FormField).WithMany(field => field.Options)
				.HasForeignKey(option => option.FormFieldId).OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<WorkflowTemplate>(entity =>
		{
			entity.ToTable("WorkflowTemplates");
			entity.HasKey(template => template.Id);
			entity.Property(template => template.Name).HasMaxLength(200).IsRequired();
			entity.Property(template => template.Description).HasMaxLength(2000);
			entity.Property(template => template.CreatedByUserId).HasMaxLength(100).IsRequired();
			entity.HasOne(template => template.Status).WithMany(status => status.WorkflowTemplates)
				.HasForeignKey(template => template.StatusId).OnDelete(DeleteBehavior.Restrict);
		});

		modelBuilder.Entity<WorkflowStep>(entity =>
		{
			entity.ToTable("WorkflowSteps");
			entity.HasKey(step => step.Id);
			entity.Property(step => step.Name).HasMaxLength(200).IsRequired();
			entity.Property(step => step.Description).HasMaxLength(2000);
			entity.Property(step => step.ApproverType).HasMaxLength(20).IsRequired();
			entity.Property(step => step.ApproverUserId).HasMaxLength(100);
			entity.Property(step => step.AllowedActionsJson).HasMaxLength(2000).IsRequired();
			entity.HasIndex(step => new { step.WorkflowTemplateId, step.StepOrder }).IsUnique();
			entity.HasOne(step => step.WorkflowTemplate).WithMany(template => template.Steps)
				.HasForeignKey(step => step.WorkflowTemplateId).OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<FormSubmission>(entity =>
		{
			entity.ToTable("FormSubmissions");
			entity.HasKey(submission => submission.Id);
			entity.Property(submission => submission.SubmittedByUserId).HasMaxLength(100).IsRequired();
			entity.HasOne(submission => submission.FormTemplate).WithMany(template => template.Submissions)
				.HasForeignKey(submission => submission.FormTemplateId).OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(submission => submission.Status).WithMany(status => status.FormSubmissions)
				.HasForeignKey(submission => submission.StatusId).OnDelete(DeleteBehavior.Restrict);
		});

		modelBuilder.Entity<FormSubmissionValue>(entity =>
		{
			entity.ToTable("FormSubmissionValues");
			entity.HasKey(value => value.Id);
			entity.Property(value => value.Value).HasMaxLength(4000);
			entity.HasOne(value => value.FormSubmission).WithMany(submission => submission.Values)
				.HasForeignKey(value => value.FormSubmissionId).OnDelete(DeleteBehavior.Cascade);
			entity.HasOne(value => value.FormField).WithMany(field => field.SubmissionValues)
				.HasForeignKey(value => value.FormFieldId).OnDelete(DeleteBehavior.Restrict);
		});

		modelBuilder.Entity<SubmissionSelectedOption>(entity =>
		{
			entity.ToTable("SubmissionSelectedOptions");
			entity.HasKey(selection => selection.Id);
			entity.HasOne(selection => selection.FormSubmission).WithMany(submission => submission.SelectedOptions)
				.HasForeignKey(selection => selection.FormSubmissionId).OnDelete(DeleteBehavior.Cascade);
			entity.HasOne(selection => selection.FormField).WithMany(field => field.SelectedOptions)
				.HasForeignKey(selection => selection.FormFieldId).OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(selection => selection.FieldOption).WithMany()
				.HasForeignKey(selection => selection.FieldOptionId).OnDelete(DeleteBehavior.Restrict);
		});

		modelBuilder.Entity<Upload>(entity =>
		{
			entity.ToTable("Uploads");
			entity.HasKey(upload => upload.Id);
			entity.Property(upload => upload.FileName).HasMaxLength(500).IsRequired();
			entity.Property(upload => upload.FileUrl).HasMaxLength(2000).IsRequired();
			entity.HasOne(upload => upload.FormSubmission).WithMany(submission => submission.Uploads)
				.HasForeignKey(upload => upload.FormSubmissionId).OnDelete(DeleteBehavior.Cascade);
			entity.HasOne(upload => upload.FormField).WithMany(field => field.Uploads)
				.HasForeignKey(upload => upload.FormFieldId).OnDelete(DeleteBehavior.Restrict);
		});

		modelBuilder.Entity<WorkflowInstance>(entity =>
		{
			entity.ToTable("WorkflowInstances");
			entity.HasKey(instance => instance.Id);
			entity.HasIndex(instance => instance.FormSubmissionId).IsUnique();
			entity.HasOne(instance => instance.FormSubmission).WithOne(submission => submission.WorkflowInstance)
				.HasForeignKey<WorkflowInstance>(instance => instance.FormSubmissionId).OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(instance => instance.WorkflowTemplate).WithMany(template => template.Instances)
				.HasForeignKey(instance => instance.WorkflowTemplateId).OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(instance => instance.Status).WithMany(status => status.WorkflowInstances)
				.HasForeignKey(instance => instance.StatusId).OnDelete(DeleteBehavior.Restrict);
		});

		modelBuilder.Entity<WorkflowStepInstance>(entity =>
		{
			entity.ToTable("WorkflowStepInstances");
			entity.HasKey(instance => instance.Id);
			entity.Property(instance => instance.AssignedToUserId).HasMaxLength(100).IsRequired();
			entity.HasOne(instance => instance.WorkflowInstance).WithMany(workflow => workflow.Steps)
				.HasForeignKey(instance => instance.WorkflowInstanceId).OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(instance => instance.WorkflowStep).WithMany(step => step.Instances)
				.HasForeignKey(instance => instance.WorkflowStepId).OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(instance => instance.Status).WithMany(status => status.WorkflowStepInstances)
				.HasForeignKey(instance => instance.StatusId).OnDelete(DeleteBehavior.Restrict);
		});

		modelBuilder.Entity<WorkflowAction>(entity =>
		{
			entity.ToTable("WorkflowActions");
			entity.HasKey(action => action.Id);
			entity.Property(action => action.ActionType).HasMaxLength(50).IsRequired();
			entity.Property(action => action.PerformedByUserId).HasMaxLength(100).IsRequired();
			entity.Property(action => action.Comment).HasMaxLength(2000);
			entity.HasOne(action => action.WorkflowInstance).WithMany(workflow => workflow.Actions)
				.HasForeignKey(action => action.WorkflowInstanceId).OnDelete(DeleteBehavior.Restrict);
			entity.HasOne(action => action.WorkflowStepInstance).WithMany(step => step.Actions)
				.HasForeignKey(action => action.WorkflowStepInstanceId).OnDelete(DeleteBehavior.Restrict);
		});

		modelBuilder.Entity<StatusType>(entity =>
		{
			entity.ToTable("StatusTypes");
			entity.HasKey(type => type.Id);
			entity.Property(type => type.Code).HasMaxLength(50).IsRequired();
			entity.Property(type => type.DisplayText).HasMaxLength(200).IsRequired();
			entity.HasIndex(type => type.Code).IsUnique();
		});

		modelBuilder.Entity<StatusValue>(entity =>
		{
			entity.ToTable("StatusValues");
			entity.HasKey(value => value.Id);
			entity.Property(value => value.ValueCode).HasMaxLength(50).IsRequired();
			entity.Property(value => value.DisplayText).HasMaxLength(200).IsRequired();
			entity.HasIndex(value => new { value.StatusTypeId, value.ValueCode }).IsUnique();
			entity.HasOne(value => value.StatusType).WithMany(type => type.Values)
				.HasForeignKey(value => value.StatusTypeId).OnDelete(DeleteBehavior.Restrict);
		});

		SeedStatuses(modelBuilder);
	}

	private static void SeedStatuses(ModelBuilder modelBuilder)
	{
		var statusTypes = new[]
		{
			new StatusType { Id = 1, Code = "FormTemplate", DisplayText = "Form template" },
			new StatusType { Id = 2, Code = "WorkflowTemplate", DisplayText = "Workflow template" },
			new StatusType { Id = 3, Code = "FormSubmission", DisplayText = "Form submission" },
			new StatusType { Id = 4, Code = "WorkflowInstance", DisplayText = "Workflow instance" },
			new StatusType { Id = 5, Code = "WorkflowStepInstance", DisplayText = "Workflow step instance" }
		};
		modelBuilder.Entity<StatusType>().HasData(statusTypes);

		var statusValues = new List<StatusValue>();
		var id = 1;
		foreach (var statusType in statusTypes)
		{
			foreach (var status in new[]
			{
				("DRAFT", "טיוטה"),
				("PUBLISHED", "פורסם"),
				("SUBMITTED", "נשלח"),
				("IN_PROGRESS", "בתהליך"),
				("APPROVED", "אושר"),
				("REJECTED", "נדחה"),
				("RETURNED_FOR_CORRECTION", "הוחזר לתיקון"),
				("CANCELLED", "בוטל")
			})
			{
				statusValues.Add(new StatusValue
				{
					Id = id++, StatusTypeId = statusType.Id, ValueCode = status.Item1,
					DisplayOrder = statusValues.Count + 1, DisplayText = status.Item2, IsActive = true
				});
			}
		}
		modelBuilder.Entity<StatusValue>().HasData(statusValues);
	}
}