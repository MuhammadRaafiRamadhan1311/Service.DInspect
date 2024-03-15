using Microsoft.Azure.Cosmos;
using System;

namespace Service.DInspect.Interfaces
{
    public interface IConnectionFactory
    {
        Database GetDatabase();
    }
}
