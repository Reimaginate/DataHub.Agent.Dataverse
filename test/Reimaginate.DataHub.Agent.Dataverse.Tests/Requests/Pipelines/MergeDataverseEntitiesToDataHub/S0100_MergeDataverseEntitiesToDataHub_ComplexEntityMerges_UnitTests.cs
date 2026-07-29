using Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Mapping;
using Reimaginate.DataHub.SharedModels.Core;
using Xunit;
using Account = DataverseModel.Account;
using Contact = DataverseModel.Contact;
using Opportunity = DataverseModel.Opportunity;
using XrmEntityReference = Microsoft.Xrm.Sdk.EntityReference;

namespace Reimaginate.DataHub.Agent.Dataverse.Tests.Requests.Pipelines.MergeDataverseEntitiesToDataHub;

public class S0100_MergeDataverseEntitiesToDataHub_ComplexEntityMerges_UnitTests
{
    [Fact(DisplayName = "S0100: Merge account preserves parent account, primary contact, and owner references")]
    [Trait("Category", "Unit")]
    public async Task S0100()
    {
        var parentAccountId = Guid.NewGuid();
        var primaryContactId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var account = new Account
        {
            Id = Guid.NewGuid(),
            Name = "DataHub account",
            ParentAccountId = new XrmEntityReference(Account.EntityLogicalName, parentAccountId),
            PrimaryContactId = new XrmEntityReference(Contact.EntityLogicalName, primaryContactId),
            OwnerId = new XrmEntityReference("systemuser", ownerId)
        };

        var mapped = await new MapDataverseAccountToDataHubAccount()
            .MapAsync(account, CancellationToken.None);

        Assert.Equal("DataHub account", mapped.Name);
        Assert.Contains(mapped.alternateKeys, key => key.Key == $"dataverse.{Account.EntityLogicalName}" && key.Value == account.Id.ToString());
        Assert.Equal(parentAccountId.ToString(), Assert.IsAssignableFrom<ExternalEntityReference>(mapped.ParentAccount).EntityId);
        Assert.Equal(primaryContactId.ToString(), Assert.IsAssignableFrom<ExternalEntityReference>(mapped.PrimaryContact).EntityId);
        Assert.Equal("systemuser", Assert.IsAssignableFrom<ExternalEntityReference>(mapped.Owner).SourceEntityType);
    }

    [Fact(DisplayName = "S0101: Merge contact preserves parent account as external Dataverse reference")]
    [Trait("Category", "Unit")]
    public async Task S0101()
    {
        var accountId = Guid.NewGuid();
        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            FirstName = "Alex",
            LastName = "Taylor",
            EmailAddress1 = "alex.taylor@example.test",
            ParentCustomerId = new XrmEntityReference(Account.EntityLogicalName, accountId)
        };

        var mapped = await new MapDataverseContactToDataHubContact()
            .MapAsync(contact, CancellationToken.None);

        Assert.NotNull(mapped.ParentAccount);
        var parentAccount = Assert.IsAssignableFrom<ExternalEntityReference>(mapped.ParentAccount);
        Assert.Equal("Dataverse", parentAccount.DataSource);
        Assert.Equal(Account.EntityLogicalName, parentAccount.SourceEntityType);
        Assert.Equal(accountId.ToString(), parentAccount.EntityId);
    }

    [Fact(DisplayName = "S0102: Merge opportunity preserves account and contact references")]
    [Trait("Category", "Unit")]
    public async Task S0102()
    {
        var accountId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var opportunity = new Opportunity
        {
            Id = Guid.NewGuid(),
            Name = "DataHub testing",
            CustomerId = new XrmEntityReference(Account.EntityLogicalName, accountId),
            ParentContactId = new XrmEntityReference(Contact.EntityLogicalName, contactId)
        };

        var mapped = await new MapDataverseOpportunityToDataHubOpportunity()
            .MapAsync(opportunity, CancellationToken.None);

        var account = Assert.IsAssignableFrom<ExternalEntityReference>(mapped.Account);
        Assert.Equal("Dataverse", account.DataSource);
        Assert.Equal(Account.EntityLogicalName, account.SourceEntityType);
        Assert.Equal(accountId.ToString(), account.EntityId);

        var contact = Assert.IsAssignableFrom<ExternalEntityReference>(mapped.Contact);
        Assert.Equal("Dataverse", contact.DataSource);
        Assert.Equal(Contact.EntityLogicalName, contact.SourceEntityType);
        Assert.Equal(contactId.ToString(), contact.EntityId);
    }

    [Fact(DisplayName = "S0103: Sync maps restore Dataverse ids from alternate keys")]
    [Trait("Category", "Unit")]
    public async Task S0103()
    {
        var accountId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var opportunityId = Guid.NewGuid();

        var account = await new MapDataHubAccountToDataverseAccount()
            .MapAsync(new Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Account
            {
                Name = "Existing account",
                alternateKeys = [new AlternateKey { Key = $"dataverse.{Account.EntityLogicalName}", Value = accountId.ToString() }]
            }, CancellationToken.None);

        var contact = await new MapDataHubContactToDataverseContact()
            .MapAsync(new Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Contact
            {
                FirstName = "Existing",
                LastName = "Contact",
                alternateKeys = [new AlternateKey { Key = $"dataverse.{Contact.EntityLogicalName}", Value = contactId.ToString() }]
            }, CancellationToken.None);

        var opportunity = await new MapDataHubOpportunityToDataverseOpportunity()
            .MapAsync(new Reimaginate.DataHub.Agent.Dataverse.Tests.Shared.Models.DataHub.Opportunity
            {
                Name = "Existing opportunity",
                alternateKeys = [new AlternateKey { Key = $"dataverse.{Opportunity.EntityLogicalName}", Value = opportunityId.ToString() }]
            }, CancellationToken.None);

        Assert.Equal(accountId, account.Id);
        Assert.Equal(accountId, account.AccountId);
        Assert.Equal(contactId, contact.Id);
        Assert.Equal(contactId, contact.ContactId);
        Assert.Equal(opportunityId, opportunity.Id);
        Assert.Equal(opportunityId, opportunity.OpportunityId);
    }

    [Fact(DisplayName = "S0104: Merge maps tolerate Dataverse DateTimeOffset timestamp attributes")]
    [Trait("Category", "Unit")]
    public async Task S0104()
    {
        var createdOn = new DateTimeOffset(2026, 7, 4, 1, 2, 3, TimeSpan.Zero);
        var modifiedOn = new DateTimeOffset(2026, 7, 4, 4, 5, 6, TimeSpan.Zero);

        var account = new Account { Id = Guid.NewGuid(), Name = "Date account" };
        account.Attributes[Account.Fields.CreatedOn] = createdOn;
        account.Attributes[Account.Fields.ModifiedOn] = modifiedOn;

        var contact = new Contact { Id = Guid.NewGuid(), FirstName = "Date", LastName = "Contact" };
        contact.Attributes[Contact.Fields.CreatedOn] = createdOn;
        contact.Attributes[Contact.Fields.ModifiedOn] = modifiedOn;

        var opportunity = new Opportunity { Id = Guid.NewGuid(), Name = "Date opportunity" };
        opportunity.Attributes[Opportunity.Fields.CreatedOn] = createdOn;
        opportunity.Attributes[Opportunity.Fields.ModifiedOn] = modifiedOn;

        var mappedAccount = await new MapDataverseAccountToDataHubAccount()
            .MapAsync(account, CancellationToken.None);
        var mappedContact = await new MapDataverseContactToDataHubContact()
            .MapAsync(contact, CancellationToken.None);
        var mappedOpportunity = await new MapDataverseOpportunityToDataHubOpportunity()
            .MapAsync(opportunity, CancellationToken.None);

        Assert.Equal(createdOn, mappedAccount.createdOn);
        Assert.Equal(modifiedOn, mappedAccount.lastUpdated);
        Assert.Equal(createdOn, mappedContact.createdOn);
        Assert.Equal(modifiedOn, mappedContact.lastUpdated);
        Assert.Equal(createdOn, mappedOpportunity.createdOn);
        Assert.Equal(modifiedOn, mappedOpportunity.lastUpdated);
    }
}
