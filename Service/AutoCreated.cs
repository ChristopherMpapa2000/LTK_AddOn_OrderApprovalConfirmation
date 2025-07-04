using Addon.Extenstion;
using Addon.Models.OpenAPI.Response;
using Addon.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static Addon.Models.OpenAPI.Request.ExtCreateMemorandumRequest;
using static Addon.Models.OpenAPI.Response.ExtCreateMemorandumResponse;
using System.Text.RegularExpressions;
using WolfApprove.API2.Extension;

namespace Addon.Service
{
    public class AutoCreated
    {
        private readonly HttpClient _client;
        private readonly string openAPIUrl;
        private NameValueCollection config;

        public AutoCreated(string conn)
        {
            config = Ext.GetAppSetting();
            _client = new HttpClient();
            openAPIUrl = config["OpenAPIUrl"] ?? "";
            _client.BaseAddress = new Uri(openAPIUrl);
        }
    }
}
