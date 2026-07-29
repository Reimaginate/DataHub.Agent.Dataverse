using Reimaginate.Mapper;
using DataHubNote = Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Note;
using DataverseAnnotation = DataverseModel.Annotation;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;

public sealed class MapDataHubNoteToDataverseAnnotation : ITypeMapper<DataHubNote, DataverseAnnotation>
{
    public Task<DataverseAnnotation> MapAsync(DataHubNote from, CancellationToken cancellationToken, Dictionary<string, object>? cache = null)
    {
        var mapped = new DataverseAnnotation
        {
            Subject = from.Subject,
            NoteText = from.Text,
            FileName = from.FileName,
            MimeType = from.MimeType,
            DocumentBody = from.DocumentBody,
            IsDocument = from.IsDocument,
            OwnerId = MappingHelpers.ResolveOwner(from.Owner, cache),
            ObjectId = MappingHelpers.ResolveActivityReference(from.Regarding, cache)
        };

        var dataverseId = MappingHelpers.GetDataverseId(from, DataverseAnnotation.EntityLogicalName);
        if (dataverseId is not null)
        {
            mapped.Id = dataverseId.Value;
            mapped.AnnotationId = dataverseId;
        }

        return Task.FromResult(mapped);
    }
}
