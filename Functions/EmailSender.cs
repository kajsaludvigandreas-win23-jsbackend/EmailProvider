using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Azure;
using Azure.Communication.Email;
using Azure.Messaging.ServiceBus;
using EmailProvider.Models;
using EmailProvider.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EmailProvider.Functions
{
    public class EmailSender(ILogger<EmailSender> logger, IEmailService emailService)
    {
        private readonly ILogger<EmailSender> _logger = logger;
        private readonly IEmailService _emailService = emailService;

        [Function(nameof(EmailSender))] //funktionens namn
        public async Task Run(
            [ServiceBusTrigger("email_request", Connection = "ServiceBusConnection")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            try
            {
                var emailRequest = _emailService.UnpackEmailRequest(message);
                if (emailRequest != null && !string.IsNullOrEmpty(emailRequest.To))
                {
                    if (_emailService.SendEmail(emailRequest))
                    {
                        _logger.LogInformation($"Email sent to {emailRequest.To}");
                        await messageActions.CompleteMessageAsync(message);
                    }
                    else
                    {
                        _logger.LogError($"Failed to send email to {emailRequest.To}");
                        await messageActions.AbandonMessageAsync(message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR : EmailSender.Run :: {ex.Message}");
            }
        }

    }
}
