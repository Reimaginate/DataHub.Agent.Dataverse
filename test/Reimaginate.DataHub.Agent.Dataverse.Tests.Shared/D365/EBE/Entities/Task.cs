namespace DataverseModel;

[System.Runtime.Serialization.DataContractAttribute()]
[Microsoft.Xrm.Sdk.Client.EntityLogicalNameAttribute("task")]
public partial class Task : Microsoft.Xrm.Sdk.Entity
{
    public partial class Fields
    {
        public const string ActivityId = "activityid";
        public const string Id = "activityid";
        public const string ActualDurationMinutes = "actualdurationminutes";
        public const string ActualEnd = "actualend";
        public const string ActualStart = "actualstart";
        public const string CreatedOn = "createdon";
        public const string Description = "description";
        public const string ModifiedOn = "modifiedon";
        public const string OwnerId = "ownerid";
        public const string RegardingObjectId = "regardingobjectid";
        public const string ScheduledDurationMinutes = "scheduleddurationminutes";
        public const string ScheduledEnd = "scheduledend";
        public const string ScheduledStart = "scheduledstart";
        public const string Subject = "subject";
    }

    public Task() : base(EntityLogicalName)
    {
    }

    public const string EntityLogicalName = "task";

    public const string PrimaryIdAttribute = "activityid";

    public const string PrimaryNameAttribute = "subject";

    public System.Guid? ActivityId
    {
        get => GetAttributeValue<System.Guid?>("activityid");
        set
        {
            SetAttributeValue("activityid", value);
            base.Id = value ?? System.Guid.Empty;
        }
    }

    public override System.Guid Id
    {
        get => base.Id;
        set => ActivityId = value;
    }

    public string Subject
    {
        get => GetAttributeValue<string>("subject");
        set => SetAttributeValue("subject", value);
    }

    public string Description
    {
        get => GetAttributeValue<string>("description");
        set => SetAttributeValue("description", value);
    }

    public System.DateTime? ScheduledStart
    {
        get => GetAttributeValue<System.DateTime?>("scheduledstart");
        set => SetAttributeValue("scheduledstart", value);
    }

    public System.DateTime? ScheduledEnd
    {
        get => GetAttributeValue<System.DateTime?>("scheduledend");
        set => SetAttributeValue("scheduledend", value);
    }

    public int? ScheduledDurationMinutes
    {
        get => GetAttributeValue<int?>("scheduleddurationminutes");
        set => SetAttributeValue("scheduleddurationminutes", value);
    }

    public System.DateTime? ActualStart
    {
        get => GetAttributeValue<System.DateTime?>("actualstart");
        set => SetAttributeValue("actualstart", value);
    }

    public System.DateTime? ActualEnd
    {
        get => GetAttributeValue<System.DateTime?>("actualend");
        set => SetAttributeValue("actualend", value);
    }

    public int? ActualDurationMinutes
    {
        get => GetAttributeValue<int?>("actualdurationminutes");
        set => SetAttributeValue("actualdurationminutes", value);
    }

    public Microsoft.Xrm.Sdk.EntityReference OwnerId
    {
        get => GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("ownerid");
        set => SetAttributeValue("ownerid", value);
    }

    public Microsoft.Xrm.Sdk.EntityReference RegardingObjectId
    {
        get => GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("regardingobjectid");
        set => SetAttributeValue("regardingobjectid", value);
    }
}
