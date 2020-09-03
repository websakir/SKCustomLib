using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Simbrella.Framework.Config.Abstractions;
using Simbrella.Framework.Factories;
using Simbrella.Framework.Integration.Http;
using Simbrella.Framework.Logging2.Abstractions;
using Simbrella.SimKredit.Core.Business;
using Simbrella.SimKredit.Core.Business.Abstractions;
using Simbrella.SimKredit.Core.Common;

namespace CustomLibrary
{
    class ResponseServiceTranslator : SmsResponseTranslator
    {
        private ILogger _logger = Factory.GetLoggerProvider().GetLogger("responseservice");
        private HttpClient _httpClient;
        
        private IConfigItem _configItem;
        private ISubscriberManager _subscriberManager;

        public ResponseServiceTranslator(IConfigItem configItem)
    : this(configItem, ManagerFactory.GetSubscriberManager())
        {

        }

        public ResponseServiceTranslator(IConfigItem configItem, ISubscriberManager subscriberManager)
        {
            _logger.LogInfo("Response Service started");
            _configItem = configItem;
            _subscriberManager = subscriberManager;
            string ip = "localhost", port = "5555";
           
            foreach (IConfigItem server in configItem["servers"].GetChildren()) 
            {
                _logger.LogInfo("server name " + server.ItemName);
                if (server.ItemName == "simulatorSMS") {
                    ip = server.GetAttribute("ip");
                    port = server.GetAttribute("port");
                }
            }
            string url = $"http://{ip}:{port}/smsresponses/";
            _logger.LogInfo("yrl " + url);
            _httpClient = new HttpClient(url, "POST");

        }



        public override bool IsChannelHealthy => true;

        public override void Dispose()
        {
            _httpClient.Close();
        }

        public override void SendSmsResponse(SmsMessage response)
        {
            HttpRequest request = new HttpRequest();

            string sourceNumber = response.SourceNumber;
            string destinationNumber = response.DestinationNumber;
            string content = response.Content;

           

            string messageBody = "<smsMessage sourceNumber=\"" + sourceNumber + "\" destinationNumber=\"" + destinationNumber + "\" content =\"" + response.Content + "\" />";
            _logger.LogInfo("message details:   " + messageBody);
            request.Body = messageBody;

            HttpResponse httpResponse = (HttpResponse)_httpClient.SendMessage(request);

            _logger.LogInfo(httpResponse.Body);

        }
    }
}
