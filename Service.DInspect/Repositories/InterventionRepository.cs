using Newtonsoft.Json.Linq;
using Service.DInspect.Interfaces;
using Service.DInspect.Models.Request;
using System.Threading.Tasks;

namespace Service.DInspect.Repositories
{
    public class InterventionRepository : RepositoryBase
    {
        public InterventionRepository(IConnectionFactory connectionFactory, string container) : base(connectionFactory, container)
        {
        }

        public virtual async Task<dynamic> GetDataInterventionReviselByKey(GetInterventionReviseRequest model)
        {
            string query = $"SELECT \r\n    c.equipment as equipment, \r\n    c.siteId as siteId, \r\n    task[\"key\"] as taskKey, \r\n    task.seqId as taskNo, \r\n    task.description, \r\n    task.category, \r\n    task.rating, \r\n    task.updatedBy, \r\n    task.updatedDate, \r\n    task.taskValue, \r\n    task.images as picture, \r\n    component.componentDescription, \r\n    c.equipmentModel as equipmentModel,\r\n    task.psType as psType,\r\n    task.category = \"CBM\" and task.rating = \"MANUAL\" ? \"\" : \r\n    task.category = \"NORMAL\" and task.items[2][\"valueItemType\"] <> \"inputUom\" ? \"\" : \r\n    task.category = \"NORMAL\" and task.items[2][\"valueItemType\"] = \"inputUom\" ? task.items[2][\"value\"] : \r\n    task.category = \"CBM-NORMAL\" ? task.items[4][\"value\"] : \r\n    task.category = \"CBM\" and task.rating = \"AUTOMATIC_REPLACEMENT\" ? task.items[3][\"value\"] : \r\n    task.category = \"CBM\" and task.rating = \"AUTOMATIC\" AND task.items[2][\"valueItemType\"] = \"inputUom\" ? task.items[2][\"value\"] : task.items[3][\"value\"] as measurementValue, \r\n    task.category = \"CBM\" and task.rating = \"MANUAL\" ? \"\" : \r\n    task.category = \"NORMAL\" and (task.items[3][\"valueItemType\"] = \"comment\" or task.items[2][\"valueItemType\"] <> \"inputUom\") ? \"\" : \r\n    task.category = \"NORMAL\" and task.items[2][\"valueItemType\"] = \"inputUom\" ? task.items[3][\"value\"] : \r\n    task.category = \"CBM-NORMAL\" and task.items[3][\"valueItemType\"] = \"inputUom\" ? task.items[4][\"value\"] : \r\n    task.category = \"CBM\" and task.rating = \"AUTOMATIC_REPLACEMENT\" and task.items[3][\"valueItemType\"] = \"inputUom\" ? task.items[4][\"value\"] : \r\n    task.category = \"CBM\" and task.rating = \"AUTOMATIC\" AND task.items[2][\"valueItemType\"] = \"inputUom\" ? task.items[3][\"value\"] : task.items[4][\"value\"] as uom \r\nFROM c \r\njoin details in c.details \r\njoin detailTasks in details.tasks \r\njoin task in detailTasks.tasks \r\njoin component in c.components where c.sapWorkOrder = \"{model.workOrder}\" and task[\"key\"] = \"{model.taskKey}\" and component.componentDescription = \"{model.component}\" and c.isActive = 'true'\r\nand c.isDeleted = 'false'";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }
            return results;
        }

        public virtual async Task<dynamic> GetDataInterventionHistoryDefaultByKey(GetInterventionReviseRequest model)
        {
            string query = $"SELECT \r\n    task.key,   \r\n    task.seqId,   \r\n    task.taskType,\r\n    task.isActive,\r\n    task.isDeleted,\r\n    \"\" as updatedBy,\r\n    task.header,\r\n    task.description,\r\n    task.category,\r\n    task.rating,\r\n    \"\" as updatedDate,\r\n    task.groupTaskId,\r\n    task.items,\r\n    \"\" as taskValue\r\nFROM c\r\njoin details in c.details\r\njoin detailTasks in details.tasks\r\njoin task in detailTasks.tasks\r\nwhere c.sapWorkOrder = \"{model.workOrder}\"\r\nand task[\"key\"] = \"{model.taskKey}\"\r\nand c.isActive = 'true'\r\nand c.isDeleted = 'false'";

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }
            return results;
        }

        public virtual async Task<dynamic> GetDataInterventionDetailByKey(DetailServiceSheet model)
        {
            string query = $"SELECT c.id, t2.key, t2.seqId, t2.taskType, t2.isActive, t2.isDeleted, t2.updatedBy, t2.header, t2.description, t2.category, t2.rating, t2.updatedDate, (IS_DEFINED(t2.adjustment) and t2.adjustment[\"rating\"] <> \"\") or (IS_DEFINED(t2.replacement) and t2.replacement[\"rating\"] <> \"\") ? true : false as cbmAdjustmentReplacement, IS_DEFINED(t2.adjustment) ? t2.adjustment[\"pictures\"] = [] ? \"\" : t2.adjustment[\"pictures\"] :     IS_DEFINED(t2.replacement) ? t2.replacement[\"pictures\"] = [] ? \"\" : t2.replacement[\"pictures\"] :    \"\" as pictures,       t2.items ,IS_DEFINED(t2.adjustment) ? t2.adjustment[\"rating\"] <> \"\" ? t2.adjustment[\"rating\"] : t2.taskValue : IS_DEFINED(t2.replacement) ? t2.replacement[\"rating\"] <> \"\" ? t2.replacement[\"rating\"] : t2.taskValue : t2.taskValue as taskValue, t2.items[0][\"value\"] as taskNo, t2.rating = \"MANUAL\" OR t2.rating = \"NORMAL\"  ? \"\" : t2.items[3][\"categoryItemType\"] = \"dropdownTool\" ? t2.items[5][\"value\"] : t2.items[3][\"categoryItemType\"] = \"brakeTypeDropdown\" ? t2.items[6][\"value\"]         : t2.items[4][\"categoryItemType\"] = \"dropdownToolDisc\" ? t2.items[6][\"value\"] : t2.items[4][\"categoryItemType\"] = \"dropdownTool\" ? t2.items[6][\"value\"] : t2.rating = \"AUTOMATIC\" AND  t2.items[3][\"valueItemType\"] = \"comment\" ? t2.items[5][\"value\"]  : t2.rating = \"AUTOMATIC\" ? t2.items[4][\"value\"] : t2.items[5][\"value\"] = [] ? \"\"          : t2.items[5][\"value\"] as uom, t2.rating = \"MANUAL\" OR t2.rating = \"NORMAL\" ? \"\"          : t2.items[3][\"categoryItemType\"] = \"dropdownTool\" ? t2.items[4][\"value\"]         : t2.items[3][\"categoryItemType\"] = \"brakeTypeDropdown\" ? t2.items[5][\"value\"]         : t2.items[4][\"categoryItemType\"] = \"dropdownToolDisc\" ? t2.items[5][\"value\"]         : IS_DEFINED(t2.adjustment) ? t2.adjustment[\"measurement\"] <> \"\" ? t2.adjustment[\"measurement\"]:t2.items[3][\"value\"]       : IS_DEFINED(t2.replacement) ? t2.replacement[\"measurement\"] <> \"\" ? t2.replacement[\"measurement\"] : t2.items[4][\"value\"] : t2.items[4][\"categoryItemType\"] = \"dropdownTool\" ? t2.items[5][\"value\"]      : t2.rating = \"AUTOMATIC\" ? IS_DEFINED(t2.items[3][\"valueItemType\"]) ? t2.items[4][\"value\"] : t2.items[3][\"value\"]           : t2.items[4][\"value\"] as measurementValue, (IS_DEFINED(t2.adjustment) and t2.adjustment[\"rating\"] <> \"\") = true ? t2.adjustment : (IS_DEFINED(t2.replacement) and t2.replacement[\"rating\"] <> \"\") = true ? t2.replacement : \"\" as cbmAdjustmentReplacementValue, (IS_DEFINED(t2.adjustment) and t2.adjustment[\"rating\"] <> \"\") or (IS_DEFINED(t2.replacement) and t2.replacement[\"rating\"] <> \"\") = true ? t2.items[3][\"value\"] : \"\"  as nonCbmAdjustmentReplacementMeasurementValue, (IS_DEFINED(t2.adjustment) and t2.adjustment[\"rating\"] <> \"\") or (IS_DEFINED(t2.replacement) and t2.replacement[\"rating\"] <> \"\") = true ? t2.items[6][\"value\"] : \"\" as nonCbmAdjustmentReplacementRating, IS_DEFINED(t2.replacement) ? t2.items[4][\"value\"] : t2.items[3][\"value\"] as currentValue, IS_DEFINED(t2.replacement) ? t2.items[6][\"value\"] : t2.items[5][\"value\"] as currentRating, IS_DEFINED(t2.replacement) ? t2.replacement.measurement : \"\" as replacementValue, IS_DEFINED(t2.replacement) ? t2.replacement.rating : \"\" as replacementRating, IS_DEFINED(t2.replacement) as taskReplacement FROM c join details in c.details join t1 in details.tasks join t2 in t1.tasks ";
            //where     c.workOrder = \"{model.workOrder}\" and  task.key = \"{model.taskKey}\"";
            if (model.taskKey == null || model.taskKey == "")
            {
                query += $"where c.sapWorkOrder = \"{model.workOrder}\" and t2.rating like \"%REPLACEMENT%\"";
            }
            else
            {
                query += $"where     c.sapWorkOrder = \"{model.workOrder}\" and  t2.key = \"{model.taskKey}\"";
            }

            var response = await Task.Run(() => _container.GetItemQueryIterator<dynamic>(query));

            JArray results = new JArray();

            while (response.HasMoreResults)
            {
                foreach (var item in await response.ReadNextAsync())
                    results.Add(item);
            }
            return results;
        }
    }
}