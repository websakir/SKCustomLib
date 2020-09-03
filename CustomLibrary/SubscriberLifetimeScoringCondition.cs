using System;
using Simbrella.Framework.Communication.Abstractions;
using Simbrella.Framework.Config.Abstractions;
using Simbrella.Framework.Factories;
using Simbrella.Framework.Logging2.Abstractions;
using Simbrella.SimKredit.Core.Common;
using Simbrella.SimKredit.Core.Common.Enums;
using Simbrella.SimKredit.Core.Messaging.Abstractions;
using Simbrella.SimKredit.Core.Portfolio.Scoring;

namespace CustomLibrary
{
    public class SubscriberLifetimeScoringCondition : NetworkLifetimeScoringCondition
    {

        ILogger _logger = Factory.GetLoggerProvider().GetLogger("scoringservice");

        public SubscriberLifetimeScoringCondition(IConfigItem configItem) : base(configItem)
        {
        }

        protected override decimal getDaysInNetwork(IScoringMessage message)
        {
            message.ScoringData.TryGetValue("ActivationDate", out object value);

            _logger.LogInfo("Activation Date => "+value.ToString());

            DateTime activationDate = Convert.ToDateTime(value);

            return (decimal)(DateTime.Now - activationDate).TotalDays;
        }


    }
}
