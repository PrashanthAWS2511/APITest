using Microsoft.AspNetCore.Mvc;
using OrderAPI.Microservices;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProductAPI.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private IOrderMs objPMS;

        public OrderController(IOrderMs iObjPMS)
        {
            objPMS = iObjPMS;
        }

        [HttpGet]
        public async Task<IActionResult> OrderList(CancellationToken token)
        {
            var ViewOrder = await objPMS.GetOrderDetails(token);
            var ViewOrder_List = ViewOrder.ToList();
            return Ok(ViewOrder_List);
        }
    }
}
