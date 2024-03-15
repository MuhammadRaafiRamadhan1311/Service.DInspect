using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Models.Request;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Service.DInspect.Controllers
{
    [ApiController]
    //[Authorize]
    public abstract class BaseController : ControllerBase, IBaseController
    {
        protected IHttpContextAccessor _httpContextAccessor;
        protected readonly IServiceWrapper Service;
        protected IServiceBase _service;
        protected readonly ILogger<BaseController> _logger;
        protected readonly TelemetryClient _telemetryClient;

        public BaseController(IHttpContextAccessor httpContextAccessor, IServiceWrapper service, ILoggerFactory logger)
        {
            service.AccessToken = httpContextAccessor.HttpContext.Request.Headers["Authorization"].ToString();
            Service = service;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger.CreateLogger<BaseController>();
            //_telemetryClient = telemetryClient;
        }

        [HttpGet]
        [Route("get_all_data")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public virtual async Task<ActionResult<ApiResponse>> GetAllData()
        {
            try
            {
                ServiceResult result = await _service.GetAllData();
                return GenerateResult(result);
            }
            catch (Exception ex)
            {
                return GenerateException(ex);
            }
        }

        [HttpGet]
        [Route("get_active_data")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public virtual async Task<ActionResult<ApiResponse>> GetActiveData()
        {
            try
            {
                ServiceResult result = await _service.GetActiveData();
                return GenerateResult(result);
            }
            catch (Exception ex)
            {
                return GenerateException(ex);
            }
        }

        [HttpGet]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public virtual async Task<ActionResult<ApiResponse>> Get(string id)
        {
            try
            {
                var customProperties = new Dictionary<string, string>
                {
                    { "idData", id}
                };

                _telemetryClient.TrackEvent($"{nameof(BaseController)}-{nameof(Get)}", customProperties);

                ServiceResult result = await _service.Get(id);
                return GenerateResult(result);
            }
            catch (Exception ex)
            {
                return GenerateException(ex);
            }
        }

        [HttpPost]
        [Route("get_data_list_by_param")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public virtual async Task<ActionResult<ApiResponse>> GetDataListByParam([FromBody] Dictionary<string, object> param)
        {
            try
            {
                var customProperties = new Dictionary<string, string>
                {
                    { "body", JsonConvert.SerializeObject(param)}
                };

                _telemetryClient.TrackEvent($"{nameof(BaseController)}-{nameof(GetDataListByParam)}", customProperties);

                ServiceResult result = await _service.GetDataListByParam(param);
                return GenerateResult(result);
            }
            catch (Exception ex)
            {
                return GenerateException(ex);
            }
        }

        [HttpPost]
        [Route("get_data_by_param")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public virtual async Task<ActionResult<ApiResponse>> GetDataByParam([FromBody] Dictionary<string, object> param)
        {
            try
            {
                var customProperties = new Dictionary<string, string>
                {
                    { "body", JsonConvert.SerializeObject(param)}
                };

                _telemetryClient.TrackEvent($"{nameof(BaseController)}-{nameof(GetDataByParam)}", customProperties);

                ServiceResult result = await _service.GetDataByParam(param);
                return GenerateResult(result);
            }
            catch (Exception ex)
            {
                return GenerateException(ex);
            }
        }

        [HttpPost]
        [Route("get_data_list_by_param_limit")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public virtual async Task<ActionResult<ApiResponse>> GetDataListByParam([FromBody] Dictionary<string, object> param, int limit, string orderBy, string orderType)
        {
            try
            {
                var customProperties = new Dictionary<string, string>
                {
                    { "body", JsonConvert.SerializeObject(param) },
                    { "limit", limit.ToString() },
                    { "orderBy", orderBy },
                    { "orderType", orderType }
                };

                _telemetryClient.TrackEvent($"{nameof(BaseController)}-{nameof(GetDataListByParam)}", customProperties);

                ServiceResult result = await _service.GetDataListByParam(param, limit, orderBy, orderType);
                return GenerateResult(result);
            }
            catch (Exception ex)
            {
                return GenerateException(ex);
            }
        }

        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public virtual async Task<ActionResult<ApiResponse>> Post([FromBody] CreateRequest createRequest)
        {
            try
            {
                var customProperties = new Dictionary<string, string>
                {
                    { "body", JsonConvert.SerializeObject(createRequest) }
                };

                _telemetryClient.TrackEvent($"{nameof(BaseController)}-{nameof(Post)}", customProperties);

                ServiceResult result = await _service.Post(createRequest);
                return GenerateResult(result);
            }
            catch (Exception ex)
            {
                return GenerateException(ex);
            }
        }

        [HttpPost]
        [Route("update")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public virtual async Task<ActionResult<ApiResponse>> Put([FromBody] UpdateRequest updateRequest)
        {
            try
            {
                var customProperties = new Dictionary<string, string>
                {
                    { "Body", JsonConvert.SerializeObject(updateRequest) }
                };

                _telemetryClient.TrackEvent($"{nameof(BaseController)}-{nameof(Put)}", customProperties);

                _logger.LogWarning("Start Update Data");

                ServiceResult result = await _service.Put(updateRequest);

                _logger.LogWarning("End Update Data");

                if (result.IsError)
                {
                    return BadRequest(new ApiResponse()
                    {
                        Title = "Error",
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Result = result
                    });
                }
                else
                {
                    return Ok(new ApiResponse()
                    {
                        Title = "Success",
                        StatusCode = (int)HttpStatusCode.OK,
                        Result = result
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());

                return BadRequest(new ApiResponse()
                {
                    Title = "Error",
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Result = new ServiceResult()
                    {
                        IsError = true,
                        Message = ex.Message
                    }
                });
            }
        }

        [HttpPost]
        [Route("delete")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public virtual async Task<ActionResult<ApiResponse>> Delete([FromBody] DeleteRequest deleteRequest)
        {
            try
            {
                var customProperties = new Dictionary<string, string>
                {
                    { "body", JsonConvert.SerializeObject(deleteRequest) }
                };

                _telemetryClient.TrackEvent($"{nameof(BaseController)}-{nameof(Delete)}", customProperties);

                ServiceResult result = await _service.Delete(deleteRequest);
                return GenerateResult(result);
            }
            catch (Exception ex)
            {
                return GenerateException(ex);
            }
        }

        [HttpPost]
        [Route("get_field_value")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public virtual async Task<ActionResult<ApiResponse>> GetFieldValue([FromBody] GetFieldValueRequest getFieldValueRequest)
        {
            try
            {
                var customProperties = new Dictionary<string, string>
                {
                    { "body", JsonConvert.SerializeObject(getFieldValueRequest) }
                };

                _telemetryClient.TrackEvent($"{nameof(BaseController)}-{nameof(GetFieldValue)}", customProperties);

                ServiceResult result = await _service.GetFieldValue(getFieldValueRequest);
                return GenerateResult(result);
            }
            catch (Exception ex)
            {
                return GenerateException(ex);
            }
        }

        [HttpPost]
        [Route("upsert")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public virtual async Task<ActionResult<ApiResponse>> Upsert([FromBody] CreateRequest createRequest)
        {
            try
            {
                var customProperties = new Dictionary<string, string>
                {
                    { "body", JsonConvert.SerializeObject(createRequest) }
                };

                _telemetryClient.TrackEvent($"{nameof(BaseController)}-{nameof(Post)}", customProperties);

                ServiceResult result = await _service.Upsert(createRequest);
                return GenerateResult(result);
            }
            catch (Exception ex)
            {
                return GenerateException(ex);
            }
        }

        protected ApiResponse GenerateResult(ServiceResult serviceResult)
        {
            ApiResponse result = new ApiResponse();

            if (serviceResult.IsError)
            {
                result.Title = "Error";
                result.StatusCode = (int)HttpStatusCode.BadRequest;
                result.Result = serviceResult;
            }
            else
            {
                result.Title = "Success";
                result.StatusCode = (int)HttpStatusCode.OK;
                result.Result = serviceResult;
            }

            return result;
        }

        protected ApiResponse GenerateException(Exception ex)
        {
            ApiResponse result = new ApiResponse()
            {
                Title = "Error",
                StatusCode = (int)HttpStatusCode.BadRequest,
                Result = new ServiceResult()
                {
                    IsError = true,
                    Message = ex.Message
                }
            };

            return result;
        }
    }
}
