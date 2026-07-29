namespace DataverseModel;

[System.Runtime.Serialization.DataContractAttribute()]
[Microsoft.Xrm.Sdk.Client.EntityLogicalNameAttribute("appointment")]
public partial class Appointment : Microsoft.Xrm.Sdk.Entity
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
        public const string IsAllDayEvent = "isalldayevent";
        public const string Location = "location";
        public const string ModifiedOn = "modifiedon";
        public const string OptionalAttendees = "optionalattendees";
        public const string Organizer = "organizer";
        public const string OwnerId = "ownerid";
        public const string RegardingObjectId = "regardingobjectid";
        public const string RequiredAttendees = "requiredattendees";
        public const string ScheduledDurationMinutes = "scheduleddurationminutes";
        public const string ScheduledEnd = "scheduledend";
        public const string ScheduledStart = "scheduledstart";
        public const string Subject = "subject";
    }

    public Appointment() : base(EntityLogicalName)
    {
    }

    public const string EntityLogicalName = "appointment";

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

    public string Subject { get => GetAttributeValue<string>("subject"); set => SetAttributeValue("subject", value); }

    public string Description { get => GetAttributeValue<string>("description"); set => SetAttributeValue("description", value); }

    public string Location { get => GetAttributeValue<string>("location"); set => SetAttributeValue("location", value); }

    public bool? IsAllDayEvent { get => GetAttributeValue<bool?>("isalldayevent"); set => SetAttributeValue("isalldayevent", value); }

    public System.DateTime? ScheduledStart { get => GetAttributeValue<System.DateTime?>("scheduledstart"); set => SetAttributeValue("scheduledstart", value); }

    public System.DateTime? ScheduledEnd { get => GetAttributeValue<System.DateTime?>("scheduledend"); set => SetAttributeValue("scheduledend", value); }

    public int? ScheduledDurationMinutes { get => GetAttributeValue<int?>("scheduleddurationminutes"); set => SetAttributeValue("scheduleddurationminutes", value); }

    public System.DateTime? ActualStart { get => GetAttributeValue<System.DateTime?>("actualstart"); set => SetAttributeValue("actualstart", value); }

    public System.DateTime? ActualEnd { get => GetAttributeValue<System.DateTime?>("actualend"); set => SetAttributeValue("actualend", value); }

    public int? ActualDurationMinutes { get => GetAttributeValue<int?>("actualdurationminutes"); set => SetAttributeValue("actualdurationminutes", value); }

    public Microsoft.Xrm.Sdk.EntityReference OwnerId { get => GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("ownerid"); set => SetAttributeValue("ownerid", value); }

    public Microsoft.Xrm.Sdk.EntityReference RegardingObjectId { get => GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("regardingobjectid"); set => SetAttributeValue("regardingobjectid", value); }

    public System.Collections.Generic.IEnumerable<DataverseModel.ActivityParty> RequiredAttendees
    {
        get => GetParties("requiredattendees");
        set => SetParties("requiredattendees", value);
    }

    public System.Collections.Generic.IEnumerable<DataverseModel.ActivityParty> OptionalAttendees
    {
        get => GetParties("optionalattendees");
        set => SetParties("optionalattendees", value);
    }

    public System.Collections.Generic.IEnumerable<DataverseModel.ActivityParty> Organizer
    {
        get => GetParties("organizer");
        set => SetParties("organizer", value);
    }

    private System.Collections.Generic.IEnumerable<DataverseModel.ActivityParty> GetParties(string attributeName)
    {
        var collection = GetAttributeValue<Microsoft.Xrm.Sdk.EntityCollection>(attributeName);
        return collection?.Entities?.Select(entity => entity.ToEntity<DataverseModel.ActivityParty>());
    }

    private void SetParties(string attributeName, System.Collections.Generic.IEnumerable<DataverseModel.ActivityParty> value)
    {
        SetAttributeValue(attributeName, value is null ? null : new Microsoft.Xrm.Sdk.EntityCollection(new System.Collections.Generic.List<Microsoft.Xrm.Sdk.Entity>(value)));
    }
}
