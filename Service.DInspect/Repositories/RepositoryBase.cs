using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Service.DInspect.Interfaces;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Helper;
using Service.DInspect.Models.Request;
using Service.DInspect.Models.Response;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.DInspect.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.DInspect.Repositories
{
    public abstract class RepositoryBase : IRepositoryBase
    {
        protected readonly Database _database;
        protected readonly Container _container;
        protected readonly string _partitionKey;
        protected readonly IConnectionFactory _connectionFactory;

        public RepositoryBase(IConnectionFactory connectionFactory, string container)
        {
            _connectionFactory = connectionFactory;
            _database = connectionFactory.GetDatabase();

            if (container == EnumContainer.ServiceSheetDetail || container == EnumContainer.InterimEngineDetail || container == EnumContainer.CbmHistory)
            {
                _partitionKey = $"/workOrder";
            }
            else
            {
                _partitionKey = $"/id";
            }

            ContainerProperties properties = new ContainerProperties()
            {
                Id = container,
                PartitionKeyPath = _partitionKey
                //DefaultTimeToLive = -1
            };

            //var containerResponse = Task.Run(() => _database.CreateContainerIfNotExistsAsync(properties)).Result;
            _container = _database.GetContainer(container);
        }

        public virtual async Task<IEnumerable<dynamic>> GetAllData()
        {
            //string query = $"select * from c where ToString(c.{EnumCommonProperty.IsDeleted}) = \"false\"";
            string query = $"select * from c where c.{EnumCommonProperty.IsDeleted} = \"false\" OFFSET 0 LIMIT 1";
            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            List<dynamic> results = new List<dynamic>();

            while (response.HasMoreResults)
                results.AddRange(await response.ReadNextAsync());

            return results;
        }

        public virtual async Task<IEnumerable<dynamic>> GetActiveData()
        {
            string query = $"select * from c where ToString(c.{EnumCommonProperty.IsDeleted}) = \"false\"";
            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            List<dynamic> results = new List<dynamic>();

            while (response.HasMoreResults)
                results.AddRange(await response.ReadNextAsync());

            return results;
        }

        public virtual async Task<dynamic> Get(string id)
        {
            //string query = $"select * from c where ToString(c.{EnumCommonProperty.ID}) = \"{id}\" and ToString(c.{EnumCommonProperty.IsActive}) = \"true\" and ToString(c.{EnumCommonProperty.IsDeleted}) = \"false\"";
            //string query = $"select * from c where ToString(c.{EnumCommonProperty.ID}) = \"{id}\" and ToString(c.{EnumCommonProperty.IsDeleted}) = \"false\"";

            string query = $"select * from c where c.{EnumCommonProperty.ID} = \"{id}\" and c.{EnumCommonProperty.IsDeleted} = \"false\"";
            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));
            return response.ReadNextAsync().Result.FirstOrDefault();
        }

        public virtual async Task<dynamic> GetDataListByParam(Dictionary<string, object> param, bool includeSoftDeleted = false)
        {
            var containerName = _container.Id;

            var listFields = JsonConvert.DeserializeObject<string[]>(JsonConvert.SerializeObject(param.Where(x => x.Key == EnumQuery.Fields)?.FirstOrDefault().Value));
            string fieldsQuery = listFields == null ? "*" : $"c{string.Join(", c", listFields.ToList().Select(x => $"[\"{x}\"]"))}";

            string sql = $"SELECT {fieldsQuery} FROM c ";
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
                        sbParameters.Append("c" + $"[\"{property.Key}\"]" + " IS NULL AND ");
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

                //sql += " WHERE " + parameters + $" AND ToString(c[\"{EnumCommonProperty.IsActive}\"]) = \"true\" AND ToString(c[\"{EnumCommonProperty.IsDeleted}\"]) = \"false\"";
                sql += " WHERE " + parameters;

                if (includeSoftDeleted == false)
                    sql += $" AND c[\"{EnumCommonProperty.IsActive}\"] = \"true\" AND c[\"{EnumCommonProperty.IsDeleted}\"] = \"false\"";

                if (containerName == EnumContainer.DefectHeader)
                {
                    sql += " ORDER BY c[\"taskNo\"]";
                }
                else if (containerName == EnumContainer.MasterServiceSheet)
                {
                    sql += " ORDER BY c[\"groupSeq\"]";
                }
                else
                {
                    sql += " ORDER BY c[\"_ts\"] DESC";
                }
            }
            else
            {
                //sql += $" WHERE ToString(c[\"{EnumCommonProperty.IsDeleted}\"]) = \"false\" AND ToString(c[\"{EnumCommonProperty.IsActive}\"]) = \"true\"";
                sql += $" WHERE c[\"{EnumCommonProperty.IsDeleted}\"] = \"false\" AND c[\"{EnumCommonProperty.IsActive}\"] = \"true\"";

                if (containerName == EnumContainer.DefectHeader)
                {
                    sql += " ORDER BY c[\"taskNo\"]";
                }
                else if (containerName == EnumContainer.MasterServiceSheet)
                {
                    sql += " ORDER BY c[\"groupSeq\"]";
                }
                else
                {
                    sql += " ORDER BY c[\"_ts\"] DESC";
                }
            }

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(new QueryDefinition(sql)));
            List<dynamic> results = new List<dynamic>();

            while (response.HasMoreResults)
                results.AddRange(await response.ReadNextAsync());

            return results;
        }

        public virtual async Task<dynamic> GetDataByParam(Dictionary<string, object> param)
        {
            var containerName = _container.Id;

            var listFields = JsonConvert.DeserializeObject<string[]>(JsonConvert.SerializeObject(param.Where(x => x.Key == EnumQuery.Fields)?.FirstOrDefault().Value));
            string fieldsQuery = listFields == null ? "*" : $"c{string.Join(", c", listFields.ToList().Select(x => $"[\"{x}\"]"))}";

            string sql = $"SELECT TOP 1 {fieldsQuery} FROM c ";
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
                        sbParameters.Append("c" + $"[\"{property.Key}\"]" + " IS NULL AND ");
                    }
                    else
                    {
                        if (containerName == EnumContainer.ServiceSheetHeader)
                        {
                            //sbParameters.Append("ToString(c." + $"{property.Key}" + ") = \"" + property.Value + "\" AND ");
                            sbParameters.Append("c." + $"{property.Key}" + " = \"" + property.Value + "\" AND ");
                            if (args != null)
                            {
                                args[i] = property.Value;
                            }
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
                        }

                        i++;
                    }
                }

                string parameters = sbParameters.ToString();
                parameters = parameters.Substring(0, parameters.Length - 5);

                //sql += " WHERE " + parameters + $" AND ToString(c[\"{EnumCommonProperty.IsDeleted}\"]) = \"false\"";
                //sql += " WHERE " + parameters + $" AND c[\"{EnumCommonProperty.IsDeleted}\"] = \"false\"";

                sql += " WHERE " + parameters + $" AND c[\"{EnumCommonProperty.IsDeleted}\"] = \"false\"";

                if (containerName == EnumContainer.DefectHeader)
                {
                    sql += " ORDER BY c[\"taskNo\"]";
                }
                else
                {
                    sql += " ORDER BY c[\"_ts\"] DESC";
                }
            }
            else
            {
                //sql += $" WHERE ToString(c[\"{EnumCommonProperty.IsDeleted}\"]) = \"false\"";
                sql += $" WHERE c[\"{EnumCommonProperty.IsDeleted}\"] = \"false\"";

                if (containerName == EnumContainer.DefectHeader)
                {
                    sql += " ORDER BY c[\"taskNo\"]";
                }
                else
                {
                    sql += " ORDER BY c[\"_ts\"] DESC";
                }
            }

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(new QueryDefinition(sql)));
            return response.ReadNextAsync().Result.FirstOrDefault();
        }

        public virtual async Task<dynamic> GetDataListByParam(List<FilterHelperModel> param)
        {
            var containerName = _container.Id;

            var listFields = JsonConvert.DeserializeObject<string[]>(JsonConvert.SerializeObject(param.Where(x => x.field == EnumQuery.Fields)?.FirstOrDefault().value));
            string fieldsQuery = listFields == null ? "*" : $"c{string.Join(", c", listFields.ToList().Select(x => $"[\"{x}\"]"))}";

            string sql = $"SELECT {fieldsQuery} FROM c ";
            object[] args = null;

            param = param.Where(x => x.field != EnumQuery.Fields).ToList();

            if (param.Count > 0)
            {
                StringBuilder sbParameters = new StringBuilder();
                int i = 0;
                int countValues = param.Where(x => x.value != null).Count();

                if (countValues > 0) args = new object[countValues];

                foreach (var property in param)
                {
                    if (property.value == null)
                    {
                        sbParameters.Append("c" + $"[\"{property.field}\"]" + " IS NULL AND ");
                    }
                    else
                    {
                        if (property.value.GetType().Name == "JArray")
                        {
                            var arrPropValue = JsonConvert.DeserializeObject<string[]>(JsonConvert.SerializeObject(property.value));
                            string propValue = $"(\"{string.Join("\", \"", arrPropValue)}\")";
                            //sbParameters.Append("ToString(c" + $"[\"{property.field}\"]" + ") IN " + propValue + " AND ");
                            sbParameters.Append("c" + $"[\"{property.field}\"]" + " IN " + propValue + " AND ");
                        }
                        else
                        {
                            //sbParameters.Append("ToString(c" + $"[\"{property.field}\"]" + ") " + property.opr + " \"" + property.value + "\" AND ");
                            sbParameters.Append("c" + $"[\"{property.field}\"]" + " " + property.opr + " \"" + property.value + "\" AND ");
                        }

                        if (args != null)
                        {
                            args[i] = property.value;
                        }
                        i++;
                    }
                }

                string parameters = sbParameters.ToString();
                parameters = parameters.Substring(0, parameters.Length - 5);

                //sql += " WHERE " + parameters + $" AND ToString(c[\"{EnumCommonProperty.IsActive}\"]) = \"true\" AND ToString(c[\"{EnumCommonProperty.IsDeleted}\"]) = \"false\"";
                sql += " WHERE " + parameters + $" AND c[\"{EnumCommonProperty.IsActive}\"] = \"true\" AND c[\"{EnumCommonProperty.IsDeleted}\"] = \"false\"";

                if (containerName == EnumContainer.DefectHeader)
                {
                    sql += " ORDER BY c[\"taskNo\"]";
                }
                else if (containerName == EnumContainer.MasterServiceSheet)
                {
                    sql += " ORDER BY c[\"groupSeq\"]";
                }
                else
                {
                    sql += " ORDER BY c[\"_ts\"] DESC";
                }
            }
            else
            {
                //sql += $" WHERE ToString(c[\"{EnumCommonProperty.IsDeleted}\"]) = \"false\" AND ToString(c[\"{EnumCommonProperty.IsActive}\"]) = \"true\"";
                sql += $" WHERE c[\"{EnumCommonProperty.IsDeleted}\"] = \"false\" AND c[\"{EnumCommonProperty.IsActive}\"]) = \"true\"";

                if (containerName == EnumContainer.DefectHeader)
                {
                    sql += " ORDER BY c[\"taskNo\"]";
                }
                else if (containerName == EnumContainer.MasterServiceSheet)
                {
                    sql += " ORDER BY c[\"groupSeq\"]";
                }
                else
                {
                    sql += " ORDER BY c[\"_ts\"] DESC";
                }
            }

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(new QueryDefinition(sql)));
            List<dynamic> results = new List<dynamic>();

            while (response.HasMoreResults)
                results.AddRange(await response.ReadNextAsync());

            return results;
        }

        public virtual async Task<dynamic> GetDataListByParam(Dictionary<string, object> param, int limit, string orderBy, string orderType)
        {
            var containerName = _container.Id;
            string limitQuery = limit == 0 ? string.Empty : $"TOP {limit}";

            var listFields = JsonConvert.DeserializeObject<string[]>(JsonConvert.SerializeObject(param.Where(x => x.Key == EnumQuery.Fields)?.FirstOrDefault().Value));
            string fieldsQuery = listFields == null ? "*" : $"c{string.Join(", c", listFields.ToList().Select(x => $"[\"{x}\"]"))}";

            string sql = $"SELECT {limitQuery} {fieldsQuery} FROM c ";
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
                        sbParameters.Append("c" + $"[\"{property.Key}\"]" + " IS NULL AND ");
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

                //sql += " WHERE " + parameters + $" AND ToString(c[\"{EnumCommonProperty.IsDeleted}\"]) = \"false\"";
                sql += " WHERE " + parameters + $" AND c[\"{EnumCommonProperty.IsDeleted}\"] = \"false\"";
            }
            else
            {
                //sql += $" WHERE ToString(c[\"{EnumCommonProperty.IsDeleted}\"]) = \"false\"";
                sql += $" WHERE c[\"{EnumCommonProperty.IsDeleted}\"] = \"false\"";
            }

            if (!string.IsNullOrEmpty(orderBy))
                sql += $" ORDER BY c[\"{orderBy}\"] {orderType}";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(new QueryDefinition(sql)));
            List<dynamic> results = new List<dynamic>();

            while (response.HasMoreResults)
                results.AddRange(await response.ReadNextAsync());

            return results;
        }

        public virtual async Task<dynamic> Create(CreateRequest createRequest)
        {
            string id = Guid.NewGuid().ToString();
            JObject newObj = new JObject();

            newObj.Add(EnumCommonProperty.ID, id);

            JToken newObjectEmployee = JToken.FromObject(createRequest.employee);

            foreach (var x in JObject.Parse(JsonConvert.SerializeObject(createRequest.entity)))
                newObj.Add(x);

            newObj.Add(EnumCommonProperty.IsActive, "true");
            newObj.Add(EnumCommonProperty.IsDeleted, "false");
            newObj.Add(EnumCommonProperty.CreatedBy, newObjectEmployee);
            newObj.Add(EnumCommonProperty.CreatedDate, GetSettingValue(EnumCommonProperty.ServerDateTime));
            newObj.Add(EnumCommonProperty.UpdatedBy, string.Empty);
            newObj.Add(EnumCommonProperty.UpdatedDate, string.Empty);

            var data = await _container.CreateItemAsync(newObj);
            return data.Resource;
        }

        public virtual async Task<dynamic> Create(CreateRequest createRequest, Dictionary<string, string> adjustFields)
        {
            JObject newObj = new JObject();

            JToken newObjectEmployee = JToken.FromObject(createRequest.employee);

            foreach (var x in JObject.Parse(JsonConvert.SerializeObject(createRequest.entity)))
                newObj.Add(x);

            foreach (var adjustField in adjustFields)
                newObj.Add(adjustField.Key, adjustField.Value);

            if (!newObj.ContainsKey(EnumCommonProperty.ID))
            {
                newObj.Add(EnumCommonProperty.ID, Guid.NewGuid().ToString());
            }
            else
            {
                if (newObj[EnumQuery.ID] == null || newObj[EnumQuery.ID].Type == JTokenType.Null)
                {
                    newObj[EnumQuery.ID] = Guid.NewGuid().ToString();
                }
            }

            newObj.Add(EnumCommonProperty.IsActive, "true");
            newObj.Add(EnumCommonProperty.IsDeleted, "false");
            newObj.Add(EnumCommonProperty.CreatedBy, newObjectEmployee);
            newObj.Add(EnumCommonProperty.CreatedDate, GetSettingValue(EnumCommonProperty.ServerDateTime));
            newObj.Add(EnumCommonProperty.UpdatedBy, string.Empty);
            newObj.Add(EnumCommonProperty.UpdatedDate, string.Empty);

            var data = await _container.CreateItemAsync(newObj);
            return data.Resource;
        }

        public virtual async Task<dynamic> Update(UpdateRequest updateRequest, dynamic rsc)
        {
            string updatedDate = GetSettingValue(EnumCommonProperty.ServerDateTime);
            string tsUpdatedDate = GetSettingValue(EnumCommonProperty.ServerTimeStamp);

            string jsonUpdateRequest = JsonConvert.SerializeObject(updateRequest);
            jsonUpdateRequest = jsonUpdateRequest.Replace(EnumCommonProperty.ServerDateTime, updatedDate).Replace(EnumCommonProperty.ServerTimeStamp, tsUpdatedDate);

            updateRequest = JsonConvert.DeserializeObject<UpdateRequest>(jsonUpdateRequest);

            List<PatchOperation> patchOperations = new List<PatchOperation>();
            string id = updateRequest.id;
            string partitionKey = string.Empty;

            if (_container.Id == EnumContainer.ServiceSheetDetail ||
                _container.Id == EnumContainer.InterimEngineDetail ||
                _container.Id == EnumContainer.CbmHistory)
            {
                partitionKey = updateRequest.workOrder;
            }
            else
            {
                partitionKey = updateRequest.id;
            }

            //var result = await Get(id);

            //if (result == null)
            //    throw new Exception("Data not found!");

            string json = JsonConvert.SerializeObject(rsc);
            JObject jObj = JObject.Parse(json);
            JToken token = JToken.Parse(json);

            foreach (var updateParam in updateRequest.updateParams)
            {
                //var putModels = token.SelectTokens($"$..{ EnumQuery.Key }")
                //.Select(x => new UpdateHelperModel() { ItemPath = x.Path, ParentPath = x.Parent.Parent.Path, ItemValue = jObj.SelectToken(x.Path).Value<string>() })
                //.ToList();

                //if (putModels == null || putModels.Count == 0)
                //    throw new Exception("Key not found!");

                //var parentPath = putModels.FirstOrDefault(x => x.ItemValue == updateParam.keyValue)?.ParentPath.Replace(".", "/").Replace("[", "/").Replace("]", string.Empty);

                var path = token.SelectTokens($"$..[?(@.{EnumQuery.Key}=='{updateParam.keyValue}')]")
                       .Select(x => x.Path)
                       .FirstOrDefault()?.Replace(".", "/").Replace("[", "/").Replace("]", string.Empty);

                var prefix = !string.IsNullOrEmpty(path) ? "/" : string.Empty;
                var itemPath = $"{prefix}{path}";

                foreach (var propertyParam in updateParam.propertyParams)
                {
                    #region move logic to services
                    //if ((_container.Id == EnumContainer.ServiceSheetHeader || _container.Id == EnumContainer.InterimEngineHeader) && propertyParam.propertyName == EnumQuery.ServicePersonnels)
                    //{
                    //    var paramServicePersonels = new Dictionary<string, object>();
                    //    paramServicePersonels.Add("id", updateRequest.id);

                    //    var oldDataPersonels = await GetDataByParam(paramServicePersonels);

                    //    List<ServicePersonnelsResponse> oldJsonPersonels = JsonConvert.DeserializeObject<List<ServicePersonnelsResponse>>(JsonConvert.SerializeObject(oldDataPersonels.servicePersonnels));

                    //    ServicePersonnelsResponse newJsonPersonel = JsonConvert.DeserializeObject<ServicePersonnelsResponse>(propertyParam.propertyValue);
                    //    List<ServicePersonnelsResponse> newJsonPersonels = new List<ServicePersonnelsResponse>();
                    //    newJsonPersonels.Add(newJsonPersonel);

                    //    #region get shift data
                    //    List<ShiftModel> shiftModel = new List<ShiftModel>();

                    //    for (int i = updateRequest.updateParams.Count - 1; i >= 0; i--) // to get shift data
                    //    {
                    //        if (updateRequest.updateParams[i].keyValue == EnumGroup.General && updateRequest.updateParams[i].propertyParams[0].propertyName == EnumQuery.Shift)
                    //        {
                    //            shiftModel = JsonConvert.DeserializeObject<List<ShiftModel>>(updateRequest.updateParams[i].propertyParams[0].propertyValue);
                    //        }
                    //    }
                    //    #endregion
                    //    string idPersonnel = "";

                    //    if (propertyParam.propertyValue == "") //submited form
                    //    {
                    //        foreach (var updateParamStatus in updateRequest.updateParams)
                    //        {
                    //            foreach (var propertyIdParamStatus in updateParamStatus.propertyParams)
                    //            {
                    //                if (propertyIdParamStatus.propertyName == EnumQuery.UpdatedBy)
                    //                {
                    //                    UpdatedByResponse personnel = JsonConvert.DeserializeObject<UpdatedByResponse>(propertyIdParamStatus.propertyValue);
                    //                    idPersonnel = personnel.id;
                    //                    break;
                    //                }
                    //            }
                    //        }
                    //    }

                    //    var newJsonPersonnels = CalculateServicePersonnel(oldJsonPersonels, newJsonPersonels, shiftModel, updateParam.propertyParams, idPersonnel);
                    //    propertyParam.propertyValue = JsonConvert.SerializeObject(newJsonPersonnels);
                    //}
                    //else if (_container.Id == EnumContainer.ServiceSheetHeader && updateParam.keyValue == EnumQuery.KeyValueRiskAssesment)
                    //{
                    //    var paramRiskAssesment = new Dictionary<string, object>();
                    //    paramRiskAssesment.Add("id", updateRequest.id);

                    //    var oldDataRiskAssesment = await GetDataByParam(paramRiskAssesment);

                    //    List<RiskAssesmentValue> oldJsonRiskAssesment = JsonConvert.DeserializeObject<List<RiskAssesmentValue>>(JsonConvert.SerializeObject(oldDataRiskAssesment.riskAssesment[0].value));

                    //    List<RiskAssesmentValue> newJsonRiskAssesment = JsonConvert.DeserializeObject<List<RiskAssesmentValue>>(propertyParam.propertyValue);

                    //    var newRiskAssesment = newJsonRiskAssesment.Where(x => !oldJsonRiskAssesment.Select(p => p.image).Contains(x.image)).ToList();
                    //    if (newRiskAssesment.Count > 0)
                    //    {
                    //        oldJsonRiskAssesment.AddRange(newRiskAssesment);
                    //    }

                    //    propertyParam.propertyValue = JsonConvert.SerializeObject(oldJsonRiskAssesment);

                    //}
                    //else if (_container.Id == EnumContainer.ServiceSheetHeader && propertyParam.propertyName == EnumQuery.Log)
                    //{
                    //    var paramLog = new Dictionary<string, object>();
                    //    paramLog.Add("id", updateRequest.id);

                    //    var oldDataLog = await GetDataByParam(paramLog);

                    //    List<LogResponse> oldJsonLog = JsonConvert.DeserializeObject<List<LogResponse>>(JsonConvert.SerializeObject(oldDataLog.log));

                    //    JToken type = JToken.Parse(propertyParam.propertyValue);
                    //    if (type.Type == JTokenType.Array)
                    //    {
                    //        List<LogResponse> newJsonLog = JsonConvert.DeserializeObject<List<LogResponse>>(propertyParam.propertyValue);

                    //        var newLog = newJsonLog.Where(x => !oldJsonLog.Select(p => p.id).Contains(x.id)).ToList();
                    //        if (newLog.Count > 0)
                    //        {
                    //            oldJsonLog.AddRange(newLog);
                    //        }
                    //    }
                    //    else
                    //    {
                    //        LogResponse newJsonLog = JsonConvert.DeserializeObject<LogResponse>(propertyParam.propertyValue);

                    //        oldJsonLog.Add(newJsonLog);
                    //    }

                    //    propertyParam.propertyValue = JsonConvert.SerializeObject(oldJsonLog);
                    //}
                    //else if (_container.Id == EnumContainer.ServiceSheetHeader && updateParam.keyValue == EnumGroup.General && propertyParam.propertyName == EnumQuery.Status)
                    //{
                    //    if (propertyParam.propertyValue == EnumStatus.EFormApprovedSPV)
                    //    {
                    //        propertyParam.propertyValue = EnumStatus.EFormClosed;
                    //    }
                    //}

                    //if (propertyParam.propertyName == EnumQuery.Shift) //dont add shift data to db
                    //{
                    //    continue;
                    //}
                    #endregion

                    propertyParam.propertyValue = GetSettingValue(propertyParam.propertyValue);

                    try
                    {
                        var tokenValue = JToken.Parse(propertyParam.propertyValue);

                        if (tokenValue is JObject || tokenValue is JArray)
                            patchOperations.Add(PatchOperation.Set($"{itemPath}/{propertyParam.propertyName}", tokenValue));
                        else
                            patchOperations.Add(PatchOperation.Set($"{itemPath}/{propertyParam.propertyName}", propertyParam.propertyValue));
                    }
                    catch (Exception)
                    {
                        patchOperations.Add(PatchOperation.Set($"{itemPath}/{propertyParam.propertyName}", propertyParam.propertyValue));
                    }
                }
            }

            JToken newObjectEmployee = JToken.FromObject(updateRequest.employee);

            patchOperations.Add(PatchOperation.Set($"/{EnumCommonProperty.UpdatedBy}", newObjectEmployee));
            patchOperations.Add(PatchOperation.Set($"/{EnumCommonProperty.UpdatedDate}", updatedDate));

            for (int i = 0; i < patchOperations.Count; i = i + 10)
            {
                ItemResponse<dynamic> item = await _container.PatchItemAsync<dynamic>(id, new PartitionKey(partitionKey), patchOperations.Skip(i).Take(10).ToList());
            }

            if (_container.Id == EnumContainer.ServiceSheetHeader)
            {
                List<UpdateParam> resultCheckBeforTruck = new List<UpdateParam>();

                var paramCheckBeforeTruck = new Dictionary<string, object>();
                paramCheckBeforeTruck.Add("id", updateRequest.id);

                var oldDataCheckBeforTruck = await GetDataByParam(paramCheckBeforeTruck);

                List<dynamic> oldCheckBeforeTruck = JsonConvert.DeserializeObject<List<dynamic>>(JsonConvert.SerializeObject(oldDataCheckBeforTruck.checkBeforeTruck.items));
                foreach (var itemBeforTruck in oldCheckBeforeTruck)
                {
                    var resulTruck = new Dictionary<string, object>();
                    resulTruck.Add("keyValue", itemBeforTruck.key);

                    var resultItemTruck = new List<Dictionary<string, object>>();

                    var valueResult = new Dictionary<string, object>();
                    valueResult.Add("propertyName", "value");
                    valueResult.Add("propertyValue", itemBeforTruck.value);

                    var updateDateResult = new Dictionary<string, object>();
                    updateDateResult.Add("propertyName", "updatedDate");
                    updateDateResult.Add("propertyValue", itemBeforTruck.updatedDate);

                    var updatedByResult = new Dictionary<string, object>();
                    updatedByResult.Add("propertyName", "updatedBy");
                    updatedByResult.Add("propertyValue", JsonConvert.SerializeObject(itemBeforTruck.updatedBy));

                    resultItemTruck.Add(valueResult);
                    resultItemTruck.Add(updateDateResult);
                    resultItemTruck.Add(updatedByResult);

                    resulTruck.Add("propertyParams", resultItemTruck);

                    UpdateParam resultParam = JsonConvert.DeserializeObject<UpdateParam>(JsonConvert.SerializeObject(resulTruck));

                    resultCheckBeforTruck.Add(resultParam);
                }

                UpdateHeaderResponse updateResponse = new UpdateHeaderResponse()
                {
                    id = updateRequest.id,
                    updateParams = updateRequest.updateParams,
                    employee = updateRequest.employee,
                    updatedDate = updatedDate,
                    checkBeforeTruck = resultCheckBeforTruck
                };

                return updateResponse;
            }
            else
            {
                UpdateResponse updateResponse = new UpdateResponse()
                {
                    id = updateRequest.id,
                    updateParams = updateRequest.updateParams,
                    employee = updateRequest.employee,
                    updatedDate = updatedDate
                };

                return updateResponse;
            }
        }

        public virtual async Task<dynamic> Delete(DeleteRequest deleteRequest)
        {
            JToken newObjectEmployee = JToken.FromObject(deleteRequest.employee);

            List<PatchOperation> patchOperations = new List<PatchOperation>();
            {
                patchOperations.Add(PatchOperation.Set($"/{EnumCommonProperty.IsDeleted}", "true"));
                patchOperations.Add(PatchOperation.Set($"/{EnumCommonProperty.UpdatedBy}", newObjectEmployee));
                patchOperations.Add(PatchOperation.Set($"/{EnumCommonProperty.UpdatedDate}", GetSettingValue(EnumCommonProperty.ServerDateTime)));
            }

            deleteRequest.partitionKey = string.IsNullOrEmpty(deleteRequest.partitionKey) ? deleteRequest.id : deleteRequest.partitionKey;

            ItemResponse<dynamic> item = await _container.PatchItemAsync<dynamic>(deleteRequest.id, new PartitionKey(deleteRequest.partitionKey), patchOperations);

            return deleteRequest;
        }

        public virtual async Task<dynamic> HardDelete(DeleteRequest deleteRequest)
        {
            ItemResponse<dynamic> item = await _container.DeleteItemAsync<dynamic>(deleteRequest.id, new PartitionKey(deleteRequest.partitionKey));
            return item;
        }

        public virtual async Task<dynamic> DeleteByParam(DeleteByParamRequest deleteByParamRequest)
        {
            var deleteList = await GetDataListByParam(deleteByParamRequest.deleteParams);

            foreach (var deleteData in deleteList)
            {
                DeleteRequest deleteRequest = new DeleteRequest()
                {
                    id = deleteData.id,
                    employee = deleteByParamRequest.employee
                };

                if (_container.Id == EnumContainer.ServiceSheetDetail ||
                _container.Id == EnumContainer.InterimEngineDetail ||
                _container.Id == EnumContainer.CbmHistory)
                {
                    deleteRequest.partitionKey = deleteData.workOrder;
                }
                else
                {
                    deleteRequest.partitionKey = deleteRequest.id;
                }

                await Delete(deleteRequest);
            }

            return deleteByParamRequest;
        }

        public virtual async Task<dynamic> HardDeleteByParam(DeleteByParamRequest deleteByParamRequest)
        {
            var deleteList = await GetDataListByParam(deleteByParamRequest.deleteParams, true);

            foreach (var deleteData in deleteList)
            {
                DeleteRequest deleteRequest = new DeleteRequest()
                {
                    id = deleteData.id,
                    employee = deleteByParamRequest.employee
                };

                if (_container.Id == EnumContainer.ServiceSheetDetail ||
                _container.Id == EnumContainer.InterimEngineDetail ||
                _container.Id == EnumContainer.CbmHistory)
                {
                    deleteRequest.partitionKey = deleteData.workOrder;
                }
                else
                {
                    deleteRequest.partitionKey = deleteRequest.id;
                }

                await HardDelete(deleteRequest);
            }

            return deleteByParamRequest;
        }

        public virtual async Task<dynamic> GetFieldValue(GetFieldValueRequest getFieldValueRequest, bool isAllowNoField = false)
        {
            dynamic result = null;
            var data = await Get(getFieldValueRequest.id);

            if (data == null)
                throw new Exception("Data not found!");

            string json = JsonConvert.SerializeObject(data);
            JObject jObj = JObject.Parse(json);
            JToken token = JToken.Parse(json);

            var searchModels = token.SelectTokens($"$..{EnumQuery.Key}")
                .Select(x => new UpdateHelperModel() { ItemPath = x.Path, ParentPath = x.Parent.Parent.Path, ItemValue = jObj.SelectToken(x.Path).Value<string>() })
                .ToList();

            if (searchModels == null || searchModels.Count == 0)
                throw new Exception($"Field {EnumQuery.Key} not found!");

            var parentPath = searchModels.FirstOrDefault(x => x.ItemValue == getFieldValueRequest.keyValue)?.ParentPath;
            parentPath = !string.IsNullOrEmpty(parentPath) ? $"{parentPath}[\"" : string.Empty;
            string closeTag = !string.IsNullOrEmpty(parentPath) ? "\"]" : string.Empty;

            //var query = $"select c.{parentPath}{getFieldValueRequest.propertyName}{closeTag} from c where ToString(c.{EnumCommonProperty.ID}) = \"{getFieldValueRequest.id}\" and ToString(c.{EnumCommonProperty.IsDeleted}) = \"false\"";
            var query = $"select c.{parentPath}{getFieldValueRequest.propertyName}{closeTag} from c where c.{EnumCommonProperty.ID} = \"{getFieldValueRequest.id}\" and c.{EnumCommonProperty.IsDeleted} = \"false\"";
            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));
            data = response.ReadNextAsync().Result.FirstOrDefault();

            json = JsonConvert.SerializeObject(data);
            token = JToken.Parse(json);

            if (!token.HasValues && !isAllowNoField)
                throw new Exception($"Field {getFieldValueRequest.propertyName} with {EnumQuery.Key} {getFieldValueRequest.keyValue} not found!");
            else
                result = token.Value<dynamic>(getFieldValueRequest.propertyName);

            return result;
        }

        public virtual async Task<dynamic> GetFieldValueList(GetFieldValueListRequest getFieldValueRequest, bool isAllowNoField = false)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            string columnName = string.Empty;

            var data = await Get(getFieldValueRequest.id);

            if (data == null)
                throw new Exception("Data not found!");

            string json = JsonConvert.SerializeObject(data);
            JObject jObj = JObject.Parse(json);
            JToken token = JToken.Parse(json);

            var searchModels = token.SelectTokens($"$..{EnumQuery.Key}")
                .Select(x => new UpdateHelperModel() { ItemPath = x.Path, ParentPath = x.Parent.Parent.Path, ItemValue = jObj.SelectToken(x.Path).Value<string>() })
                .ToList();

            if (searchModels == null || searchModels.Count == 0)
                throw new Exception($"Field {EnumQuery.Key} not found!");

            var parentPath = searchModels.FirstOrDefault(x => x.ItemValue == getFieldValueRequest.keyValue)?.ParentPath;
            parentPath = !string.IsNullOrEmpty(parentPath) ? $"{parentPath}[\"" : string.Empty;
            string closeTag = !string.IsNullOrEmpty(parentPath) ? "\"]" : string.Empty;

            foreach (var itemField in getFieldValueRequest.propertyName)
            {
                columnName += $"c.{parentPath}{itemField}{closeTag},";
            }

            columnName = columnName.Substring(0, columnName.Length - 1);

            //var query = $"select {columnName} from c where ToString(c.{EnumCommonProperty.ID}) = \"{getFieldValueRequest.id}\" and ToString(c.{EnumCommonProperty.IsDeleted}) = \"false\"";
            var query = $"select {columnName} from c where c.{EnumCommonProperty.ID} = \"{getFieldValueRequest.id}\" and c.{EnumCommonProperty.IsDeleted} = \"false\"";
            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));
            data = response.ReadNextAsync().Result.FirstOrDefault();

            json = JsonConvert.SerializeObject(data);
            token = JToken.Parse(json);

            foreach (var itemFieldResult in getFieldValueRequest.propertyName)
            {
                if (!token.HasValues && !isAllowNoField)
                    throw new Exception($"Field {itemFieldResult} with {EnumQuery.Key} {getFieldValueRequest.keyValue} not found!");
                else
                    result.Add(itemFieldResult, token.Value<dynamic>(itemFieldResult));
            }

            return result;
        }

        public async Task<dynamic> CreateList(CreateRequest createRequest)
        {
            string id = Guid.NewGuid().ToString();
            JObject newObj = new JObject();

            newObj.Add(EnumCommonProperty.ID, id);

            JToken newObjectEmployee = JToken.FromObject(createRequest.employee);

            foreach (var dataItem in createRequest.entity)
            {
                foreach (var x in JObject.Parse(JsonConvert.SerializeObject(dataItem)))
                    newObj.Add(x);
            }

            newObj.Add(EnumCommonProperty.IsActive, "true");
            newObj.Add(EnumCommonProperty.IsDeleted, "false");
            newObj.Add(EnumCommonProperty.CreatedBy, newObjectEmployee);
            newObj.Add(EnumCommonProperty.CreatedDate, GetSettingValue(EnumCommonProperty.ServerDateTime));
            newObj.Add(EnumCommonProperty.UpdatedBy, string.Empty);
            newObj.Add(EnumCommonProperty.UpdatedDate, string.Empty);

            var data = await _container.CreateItemAsync(newObj);
            return data.Resource;
        }

        public virtual async Task<JArray> GetActiveDataJArray()
        {
            var containerName = _container.Id;
            //string query = $"select * from c where ToString(c.{EnumCommonProperty.IsActive}) = \"true\" and ToString(c.{EnumCommonProperty.IsDeleted}) = \"false\"";
            string query = $"select * from c where c.{EnumCommonProperty.IsActive} = \"true\" and c.{EnumCommonProperty.IsDeleted} = \"false\"";

            if (containerName == EnumContainer.DefectHeader)
            {
                query += " ORDER BY c[\"taskNo\"]";
            }
            else if (containerName == EnumContainer.MasterServiceSheet)
            {
                query += " ORDER BY c[\"groupSeq\"]";
            }
            else
            {
                query += " ORDER BY c[\"_ts\"] DESC";
            }

            var response = await Task.Run(() => _container.GetItemQueryIterator<JObject>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }

            return results;
        }

        public virtual async Task<JArray> GetDataListByParamJArray(Dictionary<string, object> param)
        {
            var containerName = _container.Id;

            var listFields = JsonConvert.DeserializeObject<string[]>(JsonConvert.SerializeObject(param.Where(x => x.Key == EnumQuery.Fields)?.FirstOrDefault().Value));
            string fieldsQuery = listFields == null ? "*" : $"c{string.Join(", c", listFields.ToList().Select(x => $"[\"{x}\"]"))}";

            string sql = $"SELECT {fieldsQuery} FROM c ";
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
                        sbParameters.Append("c" + $"[\"{property.Key}\"]" + " IS NULL AND ");
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

                //sql += " WHERE " + parameters + $" AND ToString(c[\"{EnumCommonProperty.IsActive}\"]) = \"true\" AND ToString(c[\"{EnumCommonProperty.IsDeleted}\"]) = \"false\"";
                sql += " WHERE " + parameters + $" AND c[\"{EnumCommonProperty.IsActive}\"] = \"true\" AND c[\"{EnumCommonProperty.IsDeleted}\"] = \"false\"";

                if (containerName == EnumContainer.DefectHeader)
                {
                    sql += " ORDER BY c[\"taskNo\"]";
                }
                else if (containerName == EnumContainer.MasterServiceSheet)
                {
                    sql += " ORDER BY c[\"groupSeq\"]";
                }
                else
                {
                    sql += " ORDER BY c[\"_ts\"] DESC";
                }
            }
            else
            {
                //sql += $" WHERE ToString(c[\"{EnumCommonProperty.IsDeleted}\"]) = \"false\" AND ToString(c[\"{EnumCommonProperty.IsActive}\"]) = \"true\"";
                sql += $" WHERE c[\"{EnumCommonProperty.IsDeleted}\"] = \"false\" AND c[\"{EnumCommonProperty.IsActive}\"] = \"true\"";

                if (containerName == EnumContainer.DefectHeader)
                {
                    sql += " ORDER BY c[\"taskNo\"]";
                }
                else if (containerName == EnumContainer.MasterServiceSheet)
                {
                    sql += " ORDER BY c[\"groupSeq\"]";
                }
                else
                {
                    sql += " ORDER BY c[\"_ts\"] DESC";
                }
            }

            var response = await Task.Run(() => _container.GetItemQueryIterator<JObject>(sql));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }

            return results;
        }

        #region Setting

        public string GetSettingValue(string settingCode)
        {
            string result = settingCode;

            MasterSettingRepository _settingRepo = new MasterSettingRepository(_connectionFactory, EnumContainer.MasterSetting);
            var dbSettings = _settingRepo.GetAllData().Result;
            List<DBSetting> dBSettings = JsonConvert.DeserializeObject<List<DBSetting>>(JsonConvert.SerializeObject(dbSettings));

            EnumCommonProperty.appTimeZone = dBSettings.Where(x => x.key == EnumQuery.TimeZone).FirstOrDefault()?.value;
            EnumFormatting.appTimeZoneDesc = dBSettings.Where(x => x.key == EnumQuery.TimeZoneDesc).FirstOrDefault()?.value;

            if (settingCode == EnumCommonProperty.ServerDateTime)
                result = EnumCommonProperty.CurrentDateTime.ToString(EnumFormatting.DateTimeToString);
            else if (settingCode == EnumCommonProperty.ServerTimeStamp)
                result = EnumCommonProperty.CurrentTimeStamp.ToString();

            return result;
        }

        #endregion

        //private List<ServicePersonnelsResponse> CalculateServicePersonnel(List<ServicePersonnelsResponse> oldJsonPersonels, List<ServicePersonnelsResponse> newJsonPersonels, List<ShiftModel> shiftModel, List<PropertyParam> propertyParam, string idPersonnel = "")
        //{
        //    if (!string.IsNullOrWhiteSpace(idPersonnel)) // final submit form
        //    {
        //        UpdatedByResponse personel = new UpdatedByResponse();
        //        personel.id = idPersonnel;

        //        DateTime serverTime = EnumCommonProperty.CurrentDateTime;
        //        string newEndTime = serverTime.ToString(EnumFormatting.DateTimeToString);
        //        foreach (var propertyParamUpdate in propertyParam)
        //        {
        //            if (propertyParamUpdate.propertyName == EnumQuery.UpdatedBy)
        //            {
        //                personel = JsonConvert.DeserializeObject<UpdatedByResponse>(propertyParamUpdate.propertyValue);
        //            }
        //            else if(propertyParamUpdate.propertyName == EnumQuery.UpdatedDate)
        //            {
        //                newEndTime = propertyParamUpdate.propertyValue;
        //            }
        //        }
        //        var lastDataHistory = oldJsonPersonels.Where(x => x.mechanic.id == personel.id).LastOrDefault();
        //        var lastStartTimeHistory = lastDataHistory.serviceStart;
        //        var lastStartService = DateTime.ParseExact(lastStartTimeHistory, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture);
        //        var newStartService = DateTime.ParseExact(newEndTime, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture);

        //        var startHourDay = lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.startHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.startHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
        //        var endHourDay = lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.endHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.endHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
        //        var startHourNight = lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.startHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.startHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
        //        var endHourNight = (lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.endHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.endHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay).AddDays(1);

        //        bool isChangeOfDayEndShift = true;
        //        bool isChangeOfDayStartNextShift = true;
        //        var endShiftService = endHourNight;
        //        var startNextShiftService = startHourNight;

        //        if (lastStartService >= startHourDay && lastStartService <= endHourDay)
        //        {
        //            endShiftService = endHourDay;
        //            isChangeOfDayEndShift = false;
        //        }

        //        startHourDay = newStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.startHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.startHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
        //        endHourDay = newStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.endHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.endHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
        //        startHourNight = newStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.startHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.startHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
        //        endHourNight = (newStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.endHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.endHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay).AddDays(1);

        //        if (newStartService >= startHourDay && newStartService <= endHourDay)
        //        {
        //            startNextShiftService = startHourDay;
        //            isChangeOfDayStartNextShift = false;
        //        }
        //        DateTime dateEndShiftService = new DateTime();
        //        DateTime dateStartShiftService = new DateTime();

        //        if (isChangeOfDayEndShift)
        //        {
        //            dateEndShiftService = lastStartService.Date.AddHours(endShiftService.Hour).AddMinutes(endShiftService.Minute).AddSeconds(endShiftService.Second).AddDays(1);
        //        }
        //        else
        //        {
        //            dateEndShiftService = lastStartService.Date.AddHours(endShiftService.Hour).AddMinutes(endShiftService.Minute).AddSeconds(endShiftService.Second);
        //        }

        //        if (isChangeOfDayStartNextShift)
        //        {
        //            if(newStartService.Hour < startHourDay.Hour)
        //            {
        //                dateStartShiftService = newStartService.Date.AddHours(startNextShiftService.Hour).AddMinutes(startNextShiftService.Minute).AddSeconds(startNextShiftService.Second).AddDays(1);
        //            }
        //            else
        //            {
        //                dateStartShiftService = newStartService.Date.AddHours(startNextShiftService.Hour).AddMinutes(startNextShiftService.Minute).AddSeconds(startNextShiftService.Second);
        //            }
        //        }
        //        else
        //        {
        //            dateStartShiftService = newStartService.Date.AddHours(startNextShiftService.Hour).AddMinutes(startNextShiftService.Minute).AddSeconds(startNextShiftService.Second);
        //        }

        //        if (newStartService <= dateEndShiftService)
        //        {
        //            oldJsonPersonels.ForEach(c =>
        //            {
        //                if (string.IsNullOrWhiteSpace(c.serviceEnd))
        //                {
        //                    c.serviceEnd = newEndTime;
        //                }
        //            });
        //        }
        //        else
        //        {
        //            oldJsonPersonels.ForEach(c =>
        //            {
        //                if (string.IsNullOrWhiteSpace(c.serviceEnd))
        //                {
        //                    c.serviceEnd = dateEndShiftService.ToString(EnumFormatting.DateTimeToString);
        //                }
        //            });
        //            var newShiftData = "";
        //            if(isChangeOfDayStartNextShift)
        //            {
        //                newShiftData = EnumShiftValue.Night + " " + "Shift";
        //            }
        //            else
        //            {
        //                newShiftData = EnumShiftValue.Day + " " + "Shift";
        //            }
        //            ServicePersonnelsResponse generatedData = new ServicePersonnelsResponse()
        //            {
        //                key = Guid.NewGuid().ToString(),
        //                serviceStart = dateStartShiftService.ToString(EnumFormatting.DateTimeToString),
        //                serviceEnd = newEndTime,
        //                shift = newShiftData,
        //                mechanic = new EmployeeModel()
        //                {
        //                    id = lastDataHistory.mechanic.id,
        //                    name = lastDataHistory.mechanic.name
        //                }
        //            };
        //            oldJsonPersonels.Add(generatedData);
        //        }
        //    }
        //    else if (string.IsNullOrWhiteSpace(newJsonPersonels[0].serviceEnd)) // general submit
        //    {
        //        if (oldJsonPersonels.Where(x => x.mechanic.id == newJsonPersonels.FirstOrDefault().mechanic.id).FirstOrDefault() == null) // no history for same user
        //        {
        //            oldJsonPersonels.AddRange(newJsonPersonels);
        //        }
        //        else // there is record history for same user
        //        {
        //            var lastDataHistory = oldJsonPersonels.Where(x => x.mechanic.id == newJsonPersonels.FirstOrDefault().mechanic.id).LastOrDefault();
        //            var lastEndTimeHistory = lastDataHistory.serviceEnd;
        //            var lastStartTimeHistory = lastDataHistory.serviceStart;
        //            if (!string.IsNullOrWhiteSpace(lastEndTimeHistory)) //endtime last history exists
        //            {
        //                #region logic check endshift 
        //                bool isChangeOfDay = false;
        //                var lastStartService = DateTime.ParseExact(lastStartTimeHistory, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture);

        //                var startHourDay = lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.startHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.startHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
        //                var endHourDay = lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.endHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.endHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
        //                var startHourNight = lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.startHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.startHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
        //                var endHourNight = (lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.endHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.endHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay).AddDays(1);
        //                var endShiftService = endHourDay;
        //                if (lastStartService >= startHourNight && lastStartService <= endHourNight)
        //                {
        //                    endShiftService = endHourNight;
        //                    isChangeOfDay = true;
        //                }
        //                DateTime dateEndShiftService;
        //                if (isChangeOfDay)
        //                {
        //                    dateEndShiftService = lastStartService.Date.AddHours(endShiftService.Hour).AddMinutes(endShiftService.Minute).AddSeconds(endShiftService.Second).AddDays(1);
        //                }
        //                else
        //                {
        //                    dateEndShiftService = lastStartService.Date.AddHours(endShiftService.Hour).AddMinutes(endShiftService.Minute).AddSeconds(endShiftService.Second);
        //                }

        //                #endregion

        //                var newStartTime = newJsonPersonels.FirstOrDefault().serviceStart;
        //                if ((DateTime.ParseExact(newStartTime, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture) - DateTime.ParseExact(lastEndTimeHistory, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture)).TotalHours <= 3 
        //                    && (DateTime.ParseExact(newStartTime, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture)) <= dateEndShiftService)
        //                {
        //                    oldJsonPersonels.ForEach(c =>
        //                    {
        //                        if (c.key == lastDataHistory.key)
        //                        {
        //                            c.serviceEnd = "";
        //                        }
        //                    });
        //                }
        //                else
        //                {
        //                    oldJsonPersonels.AddRange(newJsonPersonels);
        //                }
        //            }
        //            else
        //            {
        //                var newStartTime = newJsonPersonels.FirstOrDefault().serviceStart;

        //                #region logic check endshift 
        //                bool isChangeOfDay = false;
        //                var lastStartService = DateTime.ParseExact(lastStartTimeHistory, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture);

        //                var startHourDay = lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.startHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.startHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
        //                var endHourDay = lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.endHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.endHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
        //                var startHourNight = lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.startHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.startHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
        //                var endHourNight = (lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.endHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.endHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay).AddDays(1);
        //                var endShiftService = endHourDay;
        //                if (lastStartService >= startHourNight && lastStartService <= endHourNight)
        //                {
        //                    endShiftService = endHourNight;
        //                    isChangeOfDay = true;
        //                }
        //                DateTime dateEndShiftService;
        //                if (isChangeOfDay)
        //                {
        //                    dateEndShiftService = lastStartService.Date.AddHours(endShiftService.Hour).AddMinutes(endShiftService.Minute).AddSeconds(endShiftService.Second).AddDays(1);
        //                }
        //                else
        //                {
        //                    dateEndShiftService = lastStartService.Date.AddHours(endShiftService.Hour).AddMinutes(endShiftService.Minute).AddSeconds(endShiftService.Second);
        //                }

        //                #endregion
        //                if ((!string.IsNullOrWhiteSpace(lastStartTimeHistory) && ((DateTime.ParseExact(newStartTime, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture) - DateTime.ParseExact(lastStartTimeHistory, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture)).TotalHours > 3)) 
        //                    || DateTime.ParseExact(newStartTime, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture) > dateEndShiftService)
        //                {
        //                    if ((DateTime.ParseExact(newStartTime, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture) > dateEndShiftService))
        //                    {
        //                        oldJsonPersonels.ForEach(c =>
        //                        {
        //                            if (c.key == lastDataHistory.key)
        //                            {
        //                                c.serviceEnd = dateEndShiftService.ToString(EnumFormatting.DateTimeToString);
        //                            }
        //                        });
        //                    }
        //                    else
        //                    {
        //                        oldJsonPersonels.ForEach(c =>
        //                        {
        //                            if (c.key == lastDataHistory.key)
        //                            {
        //                                c.serviceEnd = newJsonPersonels.FirstOrDefault().serviceStart;
        //                            }
        //                        });
        //                    }
        //                    oldJsonPersonels.AddRange(newJsonPersonels);
        //                }
        //            }
        //        }
        //    }
        //    else //finish button
        //    {
        //        var lastDataHistory = oldJsonPersonels.Where(x => x.mechanic.id == newJsonPersonels.FirstOrDefault().mechanic.id).LastOrDefault();
        //        var newEndTime = newJsonPersonels.FirstOrDefault().serviceEnd;
        //        //var newStartTime = newJsonPersonels.FirstOrDefault().serviceStart;
        //        UpdatedByResponse personel = new UpdatedByResponse();
        //        foreach (var propertyParamUpdate in propertyParam)
        //        {
        //            if (propertyParamUpdate.propertyName == EnumQuery.UpdatedBy)
        //            {
        //                personel = JsonConvert.DeserializeObject<UpdatedByResponse>(propertyParamUpdate.propertyValue);
        //            }
        //        }

        //        var lastStartTimeHistory = lastDataHistory.serviceStart;
        //        var lastStartService = DateTime.ParseExact(lastStartTimeHistory, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture);
        //        var newStartService = DateTime.ParseExact(newEndTime, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture);
        //        bool isChangeOfDayEndShift = true;
        //        bool isChangeOfDayStartNextShift = true;

        //        var startHourDay = lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.startHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.startHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
        //        var endHourDay = lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.endHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.endHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
        //        var startHourNight = lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.startHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.startHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
        //        var endHourNight = (lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.endHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.endHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay).AddDays(1);

        //        var endShiftService = endHourNight;
        //        var startNextShiftService = startHourNight;
        //        if (lastStartService >= startHourDay && lastStartService <= endHourDay)
        //        {
        //            endShiftService = endHourDay;
        //            isChangeOfDayEndShift = false;
        //        }

        //        startHourDay = newStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.startHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.startHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
        //        endHourDay = newStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.endHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.endHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
        //        startHourNight = newStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.startHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.startHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
        //        endHourNight = (newStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.endHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.endHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay).AddDays(1);

        //        if (newStartService >= startHourDay && newStartService <= endHourDay)
        //        {
        //            startNextShiftService = startHourDay;
        //            isChangeOfDayStartNextShift = false;
        //        }
        //        DateTime dateEndShiftService = new DateTime();
        //        DateTime dateStartShiftService = new DateTime();
        //        if (isChangeOfDayEndShift)
        //        {
        //             dateEndShiftService = lastStartService.Date.AddHours(endShiftService.Hour).AddMinutes(endShiftService.Minute).AddSeconds(endShiftService.Second).AddDays(1);
        //        }
        //        else
        //        {
        //            dateEndShiftService = lastStartService.Date.AddHours(endShiftService.Hour).AddMinutes(endShiftService.Minute).AddSeconds(endShiftService.Second);
        //        }
        //        if (isChangeOfDayStartNextShift)
        //        {
        //            if(newStartService.Hour < startHourDay.Hour)
        //            {
        //                dateStartShiftService = newStartService.Date.AddHours(startNextShiftService.Hour).AddMinutes(startNextShiftService.Minute).AddSeconds(startNextShiftService.Second).AddDays(-1);
        //            }
        //            else
        //            {
        //                dateStartShiftService = newStartService.Date.AddHours(startNextShiftService.Hour).AddMinutes(startNextShiftService.Minute).AddSeconds(startNextShiftService.Second);
        //            }
        //        }
        //        else
        //        {
        //            dateStartShiftService = newStartService.Date.AddHours(startNextShiftService.Hour).AddMinutes(startNextShiftService.Minute).AddSeconds(startNextShiftService.Second);
        //        }

        //        if ((DateTime.ParseExact(newEndTime, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture) <= dateEndShiftService))
        //        {
        //            oldJsonPersonels.ForEach(c =>
        //            {
        //                if (c.key == lastDataHistory.key)
        //                {
        //                    c.serviceEnd = newEndTime;
        //                }
        //            });
        //        }
        //        else
        //        {
        //            oldJsonPersonels.ForEach(c =>
        //            {
        //                if (c.key == lastDataHistory.key)
        //                {
        //                    c.serviceEnd = dateEndShiftService.ToString(EnumFormatting.DateTimeToString);
        //                }
        //            });
        //            newJsonPersonels.ForEach(c =>
        //            {
        //                if (c.key == newJsonPersonels[0].key)
        //                {
        //                    c.serviceStart = dateStartShiftService.ToString(EnumFormatting.DateTimeToString);
        //                }
        //            });
        //            oldJsonPersonels.AddRange(newJsonPersonels);
        //        }
        //    }
        //    return oldJsonPersonels;
        //}
        #region get lates service smu 
        public virtual async Task<dynamic> GetDataServiceSheetLatest(Dictionary<string, object> paramLatestSmu)
        {
            //string query = $"SELECT TOP 1 * FROM c where c.modelId = \"{paramLatestSmu[EnumQuery.ModelId]}\" and c.psTypeId = \"{paramLatestSmu[EnumQuery.PsTypeId]}\" and ToString(c.{EnumCommonProperty.IsDeleted}) = \"false\" order by c.updateDate DESC";
            string query = $"SELECT TOP 1 * FROM c where c.modelId = \"{paramLatestSmu[EnumQuery.ModelId]}\" and c.psTypeId = \"{paramLatestSmu[EnumQuery.PsTypeId]}\" and c.{EnumCommonProperty.IsDeleted} = \"false\" order by c.updateDate DESC";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }

            var topResult = results.OrderByDescending(x => x["serviceDataConvert"]).FirstOrDefault();

            return topResult;
        }
        #endregion

        public virtual async Task<dynamic> Upsert(CreateRequest createRequest)
        {
            JObject newObj = new JObject();

            if (createRequest.entity.GetType().GetProperty(EnumQuery.ID) == null)
                newObj.Add(EnumCommonProperty.ID, Guid.NewGuid().ToString());

            JToken newObjectEmployee = JToken.FromObject(createRequest.employee);

            foreach (var x in JObject.Parse(JsonConvert.SerializeObject(createRequest.entity)))
                newObj.Add(x);

            newObj.Add(EnumCommonProperty.IsActive, "true");
            newObj.Add(EnumCommonProperty.IsDeleted, "false");
            newObj.Add(EnumCommonProperty.CreatedBy, newObjectEmployee);
            newObj.Add(EnumCommonProperty.CreatedDate, GetSettingValue(EnumCommonProperty.ServerDateTime));
            newObj.Add(EnumCommonProperty.UpdatedBy, string.Empty);
            newObj.Add(EnumCommonProperty.UpdatedDate, string.Empty);

            var data = await _container.UpsertItemAsync(newObj);
            return data.Resource;
        }
    }
}
