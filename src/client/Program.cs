using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Multitenant.Policy;
using Multitenant.OpenAIClientExtensions; 
using static Azure.AI.OpenAI.OpenAIClientOptions;

string apiEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "http://Null";
string apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? "API-KEY-NOT-SET";
string modelDeploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENTNAME") ?? "API-MODELDEPLOYMENTNAME NOT SET";

AzureKeyCredential azureKeyCredential = new AzureKeyCredential(apiKey);
OpenAIClientOptions openAIClientOptions = new OpenAIClientOptions(ServiceVersion.V2023_03_15_Preview); 
openAIClientOptions.AddPolicy(new MultiTenantPolicy(), HttpPipelinePosition.BeforeTransport);
OpenAIClient openAIClient = new OpenAIClient(new Uri(apiEndpoint), azureKeyCredential, openAIClientOptions);

string tenantId = "6c9ee2ff-0333-4104-b042-ff93f2837737";
string system = "You are an AI assistant that extracts sentiment provided to you. You respond the detected sentiment in JSON format";
string user1 = "Hello! How are you doing? I'm doing fine";
string assistant1 = @"{""Sentiment:"" ""positive""}";
string user2 = "What's going on? I'm waiting since quite some time now!"; 
string assistant2 = @"{""Sentiment:"" ""negative""}";

string textToBeAnalyzed = "Hello, nice talking with you!";

//Compose Chat
ChatCompletionsOptions chatCompletionsOptions = new ChatCompletionsOptions();
chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.System, String.Concat(tenantId, system)));

chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.User, user1));
chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.Assistant, assistant1));

chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.User, user2));
chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.Assistant, assistant2));

chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.User, textToBeAnalyzed));
chatCompletionsOptions.Temperature = 1;  

//Call OpenAI Async
Response<ChatCompletions> response = await openAIClient.GetChatCompletionsAsync(
    tenantId,
    modelDeploymentName,
    chatCompletionsOptions);
Console.WriteLine($"Assistant: {response.Value.Choices[0].Message.Content} \n\n".Trim());


//Waiting for 10 seconds to avoid throttling errors (AOAI dev instances might have quotas)
Console.WriteLine("Waiting for 10 seconds to avoid throttling challenges! \n\n");
await Task.Delay(TimeSpan.FromSeconds(10));

//Call OpenAI Sync
response = openAIClient.GetChatCompletions(
    modelDeploymentName, 
    chatCompletionsOptions
);
Console.WriteLine($"Assistant: {response.Value.Choices[0].Message.Content}".Trim());    

