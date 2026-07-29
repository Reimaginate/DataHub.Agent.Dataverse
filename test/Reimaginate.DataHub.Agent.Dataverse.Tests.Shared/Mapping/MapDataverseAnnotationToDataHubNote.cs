using Reimaginate.Mapper;
using DataHubNote = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Note;
using DataverseAnnotation = DataverseModel.Annotation;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataverseAnnotationToDataHubNote : ITypeMapper<DataverseAnnotation, DataHubNote>
{
    public Task<DataHubNote> MapAsync(DataverseAnnotation from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null)
    {
        return Task.FromResult(new DataHubNote
        {
            id = from.Id.ToString(),
            alternateKeys = MappingHelpers.DataverseAlternateKeys(DataverseAnnotation.EntityLogicalName, from.Id),
            Subject = from.Subject,
            Text = from.NoteText,
            FileName = from.FileName,
            MimeType = from.MimeType,
            DocumentBody = from.DocumentBody,
            IsDocument = from.IsDocument,
            Owner = MappingHelpers.ToOwnerReference(from.OwnerId),
            Regarding = MappingHelpers.ToActivityReference(from.ObjectId),
            createdOn = MappingHelpers.ToDateTimeOffset(from, DataverseAnnotation.Fields.CreatedOn),
            lastUpdated = MappingHelpers.ToDateTimeOffset(from, DataverseAnnotation.Fields.ModifiedOn)
        });
    }
}
