using System.Threading.Tasks;
using Finbuckle.MultiTenant;
using GreenPipes;
using MassTransit;

namespace WebApi.Messaging
{
    public class MultiTenantSendContextPipe : IPipe<SendContext>
    {
        private readonly TenantInfo _tenantInfo;

        public MultiTenantSendContextPipe(TenantInfo tenantInfo)
        {
            _tenantInfo = tenantInfo;
        }
        
        public Task Send(SendContext context)
        {
            context.Headers.Set("tenant-id", _tenantInfo.Identifier);
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
            context.CreateScope("tenant");
        }
    }
}