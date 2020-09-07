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

namespace CustomLibrary
{
    public class SimulatorBillingAdapter : IBillingAdapter, IDisposable
    {
        private ILogger _logger = Factory.GetLoggerProvider().GetLogger("billingservice");
        private HttpClient _httpClient = new HttpClient("http://localhost:9000/", "POST");
        private HttpClient _httpBillingSimulator = new HttpClient("http://localhost:9000/billingserver/", "POST");
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
            

            _logger.LogInfo(" billing message started" + message.BillingCommand);
            

            if (message.BillingCommand == (int)BillingCommand.QueryCollectionData)
            {
                string messageText = "<billingRequest command = \"balance\" msisdn = \""+ message.BillingSubscriberID + "\" />";
                request.Body = messageText;


                   httpResponse = (HttpResponse)_httpBillingSimulator.SendMessage(request);

                Dictionary<string, string> keyValues = getResponseParameters(httpResponse.Body);

                keyValues.TryGetValue("balance", out string balance);
                _logger.LogInfo("Subscriber balance:  "+balance);
                message.BillingOutputParameters["SubscriberBalance"] = Convert.ToDecimal(balance ?? "0.0");
            }
            else if (message.BillingCommand == (int)BillingCommand.QueryScoringData)
            {

                string messageBody = "<billingRequest command=\"scoring\" msisdn=\"" + message.BillingSubscriberID + "\" />";
                request.Body = messageBody;

                httpResponse = (HttpResponse)_httpBillingSimulator.SendMessage(request);

                Dictionary<string, string> keyValues = getResponseParameters( httpResponse.Body);

                foreach (var a in keyValues.Keys)
                {
                    _logger.LogInfo(a + " => "+keyValues[a]);
                }


                keyValues.TryGetValue("activationDate", out string activationDate);
                keyValues.TryGetValue("lifecycleStatus", out string lifecycleStatus);
                keyValues.TryGetValue("tariffType", out string tariffType);

                _logger.LogInfo("activationDate: "+ activationDate);
                _logger.LogInfo("lifecycleStatus: " + lifecycleStatus);
                message.BillingOutputParameters["ActivationDate"] = activationDate ?? string.Empty;
                message.BillingOutputParameters["LifecycleStatus"] = lifecycleStatus ?? string.Empty;
                message.BillingOutputParameters["SubscriberType"] = tariffType ?? string.Empty;
    

            }
            else if (message.BillingCommand == (int)BillingCommand.BalanceAdjustment)
            {
                _logger.LogInfo("message.BillingSubscriberID *** "+ message.BillingSubscriberID);

                message.BillingInputParameters.TryGetValue("AdjustAmount", out object adjustAmount);
                _logger.LogInfo("message.BillingOutputParameters[\"ChargedAmount\"] *** " + adjustAmount);
                _logger.LogInfo(" message.ID *** " + message.ID);

                string messageBody = "<billingRequest command=\"balanceadjustment\" msisdn=\"" + message.BillingSubscriberID
                    + "\" amount=\"" + (adjustAmount ?? 0) + "\" transactionId=\"" + message.ID + "\" />";

                message.Parameters.TryGetValue("transactionID", out object value);
                

                string transactionID = (string)value;
                request.Body = messageBody;
                _logger.LogInfo("transaction id 23 " + value);
                httpResponse = (HttpResponse)_httpBillingSimulator.SendMessage(request);
                _logger.LogInfo("transaction id 24 " + value);
                Dictionary<string, string> keyValues  = getResponseParameters(httpResponse.Body);


                keyValues.TryGetValue("transactionId", out  transactionID);
                keyValues.TryGetValue("adjustedAmount", out string adjustedAmount);

                _logger.LogInfo("transaction id 25 " + transactionID);
                if (value == null)
                {
                    _logger.LogInfo("transaction id 26 ");
                    message.Parameters.Add("transactionID", transactionID);
                }
                else 
                {
                    _logger.LogInfo("transaction id 27" + value);
                    message.Parameters["transactionID"] =  transactionID;
                }
                message.BillingOutputParameters["ChargedAmount"] = Math.Abs(Convert.ToDecimal(adjustedAmount ?? "0"));
                _logger.LogInfo("transaction id in parameters "+message.Parameters["transactionID"]);

            }

            if (httpResponse != null & httpResponse.StatusCode == 200)
            {
                message.BillingSuccess = true;
                message.ReceiptRequired = false;
                

                return message.BillingCommand;
            }

            return httpResponse.StatusCode;
        }

        protected virtual Dictionary<string, string> getResponseParameters(string body)
        {
            Dictionary<string, string> keyValues = new Dictionary<string, string>();

            string pattern = "([a-zA-Z0-9]+=\"[a-zA-Z0-9\\.\\s\\:\\*\\#\\-]+\"\\s)";
            string pattern2 = "^([a-zA-Z]+)=\"([a-zA-Z0-9\\.\\s\\:\\*\\#\\-]+)\"";

            Regex regex = new Regex(pattern);
            MatchCollection matchCollection = regex.Matches(body);

            foreach (var a in matchCollection)
            {
                string[] text = Regex.Split(a.ToString(), pattern2);
                keyValues.Add(text[1], text[2]);
                _logger.LogInfo(text[1]+ "  =>  "+text[2]);
            }

            return keyValues;
        }


    }
}
