namespace DataverseModel;

[System.Runtime.Serialization.DataContractAttribute()]
[Microsoft.Xrm.Sdk.Client.EntityLogicalNameAttribute("productpricelevel")]
public partial class ProductPriceLevel : Microsoft.Xrm.Sdk.Entity
{
    public partial class Fields
    {
        public const string ProductPriceLevelId = "productpricelevelid";
        public const string Id = "productpricelevelid";
        public const string Amount = "amount";
        public const string CreatedOn = "createdon";
        public const string ModifiedOn = "modifiedon";
        public const string PriceLevelId = "pricelevelid";
        public const string ProductId = "productid";
        public const string TransactionCurrencyId = "transactioncurrencyid";
        public const string UoMId = "uomid";
    }

    public ProductPriceLevel() : base(EntityLogicalName)
    {
    }

    public const string EntityLogicalName = "productpricelevel";

    public const string PrimaryIdAttribute = "productpricelevelid";

    public const string PrimaryNameAttribute = "productid";

    public System.Guid? ProductPriceLevelId
    {
        get => GetAttributeValue<System.Guid?>("productpricelevelid");
        set
        {
            SetAttributeValue("productpricelevelid", value);
            base.Id = value ?? System.Guid.Empty;
        }
    }

    public override System.Guid Id
    {
        get => base.Id;
        set => ProductPriceLevelId = value;
    }

    public Microsoft.Xrm.Sdk.EntityReference PriceLevelId { get => GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("pricelevelid"); set => SetAttributeValue("pricelevelid", value); }

    public Microsoft.Xrm.Sdk.EntityReference ProductId { get => GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("productid"); set => SetAttributeValue("productid", value); }

    public Microsoft.Xrm.Sdk.EntityReference UoMId { get => GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("uomid"); set => SetAttributeValue("uomid", value); }

    public Microsoft.Xrm.Sdk.EntityReference TransactionCurrencyId { get => GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("transactioncurrencyid"); set => SetAttributeValue("transactioncurrencyid", value); }

    public Microsoft.Xrm.Sdk.Money Amount { get => GetAttributeValue<Microsoft.Xrm.Sdk.Money>("amount"); set => SetAttributeValue("amount", value); }
}
