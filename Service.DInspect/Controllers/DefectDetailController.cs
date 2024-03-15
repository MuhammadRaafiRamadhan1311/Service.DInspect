using Azure.Core;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Request;
using Service.DInspect.Services;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Service.DInspect.Controllers;
using Service.DInspect.Interfaces;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models;
using Service.DInspect.Services;

namespace Service.DInspect.Controllers
{
    [Route("api/defect_detail")]
    public class DefectDetailController : BaseController
    {
        public DefectDetailController(IHttpContextAccessor httpContextAccessor, IServiceWrapper service, ILoggerFactory logger) : base(httpContextAccessor, service, logger)
        {
            _service = Service.DefectDetail;
        }

        [HttpGet]
        [Route("get_defect_excel")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public virtual async Task<ActionResult<ApiResponse>> GetDefectExcel(string workOrder)
        {
            try
            {
                var customProperties = new Dictionary<string, object>
                {
                    { EnumQuery.Workorder, workOrder}
                };

                //_telemetryClient.TrackEvent($"{nameof(ServiceSheetDetailController)}-{nameof(GetServiceSheetExcel)}", customProperties);

                ServiceResult result = await ((DefectDetailService)_service).GetDefectExcel(customProperties);

                return File(result.Content, System.Net.Mime.MediaTypeNames.Application.Octet, string.Format("DefectDetail - {0}.xlsx", DateTime.Now.ToString("ddMMyyyy hhmmss")), true);
            }
            catch (Exception ex)
            {
                var properties = new Dictionary<string, string> { ["Controller"] = nameof(DefectHeaderController) };
                var measurements = new Dictionary<string, double> { ["Function"] = 0 };     // Send the exception telemetry:
                //_telemetryClient.TrackException(ex, properties, measurements);

                return GenerateException(ex);
            }
        }
    }
}
