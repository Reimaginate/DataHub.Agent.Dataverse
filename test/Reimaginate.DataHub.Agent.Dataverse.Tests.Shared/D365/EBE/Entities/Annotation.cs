namespace DataverseModel;

[System.Runtime.Serialization.DataContractAttribute()]
[Microsoft.Xrm.Sdk.Client.EntityLogicalNameAttribute("annotation")]
public partial class Annotation : Microsoft.Xrm.Sdk.Entity
{
    public partial class Fields
    {
        public const string AnnotationId = "annotationid";
        public const string Id = "annotationid";
        public const string CreatedOn = "createdon";
        public const string DocumentBody = "documentbody";
        public const string FileName = "filename";
        public const string IsDocument = "isdocument";
        public const string MimeType = "mimetype";
        public const string ModifiedOn = "modifiedon";
        public const string NoteText = "notetext";
        public const string ObjectId = "objectid";
        public const string OwnerId = "ownerid";
        public const string Subject = "subject";
    }

    public Annotation() : base(EntityLogicalName)
    {
    }

    public const string EntityLogicalName = "annotation";

    public const string PrimaryIdAttribute = "annotationid";

    public const string PrimaryNameAttribute = "subject";

    public System.Guid? AnnotationId
    {
        get => GetAttributeValue<System.Guid?>("annotationid");
        set
        {
            SetAttributeValue("annotationid", value);
            base.Id = value ?? System.Guid.Empty;
        }
    }

    public override System.Guid Id
    {
        get => base.Id;
        set => AnnotationId = value;
    }

    public string Subject { get => GetAttributeValue<string>("subject"); set => SetAttributeValue("subject", value); }

    public string NoteText { get => GetAttributeValue<string>("notetext"); set => SetAttributeValue("notetext", value); }

    public string FileName { get => GetAttributeValue<string>("filename"); set => SetAttributeValue("filename", value); }

    public string MimeType { get => GetAttributeValue<string>("mimetype"); set => SetAttributeValue("mimetype", value); }

    public string DocumentBody { get => GetAttributeValue<string>("documentbody"); set => SetAttributeValue("documentbody", value); }

    public bool? IsDocument { get => GetAttributeValue<bool?>("isdocument"); set => SetAttributeValue("isdocument", value); }

    public Microsoft.Xrm.Sdk.EntityReference ObjectId { get => GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("objectid"); set => SetAttributeValue("objectid", value); }

    public Microsoft.Xrm.Sdk.EntityReference OwnerId { get => GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("ownerid"); set => SetAttributeValue("ownerid", value); }
}
