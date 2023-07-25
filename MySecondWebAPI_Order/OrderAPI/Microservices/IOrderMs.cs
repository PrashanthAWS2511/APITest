using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderAPI.Microservices
{
    public interface IOrderMs
    {
        Task<IEnumerable<dynamic>> GetOrderDetails(CancellationToken token);
    }
}