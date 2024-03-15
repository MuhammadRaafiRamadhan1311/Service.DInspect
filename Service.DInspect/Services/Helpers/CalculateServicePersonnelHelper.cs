using Newtonsoft.Json;
using Service.DInspect.Models.Entity;
using System.Collections.Generic;
using System.Globalization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Response;
using Service.DInspect.Models.ADM;
using Service.DInspect.Models.Request;

namespace Service.DInspect.Services.Helpers
{
    public class CalculateServicePersonnelHelper
    {
        public List<ServicePersonnelsResponse> CalculateServicePersonnel(List<ServicePersonnelsResponse> oldJsonPersonels, List<ServicePersonnelsResponse> newJsonPersonels, List<ShiftModel> shiftModel, List<PropertyParam> propertyParam, string idPersonnel = "", List<PropertyParam> propertyParamSubmit = null)
        {
            if (!string.IsNullOrWhiteSpace(idPersonnel)) // final submit form
            {
                var propertyParams = propertyParamSubmit == null ? propertyParam : propertyParamSubmit;
                var emptyEndTimePersonnel = oldJsonPersonels.Where(x => string.IsNullOrWhiteSpace(x.serviceEnd)).ToList();
                UpdatedByResponse personel = new UpdatedByResponse();
                string newEndTime = "";
                foreach (var propertyParamUpdate in propertyParams)
                {
                    if (propertyParamUpdate.propertyName == EnumQuery.UpdatedDate)
                    {
                        newEndTime = propertyParamUpdate.propertyValue;
                    }
                }
                foreach (var personnel in emptyEndTimePersonnel)
                {
                    var lastDataHistory = oldJsonPersonels.Where(x => x.mechanic.id == personnel.mechanic.id).LastOrDefault();
                    var lastStartTimeHistory = lastDataHistory.serviceStart;
                    var lastStartService = DateTime.ParseExact(lastStartTimeHistory, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture);
                    var newStartService = DateTime.ParseExact(newEndTime, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture);

                    var startHourDay = lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.startHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.startHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
                    var endHourDay = lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.endHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.endHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
                    var startHourNight = lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.startHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.startHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
                    var endHourNight = (lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.endHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.endHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay).AddDays(1);

                    bool isChangeOfDayEndShift = false;
                    bool isChangeOfDayStartNextShift = false;
                    var endShiftService = endHourDay;
                    var startNextShiftService = startHourDay;

                    if (lastStartService >= startHourNight && lastStartService <= endHourNight || lastStartService >= startHourNight.AddDays(-1) && lastStartService <= endHourNight.AddDays(-1))
                    {
                        endShiftService = endHourNight;
                        if (lastStartService.Hour > startHourDay.Hour)
                        {
                            isChangeOfDayEndShift = true;
                        }
                    }

                    startHourDay = newStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.startHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.startHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
                    endHourDay = newStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.endHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.endHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
                    startHourNight = newStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.startHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.startHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
                    endHourNight = (newStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.endHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.endHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay).AddDays(1);

                    if (newStartService >= startHourNight && newStartService <= endHourNight || newStartService >= startHourNight.AddDays(-1) && newStartService <= endHourNight.AddDays(-1))
                    {
                        startNextShiftService = startHourNight;
                        if (lastStartService.Hour > startHourDay.Hour)
                        {
                            isChangeOfDayStartNextShift = true;
                        }
                    }
                    DateTime dateEndShiftService = new DateTime();
                    DateTime dateStartShiftService = new DateTime();

                    if (isChangeOfDayEndShift)
                    {
                        dateEndShiftService = lastStartService.Date.AddHours(endShiftService.Hour).AddMinutes(endShiftService.Minute).AddSeconds(endShiftService.Second).AddDays(1);
                    }
                    else
                    {
                        dateEndShiftService = lastStartService.Date.AddHours(endShiftService.Hour).AddMinutes(endShiftService.Minute).AddSeconds(endShiftService.Second);
                    }

                    if (isChangeOfDayStartNextShift)
                    {
                        if (newStartService.Hour < startHourDay.Hour)
                        {
                            dateStartShiftService = newStartService.Date.AddHours(startNextShiftService.Hour).AddMinutes(startNextShiftService.Minute).AddSeconds(startNextShiftService.Second).AddDays(-1);
                        }
                        else
                        {
                            dateStartShiftService = newStartService.Date.AddHours(startNextShiftService.Hour).AddMinutes(startNextShiftService.Minute).AddSeconds(startNextShiftService.Second);
                        }
                    }
                    else
                    {
                        dateStartShiftService = newStartService.Date.AddHours(startNextShiftService.Hour).AddMinutes(startNextShiftService.Minute).AddSeconds(startNextShiftService.Second);
                    }

                    if (newStartService <= dateEndShiftService)
                    {
                        var update = oldJsonPersonels.Where(c => c.mechanic.id == personnel.mechanic.id).LastOrDefault();
                        if (update != null)
                        {
                            update.serviceEnd = newEndTime;
                        }
                    }
                    else
                    {
                        var update = oldJsonPersonels.Where(c => c.mechanic.id == personnel.mechanic.id).LastOrDefault();
                        if (update != null)
                        {
                            update.serviceEnd = dateEndShiftService.ToString(EnumFormatting.DateTimeToString);
                        }

                        if (personnel.mechanic.id == idPersonnel)
                        {
                            var newShiftData = "";
                            if (isChangeOfDayStartNextShift)
                            {
                                newShiftData = EnumShiftValue.Night + " " + "Shift";
                            }
                            else
                            {
                                newShiftData = EnumShiftValue.Day + " " + "Shift";
                            }
                            ServicePersonnelsResponse generatedData = new ServicePersonnelsResponse()
                            {
                                key = Guid.NewGuid().ToString(),
                                serviceStart = dateStartShiftService.ToString(EnumFormatting.DateTimeToString),
                                serviceEnd = newEndTime,
                                shift = newShiftData,
                                mechanic = new EmployeeModel()
                                {
                                    id = lastDataHistory.mechanic.id,
                                    name = lastDataHistory.mechanic.name
                                }
                            };
                            oldJsonPersonels.Add(generatedData);
                        }
                    }
                }
            }
            else if (string.IsNullOrWhiteSpace(newJsonPersonels[0].serviceEnd)) // general submit
            {
                if (oldJsonPersonels.Where(x => x.mechanic.id == newJsonPersonels.FirstOrDefault().mechanic.id).FirstOrDefault() == null) // no history for same user
                {
                    oldJsonPersonels.AddRange(newJsonPersonels);
                }
                else // there is record history for same user
                {
                    var lastDataHistory = oldJsonPersonels.Where(x => x.mechanic.id == newJsonPersonels.FirstOrDefault().mechanic.id).LastOrDefault();
                    var lastEndTimeHistory = lastDataHistory.serviceEnd;
                    var lastStartTimeHistory = lastDataHistory.serviceStart;
                    if (!string.IsNullOrWhiteSpace(lastEndTimeHistory)) //endtime last history exists
                    {
                        #region logic check endshift 
                        bool isChangeOfDay = false;
                        var lastStartService = DateTime.ParseExact(lastStartTimeHistory, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture);

                        var startHourDay = lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.startHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.startHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
                        var endHourDay = lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.endHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.endHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
                        var startHourNight = lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.startHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.startHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
                        var endHourNight = (lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.endHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.endHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay).AddDays(1);
                        var endShiftService = endHourDay;
                        if (lastStartService >= startHourNight && lastStartService <= endHourNight)
                        {
                            endShiftService = endHourNight;
                            isChangeOfDay = true;
                        }
                        DateTime dateEndShiftService;
                        if (isChangeOfDay)
                        {
                            dateEndShiftService = lastStartService.Date.AddHours(endShiftService.Hour).AddMinutes(endShiftService.Minute).AddSeconds(endShiftService.Second).AddDays(1);
                        }
                        else
                        {
                            dateEndShiftService = lastStartService.Date.AddHours(endShiftService.Hour).AddMinutes(endShiftService.Minute).AddSeconds(endShiftService.Second);
                        }

                        #endregion

                        var newStartTime = newJsonPersonels.FirstOrDefault().serviceStart;
                        if ((DateTime.ParseExact(newStartTime, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture) - DateTime.ParseExact(lastEndTimeHistory, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture)).TotalHours <= 3
                            && DateTime.ParseExact(newStartTime, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture) <= dateEndShiftService)
                        {
                            oldJsonPersonels.ForEach(c =>
                            {
                                if (c.key == lastDataHistory.key)
                                {
                                    c.serviceEnd = "";
                                }
                            });
                        }
                        else
                        {
                            oldJsonPersonels.AddRange(newJsonPersonels);
                        }
                    }
                    else
                    {
                        var newStartTime = newJsonPersonels.FirstOrDefault().serviceStart;

                        #region logic check endshift 
                        bool isChangeOfDay = false;
                        var lastStartService = DateTime.ParseExact(lastStartTimeHistory, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture);

                        var startHourDay = lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.startHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.startHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
                        var endHourDay = lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.endHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.endHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
                        var startHourNight = lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.startHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.startHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
                        var endHourNight = (lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.endHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.endHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay).AddDays(1);
                        var endShiftService = endHourDay;
                        if (lastStartService >= startHourNight && lastStartService <= endHourNight || lastStartService >= startHourNight.AddDays(-1) && lastStartService <= endHourNight.AddDays(-1))
                        {
                            endShiftService = endHourNight;
                            if (lastStartService.Hour > 6)
                            {
                                isChangeOfDay = true;
                            }
                        }
                        DateTime dateEndShiftService;
                        if (isChangeOfDay)
                        {
                            dateEndShiftService = lastStartService.Date.AddHours(endShiftService.Hour).AddMinutes(endShiftService.Minute).AddSeconds(endShiftService.Second).AddDays(1);
                        }
                        else
                        {
                            dateEndShiftService = lastStartService.Date.AddHours(endShiftService.Hour).AddMinutes(endShiftService.Minute).AddSeconds(endShiftService.Second);
                        }

                        #endregion
                        if (!string.IsNullOrWhiteSpace(lastStartTimeHistory) && (DateTime.ParseExact(newStartTime, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture) - DateTime.ParseExact(lastStartTimeHistory, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture)).TotalHours > 3
                            || DateTime.ParseExact(newStartTime, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture) > dateEndShiftService)
                        {
                            if (DateTime.ParseExact(newStartTime, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture) > dateEndShiftService)
                            {
                                oldJsonPersonels.ForEach(c =>
                                {
                                    if (c.key == lastDataHistory.key)
                                    {
                                        c.serviceEnd = dateEndShiftService.ToString(EnumFormatting.DateTimeToString);
                                    }
                                });
                            }
                            else
                            {
                                oldJsonPersonels.ForEach(c =>
                                {
                                    if (c.key == lastDataHistory.key)
                                    {
                                        c.serviceEnd = newJsonPersonels.FirstOrDefault().serviceStart;
                                    }
                                });
                            }
                            oldJsonPersonels.AddRange(newJsonPersonels);
                        }
                    }
                }
            }
            else //finish button
            {
                var lastDataHistory = oldJsonPersonels.Where(x => x.mechanic.id == newJsonPersonels.FirstOrDefault().mechanic.id).LastOrDefault();
                var newEndTime = newJsonPersonels.FirstOrDefault().serviceEnd;
                //var newStartTime = newJsonPersonels.FirstOrDefault().serviceStart;
                UpdatedByResponse personel = new UpdatedByResponse();
                foreach (var propertyParamUpdate in propertyParam)
                {
                    if (propertyParamUpdate.propertyName == EnumQuery.UpdatedBy)
                    {
                        personel = JsonConvert.DeserializeObject<UpdatedByResponse>(propertyParamUpdate.propertyValue);
                    }
                }

                var lastStartTimeHistory = lastDataHistory.serviceStart;
                var lastStartService = DateTime.ParseExact(lastStartTimeHistory, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture);
                var newStartService = DateTime.ParseExact(newEndTime, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture);
                bool isChangeOfDayEndShift = false;
                bool isChangeOfDayStartNextShift = false;

                var startHourDay = lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.startHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.startHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
                var endHourDay = lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.endHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.endHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
                var startHourNight = lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.startHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.startHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
                var endHourNight = (lastStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.endHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.endHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay).AddDays(1);

                var endShiftService = endHourDay;
                var startNextShiftService = startHourDay;
                if (lastStartService >= startHourNight && lastStartService <= endHourNight || lastStartService >= startHourNight.AddDays(-1) && lastStartService <= endHourNight.AddDays(-1))
                {
                    endShiftService = startHourDay;
                    if (lastStartService.Hour > startHourDay.Hour)
                    {
                        isChangeOfDayEndShift = true;
                    }
                }

                startHourDay = newStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.startHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.startHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
                endHourDay = newStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.endHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Day).Select(x => x.endHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
                startHourNight = newStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.startHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.startHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay;
                endHourNight = (newStartService.Date + DateTime.ParseExact(shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.endHour).FirstOrDefault() + " " + shiftModel.Where(x => x.shift == EnumShiftValue.Night).Select(x => x.endHourType).FirstOrDefault(), EnumFormatting.Time12, CultureInfo.InvariantCulture).TimeOfDay).AddDays(1);

                if (newStartService >= startHourNight && newStartService <= endHourNight || newStartService >= startHourNight.AddDays(-1) && newStartService <= endHourNight.AddDays(-1))
                {
                    startNextShiftService = startHourNight;
                    if (lastStartService.Hour > startHourDay.Hour)
                    {
                        isChangeOfDayStartNextShift = true;
                    }
                }
                DateTime dateEndShiftService = new DateTime();
                DateTime dateStartShiftService = new DateTime();
                if (isChangeOfDayEndShift)
                {
                    dateEndShiftService = lastStartService.Date.AddHours(endShiftService.Hour).AddMinutes(endShiftService.Minute).AddSeconds(endShiftService.Second).AddDays(1);
                }
                else
                {
                    dateEndShiftService = lastStartService.Date.AddHours(endShiftService.Hour).AddMinutes(endShiftService.Minute).AddSeconds(endShiftService.Second);
                }
                if (isChangeOfDayStartNextShift)
                {
                    if (newStartService.Hour < startHourDay.Hour)
                    {
                        dateStartShiftService = newStartService.Date.AddHours(startNextShiftService.Hour).AddMinutes(startNextShiftService.Minute).AddSeconds(startNextShiftService.Second).AddDays(-1);
                    }
                    else
                    {
                        dateStartShiftService = newStartService.Date.AddHours(startNextShiftService.Hour).AddMinutes(startNextShiftService.Minute).AddSeconds(startNextShiftService.Second);
                    }
                }
                else
                {
                    dateStartShiftService = newStartService.Date.AddHours(startNextShiftService.Hour).AddMinutes(startNextShiftService.Minute).AddSeconds(startNextShiftService.Second);
                }

                if (DateTime.ParseExact(newEndTime, EnumFormatting.DateTimeToString, CultureInfo.InvariantCulture) <= dateEndShiftService)
                {
                    oldJsonPersonels.ForEach(c =>
                    {
                        if (c.key == lastDataHistory.key)
                        {
                            c.serviceEnd = newEndTime;
                        }
                    });
                }
                else
                {
                    oldJsonPersonels.ForEach(c =>
                    {
                        if (c.key == lastDataHistory.key)
                        {
                            c.serviceEnd = dateEndShiftService.ToString(EnumFormatting.DateTimeToString);
                        }
                    });
                    newJsonPersonels.ForEach(c =>
                    {
                        if (c.key == newJsonPersonels[0].key)
                        {
                            c.serviceStart = dateStartShiftService.ToString(EnumFormatting.DateTimeToString);
                            if (isChangeOfDayStartNextShift)
                            {
                                c.shift = EnumShiftValue.Night + " " + "Shift";
                            }
                            else
                            {
                                c.shift = EnumShiftValue.Day + " " + "Shift";
                            }
                        }
                    });
                    oldJsonPersonels.AddRange(newJsonPersonels);
                }
            }
            return oldJsonPersonels;
        }
    }
}
