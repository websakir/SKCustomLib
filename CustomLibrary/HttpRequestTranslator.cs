using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

using Simbrella.Framework.Config.Abstractions;
using Simbrella.Framework.Core;
using Simbrella.Framework.DAL.Abstractions;
using Simbrella.Framework.Factories;
using Simbrella.Framework.Integration.Http;
using Simbrella.Framework.Logging2.Abstractions;
using Simbrella.SimKredit.Core.Common;

using HttpRequest = Simbrella.Framework.Integration.Http.HttpRequest;

namespace CustomLibrary
{
    public class HttpRequestTranslator : SmsRequestTranslator
    {
        private HttpServer _httpServer;
        private IDAL _dAL = Factory.GetDal();
        private ILogger _logger = Factory.GetLoggerProvider().GetLogger("requestservice");

        public HttpRequestTranslator(IConfigItem config)
        {
            _httpServer = new HttpServer("http://+:9000/", false);
            _httpServer.MessageReceived += RequestReceived;
        }
        public override bool IsChannelHealthy => true;

        public override event EventHandler<SmsRequestReceivedEventArgs> SmsRequestReceived;

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

            Simbrella.Framework.Integration.Http.HttpResponse httpResponse = new Simbrella.Framework.Integration.Http.HttpResponse();
            if (SmsRequestReceived != null)
            {
                HttpRequest httpReq = (HttpRequest)e.Request;

                if (httpReq.Body == null)
                {
                    httpResponse.Body = "ERROR";
                    _logger.LogCritical("Request body invalid or missing!!!");
                }
                else
                {
                  //  _logger.LogInfo("smsMessage " + httpReq.Body);

                    Dictionary<string, string> requestParameters = getRequestParameters(httpReq.Body);
                    requestParameters.TryGetValue("sourceNumber", out string sourceNumber);
                    requestParameters.TryGetValue("destinationNumber", out string destNumber);
                    requestParameters.TryGetValue("content", out string content);
                    requestParameters.TryGetValue("eventTime", out string eventTime);


                    _logger.LogInfo("message details ", new { sourceNumber, destNumber , content , eventTime });



                    SmsMessage request = new SmsMessage(sourceNumber ?? string.Empty, destNumber ?? string.Empty, content ?? string.Empty);

                    request.Parameters["localEventTime"] = eventTime;
                    SmsRequestReceived(this, new SmsRequestReceivedEventArgs(request));

                    httpResponse.Body = "OK";
                }
                e.Response = httpResponse;
                return;
            }

            throw new InvalidOperationException("Please try again later.");

        }

        protected virtual Dictionary<string, string> getRequestParameters(string body)
        {
            Dictionary<string, string> keyValues = new Dictionary<string, string>();

            string pattern = "(\\S+)=[\"\']?((?:.(?![\"\']?\\s + (?:\\S +)=|[> \"\']))+.)[\"\']?";
            string pattern2 = "^([a-zA-Z]+)=\"([a-zA-Z0-9]+)\"";

            Regex regex = new Regex(pattern);
            MatchCollection matchCollection = regex.Matches(body);

            foreach (var a in matchCollection)
            {
                string[] text = Regex.Split(a.ToString(), pattern2);

                keyValues.Add(text[1], text[2]);

            }




            return keyValues;
        }

    }
}
