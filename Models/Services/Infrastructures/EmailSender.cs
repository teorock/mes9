using System;
using System.Threading.Tasks;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace mes.Models.Services.Infrastructures
{
    public class EmailSender
    {
        string serverIp="192.168.2.128";
        int serverPort = 25;
        string userName ="automation@intranet";
        string passWd ="4ut0m4t10n!";
        public bool SendEmail(string subject, string body, string to, string from)
        {
            //var apiKey = "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";
            //var client = new SendGridClient(apiKey);
            //var msg = new SendGridMessage()
            //{
            //    From = new EmailAddress(from, fromExtended),
            //    Subject = subject,
            //    PlainTextContent = body
            //};
            //
            //msg.AddTo(new EmailAddress(to));
            //var response = client.SendEmailAsync(msg);
            //return response.IsCompletedSuccessfully;

            MailAddress From = new MailAddress(from);
            MailAddress To = new MailAddress(to);
            MailMessage email = new MailMessage(From, To);
            email.Subject = subject;
            email.Body = body;

            try
            {
                SmtpClient smtp = new SmtpClient(serverIp, serverPort);
                smtp.EnableSsl = false;

                smtp.Credentials = new System.Net.NetworkCredential(userName, passWd);
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