using FluentValidation;

namespace Reimaginate.DataHub.Agent.Dataverse.DataAccess.Commands.DeleteDataverseRecord;

public class DeleteDataverseRecordCommandValidator : AbstractValidator<DeleteDataverseRecordCommand>
{
    public DeleteDataverseRecordCommandValidator()
    {
        RuleFor(i => i.DataverseRecordType).NotEmpty();
        RuleFor(r => r.DataverseRecordId).NotEmpty();
    }
}