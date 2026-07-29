using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json.Linq;
using Reimaginate.DataHub.Agent.Dataverse.Config;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.CreateDataverseRecord;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.CreateDataverseRecords;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.DeleteDataverseRecord;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.UpdateDataverseRecord;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.UpdateDataverseRecords;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Queries.GetAllDataverseEntities;
using Reimaginate.DataHub.Agent.Dataverse.DataAccess.Queries.GetSpecificDataverseEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.External.MergeSpecificDataverseEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.External.SyncDependencyDataHubEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.External.SyncSpecificDataHubEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.EnsureReferencedEntitiesAreSyncd;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.MergeEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.MergeReferencedEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessMerge;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessNewEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessSync;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessUntrackedEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ProcessUpdatedEntities;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.ResolveResolutionPromises;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SendMergeFailuresToDataHub;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SendMergeSuccessesToDataHub;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SendSyncFailuresToDataHub;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SendSyncSuccessesToDataHub;
using Reimaginate.DataHub.Agent.Dataverse.Requests.Internal.SyncEntities;
using Reimaginate.DataHub.Agent.Dataverse.Services.Dataverse;
using Reimaginate.DataHub.Agent.TestFramework;
using Reimaginate.DataHub.Agent.TestFramework.Dataverse.IntegrationTesting.Containers;
using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;
using Reimaginate.DataHub.Auth;
using Reimaginate.DataHub.Config;
using Reimaginate.DataHub.Helpers;
using Reimaginate.DataHub.Requests.External.Client.DeserializeClientRequest;
using Reimaginate.DataHub.Services.DataHubEntityData;
using Reimaginate.DataHub.SharedModels.Core;
using Reimaginate.DataHub.SharedModels.Core.Models.Duplicates;
using Reimaginate.DataHub.SharedModels.Core.Models.Jobs;
using Reimaginate.DataHub.SharedModels.Markers;
using Reimaginate.DataHub.SharedModels.Requests.Client;
using Reimaginate.DataServices;
using Reimaginate.DataServices.Cosmos;
using Reimaginate.Mapper;
using Reimaginate.Mediator;
using DataHubAccount = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Account;
using DataHubActivity = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Activity;
using DataHubAppointment = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Appointment;
using DataHubCase = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Case;
using DataHubContact = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Contact;
using DataHubCustomerAddress = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.CustomerAddress;
using DataHubEmail = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Email;
using DataHubLead = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Lead;
using DataHubNote = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Note;
using DataHubOpportunity = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Opportunity;
using DataHubPhoneCall = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.PhoneCall;
using DataHubPriceList = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.PriceList;
using DataHubPriceListItem = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.PriceListItem;
using DataHubProduct = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Product;
using DataHubQuote = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Quote;
using DataHubQuoteLine = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.QuoteLine;
using DataHubSystemUser = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.SystemUser;
using DataHubTaskActivity = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.TaskActivity;
using DataHubTeam = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Team;
using DataverseAccount = DataverseModel.Account;
using DataverseActivity = DataverseModel.ActivityPointer;
using DataverseActivityParty = DataverseModel.ActivityParty;
using DataverseAnnotation = DataverseModel.Annotation;
using DataverseAppointment = DataverseModel.Appointment;
using DataverseContact = DataverseModel.Contact;
using DataverseCustomerAddress = DataverseModel.CustomerAddress;
using DataverseEmail = DataverseModel.Email;
using DataverseEntitlement = DataverseModel.Entitlement;
using DataverseIncident = DataverseModel.Incident;
using DataverseLead = DataverseModel.Lead;
using DataverseOpportunity = DataverseModel.Opportunity;
using DataversePhoneCall = DataverseModel.PhoneCall;
using DataversePriceLevel = DataverseModel.PriceLevel;
using DataverseProduct = DataverseModel.Product;
using DataverseProductPriceLevel = DataverseModel.ProductPriceLevel;
using DataverseQuote = DataverseModel.Quote;
using DataverseQuoteDetail = DataverseModel.QuoteDetail;
using DataverseSla = DataverseModel.SLA;
using DataverseSubject = DataverseModel.Subject;
using DataverseSystemUser = DataverseModel.SystemUser;
using DataverseTask = DataverseModel.Task;
using DataverseTeam = DataverseModel.Team;
using DataverseUoM = DataverseModel.UoM;
using DataverseUoMSchedule = DataverseModel.UoMSchedule;
using TestMapper = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapper;
using XrmEntityReference = Microsoft.Xrm.Sdk.EntityReference;

namespace Reimaginate.DataHub.Agent.TestFramework.Dataverse.IntegrationTesting;

public sealed class DataverseIntegrationTestHost : IDisposable
{
    private DataverseIntegrationTestHost(
        IConfigurationRoot configuration,
        IServiceProvider serviceProvider,
        DataverseIntegrationTestOptions options,
        string testInstanceId)
    {
        Configuration = configuration;
        ServiceProvider = serviceProvider;
        Options = options;
        TestInstanceId = testInstanceId;
    }

    public IConfigurationRoot Configuration { get; }

    public IServiceProvider ServiceProvider { get; }

    public DataverseIntegrationTestOptions Options { get; }

    public string TestInstanceId { get; }

    public string TestPrefix => $"dh-it-{TestInstanceId}";

    public IMediator Mediator => ServiceProvider.GetRequiredService<IMediator>();

    public IDataHubClient DataHubClient => ServiceProvider.GetRequiredService<IDataHubClient>();

    public IDataverseDataService DataverseDataService => ServiceProvider.GetRequiredService<IDataverseDataService>();

    public static DataverseIntegrationTestHost Create(
        Type userSecretsMarkerType,
        DataverseIntegrationTestOptions? options = null)
    {
        options ??= new DataverseIntegrationTestOptions();
        var testInstanceId = new AgentTestInstance(options.TestInstancePrefix).Id;

        var cosmos = DataHubCosmosDbEmulator.Current;
        if (cosmos?.SkipReason is not null)
        {
            throw new DataHubTestSkippedException(cosmos.SkipReason);
        }

        if (cosmos is null || string.IsNullOrWhiteSpace(cosmos.ConnectionString))
        {
            throw new DataHubTestSkippedException("Dataverse integration tests require TestFixtures:UseLocalCosmosDb=true so the Cosmos fixture provides a connection string.");
        }

        var redis = DataHubRedisContainer.Current;
        if (redis?.SkipReason is not null)
        {
            throw new DataHubTestSkippedException(redis.SkipReason);
        }

        if (redis is null || string.IsNullOrWhiteSpace(redis.ConnectionString))
        {
            throw new DataHubTestSkippedException("Dataverse integration tests require the Redis fixture to provide a connection string.");
        }

        var configuration = AgentTestConfiguration.Build(
            userSecretsMarkerType,
            basePath: AppContext.BaseDirectory,
            configure: builder => builder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataHub:ProcessingLockOptions:RedisClientOptions:ConnString"] = redis.ConnectionString,
                ["DataverseAgentOptions:ProcessingLockOptions:RedisClientOptions:ConnString"] = redis.ConnectionString
            }));

        if (string.IsNullOrWhiteSpace(configuration[$"{options.DataverseAgentConfigurationSection}:DataverseConnectionString"]))
        {
            throw new DataHubTestSkippedException($"{options.DataverseAgentConfigurationSection}:DataverseConnectionString is required in user-secrets or environment variables.");
        }

        var serviceProvider = new AgentTestServiceBuilder()
            .Add((services, config) => ConfigureServices(services, config, cosmos, options))
            .Build(configuration);

        return new DataverseIntegrationTestHost(configuration, serviceProvider, options, testInstanceId);
    }

    public async Task DeleteDataverseRecordAsync(string logicalName, Guid id)
    {
        try
        {
            await DataverseDataService.DeleteAsync(new XrmEntityReference(logicalName, id), CancellationToken.None);
        }
        catch
        {
            // Cleanup must be best-effort and restricted to records created by the test.
        }
    }

    public async Task DeleteDataHubEntityAsync(string entityType, string entityId)
    {
        var response = await DataHubClient.PostRequestAsync<DeleteDataHubEntitiesRequest, DeleteDataHubEntitiesResponse>(
            new DeleteDataHubEntitiesRequest
            {
                EntityType = entityType,
                EntityIds = [entityId],
                IncludeTrackingEntries = true
            },
            CancellationToken.None);

        if (response.Failures.Count != 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, response.Failures.Select(f => f.FailureReason)));
        }
    }

    public async Task<List<JObject>> GetStoredDataHubRecordsAsync()
    {
        var dataStore = ServiceProvider.GetRequiredService<IDataHubEntityDataService>();
        var response = await dataStore.WhereAsync();
        return response.Results.ToList();
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private static void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        DataHubCosmosDbEmulator cosmos,
        DataverseIntegrationTestOptions options)
    {
        services.AddDataHub(dataHubOptions => dataHubOptions
            .WithAppSettingsConfig(configuration, options.DataHubConfigurationSection)
            .WithProcessingLockOptions(cfg => cfg.UseRedisRepository(_ => { }))
            .WithDatabase(cfg => cfg.UseCosmosDatabase(db =>
            {
                db.ConnString = cosmos.ConnectionString!;
                db.Database = options.DataHubDatabaseName;
                db.AutoCreateContainers = true;
                db.DataContainer = options.DataContainer;
                db.TrackingDataContainer = options.TrackingDataContainer;
                db.SyncMarkersContainer = options.SyncMarkersContainer;
                db.SyncFailuresContainer = options.LogsContainer;
                db.ResolutionPromisesContainer = options.ResolutionPromisesContainer;
                db.ConfigsContainer = options.ConfigsContainer;
                db.ManagementContainer = options.ManagementContainer;
                db.UseGateway = true;
            }))
            .WithDataHubEntityDataServiceOptions(cfg =>
            {
                cfg.DatabaseName = options.DataHubDatabaseName;
                cfg.ContainerName = options.DataContainer;
                cfg.PartitionKey = "entityType/id";
                cfg.GetItemPartitionKeyFunc = item => $"{item.Value<string>(nameof(DataHubEntity.entityType))}/{item.Value<string>(nameof(DataHubEntity.id))}";
            })
            .WithChangeTrackingDataServiceOptions(cfg =>
            {
                cfg.DatabaseName = options.DataHubDatabaseName;
                cfg.ContainerName = options.TrackingDataContainer;
                cfg.PartitionKey = "DataSource/EntityType/EntityId";
                cfg.GetItemPartitionKeyFunc = item => $"{item.DataSource}/{item.EntityType}/{item.EntityId}";
            })
            .WithSyncFailureDataServiceOptions(cfg =>
            {
                cfg.DatabaseName = options.DataHubDatabaseName;
                cfg.ContainerName = options.LogsContainer;
                cfg.PartitionKey = "Type/id";
                cfg.GetItemPartitionKeyFunc = item => $"{item.Type}/{item.id}";
            })
            .WithMergeMarkerDataServiceOptions(cfg =>
            {
                cfg.DatabaseName = options.DataHubDatabaseName;
                cfg.ContainerName = options.SyncMarkersContainer;
                cfg.PartitionKey = "id";
                cfg.GetItemPartitionKeyFunc = item => $"{item.id}";
            })
            .WithSyncMarkerDataServiceOptions(cfg =>
            {
                cfg.DatabaseName = options.DataHubDatabaseName;
                cfg.ContainerName = options.SyncMarkersContainer;
                cfg.PartitionKey = "id";
                cfg.GetItemPartitionKeyFunc = item => $"{item.id}";
            })
            .WithResolutionPromiseDataServiceOptions(cfg =>
            {
                cfg.DatabaseName = options.DataHubDatabaseName;
                cfg.ContainerName = options.ResolutionPromisesContainer;
                cfg.PartitionKey = "pk";
                cfg.GetItemPartitionKeyFunc = item => string.IsNullOrWhiteSpace(item.pk) ? item.id : item.pk;
                cfg.SetItemPartitionKeyFunc = item => item.pk = item.id;
            })
            .WithConfigDataServiceOptions(cfg =>
            {
                cfg.DatabaseName = options.DataHubDatabaseName;
                cfg.ContainerName = options.ConfigsContainer;
                cfg.PartitionKey = "id";
                cfg.GetItemPartitionKeyFunc = item => $"{item.id}";
            })
            .WithAutoNumberSequenceDataServiceOptions(cfg =>
            {
                cfg.DatabaseName = options.DataHubDatabaseName;
                cfg.ContainerName = options.ConfigsContainer;
                cfg.PartitionKey = "id";
                cfg.GetItemPartitionKeyFunc = item => $"{item.id}";
            })
            .WithUserDataServiceOptions(cfg =>
            {
                cfg.DatabaseName = options.DataHubDatabaseName;
                cfg.ContainerName = options.ManagementContainer;
                cfg.PartitionKey = "_dt/id";
                cfg.GetItemPartitionKeyFunc = item => $"{item._dt}/{item.id}";
            })
            .WithRoleDataServiceOptions(cfg =>
            {
                cfg.DatabaseName = options.DataHubDatabaseName;
                cfg.ContainerName = options.ManagementContainer;
                cfg.PartitionKey = "_dt/id";
                cfg.GetItemPartitionKeyFunc = item => $"{item._dt}/{item.id}";
            })
            .WithJobDataServiceOptions(cfg =>
            {
                cfg.DatabaseName = options.DataHubDatabaseName;
                cfg.ContainerName = options.ManagementContainer;
                cfg.PartitionKey = "_dt/id";
                cfg.GetItemPartitionKeyFunc = item => $"{item._dt}/{item.id}";
            })
            .WithDuplicateDataServiceOptions(cfg =>
            {
                cfg.DatabaseName = options.DataHubDatabaseName;
                cfg.ContainerName = options.ManagementContainer;
                cfg.PartitionKey = "_dt/id";
                cfg.GetItemPartitionKeyFunc = item => $"{item._dt}/{item.id}";
            }));

        services.RemoveAll<CosmosClient>();
        services.AddSingleton(_ => new CosmosClient(
            cosmos.ConnectionString!,
            new CosmosClientOptions
            {
                Serializer = new DataHubDataSerializer(),
                MaxRetryAttemptsOnRateLimitedRequests = 500,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(300),
                HttpClientFactory = () => cosmos.CosmosDbContainer!.HttpClient,
                RequestTimeout = TimeSpan.FromMinutes(5),
                ConnectionMode = ConnectionMode.Gateway
            }));

        services.AddDataverseAgent(agentOptions => agentOptions.WithAppSettingsConfig(configuration, options.DataverseAgentConfigurationSection));

        services.RemoveAll<IDataHubClient>();
        services.AddSingleton<IDataHubClient, InProcessDataHubClient>();
        services.AddTransient<IHandler<DeserializeClientRequestRequest, IRequest>, DeserializeClientRequestRequestHandler>();
        services.AddTransient<DataverseAgent>();
        services.AddTransient<DataHubAgent>();

        RegisterDataverseHandlers(services);
        RegisterMappers(services);
        options.ConfigureServices?.Invoke(services, configuration);
    }

    private static void RegisterMappers(IServiceCollection services)
    {
        services.AddTransient<IMapper, TestMapper>();
        TestMapper.RegisterMaps(services);

        services.AddTransient<ITypeMapper<DataverseAccount, DataHubAccount>, MapDataverseAccountToDataHubAccount>();
        services.AddTransient<ITypeMapper<DataverseActivity, DataHubActivity>, MapDataverseActivityPointerToDataHubActivity>();
        services.AddTransient<ITypeMapper<DataverseAnnotation, DataHubNote>, MapDataverseAnnotationToDataHubNote>();
        services.AddTransient<ITypeMapper<DataverseAppointment, DataHubAppointment>, MapDataverseAppointmentToDataHubAppointment>();
        services.AddTransient<ITypeMapper<DataverseIncident, DataHubCase>, MapDataverseIncidentToDataHubCase>();
        services.AddTransient<ITypeMapper<DataverseContact, DataHubContact>, MapDataverseContactToDataHubContact>();
        services.AddTransient<ITypeMapper<DataverseCustomerAddress, DataHubCustomerAddress>, MapDataverseCustomerAddressToDataHubCustomerAddress>();
        services.AddTransient<ITypeMapper<DataverseEmail, DataHubEmail>, MapDataverseEmailToDataHubEmail>();
        services.AddTransient<ITypeMapper<DataverseLead, DataHubLead>, MapDataverseLeadToDataHubLead>();
        services.AddTransient<ITypeMapper<DataverseOpportunity, DataHubOpportunity>, MapDataverseOpportunityToDataHubOpportunity>();
        services.AddTransient<ITypeMapper<DataversePhoneCall, DataHubPhoneCall>, MapDataversePhoneCallToDataHubPhoneCall>();
        services.AddTransient<ITypeMapper<DataversePriceLevel, DataHubPriceList>, MapDataversePriceLevelToDataHubPriceList>();
        services.AddTransient<ITypeMapper<DataverseProduct, DataHubProduct>, MapDataverseProductToDataHubProduct>();
        services.AddTransient<ITypeMapper<DataverseProductPriceLevel, DataHubPriceListItem>, MapDataverseProductPriceLevelToDataHubPriceListItem>();
        services.AddTransient<ITypeMapper<DataverseQuote, DataHubQuote>, MapDataverseQuoteToDataHubQuote>();
        services.AddTransient<ITypeMapper<DataverseQuoteDetail, DataHubQuoteLine>, MapDataverseQuoteDetailToDataHubQuoteLine>();
        services.AddTransient<ITypeMapper<DataverseSystemUser, DataHubSystemUser>, MapDataverseSystemUserToDataHubSystemUser>();
        services.AddTransient<ITypeMapper<DataverseTask, DataHubTaskActivity>, MapDataverseTaskToDataHubTaskActivity>();
        services.AddTransient<ITypeMapper<DataverseTeam, DataHubTeam>, MapDataverseTeamToDataHubTeam>();
        services.AddTransient<ITypeMapper<DataHubAccount, DataverseAccount>, MapDataHubAccountToDataverseAccount>();
        services.AddTransient<ITypeMapper<DataHubAppointment, DataverseAppointment>, MapDataHubAppointmentToDataverseAppointment>();
        services.AddTransient<ITypeMapper<DataHubCase, DataverseIncident>, MapDataHubCaseToDataverseIncident>();
        services.AddTransient<ITypeMapper<DataHubContact, DataverseContact>, MapDataHubContactToDataverseContact>();
        services.AddTransient<ITypeMapper<DataHubCustomerAddress, DataverseCustomerAddress>, MapDataHubCustomerAddressToDataverseCustomerAddress>();
        services.AddTransient<ITypeMapper<DataHubEmail, DataverseEmail>, MapDataHubEmailToDataverseEmail>();
        services.AddTransient<ITypeMapper<DataHubLead, DataverseLead>, MapDataHubLeadToDataverseLead>();
        services.AddTransient<ITypeMapper<DataHubNote, DataverseAnnotation>, MapDataHubNoteToDataverseAnnotation>();
        services.AddTransient<ITypeMapper<DataHubOpportunity, DataverseOpportunity>, MapDataHubOpportunityToDataverseOpportunity>();
        services.AddTransient<ITypeMapper<DataHubPhoneCall, DataversePhoneCall>, MapDataHubPhoneCallToDataversePhoneCall>();
        services.AddTransient<ITypeMapper<DataHubPriceList, DataversePriceLevel>, MapDataHubPriceListToDataversePriceLevel>();
        services.AddTransient<ITypeMapper<DataHubProduct, DataverseProduct>, MapDataHubProductToDataverseProduct>();
        services.AddTransient<ITypeMapper<DataHubPriceListItem, DataverseProductPriceLevel>, MapDataHubPriceListItemToDataverseProductPriceLevel>();
        services.AddTransient<ITypeMapper<DataHubQuote, DataverseQuote>, MapDataHubQuoteToDataverseQuote>();
        services.AddTransient<ITypeMapper<DataHubQuoteLine, DataverseQuoteDetail>, MapDataHubQuoteLineToDataverseQuoteDetail>();
        services.AddTransient<ITypeMapper<DataHubTaskActivity, DataverseTask>, MapDataHubTaskActivityToDataverseTask>();
    }

    private static void RegisterDataverseHandlers(IServiceCollection services)
    {
        services.AddTransient<IHandler<SendMergeFailuresToDataHubRequest, NullResponse>, SendMergeFailuresToDataHubRequestHandler>();
        services.AddTransient<IHandler<SendMergeSuccessesToDataHubRequest, NullResponse>, SendMergeSuccessesToDataHubRequestHandler>();
        services.AddTransient<IHandler<SendSyncFailuresToDataHubRequest, NullResponse>, SendSyncFailuresToDataHubRequestHandler>();
        services.AddTransient<IHandler<SendSyncSuccessesToDataHubRequest, NullResponse>, SendSyncSuccessesToDataHubRequestHandler>();
        services.AddTransient<IHandler<UpdateDataverseRecordsCommand, UpdateDataverseRecordsResponse<Entity>>, UpdateDataverseRecordsCommandHandler>();
        services.AddTransient<IHandler<DeleteDataverseRecordCommand, NullResponse>, DeleteDataverseRecordCommandHandler>();

        RegisterDataversePair<DataverseAccount, DataHubAccount>(services);
        RegisterMergeOnlyDataversePair<DataverseActivity, DataHubActivity>(services);
        RegisterDataversePair<DataverseAnnotation, DataHubNote>(services);
        RegisterDataversePair<DataverseAppointment, DataHubAppointment>(services);
        RegisterDataversePair<DataverseIncident, DataHubCase>(services);
        RegisterDataversePair<DataverseContact, DataHubContact>(services);
        RegisterDataversePair<DataverseCustomerAddress, DataHubCustomerAddress>(services);
        RegisterDataversePair<DataverseEmail, DataHubEmail>(services);
        RegisterDataversePair<DataverseLead, DataHubLead>(services);
        RegisterDataversePair<DataverseOpportunity, DataHubOpportunity>(services);
        RegisterDataversePair<DataversePhoneCall, DataHubPhoneCall>(services);
        RegisterDataversePair<DataversePriceLevel, DataHubPriceList>(services);
        RegisterDataversePair<DataverseProduct, DataHubProduct>(services);
        RegisterDataversePair<DataverseProductPriceLevel, DataHubPriceListItem>(services);
        RegisterDataversePair<DataverseQuote, DataHubQuote>(services);
        RegisterDataversePair<DataverseQuoteDetail, DataHubQuoteLine>(services);
        RegisterMergeOnlyDataversePair<DataverseSystemUser, DataHubSystemUser>(services);
        RegisterDataversePair<DataverseTask, DataHubTaskActivity>(services);
        RegisterMergeOnlyDataversePair<DataverseTeam, DataHubTeam>(services);
        RegisterWritableDataverseEntity<DataverseActivityParty>(services);
        RegisterWritableDataverseEntity<DataverseEntitlement>(services);
        RegisterWritableDataverseEntity<DataverseSla>(services);
        RegisterWritableDataverseEntity<DataverseSubject>(services);
        RegisterWritableDataverseEntity<DataverseUoM>(services);
        RegisterWritableDataverseEntity<DataverseUoMSchedule>(services);
    }

    private static void RegisterReadOnlyDataverseEntity<TDataverseEntity>(IServiceCollection services)
        where TDataverseEntity : Entity, new()
    {
        services.AddTransient<IHandler<GetSpecificDataverseEntitiesRequest<TDataverseEntity>, GetSpecificDataverseEntitiesResponse<TDataverseEntity>>, GetSpecificDataverseEntitiesRequestHandler<TDataverseEntity>>();
        services.AddTransient<IHandler<GetAllDataverseEntitiesRequest<TDataverseEntity>, List<TDataverseEntity>>, GetAllDataverseEntitiesRequestHandler<TDataverseEntity>>();
    }

    private static void RegisterWritableDataverseEntity<TDataverseEntity>(IServiceCollection services)
        where TDataverseEntity : Entity, new()
    {
        RegisterReadOnlyDataverseEntity<TDataverseEntity>(services);
        services.AddTransient<IHandler<CreateDataverseRecordCommand<TDataverseEntity>, Guid>, CreateDataverseRecordCommandHandler<TDataverseEntity>>();
        services.AddTransient<IHandler<CreateDataverseRecordsCommand<TDataverseEntity>, CreateDataverseRecordsResponse<TDataverseEntity>>, CreateDataverseRecordsCommandHandler<TDataverseEntity>>();
        services.AddTransient<IHandler<UpdateDataverseRecordCommand<TDataverseEntity>, Guid>, UpdateDataverseRecordCommandHandler<TDataverseEntity>>();
        services.AddTransient<IHandler<UpdateDataverseRecordsCommand<TDataverseEntity>, UpdateDataverseRecordsResponse<TDataverseEntity>>, UpdateDataverseRecordsCommandHandler<TDataverseEntity>>();
    }

    private static void RegisterMergeOnlyDataversePair<TDataverseEntity, TDataHubEntity>(IServiceCollection services)
        where TDataverseEntity : Entity, new()
        where TDataHubEntity : DataHubEntity, new()
    {
        RegisterReadOnlyDataverseEntity<TDataverseEntity>(services);

        services.AddTransient<IHandler<MergeSpecificDataverseEntitiesRequest<TDataverseEntity, TDataHubEntity>, ProcessMergeResponse>, MergeSpecificDataverseEntitiesRequestHandler<TDataverseEntity, TDataHubEntity>>();
        services.AddTransient<IHandler<MergeEntitiesRequest<TDataverseEntity, TDataHubEntity>, ProcessMergeResponse>, MergeEntitiesRequestHandler<TDataverseEntity, TDataHubEntity>>();
        services.AddTransient<IHandler<ProcessMergeRequest<TDataverseEntity, TDataHubEntity>, ProcessMergeResponse>, ProcessMergeRequestHandler<TDataverseEntity, TDataHubEntity>>();
        services.AddTransient<IHandler<MergeReferencedEntitiesRequest<TDataverseEntity, TDataHubEntity>, MergeReferencedEntitiesResponse>, MergeReferencedEntitiesRequestHandler<TDataverseEntity, TDataHubEntity>>();
    }

    private static void RegisterDataversePair<TDataverseEntity, TDataHubEntity>(IServiceCollection services)
        where TDataverseEntity : Entity, new()
        where TDataHubEntity : DataHubEntity, new()
    {
        RegisterWritableDataverseEntity<TDataverseEntity>(services);

        services.AddTransient<IHandler<MergeSpecificDataverseEntitiesRequest<TDataverseEntity, TDataHubEntity>, ProcessMergeResponse>, MergeSpecificDataverseEntitiesRequestHandler<TDataverseEntity, TDataHubEntity>>();
        services.AddTransient<IHandler<MergeEntitiesRequest<TDataverseEntity, TDataHubEntity>, ProcessMergeResponse>, MergeEntitiesRequestHandler<TDataverseEntity, TDataHubEntity>>();
        services.AddTransient<IHandler<ProcessMergeRequest<TDataverseEntity, TDataHubEntity>, ProcessMergeResponse>, ProcessMergeRequestHandler<TDataverseEntity, TDataHubEntity>>();
        services.AddTransient<IHandler<MergeReferencedEntitiesRequest<TDataverseEntity, TDataHubEntity>, MergeReferencedEntitiesResponse>, MergeReferencedEntitiesRequestHandler<TDataverseEntity, TDataHubEntity>>();

        services.AddTransient<IHandler<SyncSpecificDataHubEntitiesRequest<TDataHubEntity, TDataverseEntity>, ProcessSyncResponse>, SyncSpecificDataHubEntitiesRequestHandler<TDataHubEntity, TDataverseEntity>>();
        services.AddTransient<IHandler<SyncDependencyDataHubEntitiesRequest<TDataHubEntity, TDataverseEntity>, ProcessSyncResponse>, SyncDependencyDataHubEntitiesRequestHandler<TDataHubEntity, TDataverseEntity>>();
        services.AddTransient<IHandler<SyncEntitiesRequest<TDataHubEntity, TDataverseEntity>, ProcessSyncResponse>, SyncEntitiesRequestHandler<TDataHubEntity, TDataverseEntity>>();
        services.AddTransient<IHandler<ProcessSyncRequest<TDataHubEntity, TDataverseEntity>, ProcessSyncResponse>, ProcessSyncRequestHandler<TDataHubEntity, TDataverseEntity>>();
        services.AddTransient<IHandler<ProcessNewEntitiesRequest<TDataHubEntity, TDataverseEntity>, ProcessNewEntitiesResponse>, ProcessNewEntitiesRequestHandler<TDataHubEntity, TDataverseEntity>>();
        services.AddTransient<IHandler<ProcessUpdatedEntitiesRequest<TDataHubEntity, TDataverseEntity>, ProcessUpdatedEntitiesResponse>, ProcessUpdatedEntitiesRequestHandler<TDataHubEntity, TDataverseEntity>>();
        services.AddTransient<IHandler<ProcessUntrackedEntitiesRequest<TDataHubEntity, TDataverseEntity>, ProcessUntrackedEntitiesResponse>, ProcessUntrackedEntitiesRequestHandler<TDataHubEntity, TDataverseEntity>>();
        services.AddTransient<IHandler<EnsureReferencedEntitiesAreSyncdRequest<TDataHubEntity, TDataverseEntity>, EnsureReferencedEntitiesAreSyncdResponse<TDataHubEntity, TDataverseEntity>>, EnsureReferencedEntitiesAreSyncdRequestHandler<TDataHubEntity, TDataverseEntity>>();
        services.AddTransient<IHandler<ResolveResolutionPromisesRequest<TDataHubEntity, TDataverseEntity>, ResolveResolutionPromisesResponse<TDataHubEntity, TDataverseEntity>>, ResolveResolutionPromisesRequestHandler<TDataHubEntity, TDataverseEntity>>();
    }
}
