using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Service.DInspect.Interfaces;
using Service.DInspect.Models;
using Service.DInspect.Models.Enum;
using Service.DInspect.Models.Request;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Service.DInspect.Services
{
    public abstract class ServiceBase : IServiceBase
    {
        protected IRepositoryBase _repository;
        public MySetting _appSetting { get; }
        protected string _accessToken { get; }

        public ServiceBase(MySetting appSetting, IConnectionFactory connectionFactory, string container, string accessToken)
        {
            _appSetting = appSetting;
            _accessToken = accessToken;
        }

        public virtual async Task<ServiceResult> GetAllData()
        {
            try
            {
                var result = await _repository.GetAllData();

                return new ServiceResult
                {
                    Message = "Get data successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Message = ex.Message,
                    IsError = true
                };
            }
        }

        public virtual async Task<ServiceResult> GetActiveData()
        {
            try
            {
                var result = await _repository.GetActiveData();

                return new ServiceResult
                {
                    Message = "Get data successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Message = ex.Message,
                    IsError = true
                };
            }
        }

        public virtual async Task<ServiceResult> Get(string id)
        {
            try
            {
                var result = await _repository.Get(id);

                return new ServiceResult
                {
                    Message = "Get data successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Message = ex.Message,
                    IsError = true
                };
            }
        }

        public virtual async Task<ServiceResult> GetDataListByParam(Dictionary<string, object> param)
        {
            try
            {
                var result = await _repository.GetDataListByParam(param);

                return new ServiceResult
                {
                    Message = "Get data successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Message = ex.Message,
                    IsError = true
                };
            }
        }

        public virtual async Task<ServiceResult> GetDataByParam(Dictionary<string, object> param)
        {
            try
            {
                var result = await _repository.GetDataByParam(param);

                return new ServiceResult
                {
                    Message = "Get data successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Message = ex.Message,
                    IsError = true
                };
            }
        }

        public virtual async Task<ServiceResult> GetDataListByParam(Dictionary<string, object> param, int limit, string orderBy, string orderType)
        {
            try
            {
                var result = await _repository.GetDataListByParam(param, limit, orderBy, orderType);

                return new ServiceResult
                {
                    Message = "Get data successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Message = ex.Message,
                    IsError = true
                };
            }
        }

        public virtual async Task<ServiceResult> Post(CreateRequest createRequest)
        {
            try
            {
                var result = await _repository.Create(createRequest);

                return new ServiceResult
                {
                    Message = "Data created successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Message = ex.Message,
                    IsError = true
                };
            }
        }

        public virtual async Task<ServiceResult> Post(CreateRequest createRequest, Dictionary<string, string> adjustFields)
        {
            try
            {
                var result = await _repository.Create(createRequest, adjustFields);

                return new ServiceResult
                {
                    Message = "Data created successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Message = ex.Message,
                    IsError = true
                };
            }
        }

        public virtual async Task<ServiceResult> PostList(CreateRequest createRequest)
        {
            try
            {
                var result = await _repository.CreateList(createRequest);

                return new ServiceResult
                {
                    Message = "Data created successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Message = ex.Message,
                    IsError = true
                };
            }
        }

        public virtual async Task<ServiceResult> Put(UpdateRequest updateRequest)
        {
            try
            {
                var rsc = await _repository.Get(updateRequest.id);
                var result = await _repository.Update(updateRequest, rsc);

                return new ServiceResult
                {
                    Message = "Data updated successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Message = ex.Message,
                    IsError = true
                };
            }
        }

        public virtual async Task<ServiceResult> Delete(DeleteRequest deleteRequest)
        {
            try
            {
                var result = await _repository.Delete(deleteRequest);

                return new ServiceResult
                {
                    Message = "Data deleted successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Message = ex.Message,
                    IsError = true
                };
            }
        }

        public virtual async Task<ServiceResult> GetFieldValue(GetFieldValueRequest getFieldValueRequest)
        {
            try
            {
                var result = await _repository.GetFieldValue(getFieldValueRequest);

                return new ServiceResult
                {
                    Message = "Get field value successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Message = ex.Message,
                    IsError = true
                };
            }
        }

        public ServiceResult ErrorHandling(Exception ex)
        {
            //var w32ex = ex as Win32Exception;
            //int errCode = w32ex.ErrorCode;
            string errMsg = string.Empty;

            if (ex is NullReferenceException)
                errMsg = EnumErrorMessage.ErrMsg400;
            else if (ex is IndexOutOfRangeException)
                errMsg = EnumErrorMessage.ErrMsg400;
            else if (ex is IOException)
                errMsg = EnumErrorMessage.ErrMsg400;
            else if (ex is WebException)
                errMsg = EnumErrorMessage.ErrMsg400;
            else if (ex is StackOverflowException)
                errMsg = EnumErrorMessage.ErrMsg400;
            else if (ex is OutOfMemoryException)
                errMsg = EnumErrorMessage.ErrMsg400;
            else if (ex is InvalidCastException)
                errMsg = EnumErrorMessage.ErrMsg400;
            else if (ex is InvalidOperationException)
                errMsg = EnumErrorMessage.ErrMsg400;
            else if (ex is ObjectDisposedException)
                errMsg = EnumErrorMessage.ErrMsg400;
            else
                errMsg = EnumErrorMessage.ErrMsg400;

            return new ServiceResult
            {
                Message = errMsg,
                IsError = false,
                Content = null
            };
        }

        public virtual async Task<ServiceResult> Upsert(CreateRequest createRequest)
        {
            try
            {
                var result = await _repository.Upsert(createRequest);

                return new ServiceResult
                {
                    Message = "Data created successfully",
                    IsError = false,
                    Content = result
                };
            }
            catch (Exception ex)
            {
                return new ServiceResult
                {
                    Message = ex.Message,
                    IsError = true
                };
            }
        }
    }
}
