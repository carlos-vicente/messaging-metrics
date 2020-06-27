using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApi.Messaging;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HappeningController: ControllerBase
    {
        private readonly ISendEndpointProvider _endpointProvider;
        private readonly ILogger<HappeningController> _logger;

        public HappeningController(
            ISendEndpointProvider endpointProvider,
            ILogger<HappeningController> logger)
        {
            _endpointProvider = endpointProvider;
            _logger = logger;
        }
        
        [HttpPost]
        public async Task<IActionResult> ScheduleHappening(string whatHappened, int delayInSeconds)
        {
            var schedule = DateTime.UtcNow.AddSeconds(delayInSeconds);
            _logger.LogInformation($"Scheduling the happening {whatHappened} for {schedule}");

            var endpoint = await _endpointProvider.GetSendEndpoint(new Uri("exchange:scheduler"));

            if (!EndpointConvention.TryGetDestinationAddress<NotifySomethingHappened>(out var destinationAddress))
            {
                throw new Exception("No destination address for NotifySomethingHappened");
            }
            
            await endpoint.ScheduleSend<NotifySomethingHappened>(
                destinationAddress,
                schedule,
                new NotifySomethingHappened
            {
                HappeningId = Guid.NewGuid(),
                WhatHappened = whatHappened
            });

            return this.Accepted();
        }
    }
}