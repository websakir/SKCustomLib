using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Simbrella.Framework.Config.Abstractions;
using Simbrella.Framework.DAL.Abstractions;
using Simbrella.Framework.Factories;
using Simbrella.Framework.Logging2.Abstractions;
using Simbrella.SimKredit.Core.Business;
using Simbrella.SimKredit.Core.Business.Abstractions;
using Simbrella.SimKredit.Core.Common;
using Simbrella.SimKredit.Core.Common.Abstractions;

namespace CustomLibrary
{
    public class DebtManagerCustom : DebtManager
    {
        private IDAL _dal = Factory.GetDal();
        ILogger _logger = Factory.GetLoggerProvider().GetLogger("creditprovisionservice");
        public DebtManagerCustom(IConfigItem config) : base(config)
        {
        }

        public DebtManagerCustom(IConfigItem config, IIDManager idManager, IDAL dal, ILoggerProvider loggerProvider) : base(config, idManager, dal, loggerProvider)
        {
        }

        public override IDebt CreateDebt(
          long debtId, string subscriberId, string subscriberBId, string productType, string creditType,
          decimal creditAmount, decimal serviceFee, byte status, string serviceFeeUpdateScheme,
          int serviceFeeUpdateDays, ManagerMessageInfo messageInfo, DbTransaction transaction)
        {

            _logger.LogInfo("custom CreateDebt is running");
            IDebt debt = base.CreateDebt(debtId, subscriberId, subscriberBId, productType, creditType, creditAmount, serviceFee, status,
                serviceFeeUpdateScheme, serviceFeeUpdateDays, messageInfo, transaction);

            string transactionID = (string)messageInfo.Message.Parameters["transactionID"];
            _logger.LogInfo("TRANSACTION ID is " + transactionID);
            CreateTransaction(debtId, transactionID, transaction);

            return debt;
        }



        public void CreateTransaction(long debtId, string transactionID, DbTransaction dbTransaction)
        {

            _logger.LogInfo("transaction " + transactionID + " is persisting");
            _dal.Execute("insert into DEBTSTRANSACTION (DEBTID, TRANSACTIONID) values ('" + debtId + "', '" + transactionID + "')",
                CommandType.Text, dbTransaction);

        }


        public string GetDebtTransactionID(long debtID)
        {

            string transactionID =
                (string)_dal.GetScalar("select TRANSACTIONID from DEBTSTRANSACTION where DEBTID = :DEBTID", _dal.CreateParameter("DEBTID", debtID));

            _logger.LogInfo("debt ID " + debtID + ",  transaction ID " + transactionID);
            return transactionID;
        }




    }
}
