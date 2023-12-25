using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using System.Threading.Tasks;
using MailKit.Net.Smtp;
using Microsoft.Identity.Client;

namespace CAPA_DATOS.Services
{
    public class SMTPMailServices
    {
        const string USERNAME = "wdevexp@outlook.com";
        const string PASSWORD = "%WtestDev2023%";
        //const string USERNAME = "amejia@ximtechnology.onmicrosoft.com";
        //const string PASSWORD = "%3e2w1qazsX";
        const string HOST = "smtp-mail.outlook.com";
        const int PORT = 587;
        static async Task<bool> SendMailAuth2(string from,
            List<string> toMails,
            string subject,
            string body,
            List<ModelFiles> attach,
            MailConfig mailConfig)
        {
            try
            {
                var clientId = mailConfig.CLIENT;
                var clientSecret = mailConfig.CLIENT_SECRET;
                var tenantId = mailConfig.TENAT;
                var scopes = new[] { mailConfig.HOST }; // Ajusta los ámbitos según tus necesidades.

                var cca = ConfidentialClientApplicationBuilder.Create(clientId)
                    .WithClientSecret(clientSecret)
                    .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
                    .Build();

                var result = await cca.AcquireTokenForClient(scopes)
                    .ExecuteAsync();

                var message = new MimeKit.MimeMessage();
                toMails.ForEach(m =>
                {
                    message.To.Add(new MimeKit.MailboxAddress("-", m));
                });
                message.From.Add(new MimeKit.MailboxAddress("HELPDESK", mailConfig.USERNAME));

                message.Subject = subject;
                message.Body = new MimeKit.TextPart("html")
                {
                    Text = body
                };

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    await client.ConnectAsync(mailConfig.HOST, PORT, false);
                    await client.AuthenticateAsync(mailConfig.USERNAME, result.AccessToken);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
                return true;
            }
            catch (Exception ex)
            {
                LoggerServices.AddMessageError($"error enviando correo desde {mailConfig.HOST} {mailConfig.USERNAME}", ex);
                  return false;
            }
        }

        public static bool SendMailBasic(string from,
            List<string> toMails,
            string subject,
            string body,
            List<ModelFiles> attach,
            MailConfig config)
        {
            try
            {
                //var templatePage = Path.Combine(System.IO.Path.GetFullPath("../UI/Pages/Mails"), path);
                MailMessage correo = new MailMessage();
                correo.From = new MailAddress(config.USERNAME, "HELPDESK", System.Text.Encoding.UTF8);//Correo de salida
                foreach (string toMail in toMails)
                {
                    correo.To.Add(toMail); //Correos de destino
                }

                if (attach != null)
                {
                    foreach (var files in attach)
                    {
                        Attachment AttachFile = new Attachment(files.Value);
                        correo.Attachments.Add(AttachFile);
                    }
                }
                correo.Subject = subject; //Asunto
                correo.Body = from + ": " + body;//ContractService.RenderTemplate(templatePage, model);
                correo.IsBodyHtml = true;
                correo.Priority = MailPriority.Normal;
                System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient
                {
                    UseDefaultCredentials = false,
                    Host = config.HOST ?? "",
                    Port = PORT, //Puerto de salida 
                    Credentials = new System.Net.NetworkCredential(config.USERNAME, config.PASSWORD)//Cuenta de correo
                };
                ServicePointManager.ServerCertificateValidationCallback +=
                  (sender, cert, chain, sslPolicyErrors) => true;
                smtp.EnableSsl = true;//True si el servidor de correo permite ssl
                smtp.Send(correo);
                  return true;
            }
            catch (Exception ex)
            {
                LoggerServices.AddMessageError($"error enviando correo desde {config.HOST} {config.USERNAME}", ex);
                return false;
            }
        }
        public async static Task<bool> SendMail(string from,
           List<string> toMails,
           string subject,
           string body,
           List<ModelFiles> attach,
           MailConfig config)
        {
            if (config.AutenticationType == AutenticationTypeEnum.AUTH2)
            {
                return await SendMailAuth2(from, toMails, subject, body, attach, config);
            }
            else
            {
                if (config == null)
                {
                    config = new MailConfig() { HOST = HOST, PASSWORD = PASSWORD, USERNAME = USERNAME };
                }
                return SendMailBasic(from, toMails, subject, body, attach, config);
            }
        }
    }

}
