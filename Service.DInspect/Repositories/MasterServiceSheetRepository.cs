using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.DInspect.Interfaces;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Request;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.DInspect.Repositories
{
    public class MasterServiceSheetRepository : RepositoryBase
    {
        public MasterServiceSheetRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }

        public virtual async Task<dynamic> GetDataForParameter(string modelId, string psTypeId)
        {
            string query = $"SELECT task.key, task.category, items.key as itemskey, items.itemsType, items[\"value\"], task.rating, c.modelId, c.psTypeId FROM c join subGroup in c.subGroup join taskGroup in subGroup.taskGroup join task in taskGroup.task join items in task.items WHERE (c.modelId = \"{modelId}\" OR \"{modelId}\" = \"\") AND (c.psTypeId = \"{psTypeId}\" OR \"{psTypeId}\" = \"\") AND c.key <> \"{EnumGroup.General}\" AND task.category = \"{EnumTaskType.CBM}\" AND task.rating IN (\"{EnumRatingValue.Automatic}\", \"{EnumRatingValue.Calibration}\") AND items.itemType = \"label\" AND items[\"value\"] LIKE \"[0-9]%\" and c.isActive = \"true\" and c.isDeleted = \"false\"";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }

            //var topResult = results.OrderByDescending(x => x["serviceDataConvert"]).FirstOrDefault();

            return results;
        }

        public virtual async Task<dynamic> GetDataCBM(dynamic param)
        {
            string query = $"SELECT  c.modelId,       c.psTypeId,       task.key as taskKey,        task.category,        task.rating,     REPLACE(task.items[0][\"value\"], task.items[1][\"value\"], \"\")  as taskNo,       task.items[0][\"value\"] as taskNoDetail,       CONCAT(task.items[1][\"value\"], \" \", task.items[2][\"value\"]) as taskDescription,    task.items[4][\"value\"] = [] ? \"\" : task.items[4][\"value\"] as uom    FROM c join subGroup in c.subGroup join taskGroup in subGroup.taskGroup join task in taskGroup.task where c.modelId = \"{param.modelId}\" and c.psTypeId = \"{param.psTypeId}\" and task.category = 'CBM' and c.key <> 'GENERAL' and task.key <> 'safetyTask000000' and task.taskValue = \"\" and c.isActive = \"true\" and c.isDeleted = \"false\" order by c.groupSeq";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }

            return results;
        }

        public virtual async Task<dynamic> GetTaskCrackMapping(string modelId, string psTypeId)
        {
            string query = $"SELECT c.modelId as modelId, c.psTypeId as psTypeId, task.key as taskKey, ARRAY_LENGTH(task.items) = 4 ?  task.items[1][\"value\"] : task.items[2][\"value\"] as crackCode, ARRAY_LENGTH(task.items) = 4 ? task.items[2][\"value\"] : task.items[1][\"value\"]  as locationDesc FROM c join subGroup in c.subGroup join taskGroup in subGroup.taskGroup join task in taskGroup.task where c.modelId = \"{modelId}\" and c.psTypeId = \"{psTypeId}\" and c.key <> 'GENERAL' and task.category = 'CRACK' and c.isActive = \"true\" and c.isDeleted = \"false\"";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }

            return results;
        }

        public virtual async Task<dynamic> GetDataMasterServiceByCalculateKey(string modelId, string calculateKey, string psTypeId)
        {
            string query = $"SELECT  task.key,   task.seqId,   task.taskType,   task.isActive,   task.isDeleted,   task.updatedBy,   task.header,   task.description,   task.category,   task.rating,   task.updatedDate,   task.groupTaskId,   task.items,   task.taskValue FROM c join subGroup in c.subGroup join taskGroup in subGroup.taskGroup join task in taskGroup.task where     c.modelId = \"{modelId}\" and  task.items[3][\"targetCalculateKeyId\"] = \"{calculateKey}\" and  c.psTypeId = \"{psTypeId}\" and c.isActive = \"true\" and c.isDeleted = \"false\"";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }
            return results;
        }

        public virtual async Task<dynamic> GetMasterServiceSheetTemplate(string modelId, string psTypeId)
        {
            string query = $"SELECT   c.modelId, c.psTypeId, task.items[0][\"value\"] as taskNo,   subGroup.name as tab,   taskGroup.name as section,   CONCAT(task.items[1][\"value\"], \". \", task.items[2][\"value\"]) as description,   task.category = 'NORMAL' and task.rating = 'NO' ? 'Defect'          : task.category = 'CBM' and task.rating = 'AUTOMATIC' ? 'CBM'         : task.category = 'CBM' and task.rating = 'MANUAL' ? 'CBM_MANUAL'         : task.category = 'CBM' and task.rating = 'NORMAL' ? 'CBM_NORMAL'         : task.category = 'CRACK' ? 'CRACK'          : '' as type,   c.psTypeId = '250' ? task.items[0][\"value\"] : '' as Number250,   c.psTypeId = '500' ? task.items[0][\"value\"] : '' as Number500,   c.psTypeId = '1000' ? task.items[0][\"value\"] : '' as Number1000,   c.psTypeId = '2000' ? task.items[0][\"value\"] : '' as Number2000,   c.psTypeId = '4000' ? task.items[0][\"value\"] : '' as Number4000, '' as guideTable,     '' as imageId,     '' as tableId,     task.items[2][\"categoryItemType\"] = 'mappingParamKey' ? task.items[2][\"value\"] : '' as serviceMapping,     '' as sos,     '' as sectionColumn, task.key as taskKey, task.groupTaskId as groupTaskId FROM c join subGroup in c.subGroup join taskGroup in subGroup.taskGroup join task in taskGroup.task where c.modelId = \"{modelId}\" and c.key <> 'GENERAL' and task.key <> 'safetyTask000000' and task.taskValue = \"\" and c.isActive = \"true\" and c.isDeleted = \"false\" order by c.groupSeq";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }

            return results;
        }
        public virtual async Task<dynamic> GetTaskCollection(GetTaskCollectionRequest request)
        {
            var pagination = "";
            var order = " ORDER BY c.groupSeq asc";
            if (request.pageSize != 0) pagination = $" OFFSET {request.page * request.pageSize - request.pageSize} LIMIT {request.pageSize}";
            #region order by
            if (!string.IsNullOrWhiteSpace(request.orderBy))
            {
                var orderQuery = request.orderBy.Split('_').Last();
                if (request.orderBy.Contains(EnumQuery.ModelIdUnderscore))
                {
                    order = " ORDER BY c.modelId " + orderQuery;
                }
                else if (request.orderBy.Contains(EnumQuery.PsTypeUnderscore))
                {
                    //order = "ORDER BY c.psTypeId " + orderQuery;
                    pagination = "";
                }
                else if (request.orderBy.Contains(EnumQuery.Version))
                {
                    order = " ORDER BY c.version " + orderQuery;
                }
                else if (request.orderBy.Contains(EnumQuery.Category))
                {
                    //order = "ORDER BY task.category " + orderQuery;
                    pagination = "";
                }
                else if (request.orderBy.Contains(EnumQuery.Rating))
                {
                    //order = "ORDER BY task.rating " + orderQuery;
                    pagination = "";
                }
                else if (request.orderBy.Contains(EnumQuery.Description))
                {
                    //order = "ORDER BY task.description " + orderQuery;
                    pagination = "";
                }
                else if (request.orderBy.Contains(EnumQuery.SubTaskUnderscore))
                {
                    //order = "ORDER BY task.items[0][\"value\"] " + orderQuery;
                    pagination = "";
                }
                else if (request.orderBy.Contains(EnumQuery.Status))
                {
                    order = " ORDER BY c.isActive " + orderQuery;
                }

            }
            #endregion
            string query = $"SELECT c.modelId, c.psTypeId, c.version, task.category, task.rating as rating, REPLACE(REPLACE(task.description, ';', '. '), '. . ', '. ') as description, task.items[0][\"value\"] as subTask, c.isActive as status FROM c join subGroup in c.subGroup join taskGroup in subGroup.taskGroup join task in taskGroup.task join items in task.items WHERE IS_DEFINED(task.category) = true and IS_DEFINED(task.rating) = true and IS_DEFINED(task.description) = true and IS_DEFINED(task.items[0][\"value\"]) = true and items[\"value\"] LIKE \"[0-9]%\" and task.description LIKE \"%;%;%\"";

            #region condition
            if (!string.IsNullOrWhiteSpace(request.modelId))
            {
                query += $" AND c.modelId = \"{request.modelId}\"";
            }
            if (!string.IsNullOrWhiteSpace(request.psTypeId))
            {
                query += $" AND c.psTypeId = \"{request.psTypeId}\"";
            }
            if (!string.IsNullOrWhiteSpace(request.version))
            {
                query += $" AND c.version = \"{request.version}\"";
            }
            if (!string.IsNullOrWhiteSpace(request.category))
            {
                query += $" AND task.category = \"{request.category}\"";
            }
            if (!string.IsNullOrWhiteSpace(request.description))
            {
                query += $" AND CONTAINS(task.description,\"{request.description}\",true)";
            }
            if (!string.IsNullOrWhiteSpace(request.subTask))
            {
                query += $" AND items[\"value\"] like \"%{request.subTask}%\"";
                pagination = "";
            }
            if (!string.IsNullOrWhiteSpace(request.status))
            {
                query += $" AND c.isActive = \"{request.status}\"";
            }
            query += order + pagination;
            #endregion
            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }
            return results;
        }

        public virtual async Task<dynamic> GetTotalData(GetTaskCollectionRequest request)
        {
            string query = $"SELECT COUNT(1) as TotalData FROM c join subGroup in c.subGroup join taskGroup in subGroup.taskGroup join task in taskGroup.task join items in task.items WHERE IS_DEFINED(task.category) = true and IS_DEFINED(task.rating) = true and IS_DEFINED(task.description) = true and IS_DEFINED(task.items[0][\"value\"]) = true and items[\"value\"] LIKE \"[0-9]%\" and task.description LIKE \"%;%;%\"";

            #region condition
            if (!string.IsNullOrWhiteSpace(request.modelId))
            {
                query += $" AND c.modelId = \"{request.modelId}\"";
            }
            if (!string.IsNullOrWhiteSpace(request.psTypeId))
            {
                query += $" AND c.psTypeId = \"{request.psTypeId}\"";
            }
            if (!string.IsNullOrWhiteSpace(request.version))
            {
                query += $" AND c.version = \"{request.version}\"";
            }
            if (!string.IsNullOrWhiteSpace(request.category))
            {
                query += $" AND task.category = \"{request.category}\"";
            }
            if (!string.IsNullOrWhiteSpace(request.description))
            {
                query += $" AND CONTAINS(task.description,\"{request.description}\",true)";
            }
            if (!string.IsNullOrWhiteSpace(request.subTask))
            {
                query += $" AND items[\"value\"] like \"%{request.subTask}%\"";
            }
            if (!string.IsNullOrWhiteSpace(request.status))
            {
                query += $" AND c.isActive = \"{request.status}\"";
            }
            #endregion

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }
            return results;
        }

        public virtual async Task<dynamic> GetModelTaskCollection(GetTaskCollectionRequest request)
        {
            string query = $"SELECT DISTINCT c.modelId FROM c join subGroup in c.subGroup join taskGroup in subGroup.taskGroup join task in taskGroup.task join items in task.items WHERE c.psTypeId LIKE \"%{request.psTypeId}%\" AND c.version LIKE \"%{request.version}%\" AND task.category LIKE \"%{request.category}%\" and items[\"value\"] LIKE \"%{request.subTask}%\" and c.isActive LIKE \"%{request.status}%\"";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }
            return results;
        }

        public virtual async Task<dynamic> GetPsTypeTaskCollection(GetTaskCollectionRequest request)
        {
            string query = $"SELECT DISTINCT c.psTypeId FROM c join subGroup in c.subGroup join taskGroup in subGroup.taskGroup join task in taskGroup.task join items in task.items WHERE c.modelId LIKE \"%{request.modelId}%\" AND c.version LIKE \"%{request.version}%\" AND task.category LIKE \"%{request.category}%\" and items[\"value\"] LIKE \"%{request.subTask}%\" and c.isActive LIKE \"%{request.status}%\"";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }
            return results;
        }

        public virtual async Task<dynamic> GetVersionTaskCollection(GetTaskCollectionRequest request)
        {
            string query = $"SELECT DISTINCT c.version FROM c join subGroup in c.subGroup join taskGroup in subGroup.taskGroup join task in taskGroup.task join items in task.items WHERE c.modelId LIKE \"%{request.modelId}%\" AND c.psTypeId LIKE \"%{request.psTypeId}%\" AND task.category LIKE \"%{request.category}%\" and items[\"value\"] LIKE \"%{request.subTask}%\" and c.isActive LIKE \"%{request.status}%\"";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }
            return results;
        }

        public virtual async Task<dynamic> GetVersionTaskCollection(GetTaskCollectionRequest request, bool ignoreStatus)
        {
            string query = ignoreStatus
                            ? $"SELECT DISTINCT c.version FROM c" +
                                $" join subGroup in c.subGroup" +
                                $" join taskGroup in subGroup.taskGroup" +
                                $" join task in taskGroup.task" +
                                $" join items in task.items" +
                                $" WHERE c.modelId LIKE \"%{request.modelId}%\"" +
                                $" AND c.psTypeId LIKE \"%{request.psTypeId}%\"" +
                                $" AND task.category LIKE \"%{request.category}%\"" +
                                $" and items[\"value\"] LIKE \"%{request.subTask}%\""
                            : $"SELECT DISTINCT c.version FROM c" +
                                $" join subGroup in c.subGroup" +
                                $" join taskGroup in subGroup.taskGroup" +
                                $" join task in taskGroup.task" +
                                $" join items in task.items" +
                                $" WHERE c.modelId LIKE \"%{request.modelId}%\"" +
                                $" AND c.psTypeId LIKE \"%{request.psTypeId}%\"" +
                                $" AND task.category LIKE \"%{request.category}%\"" +
                                $" and items[\"value\"] LIKE \"%{request.subTask}%\"" +
                                $" and c.isActive LIKE \"%{request.status}%\"";


            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }
            return results;
        }

        public virtual async Task<dynamic> GetCategoryTaskCollection(GetTaskCollectionRequest request)
        {
            string query = $"SELECT DISTINCT task.category FROM c join subGroup in c.subGroup join taskGroup in subGroup.taskGroup join task in taskGroup.task join items in task.items WHERE c.modelId LIKE \"%{request.modelId}%\" AND c.psTypeId LIKE \"%{request.psTypeId}%\" AND c.version LIKE \"%{request.version}%\" and items[\"value\"] LIKE \"%{request.subTask}%\" and c.isActive LIKE \"%{request.status}%\"";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }
            return results;
        }

        public virtual async Task<dynamic> GetSubTaskTaskCollection(GetTaskCollectionRequest request)
        {
            string query = $"SELECT DISTINCT items[\"value\"] as subTask FROM c join subGroup in c.subGroup join taskGroup in subGroup.taskGroup join task in taskGroup.task join items in task.items WHERE c.modelId LIKE \"%{request.modelId}%\" AND c.psTypeId LIKE \"%{request.psTypeId}%\" AND c.version LIKE \"%{request.version}%\" and task.category LIKE \"%{request.category}%\" and c.isActive LIKE \"%{request.status}%\" and items[\"value\"] LIKE \"[0-9]%\"";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }
            return results;
        }

        public virtual async Task<dynamic> GetStatusTaskCollection(GetTaskCollectionRequest request)
        {
            string query = $"SELECT DISTINCT c.isActive as status FROM c join subGroup in c.subGroup join taskGroup in subGroup.taskGroup join task in taskGroup.task join items in task.items WHERE c.modelId LIKE \"%{request.modelId}%\" AND c.psTypeId LIKE \"%{request.psTypeId}%\" AND c.version LIKE \"%{request.version}%\" and task.category LIKE \"%{request.category}%\" and items[\"value\"] LIKE \"%{request.subTask}%\"";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }
            return results;
        }

        public virtual async Task<dynamic> GetServicesheet(ServicesheetRequest servicesheetRequest)
        {
            var containerName = _container.Id;
            var selectedFields = servicesheetRequest?.selectedFields?.Select(x => $"{x.level}[\"{x.fieldName}\"]");
            string strSelectedFields = selectedFields == null ? "*" : string.Join(",", selectedFields);

            string sql = $"SELECT DISTINCT {strSelectedFields} FROM c as tab JOIN subGroup in tab.subGroup JOIN taskGroup in subGroup.taskGroup JOIN task in taskGroup.task JOIN items in task.items WHERE tab[\"isActive\"] = \"true\" AND tab[\"isDeleted\"] = \"false\" ";

            object[] args = null;

            if (servicesheetRequest.parameters?.Count > 0)
            {
                StringBuilder sbParameters = new StringBuilder();
                int i = 0;
                int countValues = servicesheetRequest.parameters.Where(x => x.fieldValue != null).Count();

                if (countValues > 0) args = new object[countValues];

                foreach (var property in servicesheetRequest.parameters)
                {
                    if (property.fieldValue == null)
                    {
                        sbParameters.Append($"{property.level}[\"{property.fieldName}\"]" + " IS NULL AND ");
                    }
                    else
                    {
                        if (property.fieldValue.GetType().Name == "JArray")
                        {
                            var arrPropValue = JsonConvert.DeserializeObject<string[]>(JsonConvert.SerializeObject(property.fieldValue));
                            string propValue = $"(\"{string.Join("\", \"", arrPropValue)}\")";
                            sbParameters.Append($"{property.level}[\"{property.fieldName}\"]" + " IN " + propValue + " AND ");
                        }
                        else
                        {
                            sbParameters.Append($"{property.level}[\"{property.fieldName}\"]" + " = \"" + property.fieldValue + "\" AND ");
                        }

                        if (args != null)
                        {
                            args[i] = property.fieldValue;
                        }
                        i++;
                    }
                }

                string parameters = sbParameters.ToString();
                parameters = parameters.Substring(0, parameters.Length - 5);
                sql += $"AND {parameters}";
            }

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(new QueryDefinition(sql)));
            List<dynamic> results = new List<dynamic>();

            while (response.HasMoreResults)
                results.AddRange(await response.ReadNextAsync());

            return results;
        }

        public virtual async Task<dynamic> GetDataMasterServiceWithUpdatedDate(Dictionary<string, object> param)
        {
            string taskQuery = $"SELECT c.id, task.key " +
                $"FROM c " +
                $"JOIN subGroup IN c.subGroup " +
                $"JOIN taskGroup IN subGroup.taskGroup " +
                $"JOIN task IN taskGroup.task " +
                $"WHERE task.updatedDate <> \"\" and task.description <> \"\" and c.isActive = \"true\" and c.isDeleted = \"false\"";

            string itemQuery = $"SELECT c.id, taskItem.key " +
                $"FROM c " +
                $"JOIN subGroup IN c.subGroup " +
                $"JOIN taskGroup IN subGroup.taskGroup " +
                $"JOIN task IN taskGroup.task " +
                $"JOIN taskItem IN task.items " +
                $"WHERE taskItem.updatedDate <> \"\" and c.isActive = \"true\" and c.isDeleted = \"false\"";

            object[] args = null;

            param = param.Where(x => x.Key != EnumQuery.Fields)?.ToDictionary(x => x.Key, x => x.Value);

            if (param.Count > 0)
            {
                StringBuilder sbParameters = new StringBuilder();
                int i = 0;
                int countValues = param.Where(x => x.Value != null).Count();

                if (countValues > 0) args = new object[countValues];

                foreach (var property in param)
                {
                    if (property.Value == null)
                    {
                        sbParameters.Append($"IS_NULL(c[\"{property.Key}\"]) = true AND ");
                    }
                    else
                    {
                        if (property.Value.GetType().Name == "JArray")
                        {
                            var arrPropValue = JsonConvert.DeserializeObject<string[]>(JsonConvert.SerializeObject(property.Value));
                            string propValue = $"(\"{string.Join("\", \"", arrPropValue)}\")";
                            //sbParameters.Append("ToString(c" + $"[\"{property.Key}\"]" + ") IN " + propValue + " AND ");
                            sbParameters.Append("c" + $"[\"{property.Key}\"]" + " IN " + propValue + " AND ");
                        }
                        else
                        {
                            //sbParameters.Append("ToString(c" + $"[\"{property.Key}\"]" + ") = \"" + property.Value + "\" AND ");
                            sbParameters.Append("c" + $"[\"{property.Key}\"]" + " = \"" + property.Value + "\" AND ");
                        }

                        if (args != null)
                        {
                            args[i] = property.Value;
                        }
                        i++;
                    }
                }

                string parameters = sbParameters.ToString();
                parameters = parameters.Substring(0, parameters.Length - 5);

                taskQuery += $" and {parameters}";
                itemQuery += $" and {parameters}";
            }

            JArray results = new JArray();

            var taskResponse = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(taskQuery));

            while (taskResponse.HasMoreResults)
            {
                foreach (var item in await taskResponse.ReadNextAsync())
                    results.Add(item);
            }

            var itemResponse = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(itemQuery));

            while (itemResponse.HasMoreResults)
            {
                foreach (var item in await itemResponse.ReadNextAsync())
                    results.Add(item);
            }

            return results;
        }

        public virtual async Task<dynamic> CheckModelDocuments(List<string> modelId)
        {
            Dictionary<string, Dictionary<string, List<string>>> content = new Dictionary<string, Dictionary<string, List<string>>>();

            if (modelId.Count < 1)
            {
                string modelQuery = $"SELECT DISTINCT VALUE c.modelId " +
                $"FROM c " +
                $"WHERE c.isActive = \"true\" and c.isDeleted = \"false\"";

                var modelResponse = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(modelQuery));

                while (modelResponse.HasMoreResults)
                {
                    foreach (var item in await modelResponse.ReadNextAsync())
                        modelId.Add((string)item);
                }
            }



            foreach (var id in modelId)
            {
                string psTypeQuery = $"SELECT DISTINCT VALUE c.psTypeId " +
                $"FROM c " +
                $"WHERE c.isActive = \"true\" and c.isDeleted = \"false\" and c.modelId = \"{id}\"";

                var psTypeIds = new List<string>();

                var psTypeResponse = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(psTypeQuery));

                while (psTypeResponse.HasMoreResults)
                {
                    foreach (var item in await psTypeResponse.ReadNextAsync())
                        psTypeIds.Add(item);
                }

                psTypeIds = psTypeIds.OrderBy(n => int.TryParse(n, out _) ? 1 : 0)
                                          .ThenBy(n => int.TryParse(n, out int result) ? result : 0)
                                          .ToList();

                var psTypes = new Dictionary<string, List<string>>();

                foreach (var psType in psTypeIds)
                {
                    string tabQuery = $"SELECT DISTINCT VALUE c.groupName " +
                                        $"FROM c " +
                                        $"WHERE c.isActive = \"true\" and c.isDeleted = \"false\" and c.modelId = \"{id}\" and c.psTypeId = \"{psType}\"" +
                                        $"ORDER BY c.groupSeq";
                    var tabResponse = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(tabQuery));

                    var tabNames = new List<string>();

                    while (tabResponse.HasMoreResults)
                    {
                        foreach (var item in await tabResponse.ReadNextAsync())
                            tabNames.Add(item);
                    }

                    psTypes.Add(psType, tabNames);
                }
                content.Add(id, psTypes);
            }

            return content;
        }
    }
}