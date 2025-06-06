namespace PreventDataDuplication
{
    public static class Constants
    {
        public const string TargetParameter = "Target";
        public const string TargetEntityLogicalName = "trax_accountmapping";

        public const string AttributeSourceLegalEntity = "trax_sourcelegalentity";
        public const string AttributeSourceAccountNumber = "trax_orignalname";
        public const string AttributeSourceProperty = "trax_sourceproperty";
        public const string AttributeSourceSystem = "trax_sourcesystem";

        public const string ErrorMessageDuplicateRecord = "A record with the same source property, source system, source legal and source account number already exists.";
    }
}