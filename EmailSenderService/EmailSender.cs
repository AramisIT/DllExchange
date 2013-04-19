using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;
using System.ServiceProcess;
using System.Text;
using System.Timers;
using System.Diagnostics;

namespace EmailSenderService
{
    public partial class Sender : ServiceBase
    {
        private struct Email
        {
            public string IdMail;
            public string SendTo;
            public string Topic;
            public string Body;
        }

        private struct EmailOptions
        {
            public string EmailSender;
            public string Sender;
            public string SmtpServer;
            public string SmtpPort;
            public string User;
            public string Password;
        }

        private SqlConnection connection;
        private const string TABLE_NAME = "AramisUTK.dbo.EmailSending";
        private bool isConnected;
        public const string eventSource = "EmailSenderLog";

        public Sender()
        {
            InitializeComponent();


            if (!EventLog.SourceExists(eventSource))
            {
                EventLog.CreateEventSource(eventSource, eventSource);
            }

            //eventLog.Source = eventSource;

            int delay = 5000;
            try
            {
                int.TryParse((ConfigurationSettings.AppSettings["Delay"]), out delay);
            }
            catch (Exception)
            {
            }

            Timer checkMailTable = new Timer(delay) {Enabled = true};
            checkMailTable.Elapsed += checkMailTable_Elapsed;
            checkMailTable.Start();
        }

        void checkMailTable_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (isConnected)
            {
                if (checkTable())
                {
                    Dictionary<Email, bool> newMails = getEmailDataFromTable();

                    foreach (Email mail in newMails.Keys)
                    {
                        if (sendMessage(mail, getEmailOptions()))
                        {
                            setFlagForMail(mail.IdMail);
                        }
                    }
                }
            }
        }

        protected override void OnStart(string[] args)
        {
            isConnected = connectToDB();
        }

        protected override void OnStop()
        {
            connection.Close();
            //eventLog.WriteEntry("Connection is CLOSED");
        }

        #region Работа с таблицей
        private bool connectToDB()
        {
            //eventLog.WriteEntry("Connection is started ...");
            string cStr = ConfigurationSettings.AppSettings["MSSQL2008ConnectionString"];
            connection = new SqlConnection(cStr);

            try
            {
                connection.Open();
                //eventLog.WriteEntry("Connected to DB!");

                return true;
            }
            catch (Exception exc)
            {
                //eventLog.WriteEntry("Connected is failed!");
                //eventLog.WriteEntry(exc.Message);

                return false;
            }
        }

        private bool checkTable()
        {
            try
            {
                SqlCommand command = new SqlCommand(string.Format("SELECT COUNT(1) FROM {0} WHERE Sended=0", TABLE_NAME), connection);
                object flag = command.ExecuteScalar();
                bool newMail = Convert.ToInt32(flag) != 0;

                //eventLog.WriteEntry(newMail.ToString());

                return newMail;
            }
            catch (Exception exc)
            {
                //eventLog.WriteEntry("Warning: " + exc.Message);

                return false;
            }
        }

        private Dictionary<Email, bool> getEmailDataFromTable()
        {
            SqlCommand command = new SqlCommand(
                string.Format("SELECT Id as IdMail, EMail as SendTo, Theme as Topic, Text as Body FROM {0} WHERE Sended=0", TABLE_NAME),
                connection);
            SqlDataReader reader = command.ExecuteReader();
            Dictionary<Email, bool> newMails = new Dictionary<Email, bool>();

            while (reader.Read())
            {
                Email mail = new Email
                {
                    IdMail = reader["IdMail"].ToString().Trim(),
                    SendTo = reader["SendTo"].ToString().Trim(),
                    Topic = reader["Topic"].ToString().Trim(),
                    Body = reader["Body"].ToString().Trim()
                };

                if (!newMails.ContainsKey(mail))
                {
                    //eventLog.WriteEntry(string.Format(
                        //"NEW E-MAIL!\r\nSendTo: '{0}'\r\nSubject: {1}",
                        //mail.SendTo,
                        //mail.Topic));
                    newMails.Add(mail, true);
                }
            }
            reader.Close();

            return newMails;
        }

        private void setFlagForMail(string id)
        {
            SqlCommand cmd = new SqlCommand(
                string.Format(
                "update {0} set Sended=1, Date='{1}-{2}-{3} {4}' where id={5}", 
                TABLE_NAME,
                DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.ToShortTimeString(),
                id),
                connection);
            cmd.ExecuteNonQuery();
        }
        #endregion

        private  EmailOptions getEmailOptions()
        {
            return new EmailOptions
            {
                EmailSender = ConfigurationSettings.AppSettings["EmailSender"],
                SmtpServer = ConfigurationSettings.AppSettings["SmtpServer"],
                SmtpPort = ConfigurationSettings.AppSettings["SmtpPort"],
                Sender = ConfigurationSettings.AppSettings["Sender"],
                User = ConfigurationSettings.AppSettings["User"],
                Password = ConfigurationSettings.AppSettings["Password"]
            };
        }

        private bool sendMessage(Email mail, EmailOptions options)
        {
            try
            {
                int port;
                int.TryParse(options.SmtpPort, out port);
                //Настройки сервера
                SmtpClient Smtp = new SmtpClient(options.SmtpServer, port)
                {
                    Credentials = new NetworkCredential(options.User, options.Password),
                    EnableSsl = true,
                    Timeout = 60000
                };

                //Формирование письма
                MailMessage Message = new MailMessage
                {
                    From = new MailAddress(options.EmailSender, options.Sender, Encoding.UTF8),
                    Subject = mail.Topic,
                    Body = mail.Body,
                    HeadersEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };

                //Формирование MailTo
                string[] mailToGroup = mail.SendTo.Split(';');
                foreach (string ardess in mailToGroup)
                {
                    if (ardess.Trim() != "")
                    {
                        Message.To.Add(new MailAddress(ardess.Trim()));
                    }
                }

                //Send
                Smtp.Send(Message);

                //eventLog.WriteEntry(string.Format("Письмо на адрес: '{0}' отослано!", mail.SendTo));
                return true;
            }
            catch (Exception exp)
            {
                //eventLog.WriteEntry(
                    //string.Format("Произошла ошибка при отправке почты.\r\nСообщение об ошибке: {0}\r\n{1}",
                    //exp.Message, exp.InnerException != null ? exp.InnerException.Message : "..."));

                return false;
            }
        }
    }
}