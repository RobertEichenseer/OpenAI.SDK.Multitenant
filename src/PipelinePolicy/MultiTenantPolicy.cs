using System.Text.Json;
using Azure.Core;
using Azure.Core.Pipeline;

namespace Multitenant.Policy;

public class MultiTenantPolicy : HttpPipelinePolicy
{
    public override void Process(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
    {
        //Get & remove tenant id
        Task<(string tenantId, string requestContent)> tenantTask = GetAndRemoveTenantId(message);
        tenantTask.Wait(); 
        message.Request.Content = RequestContent.Create(tenantTask.Result.requestContent); 

        //Process request
        ProcessNext(message, pipeline);
    
        //Get usage
        Task<(int totalTokens, int completionTokens, int promptTokens, string responseContent)> usageTask 
            = GetUsage(message, tenantTask.Result.tenantId, tenantTask.Result.requestContent); 

        //Create new Response.ContentStream
        message.Response.ContentStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(usageTask.Result.responseContent));

        //Log Request
        (LogRequestAndTokenUsage(
            tenantTask.Result.tenantId, 
            usageTask.Result.responseContent, 
            usageTask.Result.totalTokens, 
            usageTask.Result.completionTokens, 
            usageTask.Result.promptTokens)).Wait();
    }

    public override async ValueTask ProcessAsync(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline){

        //Get & remove tenant id
        (string tenantId, string requestContent) = await GetAndRemoveTenantId(message);
        message.Request.Content = RequestContent.Create(requestContent); 

        //Process request
        await ProcessNextAsync(message, pipeline);

        //Get usage
        (int totalTokens, int completionTokens, int promptTokens, string responseContent) 
            = await GetUsage(message, tenantId, requestContent); 

        //Create new Response.ContentStream
        message.Response.ContentStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(responseContent));

        //Log Request
        await LogRequestAndTokenUsage(tenantId, responseContent, totalTokens, completionTokens, promptTokens);
    }

    public async Task<(string,string)> GetAndRemoveTenantId(HttpMessage httpMessage) 
    {
        //Check if request data exists
        if (httpMessage.Request.Content == null)
            return ("",""); 

        //Read request stream
        MemoryStream memoryStream = new MemoryStream(); 
        await httpMessage.Request.Content.WriteToAsync(memoryStream, new CancellationToken());   
        string jsonContent = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
        
        JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions{PropertyNameCaseInsensitive = true}; 
        JsonElement jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonContent, jsonSerializerOptions);

        string? system = ((jsonElement.GetProperty("messages"))
                .EnumerateArray()
                .Where (x => x.GetProperty("role").GetString() == "system")
                .Select(x => x.GetProperty("content"))
                .FirstOrDefault()
                .GetString()
        ) ?? ""; 

        string tenantId = "";
        if (system.Contains("{{<TenantId>") && system.Contains("</TenantId>}}")) {
            //Get TenantId
            tenantId = system.Substring(
                system.IndexOf("{{<TenantId>") + 12,
                system.IndexOf("</TenantId>}}") - 12
            ); 

            //Remove TenantId
            jsonContent = jsonContent.Replace(String.Concat(
                "{{", "\\u", ((int)'<').ToString("X4"), "TenantId", "\\u", ((int)'>').ToString("X4"), 
                tenantId,
                "\\u", ((int)'<').ToString("X4"), "/TenantId", "\\u", ((int)'>').ToString("X4"),
                "}}"),
                ""
            );
        }
        return (tenantId, jsonContent); 
    }
    
    private async Task<(int,int,int,string)> GetUsage (HttpMessage message, string tenantId, string requestContent)
    {
        if (message.Response.ContentStream != null) {
            using (StreamReader streamReader = new StreamReader(message.Response.ContentStream, System.Text.Encoding.UTF8)) {
                string content = await streamReader.ReadToEndAsync(); 
                (int,int,int) tokenUsage = GetTokenUsage(content); 
                message.Response.ContentStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)); 
                return (tokenUsage.Item1, tokenUsage.Item2, tokenUsage.Item3, content); 
            }
        }
        return (0,0,0,"");
    }

    public (int, int, int) GetTokenUsage(string jsonContent)
    {
        JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions{PropertyNameCaseInsensitive = true}; 
        JsonElement jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonContent, jsonSerializerOptions);

        int totalTokens = (jsonElement.GetProperty("usage")).GetProperty("total_tokens").GetInt16(); 
        int completionTokens = (jsonElement.GetProperty("usage")).GetProperty("completion_tokens").GetInt16(); 
        int promptTokens = (jsonElement.GetProperty("usage")).GetProperty("prompt_tokens").GetInt16(); 
        
        return (totalTokens, completionTokens, promptTokens); 
    }

    private async Task LogRequestAndTokenUsage(string tenantId, string responseContent, int totalTokens, int completionTokens, int promptTokens)
    {
        //Todo: Implement custom tenant specific functionality
        await Task.Run( () => {
            Console.WriteLine($"Token Usage for tenant: {tenantId} \n Total tokens: {totalTokens} \n Completion tokens: {completionTokens} \n Prompt tokens: {promptTokens}"); 
            Console.WriteLine($"Response: {responseContent}");
        });
    }

}
