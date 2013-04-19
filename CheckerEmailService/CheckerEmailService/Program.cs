using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace CheckerEmailService
{
    class Program
    {
        private struct Email
        {
            public string IdMail;
            public string SendTo;
            public string Topic;
            public string Body;
        }

        private  struct EmailOptions
        {
            public string EmailSender;
            public string Sender;
            public string SmtpServer;
            public string SmtpPort;
            public string User;
            public string Password;
        }

        private static SqlConnection connection;
        private const string TABLE_NAME = "AramisUTK.dbo.EmailSending";

        static void Main(string[] args)
        {
            if (connectToDB())
            {
                if (checkTable())
                {
                    IEnumerable<Email> newMails = getEmailDataFromTable();

                    foreach (Email mail in newMails)
                    {
                        if(sendMessage(mail, getEmailOptions()))
                        {
                            setFlagForMail(mail.IdMail);
                        }
                    }
                }

                connection.Close();
                Console.WriteLine("Connection is CLOSED");
            }

            Console.WriteLine("Press any key to EXIT");
            Console.ReadKey();
        }

        #region Работа с таблицей
        private static bool connectToDB()
        {
            bool result;

            Console.WriteLine("Connection is started ...");
            string cStr = ConfigurationSettings.AppSettings["MSSQL2008ConnectionString"];
            connection = new SqlConnection(cStr);

            try
            {
                connection.Open();
                Console.WriteLine("Connected to DB!");
                result = true;
            }
            catch (Exception exc)
            {
                Console.WriteLine("Connected is failed!");
                Console.WriteLine(exc.Message);
                result = false;
            }

            return result;
        }

        private static bool checkTable()
        {
            try
            {
                SqlCommand command = new SqlCommand(string.Format("SELECT COUNT(1) FROM {0} WHERE Sended=0", TABLE_NAME), connection);
                object flag = command.ExecuteScalar();
                bool newMail = Convert.ToInt32(flag) != 0;

                Console.WriteLine(newMail);

                return newMail;
            }
            catch (Exception exc)
            {
                Console.WriteLine("Warning: " + exc.Message);

                return  false;
            }
        }

        private static IEnumerable<Email> getEmailDataFromTable()
        {
            SqlCommand command = new SqlCommand(
                string.Format("SELECT Id as IdMail, EMail as SendTo, Theme as Topic, Text as Body FROM {0} WHERE Sended=0", TABLE_NAME), 
                connection);
            SqlDataReader reader = command.ExecuteReader();
            List<Email> newMails = new List<Email>();

            while (reader.Read())
            {
                Email mail = new Email
                {
                    IdMail = reader["IdMail"].ToString().Trim(),
                    SendTo = reader["SendTo"].ToString().Trim(),
                    Topic = reader["Topic"].ToString().Trim(),
                    Body = reader["Body"].ToString().Trim()
                };

                Console.WriteLine("--- New e-mail ---");
                Console.WriteLine("SendTo: " + mail.SendTo);
                Console.WriteLine("Topic: " + mail.Topic);
                Console.WriteLine("Body: " + mail.Body);

                newMails.Add(mail);
            }
            reader.Close();

            return newMails;
        }

        private static void setFlagForMail(string id)
        {
            SqlCommand command = new SqlCommand(
                string.Format("update {0} set Sended=1 where id={1}", TABLE_NAME, id),
                connection);
            command.ExecuteNonQuery();
        }
        #endregion

        private static EmailOptions getEmailOptions()
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

        private static bool sendMessage(Email mail,EmailOptions options)
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

                Console.WriteLine(string.Format("Письмо на адрес: '{0}' отослано!", mail.SendTo));
                return true;
            }
            catch (Exception exp)
            {
                Console.Write(
                    string.Format("Произошла ошибка при отправке почты.\r\nСообщение об ошибке: {0}\r\n{1}",
                    exp.Message, exp.InnerException != null ? exp.InnerException.Message : "..."));

                return false;
            }
        }
    }
}
