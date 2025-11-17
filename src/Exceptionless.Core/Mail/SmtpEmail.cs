using System.Net;
using System.Net.Mail;
using Exceptionless.Core.Mail;

public class SmtpEmail : IMailSender
{
    private readonly IConfiguration _config;

    public SmtpEmail(IConfiguration config)
    {
        _config = config;
    }

    public Task SendAsync(MailMessage message)
    {
        var host = _config["Email:SmtpHost"];
        var port = int.Parse(_config["Email:SmtpPort"] ?? "587");
        var user = _config["Email:SmtpUser"];
        var pass = _config["Email:SmtpPassword"];
        var from = _config["Email:SmtpFrom"];

        using var smtp = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(user, pass),
            EnableSsl = true // Office365 requires STARTTLS
        };

        message.From = new MailAddress(from);
        return smtp.SendMailAsync(message);
    }
}
