using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

using Simbrella.Framework.Config.Abstractions;
using Simbrella.Framework.Core;
using Simbrella.Framework.Factories;
using Simbrella.Framework.Integration.Http;
using Simbrella.Framework.Logging2.Abstractions;
using Simbrella.SimKredit.Core.Common;

namespace CustomLibrary
{
    class UssdRequestTranslatorCustom : UssdRequestTranslator
    {
        private HttpServer _httpServer;
        private ILogger _logger = Factory.GetLoggerProvider().GetLogger("requestservice");

        public UssdRequestTranslatorCustom(IConfigItem config)
        {
            string url = config["prefix"].Value;
            
            _httpServer = new HttpServer(url, false);
            _httpServer.MessageReceived += RequestReceived;
        }

        public override bool IsChannelHealthy =>  true;

        public override event EventHandler<UssdRequestReceivedEventArgs> UssdRequestReceived;

        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        public override void Start()
        {
            _httpServer.Open();
        }

        public override void Stop()
        {
            _httpServer.Close();
        }

        public virtual void RequestReceived(object sender, MessageReceivedEventArgs e)
        {


            _logger.LogInfo("USSD Request Receiver $$$$$");
            HttpResponse httpResponse = new HttpResponse();
            if (UssdRequestReceived != null)
            {
                HttpRequest httpReq = (HttpRequest)e.Request;

                if (httpReq.Body == null)
                {
                    _logger.LogInfo("Request body invalid or missing!!!");
                    httpResponse.Body = "ERROR";
                   
                }
                else
                {
                      _logger.LogInfo("smsMessage " + httpReq.Body);
//                    _logger.LogInfo("MessageReceivedEventArgs", new { e });

                    Dictionary<string, string> requestParameters = getRequestParameters(httpReq.Body);
                    requestParameters.TryGetValue("sourceNumber", out string sourceNumber);
                    requestParameters.TryGetValue("destinationNumber", out string destNumber);
                    requestParameters.TryGetValue("content", out string content);
                    requestParameters.TryGetValue("eventTime", out string eventTime);


                    _logger.LogInfo("message details ", new { sourceNumber, destNumber, content, eventTime });



                    UssdRequest request = new UssdRequest(sourceNumber ?? string.Empty, destNumber ?? string.Empty, content ?? string.Empty, "en", "1211");

                    //request.Parameters["localEventTime"] = eventTime;
                    UssdRequestReceivedEventArgs ussdRequestReceivedEventArgs = new UssdRequestReceivedEventArgs(request);
                    UssdRequestReceived(this, ussdRequestReceivedEventArgs);

                    _logger.LogInfo("ussdRequestReceivedEventArgs " + ussdRequestReceivedEventArgs.Response.ToString()) ;

                    string responseMessageBody =

                    // "<ussdResponse sourceNumber=\"155\" destinationNumber=\""+ destNumber + "\" content=\""+
                    // ussdRequestReceivedEventArgs.Response.ResponseContent.ToString()+ "\" isNISession=\"false\" closeSession=\"true\" />";
                    new XDocument(

             new XElement("ussdResponse",
             new XAttribute("sourceNumber", request.SourceNumber),
             new XAttribute("destinationNumber", request.DestinationNumber),
             new XAttribute("isNISession", "false"),
             new XAttribute("closeSession", "true"),
             new XAttribute("content", ussdRequestReceivedEventArgs.Response.ResponseContent))).ToString();

                    httpResponse.Body = responseMessageBody;
                }
                e.Response = httpResponse;
                return;
            }

            throw new InvalidOperationException("Please try again later.");
        }

        protected virtual Dictionary<string, string> getRequestParameters(string body)
        {
            Dictionary<string, string> keyValues = new Dictionary<string, string>();

            string pattern = "([a-zA-Z0-9]+=\"[a-zA-Z0-9\\.\\s\\:\\*\\#]+\"\\s)";
            string pattern2 = "^([a-zA-Z]+)=\"([a-zA-Z0-9\\.\\s\\:\\*\\#]+)\"";

            Regex regex = new Regex(pattern);
            MatchCollection matchCollection = regex.Matches(body);

            foreach (var a in matchCollection)
            {
                string[] text = Regex.Split(a.ToString(), pattern2);

                keyValues.Add(text[1], text[2]);

            }

            //foreach (var a in keyValues.Keys) {
            //    _logger.LogInfo(a + " -- "+keyValues[a]);
            //}


            return keyValues;
        }
    }
}
