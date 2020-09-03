using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

using Lextm.SharpSnmpLib.Security;

using Simbrella.Framework.Communication.Abstractions;
using Simbrella.Framework.Config.Abstractions;
using Simbrella.Framework.DAL.Abstractions;
using Simbrella.Framework.Factories;
using Simbrella.Framework.Integration.Ftp;
using Simbrella.Framework.Logging2.Abstractions;
using Simbrella.SimKredit.Core.Common;
using Simbrella.SimKredit.Core.Messaging.Abstractions;

namespace CustomLibrary.Jobs
{
    class PaymentAcknowledgeJob : JobBase
    {

        private FtpClient ftpClient;
        private IDAL _dAL;
        private ILogger _logger = Factory.GetLoggerProvider().GetLogger("jobexecutionservice");
        public PaymentAcknowledgeJob(IConfigItem item):base()     
        {
            string url = "", username = "", pass="";


            foreach (var i in item.GetChildren()) {

                if (i.ElementName == "serverUrl")
                { url = i.Value; }


                if (i.ElementName == "username")
                { username = i.Value; }

                if (i.ElementName == "password")
                { pass = i.Value; }

            }


            ftpClient = new FtpClient(url, username, pass);
            _dAL = Factory.GetDal();


        }


        public override void ExecuteJob(IJobExecutionMessage message, IMQTransaction transaction)
        {


            //DirectoryInfo directory = new DirectoryInfo(dir);
            //string[] files = (string[])directory.GetFiles("topup_*").Where(f => DateTime.Compare(DateTime.Now.AddHours(-1.0), f.CreationTime) < 0)
            //    .Select(z => z.Name).ToArray();

            string topups = GetTopupFileNames();

            if (!topups.Equals(""))
            {
                FtpMessage ftpMessage = new FtpMessage();
                ftpMessage.FileName = "ProcessedPaymentFiles_"+ DateTime.Now.ToString("yyyyMMddhh24mmss")+".txt";
                ftpMessage.Body = Encoding.ASCII.GetBytes(topups);
                ftpClient.SendMessage(ftpMessage);
            }

        }


        public string GetTopupFileNames() {

            DataTable dataTable =  _dAL.GetData("SELECT * FROM NOTIFICATIONS   where INSERTTIME > dateadd(HOUR, -1, CURRENT_TIMESTAMP) ");
            StringBuilder filesnames = new StringBuilder();
            for (int i = 0; i < dataTable.Rows.Count; i++) 
            {
                filesnames.Append((i+1)+". "+dataTable.Rows[i]["FILENAME"]+"\n");
    
            }

            return filesnames.ToString() ;
        }

    }
}
