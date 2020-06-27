using System;
using System.Threading.Tasks;
using GreenPipes;
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
        private readonly IPipe<SendContext> _sendConfigurator;
        private readonly ILogger<HappeningController> _logger;

        public HappeningController(
            ISendEndpointProvider endpointProvider,
            IPipe<SendContext> sendConfigurator,
            ILogger<HappeningController> logger)
        {
            _endpointProvider = endpointProvider;
            _sendConfigurator = sendConfigurator;
            _logger = logger;
        }
        
        /// <summary>
        /// Schedules an happening
        /// </summary>
        /// <remarks>
        /// Schedules the mentioned happening with the defined delay in seconds
        /// </remarks>
        /// <param name="whatHappened">The happening</param>
        /// <param name="delayInSeconds">Th delay in seconds (will be based on UtcNow to define schedule)</param>
        /// <response code="202">Happening scheduled</response>
        /// <response code="500">Failed to schedule the happening</response>
        [HttpPost]
        [ProducesResponseType(202)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ScheduleHappening(string whatHappened, int delayInSeconds)
        {
            var schedule = DateTime.UtcNow.AddSeconds(delayInSeconds);
            _logger.LogInformation($"Scheduling the happening {whatHappened} for {schedule}");

            var endpoint = await _endpointProvider.GetSendEndpoint(new Uri("exchange:scheduler"));

            if (!EndpointConvention.TryGetDestinationAddress<NotifySomethingHappened>(out var destinationAddress))
            {
                throw new Exception("No destination address for NotifySomethingHappened");
            }

            await endpoint
                .ScheduleSend(
                    destinationAddress,
                    schedule,
                    new NotifySomethingHappened
                    {
                        HappeningId = Guid.NewGuid(),
                        WhatHappened = whatHappened
                    },
                    _sendConfigurator);

            return this.Accepted();
        }
    }
}