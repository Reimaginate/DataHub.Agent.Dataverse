using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;
using Reimaginate.Mapper;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared;

[Mapper]
[ScanAssembly(typeof(MapDataverseAccountToDataHubAccount))]
public partial class Mapper
{
}
