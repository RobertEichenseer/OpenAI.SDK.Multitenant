using Azure;
using Azure.AI.OpenAI;

namespace Multitenant.OpenAIClientExtensions;

public static class OpenAIClientExtensions
{
    public static Task<Response<ChatCompletions>> GetChatCompletionsAsync(this OpenAIClient openAIClient, string tenantId, string modelDeploymentName, ChatCompletionsOptions chatCompletionOptions) 
    {
        string tenantIdTemplate = String.Concat("{{<TenantId>", tenantId, "</TenantId>}}");

        string system = (chatCompletionOptions.Messages.Where(message => message.Role == ChatRole.System)
            .FirstOrDefault<ChatMessage>()
            ??
            new ChatMessage(ChatRole.System, ""))
            .Content;

        // Duplicate messages and keep sequence (!)
        ChatCompletionsOptions modifiedChatCompletions = new ChatCompletionsOptions(); 
        foreach(ChatMessage chatMessage in chatCompletionOptions.Messages){
            if (chatMessage.Role == ChatRole.System) {
                modifiedChatCompletions.Messages.Add(new ChatMessage(ChatRole.System, String.Concat(tenantIdTemplate, system)));
            } else {
                modifiedChatCompletions.Messages.Add(chatMessage);
            }
        }
        chatCompletionOptions.Messages.Clear(); 
        foreach(ChatMessage chatMessage in modifiedChatCompletions.Messages) {
            chatCompletionOptions.Messages.Add(chatMessage); 
        }
        return openAIClient.GetChatCompletionsAsync(modelDeploymentName, chatCompletionOptions); 
    }

    public static Response<ChatCompletions> GetChatCompletions(this OpenAIClient openAIClient, string tenantId, string modelDeploymentName, ChatCompletionsOptions chatCompletionOptions) 
    {
        //Retrieve current system message
        string system = (chatCompletionOptions.Messages.Where(message => message.Role == ChatRole.System)
            .FirstOrDefault<ChatMessage>()
            ??
            new ChatMessage(ChatRole.System, ""))
            .Content;

        // Duplicate messages and keep sequence (!)
        string tenantIdTemplate = String.Concat("{{<TenantId>", tenantId, "</TenantId>}}");
        ChatCompletionsOptions modifiedChatCompletions = new ChatCompletionsOptions(); 
        foreach(ChatMessage chatMessage in chatCompletionOptions.Messages){
            if (chatMessage.Role == ChatRole.System) {
                modifiedChatCompletions.Messages.Add(new ChatMessage(ChatRole.System, String.Concat(tenantIdTemplate, system)));
            } else {
                modifiedChatCompletions.Messages.Add(chatMessage);
            }
        }
        chatCompletionOptions.Messages.Clear(); 
        foreach(ChatMessage chatMessage in modifiedChatCompletions.Messages) {
            chatCompletionOptions.Messages.Add(chatMessage); 
        }
        return openAIClient.GetChatCompletions(modelDeploymentName, chatCompletionOptions); 
    }
}