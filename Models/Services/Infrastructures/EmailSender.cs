using System;
using System.Threading.Tasks;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.IO;
using Newtonsoft.Json;
using mes.Models.InfrastructureConfigModels;

namespace mes.Models.Services.Infrastructures
{
    public class EmailSender
    {
        string emailerConfigPath =@"C:\core\mes\ControllerConfig\InfrastructureConfig\emailerConfig.json";
        EmailSenderConfig config = new EmailSenderConfig();
        public EmailSender()
        {
            string rawConf = "";

            using (StreamReader sr = new StreamReader(emailerConfigPath))
            {
                rawConf = sr.ReadToEnd();
            }
            config = JsonConvert.DeserializeObject<EmailSenderConfig>(rawConf);            
        }

        public bool SendEmail(string subject, string body, string to, string from)
        {
            MailAddress From = new MailAddress(from);
            MailAddress To = new MailAddress(to);
            MailMessage email = new MailMessage(From, To);
            email.Subject = subject;
            email.Body = body;

            try
            {
                SmtpClient smtp = new SmtpClient(config.ServerIP, config.ServerPort);
                smtp.EnableSsl = false;

                smtp.Credentials = new System.Net.NetworkCredential(config.Username, config.Password);
                smtp.Send(email);
                email.Dispose();

                return true;
            }
            catch (Exception excp)
            {
                //Logger(excp.Message);
                return false;
            }
        }
    }  
}