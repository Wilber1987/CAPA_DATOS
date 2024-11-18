using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Identity.Client;
using MimeKit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CAPA_DATOS.Services
{
	public class SMTPMailServices
	{
		const string USERNAME = "wdevexp@outlook.com";
		const string PASSWORD = "%WtestDev2023%1";
		const string HOST = "smtp-mail.outlook.com";
		const int PORT = 587;
		const string ZOHO_API_URL = "https://api.zeptomail.com/v1.1/email";

		public static bool SendMailWithZohoAPI(string from, List<string> toMails, string subject, string body, MailConfig mailConfig)
		{
			var message = new MimeMessage();
			message.From.Add(new MailboxAddress("noreply", "noreply@cca.edu.ni"));
			message.To.Add(new MailboxAddress("Kevin Benavides Castro", "alderhernandez@gmail.com"));
			message.Subject = "Test Email";
			message.Body = new TextPart("html")
			{
				Text = "Test email sent successfully."
			};
			var client = new MailKit.Net.Smtp.SmtpClient();
			try
			{
				client.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
				client.Connect("smtp.zeptomail.com", 587, false);
				client.Authenticate("emailapikey", "wSsVR61/qx72Da10nzSkIb85ml9cB1qlRx8pjgSl4yetHqzG8sdpl0XGAw/0G/gWEGZhRWEQrbh4mh1VhzQI2ot/mAoFXiiF9mqRe1U4J3x17qnvhDzIXmtZlBaAJIIJwApun2FoGsgr+g==");
				client.Send(message);
				client.Disconnect(true);
			}
			catch (Exception e)
			{
				Console.Write(e.Message);
				return true;
			}
			return true;

		}

		public async static Task<bool> SendMailWithZohoAPIatach(string from, List<string> toMails, string subject, string body, MailConfig mailConfig, string attachmentPath)
		{
			// Configuración de protocolo de seguridad
			System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

			// URL base de Zoho
			var baseAddress = "https://mail.zoho.com/api/accounts/123456789/messages"; // Cambiar 123456789 por tu account ID real

			// Configuración de la solicitud HTTP
			var http = (HttpWebRequest)WebRequest.Create(new Uri(baseAddress));
			http.Accept = "application/json";
			http.ContentType = "application/json";
			http.Method = "POST";
			http.PreAuthenticate = true;
			http.Headers.Add("Authorization", "Zoho-enczapikey wSsVR61/qx72Da10nzSkIb85ml9cB1qlRx8pjgSl4yetHqzG8sdpl0XGAw/0G/gWEGZhRWEQrbh4mh1VhzQI2ot/mAoFXiiF9mqRe1U4J3x17qnvhDzIXmtZlBaAJIIJwApun2FoGsgr+g=="); // Reemplaza ***** con tu token OAuth válido

			// Lee el archivo adjunto
			string fileName =  Path.GetFileName(attachmentPath);
			byte[] fileBytes = File.ReadAllBytes(attachmentPath);
			string base64File = Convert.ToBase64String(fileBytes);

			// Construcción del JSON para la solicitud
			var parsedContent = new JObject
			{
				["fromAddress"] = from,
				["toAddress"] = string.Join(",", toMails), // Convierte la lista de destinatarios en una cadena separada por comas
				["subject"] = subject,
				["content"] = body,
				["attachments"] = new JArray
		{
			new JObject
			{
				["storeName"] = "Base64", // Representación Base64 del archivo
                ["attachmentPath"] = attachmentPath, // Ruta local del archivo
                ["attachmentName"] = fileName // Nombre del archivo
            }
		}
			};

			Console.WriteLine(parsedContent.ToString());

			// Codifica el JSON y escribe en la solicitud
			ASCIIEncoding encoding = new ASCIIEncoding();
			Byte[] bytes = encoding.GetBytes(parsedContent.ToString());

			using (Stream newStream = http.GetRequestStream())
			{
				newStream.Write(bytes, 0, bytes.Length);
			}

			// Envía la solicitud y obtiene la respuesta
			try
			{
				var response = http.GetResponse();
				using (var stream = response.GetResponseStream())
				{
					var sr = new StreamReader(stream);
					var content = sr.ReadToEnd();
					Console.WriteLine(content);
					return true;
				}
			}
			catch (WebException ex)
			{
				using (var stream = ex.Response.GetResponseStream())
				{
					var sr = new StreamReader(stream);
					Console.WriteLine("Error: " + sr.ReadToEnd());
				}
				return false;
			}
		}


		public static bool SendMailWithZohoAPI2(string from, List<string> toMails, string subject, string body, MailConfig mailConfig)
		{

			System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
			var baseAddress = "https://api.zeptomail.com/v1.1/email";

			var http = (HttpWebRequest)WebRequest.Create(new Uri(baseAddress));
			http.Accept = "application/json";
			http.ContentType = "application/json";
			http.Method = "POST";
			http.PreAuthenticate = true;
			http.Headers.Add("Authorization", "Zoho-enczapikey wSsVR61/qx72Da10nzSkIb85ml9cB1qlRx8pjgSl4yetHqzG8sdpl0XGAw/0G/gWEGZhRWEQrbh4mh1VhzQI2ot/mAoFXiiF9mqRe1U4J3x17qnvhDzIXmtZlBaAJIIJwApun2FoGsgr+g==");
			JObject parsedContent = JObject.Parse("{'from': { 'address': 'noreply@cca.edu.ni'},'to': [{'email_address': {'address': '" + toMails[0] + "','name': 'cca'}}],'subject':'Actualizaci&oacuten de datos','htmlbody':'" + body + "'}");
			Console.WriteLine(parsedContent.ToString());
			ASCIIEncoding encoding = new ASCIIEncoding();
			Byte[] bytes = encoding.GetBytes(parsedContent.ToString());

			Stream newStream = http.GetRequestStream();
			newStream.Write(bytes, 0, bytes.Length);
			newStream.Close();

			var response = http.GetResponse();

			var stream = response.GetResponseStream();
			var sr = new StreamReader(stream);
			var content = sr.ReadToEnd();
			Console.WriteLine(content);
			return false;

			/*System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
			var baseAddress = "https://api.zeptomail.com/v1.1/email";

			var http = (HttpWebRequest)WebRequest.Create(new Uri(baseAddress));
			http.Accept = "application/json";
			http.ContentType = "application/json; charset=UTF-8"; // Tipo de contenido en UTF-8
			http.Method = "POST";
			http.PreAuthenticate = true;
			http.Headers.Add("Authorization", mailConfig.APIKEY);

			// Agregar Content-Transfer-Encoding
			http.Headers.Add("Content-Transfer-Encoding", "quoted-printable");

			// Construir la lista de destinatarios con el campo `name` vacío en `email_address`
			var toAddresses = new JArray();
			foreach (var email in toMails)
			{
				toAddresses.Add(new JObject
				{
					{ "email_address", new JObject { { "address", email }, { "name", "" } } }
				});
			}

			byte[] encodedBody = Encoding.UTF8.GetBytes(body); 
			string encodedBodyString = Encoding.UTF8.GetString(encodedBody);			
			JObject parsedContent = new JObject
			{
				{ "from", new JObject { { "address", "noreply@cca.edu.ni" }, { "name", "PORTAL CCA" } } },
				{ "to", toAddresses },
				{ "subject", subject },
				{ "htmlbody", encodedBodyString } 
			};

			Console.WriteLine(parsedContent.ToString());
			ASCIIEncoding encoding = new ASCIIEncoding();
			Byte[] bytes = encoding.GetBytes(parsedContent.ToString());

			using (Stream newStream = http.GetRequestStream())
			{
				newStream.Write(bytes, 0, bytes.Length);
			}

			try
			{
				using (var response = (HttpWebResponse)await http.GetResponseAsync())
				{
					using (var stream = response.GetResponseStream())
					{
						using (var sr = new StreamReader(stream))
						{
							var content = await sr.ReadToEndAsync();
							Console.WriteLine(content);
						}
					}
				}
				return true;
			}
			catch (WebException ex)
			{
				using (var responseStream = ex.Response.GetResponseStream())
				{
					using (var reader = new StreamReader(responseStream))
					{
						string errorResponse = await reader.ReadToEndAsync();
						Console.WriteLine($"Error enviando correo: {errorResponse}");
					}
				}
				return false;
			}*/
		}


		static async Task<bool> SendMailAuth2(string from,
			List<string> toMails,
			string subject,
			string body,
			List<ModelFiles> attach,
			MailConfig mailConfig,
			string? uid)
		{
			try
			{
				var clientId = mailConfig.CLIENT;
				var clientSecret = mailConfig.CLIENT_SECRET;
				var tenantId = mailConfig.TENAT;
				var cca = ConfidentialClientApplicationBuilder.Create(clientId)
					.WithClientSecret(clientSecret)
					.WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
					.Build();

				var AccessToken = await Auth2Utils.GetAccessTokenAsync(mailConfig);

				var message = new MimeMessage();
				if (toMails == null || toMails.Count == 0)
				{
					return false;

				}
				toMails.ForEach(m =>
				{
					string? mail = obtainMail(m);
					message.To.Add(new MailboxAddress("-", mail));
				});
				message.From.Add(new MailboxAddress("PORTAL CCA", mailConfig.USERNAME));

				message.Subject = subject;
				if (uid != null)
				{
					message.InReplyTo = uid;
					message.References.Add(uid);
				}

				var multipart = new Multipart("mixed");
				var htmlBody = new TextPart("html")
				{
					Text = body ?? "correo enviado desde Soporte:"
				};
				multipart.Add(htmlBody);
				if (attach != null)
				{
					foreach (var file in attach)
					{
						var attachment = new MimePart("application", "octet-stream")
						{
							ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
							ContentTransferEncoding = ContentEncoding.Base64,
							FileName = file.Name,
							// Aquí especifica la ruta del archivo
							// var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, file.Name);
							// Lee el contenido del archivo y asigna al cuerpo del adjunto
							Content = new MimeContent(File.OpenRead(file.Value))
						};
						//attachment.Content = new MimeContent(new MemoryStream(Convert.FromBase64String(base64Data)));
						multipart.Add(attachment);
					}
				}
				// Asigna el cuerpo del mensaje al mensaje principal
				message.Body = multipart;
				using (var client = new MailKit.Net.Smtp.SmtpClient())
				{
					client.Connect(mailConfig.HOST, 587, SecureSocketOptions.StartTls);
					// use the access token
					var oauth2 = new SaslMechanismOAuth2(mailConfig.USERNAME, AccessToken.Access_token);
					client.Authenticate(oauth2);
					client.Send(message);
					client.Disconnect(true);
				}
				return true;
			}
			catch (Exception ex)
			{
				LoggerServices.AddMessageError($"error enviando correo desde {mailConfig.HOST} {mailConfig.USERNAME}", ex);
				return false;
			}
		}

		public static string? obtainMail(string inputString)
		{
			if (IsValidEmail(inputString))
			{
				return new MailAddress(inputString).Address;
			}
			// Utilizamos una expresión regular para buscar direcciones de correo electrónico
			string pattern = @"<([^>]+)>"; // Buscará lo que esté dentro de los corchetes angulares
			Regex regex = new Regex(pattern);

			// Buscamos coincidencias en la cadena
			Match match = regex.Match(inputString);

			// Verificamos si se encontró una coincidencia
			if (match.Success)
			{
				string emailAddress = match.Groups[1].Value;
				return emailAddress;
			}
			else
			{
				return null;
			}
		}

		static bool IsValidEmail(string email)
		{
			try
			{
				var addr = new MailAddress(email);
				return true;
			}
			catch
			{
				return false;
			}
		}
		public static bool SendMailBasic(string from,
			List<string> toMails,
			string subject,
			string body,
			List<ModelFiles> attach,
			MailConfig config,
			string? uid)
		{
			try
			{
				//var templatePage = Path.Combine(System.IO.Path.GetFullPath("../UI/Pages/Mails"), path);
				MailMessage correo = new MailMessage();
				correo.From = new MailAddress(from == "" ?  config.USERNAME : from, "PORTAL CCA", Encoding.UTF8);//Correo de salida
				if (toMails == null || toMails.Count == 0)
				{
					return false;

				}
				foreach (string toMail in toMails)
				{
					correo.To.Add(toMail); //Correos de destino
				}

				if (attach != null)
				{
					foreach (var files in attach)
					{
						/*Attachment AttachFile = new System.Net.Mail.Attachment("c:/xampp/factura.pdf");
						correo.Attachments.Add(AttachFile);*/
						Attachment AttachFile = new Attachment(files.Value);
						correo.Attachments.Add(AttachFile);

					}
				}

				correo.Subject = subject; //Asunto
				correo.Body = from + ": " + body;//ContractService.RenderTemplate(templatePage, model);
				correo.IsBodyHtml = true;
				correo.Priority = MailPriority.Normal;
				if (uid != null)
				{
					correo.Headers.Add("In-Reply-To", uid);
				}
				System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient
				{
					UseDefaultCredentials = false,
					Host = config.HOST ?? "",
					Port = PORT, 
					Credentials = new NetworkCredential(config.USERNAME, config.PASSWORD),
					Timeout = 15000
				};
				ServicePointManager.ServerCertificateValidationCallback +=
				  (sender, cert, chain, sslPolicyErrors) => true;
				smtp.EnableSsl = true;//True si el servidor de correo permite ssl
				smtp.Send(correo);
				return true;
			}
			catch (WebException ex)
			{
				using (var stream = ex.Response.GetResponseStream())
				{
					var sr = new StreamReader(stream);
					Console.WriteLine("Error: " + sr.ReadToEnd());
				}
				return false;
			}
		}

		public async static Task<bool> SendMail(string from,
		   List<string> toMails,
		   string subject,
		   string body,
		   List<ModelFiles>? attach,
		   string? uid,
		   MailConfig? config)
		{
			if (config?.AutenticationType == AutenticationTypeEnum.AUTH2)
			{
				return await SendMailAuth2(from, toMails, subject, body, attach, config, uid);
			}
			else
			{
				if (config == null)
				{
					config = new MailConfig() { HOST = HOST, PASSWORD = PASSWORD, USERNAME = USERNAME };
				}
				return SendMailBasic(from, toMails, subject, body, attach, config, uid);
			}
		}
	}

}
