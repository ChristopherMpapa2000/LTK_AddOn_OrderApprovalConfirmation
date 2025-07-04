using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddonDemo.Models.LTK.Response
{
    public class OrderApprovalConfirmationModel
    {
        public string Company { get; set; }
        public string DocumentNumber { get; set; }
        public string DocumentStatus { get; set; }
        public string DocumentReason { get; set; }
    }

}
