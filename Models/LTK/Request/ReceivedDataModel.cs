using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddonDemo.Models.LTKAPI.Request
{
    public class ReceivedDataModel
    {
        public OrderApproval OrderApproval { get; set; }
    }

    public class OrderApproval
    {
        public int HeaderId { get; set; }
        public string Company { get; set; }
        public string DocNumber { get; set; }
        public string DocStatus { get; set; }
        public string Requester { get; set; }
        public string ROAReason { get; set; }
        public DateTime ROADatetime { get; set; }
        public string SalesmanCode { get; set; }
        public string SalesmanName { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string Address { get; set; }
        public string TelephoneNumber { get; set; }
        public string TaxNumber { get; set; }


        [Column(TypeName = "decimal(18,3)")]
        public decimal Total_GrandTotal { get; set; }


        public string DocCreateTime { get; set; }
        public string DocDate { get; set; }
        public string Attactment { get; set; }
        public List<ItemApprove> Items { get; set; }
    }

    public class ItemApprove
    {
        public int ID { get; set; }
        public string Email { get; set; }
    }
}
