using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reimaginate.DataHub.Agent.TestFramework;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Base;

public abstract class DataverseAgentDevTestBase : IDisposable
{
    protected DataverseAgentDevTestBase(
        Action<IConfigurationBuilder> configurationBuilder = null,
        Action<IServiceCollection, IConfiguration> configureServices = null)
    {
        TestInstanceId = new AgentTestInstance("DataverseAgent").Id;
        Configuration = AgentTestConfiguration.Build<DataverseAgentDevTestBase>(configure: configurationBuilder);

        ServiceProvider = new AgentTestServiceBuilder()
            .Add((services, config) => configureServices?.Invoke(services, config))
            .Build(Configuration);
    }

    public IConfigurationRoot Configuration { get; }

    public IServiceProvider ServiceProvider { get; }

    public string TestInstanceId { get; }

    public virtual void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
