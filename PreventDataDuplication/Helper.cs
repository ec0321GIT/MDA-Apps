using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace PreventDataDuplication
{
    public static class QueryHelper
    {
        public static ConditionExpression CreateCondition(string attributeName, object value)
        {
            return value == null
                ? new ConditionExpression(attributeName, ConditionOperator.Null)
                : new ConditionExpression(attributeName, ConditionOperator.Equal, value);
        }

        public static QueryExpression BuildQuery(string entityLogicalName, EntityReference sourceProperty, EntityReference sourceSystem, EntityReference sourceLegalEntity, string sourceAccountNumber)
        {
            var query = new QueryExpression(entityLogicalName)
            {
                ColumnSet = new ColumnSet(
                    Constants.AttributeSourceProperty,
                    Constants.AttributeSourceSystem,
                    Constants.AttributeSourceAccountNumber,
                    Constants.AttributeSourceLegalEntity
                ),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        CreateCondition(Constants.AttributeSourceProperty, sourceProperty?.Id),
                        CreateCondition(Constants.AttributeSourceSystem, sourceSystem?.Id),
                        CreateCondition(Constants.AttributeSourceAccountNumber, sourceAccountNumber),
                        CreateCondition(Constants.AttributeSourceLegalEntity, sourceLegalEntity?.Id)
                    }
                },
                TopCount = 1 // Limit the query to 1 record for performance
            };

            return query;
        }
    }
}