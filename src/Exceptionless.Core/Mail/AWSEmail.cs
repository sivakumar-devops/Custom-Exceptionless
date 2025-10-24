using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exceptionless.Core.Extensions;
using Exceptionless.Core.Models;
using Exceptionless.Core.Queues.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Amazon.CloudWatchLogs.Model;
using Amazon.SQS.Model;
using SendGrid.Helpers.Mail;
using Amazon.Runtime;
using Amazon;
using Foundatio.Messaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Content = Amazon.SimpleEmail.Model.Content;
using Body = Amazon.SimpleEmail.Model.Body;
using System.Threading;
namespace Exceptionless.Core.Mail
{
    public class AWSEmail
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly HcpSecretsService _secretManager;

        public AWSEmail(
            IConfiguration configuration,
            HcpSecretsService secretManager,
            ILogger<AWSEmail> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _secretManager = secretManager ?? throw new ArgumentNullException(nameof(secretManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public class SendEmailRequest
        {
            public string? Source { get; set; }
            public object? Destination { get; set; }
            public object? Message { get; set; }
        }
        public class Destination
        {
            public List<string> ToAddresses { get; set; } = new List<string>();
        }
        public class Message
        {
            public Content Subject { get; set; } = new Content();
            public Body Body { get; set; } = new Body();
        }
        public async Task SendAsync(MailMessage message)
        {
            try
            {
                var secretJson = await _secretManager.GetSecretAsync();
                if (string.IsNullOrWhiteSpace(secretJson))
                    throw new Amazon.CloudWatchLogs.Model.InvalidOperationException("Secret manager returned empty or null AWS credentials.");

                var secret = System.Text.Json.JsonSerializer.Deserialize<SecretModel>(secretJson);
                if (secret?.Secret?.DynamicInstance?.Values == null)
                    throw new Amazon.CloudWatchLogs.Model.InvalidOperationException("Invalid AWS credentials format from secret manager.");

                var credentials = new SessionAWSCredentials(
                    secret?.Secret?.DynamicInstance.Values.AccessKeyId,
                    secret?.Secret?.DynamicInstance.Values.SecretAccessKey,
                    secret?.Secret?.DynamicInstance.Values.SessionToken
                );

                var region = "us-east-1";
                if (string.IsNullOrWhiteSpace(region))
                    throw new Amazon.CloudWatchLogs.Model.InvalidOperationException("AWS region configuration is missing.");

                var config = new AmazonSimpleEmailServiceConfig
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(region)
                };

                using var sesClient = new AmazonSimpleEmailServiceClient(credentials, config);

                var fromAddress = "\"Syncfusion Centralized Logger\" <exceptionless@syncfusion.com>";
                
                // Corrected instantiation of Amazon.SimpleEmail.Model.SendEmailRequest  
                var emailRequest = new Amazon.SimpleEmail.Model.SendEmailRequest
                {
                    Source = fromAddress,
                    Destination = new Amazon.SimpleEmail.Model.Destination { ToAddresses = new List<string> { message.To } },
                    Message = new Amazon.SimpleEmail.Model.Message
                    {
                        Subject = new Amazon.SimpleEmail.Model.Content(message.Subject),
                        Body = new Amazon.SimpleEmail.Model.Body { Html = new Amazon.SimpleEmail.Model.Content(message.Body) }
                    }
                };

                var response = await sesClient.SendEmailAsync(emailRequest);
                _logger.LogInformation("Email sent successfully to {recipient}", message.To);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General error sending email to {recipient}: {message}", message.To, ex.Message);
            }
        }
    }
}
