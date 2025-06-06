using Microsoft.Xrm.Sdk;
using System;
using Microsoft.Xrm.Sdk.Query;

namespace PreventDataDuplication
{
    public class PreventDataDuplication : PluginBase
    {
        private IOrganizationService _service;

        public PreventDataDuplication(string unsecureConfiguration, string secureConfiguration)
            : base(typeof(PreventDataDuplication))
        {
        }

        public override void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var pluginExecutionContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            _service = serviceFactory.CreateOrganizationService(pluginExecutionContext.UserId);

            base.Execute(serviceProvider);
        }

        protected override void ExecuteDataversePlugin(ILocalPluginContext localPluginContext)
        {
            if (localPluginContext == null)
            {
                throw new ArgumentNullException(nameof(localPluginContext));
            }

            var context = localPluginContext.PluginExecutionContext;

            if (IsValidTargetEntity(context, out Entity entity))
            {
                var (sourceProperty, sourceSystem, sourceLegalEntity, sourceAccountNumber) = ExtractAttributes(entity);

                // On update, retrieve missing attributes from database if not present in the entity
                if (context.MessageName.Equals("Update", StringComparison.OrdinalIgnoreCase))
                {
                    var recordId = entity.Id;
                    localPluginContext.TracingService.Trace("Record ID: {0}", recordId.ToString());

                    // Retrieve missing attributes if they are null
                    if (sourceProperty == null || sourceSystem == null || sourceLegalEntity == null || string.IsNullOrEmpty(sourceAccountNumber))
                    {
                        var columns = new ColumnSet(
                            Constants.AttributeSourceProperty,
                            Constants.AttributeSourceSystem,
                            Constants.AttributeSourceLegalEntity,
                            Constants.AttributeSourceAccountNumber
                        );
                        var dbEntity = _service.Retrieve(Constants.TargetEntityLogicalName, recordId, columns);

                        if (sourceProperty == null && dbEntity.Contains(Constants.AttributeSourceProperty))
                            sourceProperty = dbEntity[Constants.AttributeSourceProperty] as EntityReference;
                        if (sourceSystem == null && dbEntity.Contains(Constants.AttributeSourceSystem))
                            sourceSystem = dbEntity[Constants.AttributeSourceSystem] as EntityReference;
                        if (sourceLegalEntity == null && dbEntity.Contains(Constants.AttributeSourceLegalEntity))
                            sourceLegalEntity = dbEntity[Constants.AttributeSourceLegalEntity] as EntityReference;
                        if (string.IsNullOrEmpty(sourceAccountNumber) && dbEntity.Contains(Constants.AttributeSourceAccountNumber))
                            sourceAccountNumber = dbEntity[Constants.AttributeSourceAccountNumber]?.ToString();
                    }

                    LogAttributes(localPluginContext, sourceProperty, sourceSystem, sourceLegalEntity, sourceAccountNumber);

                    // Exclude the current record from duplicate check during update
                    if (HasDuplicateRecords(sourceProperty, sourceSystem, sourceLegalEntity, sourceAccountNumber, recordId))
                    {
                        localPluginContext.TracingService.Trace(Constants.ErrorMessageDuplicateRecord);
                        throw new InvalidPluginExecutionException(Constants.ErrorMessageDuplicateRecord);
                    }
                }
                else if (context.MessageName.Equals("Create", StringComparison.OrdinalIgnoreCase))
                {
                    LogAttributes(localPluginContext, sourceProperty, sourceSystem, sourceLegalEntity, sourceAccountNumber);

                    if (HasDuplicateRecords(sourceProperty, sourceSystem, sourceLegalEntity, sourceAccountNumber))
                    {
                        localPluginContext.TracingService.Trace(Constants.ErrorMessageDuplicateRecord);
                        throw new InvalidPluginExecutionException(Constants.ErrorMessageDuplicateRecord);
                    }
                }

                localPluginContext.TracingService.Trace("No duplicate records found.");
            }
        }

        private static bool IsValidTargetEntity(IPluginExecutionContext context, out Entity entity)
        {
            entity = null;
            if (context.InputParameters.Contains(Constants.TargetParameter)
                && context.InputParameters[Constants.TargetParameter] is Entity targetEntity
                && targetEntity.LogicalName == Constants.TargetEntityLogicalName)
            {
                entity = targetEntity;
                return true;
            }
            return false;
        }

        private static (EntityReference sourceProperty, EntityReference sourceSystem, EntityReference sourceLegalEntity, string sourceAccountNumber) ExtractAttributes(Entity entity)
        {
            var sourceProperty = entity.Attributes.Contains(Constants.AttributeSourceProperty)
                ? entity.Attributes[Constants.AttributeSourceProperty] as EntityReference
                : null;

            var sourceSystem = entity.Attributes.Contains(Constants.AttributeSourceSystem)
                ? entity.Attributes[Constants.AttributeSourceSystem] as EntityReference
                : null;

            var sourceLegalEntity = entity.Attributes.Contains(Constants.AttributeSourceLegalEntity)
                ? entity.Attributes[Constants.AttributeSourceLegalEntity] as EntityReference
                : null;

            var sourceAccountNumber = entity.Attributes.Contains(Constants.AttributeSourceAccountNumber)
                ? entity.Attributes[Constants.AttributeSourceAccountNumber]?.ToString()
                : null;

            return (sourceProperty, sourceSystem, sourceLegalEntity, sourceAccountNumber);
        }

        private static void LogAttributes(ILocalPluginContext localPluginContext, EntityReference sourceProperty, EntityReference sourceSystem, EntityReference sourceLegalEntity, string sourceAccountNumber)
        {
            localPluginContext.TracingService.Trace("Source Property: {0}", sourceProperty?.Id.ToString() ?? "null");
            localPluginContext.TracingService.Trace("Source System: {0}", sourceSystem?.Id.ToString() ?? "null");
            localPluginContext.TracingService.Trace("Source Legal Entity: {0}", sourceLegalEntity?.Id.ToString() ?? "null");
            localPluginContext.TracingService.Trace("Source Account Number: {0}", sourceAccountNumber ?? "null");
        }

        private bool HasDuplicateRecords(EntityReference sourceProperty, EntityReference sourceSystem, EntityReference sourceLegalEntity, string sourceAccountNumber, Guid? excludeRecordId = null)
        {
            var query = QueryHelper.BuildQuery(Constants.TargetEntityLogicalName, sourceProperty, sourceSystem, sourceLegalEntity, sourceAccountNumber);

            if (excludeRecordId.HasValue)
            {
                // Exclude the current record from the duplicate check
                // during update
                // Add condition to exclude the current record during update
                query.Criteria.AddCondition(Constants.AccountMappingId, ConditionOperator.NotEqual, excludeRecordId.Value.ToString());
            }

            var existingRecords = _service.RetrieveMultiple(query);
            return existingRecords != null && existingRecords.Entities.Count > 0;
        }
    }
}