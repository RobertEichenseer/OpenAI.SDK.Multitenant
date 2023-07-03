# Azure OpenAI .NET SDK - Implement multi-tenant solutions

.NET Azure OpenAI SDK extensibility to support the development multi-tenant solutions.
The folder contains source for a simplified c# end-to-end sample implementing and using:

- `HttpPipelinePolicy` to centrally process LLM responses according to tenant needs
- `OpenAIClient` extensions to provide tenant id to the processing pipeline

## Folder

| Folder | Content | Description |
| --- | --- | --- |
| Client | .NET application showcasing the processing pipeline and extension functions (.NET code) |  |
| CreateEnv | Azure CLI script to create the necessary environment  | The Azure CLI script provides the credentials (Azure API Key, Azure Endpoint, Azure Deployment Name) used in the sample application |
| PipelinePolicy | Custom implementation of HttpPipelinePolicy and OpenAI client extensions | `MultiTenantPolicy.cs`: Custom pipeline implementation `OpenAIClientExtensions.cs`: Implementation of OpenAIClient extensions  |