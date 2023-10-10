using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using AE.Net.Mail;
using CAPA_DATOS;

namespace CAPA_DATOS.Services
{
    public class MailConfig
    {
        public string? HOST { get; set; }
        public string? USERNAME { get; set; }
        public string? PASSWORD { get; set; }
    }
    public class IMAPServices
    {
        //const string USERNAME = "wdevexp@outlook.com";
        //const string PASSWORD = "%WtestDev2023%";
        //const string USERNAME = "amejia@ximtechnology.onmicrosoft.com";
        //const string PASSWORD = "%3e2w1qazsX";
        const string HOST = "outlook.office365.com";
        const int PORT = 993;
        public ImapClient GetClient(MailConfig config)
        {
            return new ImapClient(config.HOST, config.USERNAME, config.PASSWORD, AuthMethods.Login, PORT, true);
        }
        // public static Pop3Client GetExchangeEWSClient()
        // {
        //     const string HOST = "outlook.office365.com";
        //     const int PORT = 995;
        //     //const string username = "amejia@ximtechnology.onmicrosoft.com";
        //     //const string password = "%3e2w1qazsX";

        //     const string username = "wilberj1987@hotmail.com";
        //     const string password = "%Wmatus$1987%";
        //     Pop3Client pop3Client = new Pop3Client();
        //     pop3Client.Connect(HOST, PORT, true);
        //     pop3Client.Authenticate(username, password);
        //     return pop3Client;
        // }
        // public object getData()
        // {
        //     var client = GetExchangeEWSClient();
        //     List<Message> messages = new();
        //     int i = client.GetMessageCount();
        //     while (i > 1)
        //     {
        //         messages.Add(client.GetMessage(i));
        //         i--;
        //     }
        //     return messages.Select(m => m.Headers.Subject + " - "+ m.MessagePart.ToString() ).ToList();
        // }
        public object getData()
        {
            // List<string> ids = new List<string>();
            // List<MailMessage> mails = new List<MailMessage>();

            // using (var imap = new ImapClient(HOST, USERNAME, PASSWORD, AuthMethods.Login, PORT, true))
            // {
            //     imap.SelectMailbox("INBOX");
            //     var MailMessage = imap.SearchMessages(SearchCondition.Unseen()).Select(m => m.Value).ToList();
            //     foreach (var mail in MailMessage)
            //     {
            //         mails.Add(mail);
            //         ids.Add(mail.Uid + " - " + mail.Subject + " - " + mail.Body);
            //         imap.MoveMessage(mail.Uid, "READY");
            //     }
            //     imap.Expunge();
            // for (int i = 0; i < msgs.Length; i++)
            // {
            //     Lazy<MailMessage> msgId = msgs[i];
            //     ids.Add(msgId.Value.Subject + " - " +msgId.Value.Body);
            //     mails.Add(msgId.Value);
            // }

            // foreach (string id in ids)
            // {
            //     mails.Add(imap.GetMessage(id, headersonly: false));
            // }
            //}

            // foreach (var msg in mails)
            // {
            //     foreach (var att in msg.Attachments)
            //     {
            //         string fName;
            //         fName = att.Filename;
            //     }
            // }
            return true; //ids;
        }
        public object GetData2()
        {
            // Datos de la cuenta de correo
            string server = "outlook.office365.com";
            int port = 993; // Puerto seguro IMAPS
            string username = "amejia@ximtechnology.onmicrosoft.com";
            string password = "%3e2w1qazsX";

            using (var client = new MailKit.Net.Imap.ImapClient())
            {
                // Configurar la conexión
                client.Connect(server, port, MailKit.Security.SecureSocketOptions.StartTls);

                // Autenticación
                client.Authenticate(username, password);


                // Realizar acciones con el cliente IMAP
                // Por ejemplo, puedes listar carpetas, leer correos, etc.

                // Desconectar
                client.Disconnect(true);
            }
            return true;
        }
    }


}
