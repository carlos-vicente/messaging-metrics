using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace WebApi.Messaging
{
    public class CanNotCurrentlyProcessException : Exception
    {
        public CanNotCurrentlyProcessException()
        {
        }

        public CanNotCurrentlyProcessException(string message)
            : base(message)
        {
        }

        public CanNotCurrentlyProcessException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
    
    public class NotificationConsumer : IConsumer<NotifySomethingHappened>
    {
        private readonly ILogger<NotificationConsumer> _logger;

        public NotificationConsumer(ILogger<NotificationConsumer> logger)
        {
            _logger = logger;
        }
        
        public Task Consume(ConsumeContext<NotifySomethingHappened> context)
        {
            var id = $"[happeningId:{context.Message.HappeningId}]";
            var hap = $"[whatHappened:{context.Message.WhatHappened}]";
            var redC = $"[redeliveryCount:{context.GetRedeliveryCount()}]";
            var retA = $"[retryAttempt:{context.GetRetryAttempt()}]"; 
            var retC = $"[retryCount:{context.GetRetryCount()}]";
            var ten = $"[tenantId:{context.Headers.Get<string>("tenant-id")}]";
            
            _logger.LogInformation($"Consuming NotifySomethingHappened {id}{hap}{ten}{redC}{retA}{retC}");

            if (string.IsNullOrWhiteSpace(context.Message.WhatHappened))
            {
                throw new Exception("No happening");
            }

            if (context.Message.WhatHappened.Contains("redeliver"))
            {
                throw new CanNotCurrentlyProcessException(context.Message.WhatHappened);
            }

            return Task.CompletedTask;
        }
    }
}