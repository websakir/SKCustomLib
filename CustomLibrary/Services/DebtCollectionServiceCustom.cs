
using Simbrella.Framework.Communication.Abstractions;
using Simbrella.Framework.Core;
using Simbrella.Framework.Factories;
using Simbrella.Framework.Logging2.Abstractions;
using Simbrella.Framework.SimpleMessaging.Attributes;
using Simbrella.Framework.SimpleMessaging.Common;
using Simbrella.SimKredit.Core.Business.Abstractions;
using Simbrella.SimKredit.Core.Common;
using Simbrella.SimKredit.Core.Factories.Abstractions;
using Simbrella.SimKredit.Core.Messaging.Abstractions;
using Simbrella.SimKredit.Core.Services;
using Simbrella.SimKredit.Core.Services.Settings;

namespace CustomLibrary.Services
{
    [V2Service(ServiceType = ServiceType.Simple)]
    public class DebtCollectionServiceCustom : DebtCollectionService
    {
        private DebtManagerCustom _debtManager;
        ILogger _logger = Factory.GetLoggerProvider().GetLogger("debtcollectionservice");
        public DebtCollectionServiceCustom(ISubscriberManager subscriberManager, IDebtManager debtManager, IRecordingManager recordingManager,
            IMessagingManager messagingManager, ITimeManager timeManager, IDebtCollectionSchemeFactory schemeFactory,
            IServiceMessageFactory serviceMessageFactory, DebtCollectionServiceSettings settings, Simbrella.Framework.Logging.Abstractions.ILogger legacyLogger,
            ILoggerProvider loggerProvider, IMQProvider mqProvider)
            : base(subscriberManager, debtManager, recordingManager, messagingManager, timeManager, schemeFactory, serviceMessageFactory, settings,
                  legacyLogger, loggerProvider, mqProvider)
        {
            _debtManager = (DebtManagerCustom)debtManager;
            
        }

        protected override void chargeDebt(IDebtCollectionMessage message, int delaySeconds, IMQTransaction transaction)
        {
            _logger.LogInfo("ChargeDebr started");
            DebtCollectionInfo currentDebt = message.Debts[message.DebtInProcess];
            message.Parameters.Add("transactionID", _debtManager.GetDebtTransactionID(currentDebt.Debt.DebtID));
            _logger.LogInfo("TransactionID added to parameter");
            base.chargeDebt(message, delaySeconds, transaction);

            _logger.LogInfo("Base charge Debt run");
        }


    }
}
