using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using WolfApprove.API2.Extension;

namespace AddOnGs.Extenstion
{
    public class AddonLog
    {
        private NameValueCollection config;
        public AddonLog() 
        {
            config = Ext.GetAppSetting();
        }

        public async void LogInfo(string module , string text ,string request = null)
        {
            string _logFilePath = config["LogFile"];
            if (string.IsNullOrEmpty(_logFilePath))
            {
                throw new InvalidOperationException("LogFile path is not configured.");
            }
            string basePath = $"{_logFilePath}/Addon/{module}";
            
            //Check File Path already Create 
            if (!Directory.Exists($"{basePath}")) Directory.CreateDirectory(basePath);
            using (StreamWriter sw = new StreamWriter($"{basePath}/Info_{DateTime.Now.Date:yyyyMMdd}.txt", true))
            {
                await sw.WriteLineAsync($"{DateTime.Now} : {text}");
                if (!string.IsNullOrEmpty(request))
                {
                    await sw.WriteLineAsync($"{request}");
                }
            }
        }

    }
}
