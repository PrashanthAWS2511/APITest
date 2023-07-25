using OrderAPI.CommonFunctions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderAPI.Microservices
{
    public class OrderMs : IOrderMs
    {
        //private readonly IDBLayer _dBLayer;
        public OrderMs()
        {
            //_dBLayer = dBLayer;
        }
        public async Task<IEnumerable<dynamic>> GetOrderDetails(CancellationToken token)
        {
            try
            {

                List<dynamic> OrderDetails = new List<dynamic>();

                //DataTable dt = await _dBLayer.GetDataInDataTable(CommandType.Text, "call public.sp_getorder(p_cursor:='p_GetOrderDetails');" +
                //    "fetch all in \"p_GetOrderDetails\";", token);

                //foreach (DataRow dr in dt.Rows)
                //{
                    dynamic Orderdefination = new ExpandoObject();
                    Orderdefination.OrderId = Convert.ToInt64("1");
                    Orderdefination.ProductId = Convert.ToInt64("1");
                    Orderdefination.CustomerId = Convert.ToInt64("1");
                    OrderDetails.Add(Orderdefination);
                //}

                return OrderDetails;
            }
            catch (Exception ex)
            {
                return null;
            }


        }
    }
}
