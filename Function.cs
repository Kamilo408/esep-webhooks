using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Newtonsoft.Json;


[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook
{
    public class Function
    {
        /// <summary>
        /// Processes the GitHub webhook payload and transforms it into the required output format.
        /// </summary>
        /// <param name="input">The input event from GitHub.</param>
        /// <param name="context">The Lambda context.</param>
        /// <returns>The transformed payload in the required format.</returns>
        public async Task<object> FunctionHandler(object input, ILambdaContext context)
        {
            context.Logger.LogInformation($"FunctionHandler received: {input}");

            // Deserialize the incoming payload
            dynamic json = JsonConvert.DeserializeObject<dynamic>(input.ToString());

            // Extract the issue URL from the payload
            string issueUrl = json?.issue?.html_url;
            if (string.IsNullOrEmpty(issueUrl))
            {
                context.Logger.LogInformation("No issue URL found in the payload.");
                return new { message = "No issue URL found in the payload." };
            }

            // Transform to the desired output format
            var output = new
            {
                body = new
                {
                    issue = new
                    {
                        html_url = issueUrl
                    }
                }
            };

            context.Logger.LogInformation($"Transformed output: {JsonConvert.SerializeObject(output)}");

            // Optional: Post to Slack
            string payload = $"{{\"text\": \"Issue Created: {issueUrl}\"}}";
            using (var client = new HttpClient())
            {
                var slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
                if (string.IsNullOrEmpty(slackUrl))
                {
                    context.Logger.LogInformation("Slack URL environment variable is missing.");
                    return "Slack URL environment variable is missing.";
                }

                var webRequest = new HttpRequestMessage(HttpMethod.Post, slackUrl)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };

                var response = await client.SendAsync(webRequest);
                if (!response.IsSuccessStatusCode)
                {
                    context.Logger.LogInformation($"Error: {response.StatusCode}");
                }
                else
                {
                    context.Logger.LogInformation("successfully.");
                }
            }

            return output;
        }
    }
}
