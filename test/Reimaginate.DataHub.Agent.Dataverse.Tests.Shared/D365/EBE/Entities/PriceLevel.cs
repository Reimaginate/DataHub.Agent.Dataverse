namespace DataverseModel;

[System.Runtime.Serialization.DataContractAttribute()]
[Microsoft.Xrm.Sdk.Client.EntityLogicalNameAttribute("pricelevel")]
public partial class PriceLevel : Microsoft.Xrm.Sdk.Entity
{
    public partial class Fields
    {
        public const string PriceLevelId = "pricelevelid";
        public const string Id = "pricelevelid";
        public const string BeginDate = "begindate";
        public const string CreatedOn = "createdon";
        public const string Description = "description";
        public const string EndDate = "enddate";
        public const string ModifiedOn = "modifiedon";
        public const string Name = "name";
        public const string OwnerId = "ownerid";
        public const string TransactionCurrencyId = "transactioncurrencyid";
    }

    public PriceLevel() : base(EntityLogicalName)
    {
    }

    public const string EntityLogicalName = "pricelevel";

    public const string PrimaryIdAttribute = "pricelevelid";

    public const string PrimaryNameAttribute = "name";

    public System.Guid? PriceLevelId
    {
        get => GetAttributeValue<System.Guid?>("pricelevelid");
        set
        {
            SetAttributeValue("pricelevelid", value);
            base.Id = value ?? System.Guid.Empty;
        }
    }

    public override System.Guid Id
    {
        get => base.Id;
        set => PriceLevelId = value;
    }

    public string Name { get => GetAttributeValue<string>("name"); set => SetAttributeValue("name", value); }

    public string Description { get => GetAttributeValue<string>("description"); set => SetAttributeValue("description", value); }

    public System.DateTime? BeginDate { get => GetAttributeValue<System.DateTime?>("begindate"); set => SetAttributeValue("begindate", value); }

    public System.DateTime? EndDate { get => GetAttributeValue<System.DateTime?>("enddate"); set => SetAttributeValue("enddate", value); }

    public Microsoft.Xrm.Sdk.EntityReference TransactionCurrencyId { get => GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("transactioncurrencyid"); set => SetAttributeValue("transactioncurrencyid", value); }

    public Microsoft.Xrm.Sdk.EntityReference OwnerId { get => GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("ownerid"); set => SetAttributeValue("ownerid", value); }
}
