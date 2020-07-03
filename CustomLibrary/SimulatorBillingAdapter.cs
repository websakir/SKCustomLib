using System;
using System.Collections.Generic;

using Simbrella.Framework.Communication.Abstractions;
using Simbrella.Framework.Config.Abstractions;
using Simbrella.Framework.Factories;
using Simbrella.Framework.Integration.Http;
using Simbrella.SimKredit.Core.Business;
using Simbrella.SimKredit.Core.Business.Abstractions;
using Simbrella.SimKredit.Core.Common;
using Simbrella.SimKredit.Core.Common.Abstractions;
using Simbrella.Framework.Logging2.Abstractions;
using Simbrella.SimKredit.Core.Messaging.Abstractions;
using Simbrella.SimKredit.Core.Common.Enums;
using System.Text.RegularExpressions;

namespace CustomAdapter
{
    public class SimulatorBillingAdapter : IBillingAdapter, IDisposable
    {
        ILogger logger = Factory.GetLoggerProvider().GetLogger("billingservice");
        private HttpClient _httpClient = new HttpClient("http://localhost:9000/", "POST");
        private HttpClient _httpClientScoring = new HttpClient("http://localhost:9000/billingserver/", "POST");
        private IIDManager _idManager;

        public SimulatorBillingAdapter(IConfigItem config)
          : this(config, ManagerFactory.GetIDManager())
        {
        }

        public SimulatorBillingAdapter(IConfigItem config, IIDManager idManager)
        {
            _idManager = idManager;
        }


        public event EventHandler<PerformanceDataEventArgs> PublishPerformance;

        public event EventHandler<ReceiptReceivedEventArgs> ReceiptReceived;


        object IBillingAdapter.CheckStatus(string key)
        {
            return 1;
        }

        void IDisposable.Dispose()
        {
            throw new NotImplementedException();
        }

        int IBillingAdapter.Process(IBillingMessage message, IMQTransaction tx)
        {
            HttpRequest request = new HttpRequest();

            HttpResponse httpResponse = null;
            

            logger.LogInfo(" message " + message.BillingCommand);
            

            if (message.BillingCommand == (int)BillingCommand.QueryCollectionData)
            {
                httpResponse = (HttpResponse)_httpClientScoring.SendMessage(request);
                message.BillingOutputParameters["SubscriberBalance"] = (decimal)Math.Abs(20.0);
            }
            else if (message.BillingCommand == (int)BillingCommand.QueryScoringData)
            {

                string messageBody = "<billingRequest command=\"scoring\" msisdn=\"" + message.BillingSubscriberID + "\" />";
                request.Body = messageBody;

                httpResponse = (HttpResponse)_httpClientScoring.SendMessage(request);

                Dictionary<string, string> keyValues = getResponseParameters( httpResponse.Body);

                keyValues.TryGetValue("ActivationDate", out string activationDate);
                keyValues.TryGetValue("LifecycleStatus", out string lifecycleStatus);

                message.BillingOutputParameters["ActivationDate"] = activationDate ?? string.Empty;
                message.BillingOutputParameters["LifecycleStatus"] = lifecycleStatus ?? string.Empty;

            }
            else if (message.BillingCommand == (int)BillingCommand.BalanceAdjustment)
            {
                message.Parameters.TryGetValue("transactionID", out object value);
                logger.LogInfo("transaction id 22 " + value);
                string transactionID = (string)value;
                request.Body = transactionID ?? string.Empty;

                httpResponse = (HttpResponse)_httpClient.SendMessage(request);
                message.Parameters["transactionID"] = httpResponse.Body;
            }

            if (httpResponse != null & httpResponse.StatusCode == 200)
            {
                message.BillingSuccess = true;
                message.ReceiptRequired = false;
                message.BillingOutputParameters["ChargedAmount"] = (decimal)Math.Abs(1.0);

                return message.BillingCommand;
            }

            return httpResponse.StatusCode;
        }

        protected virtual Dictionary<string, string> getResponseParameters(string body)
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
