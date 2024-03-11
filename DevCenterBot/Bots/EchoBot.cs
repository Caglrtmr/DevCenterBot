// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.22.0

using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using global::Azure.AI.OpenAI;
using static System.Net.Mime.MediaTypeNames;

namespace DevCenterBot.Bots
{
    public class EchoBot : ActivityHandler
    {

        BotSettings botSettings;
        private readonly ILogger<EchoBot> logger;
        private SearchClient srchclient;
        private OpenAIClient openAIClient;
        public EchoBot(IOptionsMonitor<BotSettings> optionsMonitor, ILogger<EchoBot> logger)
        {
            this.botSettings = optionsMonitor.CurrentValue;
            //optionsMonitor.OnChange(s=> this.options=u)

            this.logger = logger;

            // Search service instance
            Uri serviceEndpoint = new Uri($"https://" + botSettings.SEARCH_SERVICE_NAME + ".search.windows.net/");
            global::Azure.AzureKeyCredential credential = new global::Azure.AzureKeyCredential(botSettings.SEARCH_QUERY_KEY);
            srchclient = new SearchClient(serviceEndpoint, botSettings.SEARCH_INDEX_NAME, credential);


            // OpenAIClient instance

            var endpoint = new Uri(botSettings.AOAI_ENDPOINT);
            var credentials = new global::Azure.AzureKeyCredential(botSettings.AOAI_KEY);
            openAIClient = new OpenAIClient(endpoint, credentials);
        }

        public static string ReplaceRelativeMarkDownLinks(string str)
        {
            string pattern = @"\[(.*?)\]\(\/[^\)]*\)";
            return Regex.Replace(str, pattern, m =>
            {
                string linkText = m.Groups[1].Value;
                string url = m.Value.Substring(m.Value.IndexOf('(') + 1, m.Value.LastIndexOf(')') - m.Value.IndexOf('(') - 1);
                return $"[{linkText}](https://confluence.intertech.com.tr{url})";
            });
        }

        async Task<string> ConsolidatedAnswer(string userMessage)
        {
            var question = userMessage;
            var context = await GetSearchResult(question);
            var promptText = CreateQuestionAndContext(question, context);

            var responseFromGPT = await GetAnswerFromGPT(promptText);

            return responseFromGPT;
        }

        public async Task<string> GetSearchResult(string searchQuery, int? count = null, int? skip = null)
        {
            try
            {
                this.logger.Log(LogLevel.Information, "SearchQuery:" + searchQuery);
                global::Azure.Search.Documents.SearchOptions searchOptions = new global::Azure.Search.Documents.SearchOptions();

                
                searchOptions.QueryLanguage = "tr-TR";
                searchOptions.SemanticConfigurationName = "mergenmarkdown-config";
                searchOptions.QueryCaption = "extractive";
                searchOptions.QueryAnswer = "extractive";
                searchOptions.QueryType = global::Azure.Search.Documents.Models.SearchQueryType.Semantic;
                searchOptions.Size = Convert.ToInt32(botSettings.SettingForTopK);

                var returnData = srchclient.Search<SearchDocument>(searchQuery, searchOptions);

                string searchResult = string.Empty;

                if (returnData == null)
                {
                    return searchResult;
                }

                var serializer = new JsonSerializer();

                using (var sr = new StreamReader(returnData.GetRawResponse().Content.ToStream()))
                using (var jsonTextReader = new JsonTextReader(sr))
                {
                    searchResult = "No information was found. Answer the question with your general knowledge. Let the user know that you have not found any information in the knowledge base and are responding with your general knowledge from the internet.";

                    var jsObj = serializer.Deserialize(jsonTextReader) as JObject;
                    var valueSection = jsObj["value"];
                    if (valueSection == null || !valueSection.HasValues)
                    {
                        return searchResult;
                    }

                    var reRankerScore = Convert.ToDecimal(valueSection.Children().First()["@search.rerankerScore"].Value<string>());

                    if (reRankerScore < 1)
                    {
                        return searchResult;
                    }

                    int i = 0;
                    searchResult = "";
                    foreach (var child in valueSection.Children().OrderByDescending(o => o["@search.rerankerScore"]).Take(Convert.ToInt32(botSettings.SettingForTopK)))
                    {
                        i++;
                        searchResult += "[Result " + i + "]: " + child["content"].Value<string>() + "\n\n";
                    }
                }

                return searchResult;
            }
            catch (Exception ex)
            {
                this.logger.Log(LogLevel.Error, ex.Message);
                return string.Empty;
            }

        }

        string CreateQuestionAndContext(string question, string context)
        {
            return string.Format("[Question] {0} \r\n\r\n[Context] {1} \r\n", question, context);
        }

        public async Task<string> GetAnswerFromGPT(string promptText)
        {
            var today = DateTime.Today;
            var thisWeekStart = today.AddDays(-(int)today.DayOfWeek + 1);
            var thisWeekEnd = thisWeekStart.AddDays(7).AddSeconds(-1);
            //var chatMessageAsistant = new ChatMessage(ChatRole.Assistant, "You are an assistant that helps users with software and IT questions using context provided in the prompt. You only respond in Turkish and format your response in Markdown language. You will answer the [Question] below objectively in a casual and friendly tone, using the [Context] below it, and information from your memory. If the [Context] is not really relevant to the [Question], or if the [Question] is not a question at all and more of a chit chat, ignore the [Context] completely and only respond to the question with chit chat.");
            var chatMessageAsistant = new ChatMessage(ChatRole.Assistant, string.Format(botSettings.SettingForPrompt, today.ToString("dddd, dd MMMM yyyy"), thisWeekStart.ToString("dddd, dd MMMM yyyy"), thisWeekEnd.ToString("dddd, dd MMMM yyyy")));
            var chatMessageUser = new ChatMessage(ChatRole.User, promptText);

            var completionOptions = new ChatCompletionsOptions
            {
                Messages = { chatMessageAsistant, chatMessageUser },

                MaxTokens = Convert.ToInt32(botSettings.SettingForMaxToken),
                Temperature = float.Parse(botSettings.SettingForTemperature),
                FrequencyPenalty = 0.5f,
                PresencePenalty = 0.0f,
                NucleusSamplingFactor = 0.95F, // Top P
                StopSequences = { "You:" }

            };
            this.logger.Log(LogLevel.Information, "Prompt:" + promptText);
            /*
                        ChatCompletions response = openAIClient.GetChatCompletions(this.options.AOAI_DEPLOYMENTID, completionOptions);
                        var responseText = response.Choices.First().Message.Content;
                        return responseText;
            */
            var response = openAIClient.GetChatCompletions(this.botSettings.AOAI_DEPLOYMENTID, completionOptions);
            var rawResponse = response.GetRawResponse();
            var responseText = "";
            if (rawResponse.IsError)
            {
                if (rawResponse.Status == 429)
                {
                    responseText = "Şu anda sistemde bir yoğunluk var. Lütfen bir dakika sonra tekrar deneyin.";
                }
                else
                {
                    responseText = "Beklenmeyen bir hata alındı: " + rawResponse.ReasonPhrase;
                }
            }
            else
            {
                responseText = response.Value.Choices.First().Message.Content;
            }
            this.logger.Log(LogLevel.Information, "Response:" + responseText);
            return ReplaceRelativeMarkDownLinks(responseText);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var replyText = $"Echo: {turnContext.Activity.Text}";
            var text = turnContext.Activity.Text;
            var answer = await ConsolidatedAnswer(text);

            //await turnContext.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);

            await turnContext.SendActivityAsync(MessageFactory.Text(answer, answer), cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Ben DevCenter botuyum. Size nasıl yardımcı olabilirim?";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                }
            }
        }
    }
}
