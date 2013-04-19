using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Windows;
using System.Xml;
using Microsoft.Win32;

namespace EmailSenderEditor
{
    public partial class MainWindow
    {
        private const string CONFIG_KEY = "PathToConfigFile";
        public MainWindow()
        {
            InitializeComponent();

            configFilePath.Text = ConfigurationSettings.AppSettings[CONFIG_KEY];
        }

        #region button
        private void findConfig_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            open.ShowDialog();

            if(!string.IsNullOrEmpty(open.SafeFileName))
            {
                configFilePath.Text = open.FileName;
            }
        }

        private void OpenConfig_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(configFilePath.Text))
            {
                ConfigWorker.ConfigFile config = ConfigWorker.ReadConfigFile(configFilePath.Text);
                structToControls(config);
            }
        }

        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            ConfigWorker.WriteConfigFile(controlsToStruct(), configFilePath.Text);
            ConfigurationSettings.AppSettings.Set(CONFIG_KEY, configFilePath.Text);
        }

        private void CheckConnection_Click(object sender, RoutedEventArgs e)
        {
            SqlConnection connection = new SqlConnection(ConnectionString.Text);

            try
            {
                connection.Open();
                MessageBox.Show("Подключение выполнено!");
            }
            catch(Exception)
            {
                MessageBox.Show("Подключение не установлено!", "Ошибка подключения...", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                connection.Close();
            }
        }

        private void CheckTable_Click(object sender, RoutedEventArgs e)
        {
            SqlConnection connection = new SqlConnection(ConnectionString.Text);
            const string tableName = "AramisUTK.dbo.EmailSending";

            try
            {
                connection.Open();
                SqlCommand command = new SqlCommand(string.Format("SELECT 1 FROM {0} WHERE 1=0", tableName), connection);
                command.ExecuteNonQuery();
                MessageBox.Show(string.Format("Таблица '{0}' существует!", tableName));
            }
            catch (Exception)
            {
                MessageBox.Show(string.Format("Таблица '{0}' не найдена!\r\nВозможно нет подключения к БД.", tableName),
                    "Ошибка...", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                connection.Close();
            }
        }
        #endregion

        #region Конвертация
        private void structToControls(ConfigWorker.ConfigFile config)
        {
            eMail.Text = config.Email;
            User.Text = config.User;
            Pass.Text = config.Password;
            Sender.Text = config.Sender;
            Server.Text = config.SmtpServer;
            Port.Text = config.SmtpPort;
            Delay.Text = config.Delay;
            ConnectionString.Text = config.ConnString;
        }

        private ConfigWorker.ConfigFile controlsToStruct()
        {
            return new ConfigWorker.ConfigFile
                       {
                           Email = eMail.Text,
                           User = User.Text,
                           Password = Pass.Text,
                           Sender = Sender.Text,
                           SmtpServer = Server.Text,
                           SmtpPort = Port.Text,
                           Delay = Delay.Text,
                           ConnString = ConnectionString.Text
                       };
        }
        #endregion
    }

    public static class ConfigWorker
    {
        public struct ConfigFile
        {
            public string Email;
            public string User;
            public string Password;
            public string Sender;
            public string SmtpServer;
            public string SmtpPort;
            public string Delay;
            public string ConnString;
        }

        private const string KEY = "key";
        private const string VALUE = "value";
        private const string ADD = "add";
        private const string TAG_EMAIL = "EmailSender";
        private const string TAG_USER = "User";
        private const string TAG_PASSWORD = "Password";
        private const string TAG_SENDER = "Sender";
        private const string TAG_SERVER = "SmtpServer";
        private const string TAG_PORT = "SmtpPort";
        private const string TAG_DELAY = "Delay";
        private const string TAG_CONN_STRING = "MSSQL2008ConnectionString";

        public static ConfigFile ReadConfigFile(string path)
        {
            ConfigFile file = new ConfigFile();
            XmlDocument doc = new XmlDocument();
            doc.Load(path);

            if (doc.DocumentElement != null)
            {
                try
                {
                    XmlNode node = doc.DocumentElement.SelectSingleNode("appSettings");

                    file.Email = node.ChildNodes[0].Attributes[VALUE].Value;
                    file.User = node.ChildNodes[1].Attributes[VALUE].Value;
                    file.Password = node.ChildNodes[2].Attributes[VALUE].Value;
                    file.Sender = node.ChildNodes[3].Attributes[VALUE].Value;
                    file.SmtpServer = node.ChildNodes[4].Attributes[VALUE].Value;
                    file.SmtpPort = node.ChildNodes[5].Attributes[VALUE].Value;
                    file.Delay = node.ChildNodes[6].Attributes[VALUE].Value;
                    file.ConnString = node.ChildNodes[7].Attributes[VALUE].Value;
                }
                catch (Exception)
                { }
            }

            return file;
        }

        public static void WriteConfigFile(ConfigFile file, string path)
        {
            XmlTextWriter writer = new XmlTextWriter(path, Encoding.UTF8) {Formatting = Formatting.Indented};
            writer.WriteStartDocument(false);

            writer.WriteStartElement("configuration");
            writer.WriteStartElement("appSettings");
            writeElement(writer, TAG_EMAIL, file.Email);
            writeElement(writer, TAG_USER, file.User);
            writeElement(writer, TAG_PASSWORD, file.Password);
            writeElement(writer, TAG_SENDER, file.Sender);
            writeElement(writer, TAG_SERVER, file.SmtpServer);
            writeElement(writer, TAG_PORT, file.SmtpPort);
            writeElement(writer, TAG_DELAY, file.Delay);
            writeElement(writer, TAG_CONN_STRING, file.ConnString);
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.Close();
        }

        private static void writeElement(XmlTextWriter writer, string key, string value)
        {
            writer.WriteStartElement(ADD, null);
            writer.WriteAttributeString(KEY, key);
            writer.WriteAttributeString(VALUE, value);
            writer.WriteEndElement();
        }
    }
}
