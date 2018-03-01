using LyncAsyncExtensionMethods;
using Microsoft.Rtc.Collaboration;
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;

namespace UCMA_IVR_Demo
{
    public class QnAConversation
    {
        private string _initialMessage { get; set; }
        private InstantMessagingCall _call { get; set; }
        private InstantMessagingFlow _flow { get; set; }



        public void Start(string initialMessage, InstantMessagingCall call)
        {
            this._initialMessage = initialMessage;
            this._call = call;
            call.InstantMessagingFlowConfigurationRequested += Call_InstantMessagingFlowConfigurationRequested;
            call.AcceptAsync();
        }

        private void Call_InstantMessagingFlowConfigurationRequested(object sender, InstantMessagingFlowConfigurationRequestedEventArgs e)
        {
            _flow = e.Flow;
            e.Flow.StateChanged += Flow_StateChanged;
            e.Flow.MessageReceived += Flow_MessageReceived;
        }

        private void Flow_StateChanged(object sender, MediaFlowStateChangedEventArgs e)
        {
            if (e.State == MediaFlowState.Active)
            {   //deal with the first message
                ProcessMessage(_initialMessage);
            }

        }

        private void Flow_MessageReceived(object sender, InstantMessageReceivedEventArgs e)
        {
            ProcessMessage(e.TextBody);
        }

        private void ProcessMessage(string message)
        {
            if (AreKeysMissing())
            {
                _flow.SendInstantMessageAsync("Please set QnAKnowledgebaseId and QnASubscriptionKey in App Settings. Get them at https://qnamaker.ai" );
                return;
            }
            var response = GetAnswerFromQnAMaker(message);
            _flow.SendInstantMessageAsync(response);
        }

        private bool AreKeysMissing()
        {
            return string.IsNullOrEmpty(ConfigurationManager.AppSettings["QnAKnowledgebaseId"]) || string.IsNullOrEmpty(ConfigurationManager.AppSettings["QnASubscriptionKey"]);
        }

        private string GetAnswerFromQnAMaker(string input)
        {
            var responseBody = CallAPI(input);

            return ExtractAnswer(responseBody);
        }

        private string CallAPI(string input)
        {
            string url = string.Format("https://westus.api.cognitive.microsoft.com/qnamaker/v2.0/knowledgebases/{0}/generateAnswer", ConfigurationManager.AppSettings["QnAKnowledgebaseId"]);
            WebRequest request = WebRequest.Create(url);
            request.Method = "POST";
            request.Headers.Add("Ocp-Apim-Subscription-Key", ConfigurationManager.AppSettings["QnASubscriptionKey"]);
            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                string json = "{\"question\":\"" + input + "\"}";
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();
            }
            var response = (HttpWebResponse)request.GetResponse();

            using (var streamReader = new StreamReader(response.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                return result;
            }
        }

        public string ExtractAnswer(string response)
        {
            JToken token = JObject.Parse(response);
            var answers = token.SelectTokens("answers");
            return answers.First().First.SelectToken("answer").ToString();
        }
    }
}
