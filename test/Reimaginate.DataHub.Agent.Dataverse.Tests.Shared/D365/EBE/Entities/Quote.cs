namespace DataverseModel;

[System.Runtime.Serialization.DataContractAttribute()]
[Microsoft.Xrm.Sdk.Client.EntityLogicalNameAttribute("quote")]
public partial class Quote : Microsoft.Xrm.Sdk.Entity
{
    public partial class Fields
    {
        public const string QuoteId = "quoteid";
        public const string Id = "quoteid";
        public const string CreatedOn = "createdon";
        public const string CustomerId = "customerid";
        public const string Description = "description";
        public const string EffectiveFrom = "effectivefrom";
        public const string EffectiveTo = "effectiveto";
        public const string ModifiedOn = "modifiedon";
        public const string Name = "name";
        public const string OpportunityId = "opportunityid";
        public const string OwnerId = "ownerid";
        public const string PriceLevelId = "pricelevelid";
        public const string QuoteNumber = "quotenumber";
        public const string TotalAmount = "totalamount";
        public const string TotalLineItemAmount = "totallineitemamount";
        public const string TransactionCurrencyId = "transactioncurrencyid";
    }

    public Quote() : base(EntityLogicalName)
    {
    }

    public const string EntityLogicalName = "quote";

    public const string PrimaryIdAttribute = "quoteid";

    public const string PrimaryNameAttribute = "name";

    public System.Guid? QuoteId
    {
        get => GetAttributeValue<System.Guid?>("quoteid");
        set
        {
            SetAttributeValue("quoteid", value);
            base.Id = value ?? System.Guid.Empty;
        }
    }

    public override System.Guid Id
    {
        get => base.Id;
        set => QuoteId = value;
    }

    public string Name { get => GetAttributeValue<string>("name"); set => SetAttributeValue("name", value); }

    public string QuoteNumber { get => GetAttributeValue<string>("quotenumber"); set => SetAttributeValue("quotenumber", value); }

    public string Description { get => GetAttributeValue<string>("description"); set => SetAttributeValue("description", value); }

    public System.DateTime? EffectiveFrom { get => GetAttributeValue<System.DateTime?>("effectivefrom"); set => SetAttributeValue("effectivefrom", value); }

    public System.DateTime? EffectiveTo { get => GetAttributeValue<System.DateTime?>("effectiveto"); set => SetAttributeValue("effectiveto", value); }

    public Microsoft.Xrm.Sdk.EntityReference CustomerId { get => GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("customerid"); set => SetAttributeValue("customerid", value); }

    public Microsoft.Xrm.Sdk.EntityReference OpportunityId { get => GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("opportunityid"); set => SetAttributeValue("opportunityid", value); }

    public Microsoft.Xrm.Sdk.EntityReference PriceLevelId { get => GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("pricelevelid"); set => SetAttributeValue("pricelevelid", value); }

    public Microsoft.Xrm.Sdk.EntityReference TransactionCurrencyId { get => GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("transactioncurrencyid"); set => SetAttributeValue("transactioncurrencyid", value); }

    public Microsoft.Xrm.Sdk.EntityReference OwnerId { get => GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("ownerid"); set => SetAttributeValue("ownerid", value); }

    public Microsoft.Xrm.Sdk.Money TotalAmount { get => GetAttributeValue<Microsoft.Xrm.Sdk.Money>("totalamount"); set => SetAttributeValue("totalamount", value); }

    public Microsoft.Xrm.Sdk.Money TotalLineItemAmount { get => GetAttributeValue<Microsoft.Xrm.Sdk.Money>("totallineitemamount"); set => SetAttributeValue("totallineitemamount", value); }
}
