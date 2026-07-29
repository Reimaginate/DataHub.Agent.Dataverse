namespace DataverseModel;

[System.Runtime.Serialization.DataContractAttribute()]
[Microsoft.Xrm.Sdk.Client.EntityLogicalNameAttribute("quotedetail")]
public partial class QuoteDetail : Microsoft.Xrm.Sdk.Entity
{
    public partial class Fields
    {
        public const string QuoteDetailId = "quotedetailid";
        public const string Id = "quotedetailid";
        public const string BaseAmount = "baseamount";
        public const string CreatedOn = "createdon";
        public const string Description = "description";
        public const string ExtendedAmount = "extendedamount";
        public const string IsPriceOverridden = "ispriceoverridden";
        public const string IsProductOverridden = "isproductoverridden";
        public const string ManualDiscountAmount = "manualdiscountamount";
        public const string ModifiedOn = "modifiedon";
        public const string PricePerUnit = "priceperunit";
        public const string ProductId = "productid";
        public const string Quantity = "quantity";
        public const string QuoteId = "quoteid";
        public const string TransactionCurrencyId = "transactioncurrencyid";
        public const string UoMId = "uomid";
    }

    public QuoteDetail() : base(EntityLogicalName)
    {
    }

    public const string EntityLogicalName = "quotedetail";

    public const string PrimaryIdAttribute = "quotedetailid";

    public const string PrimaryNameAttribute = "productdescription";

    public System.Guid? QuoteDetailId
    {
        get => GetAttributeValue<System.Guid?>("quotedetailid");
        set
        {
            SetAttributeValue("quotedetailid", value);
            base.Id = value ?? System.Guid.Empty;
        }
    }

    public override System.Guid Id
    {
        get => base.Id;
        set => QuoteDetailId = value;
    }

    public Microsoft.Xrm.Sdk.EntityReference QuoteId { get => GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("quoteid"); set => SetAttributeValue("quoteid", value); }

    public Microsoft.Xrm.Sdk.EntityReference ProductId { get => GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("productid"); set => SetAttributeValue("productid", value); }

    public Microsoft.Xrm.Sdk.EntityReference UoMId { get => GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("uomid"); set => SetAttributeValue("uomid", value); }

    public Microsoft.Xrm.Sdk.EntityReference TransactionCurrencyId { get => GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("transactioncurrencyid"); set => SetAttributeValue("transactioncurrencyid", value); }

    public decimal? Quantity { get => GetAttributeValue<decimal?>("quantity"); set => SetAttributeValue("quantity", value); }

    public Microsoft.Xrm.Sdk.Money PricePerUnit { get => GetAttributeValue<Microsoft.Xrm.Sdk.Money>("priceperunit"); set => SetAttributeValue("priceperunit", value); }

    public Microsoft.Xrm.Sdk.Money ManualDiscountAmount { get => GetAttributeValue<Microsoft.Xrm.Sdk.Money>("manualdiscountamount"); set => SetAttributeValue("manualdiscountamount", value); }

    public Microsoft.Xrm.Sdk.Money BaseAmount { get => GetAttributeValue<Microsoft.Xrm.Sdk.Money>("baseamount"); set => SetAttributeValue("baseamount", value); }

    public Microsoft.Xrm.Sdk.Money ExtendedAmount { get => GetAttributeValue<Microsoft.Xrm.Sdk.Money>("extendedamount"); set => SetAttributeValue("extendedamount", value); }

    public string Description { get => GetAttributeValue<string>("description"); set => SetAttributeValue("description", value); }

    public bool? IsPriceOverridden { get => GetAttributeValue<bool?>("ispriceoverridden"); set => SetAttributeValue("ispriceoverridden", value); }

    public bool? IsProductOverridden { get => GetAttributeValue<bool?>("isproductoverridden"); set => SetAttributeValue("isproductoverridden", value); }
}
