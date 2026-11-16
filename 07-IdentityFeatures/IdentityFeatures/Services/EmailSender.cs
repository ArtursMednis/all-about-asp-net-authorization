namespace IdentityFeatures.Services;

// A fake email sender: prints the email to the app's console.
// In a real app, send it with an email service (SMTP, SendGrid, ...).
public class EmailSender(ILogger<EmailSender> logger)
{
    public void Send(string to, string subject, string body)
    {
        logger.LogWarning("EMAIL to {To}\nSubject: {Subject}\n{Body}", to, subject, body);
    }
}
