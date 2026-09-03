# AWS infrastructure starting point

This folder intentionally contains no deployed resources or data. Add AWS CDK, Terraform, or CloudFormation here when the hosting shape is chosen.

Recommended first components:

- An AWS-managed hosting service for the `server` API, such as Elastic Beanstalk or App Runner
- An S3 bucket and CloudFront distribution for the Angular `client`
- CloudWatch logs and health checks
- Secrets Manager or Systems Manager Parameter Store for configuration
- Amazon RDS when persistent relational storage is required