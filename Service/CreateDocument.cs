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
using AddonDemo.Models.LTKAPI.Request;
using WolfApprove.Model;
using WolfApprove.Model.CustomClass;
using Newtonsoft.Json.Linq;
using WolfApprove.API2.Controllers.Utils;
using Newtonsoft.Json;
using AddOnGs.Extenstion;
using System.Data.SqlClient;
using System.Data;
using System.Globalization;
using AddonDemo.Models.LTK.Response;

namespace Addon.Service
{
    public class CreateDocument
    {
        private NameValueCollection config;
        public CreateDocument()
        {
            config = Ext.GetAppSetting();
        }
        public void insertMemo(OrderApproval order)
        {
            var logpala = new AddonLog();
            try
            {
                var ConnectionToCenter = config["_ConnectionToCenter"];
                string MAdvanceForm = string.Empty;
                string guids = Guid.NewGuid().ToString().Replace("-", "");

                #region getdatabase
                string Username = "wolf01@ltkintertrading.com";
                string ConectionStringCustomer = string.Empty;
                string strAccount = Username.Contains("@") ? Username.Substring(Username.IndexOf("@")) : string.Empty;

                using (SqlConnection myConnection = new SqlConnection(ConnectionToCenter))
                {
                    string oString = "SELECT * FROM ContactUS WHERE Account = @Account";
                    SqlCommand oCmd = new SqlCommand(oString, myConnection);

                    oCmd.Parameters.Add("@Account", SqlDbType.NVarChar, 256).Value = strAccount;

                    myConnection.Open();
                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            ConectionStringCustomer = oReader["ConnectionString"].ToString();
                        }
                    }
                }
                #endregion

                using (WolfApproveModel db = DBContext.OpenConnection(ConectionStringCustomer))
                {
                    #region setmasterdata
                    var MasterDocCode = db.MSTMasterDatas.Where(x => x.MasterType.ToLower() == "addonltk").FirstOrDefault();
                    if (MasterDocCode == null)
                    {
                        logpala.LogInfo("ReceivedDataCreateDocument", "ไม่พบ MasterData สำหรับ addonltk");
                        throw new Exception("ไม่พบ MasterData สำหรับ addonltk");
                    }
                    var sTemplate = db.MSTTemplates.Where(x => x.DocumentCode == MasterDocCode.Value1).FirstOrDefault();
                    if (sTemplate == null)
                    {
                        logpala.LogInfo("ReceivedDataCreateDocument", "ไม่พบ Template ที่ตรงกับ DocumentCode : " + MasterDocCode.Value1);
                        throw new Exception("ไม่พบ Template ที่ตรงกับ DocumentCode : " + MasterDocCode.Value1);
                    }

                    List<string> sMaster = MasterDocCode?.Value2?.Split('|')?.ToList() ?? new List<string>();
                    MSTCompany Company = new MSTCompany();
                    int CompanyId = 1;
                    if (sMaster.Count > 2)
                    {
                        CompanyId = Convert.ToInt32(sMaster[2]);
                        Company = db.MSTCompanies.Where(x => x.CompanyId == CompanyId).FirstOrDefault();
                        if (Company == null)
                        {
                            logpala.LogInfo("ReceivedDataCreateDocument", "ไม่พบข้อมูล CompanyId : " + CompanyId);
                            throw new Exception("ไม่พบข้อมูล CompanyId : " + CompanyId);
                        }
                    }
                    else
                    {
                        Company = db.MSTCompanies.Where(x => x.CompanyCode == order.Company).FirstOrDefault();
                        if (Company == null)
                        {
                            logpala.LogInfo("ReceivedDataCreateDocument", "ไม่พบข้อมูล Company : " + order.Company);
                            throw new Exception("ไม่พบข้อมูล Company : " + order.Company);
                        }
                    }
                    string Creator = sMaster.Count > 0 ? sMaster[0] : string.Empty;
                    string Requestor = sMaster.Count > 1 ? sMaster[1] : string.Empty;

                    #endregion

                    #region createmavan
                    if (sTemplate != null)
                    {
                        JObject jsonAdvanceForm = new JObject();
                        jsonAdvanceForm = JsonUtils.createJsonObject(sTemplate.AdvanceForm);
                        JArray itemsArray = (JArray)jsonAdvanceForm["items"];
                        foreach (JObject jItems in itemsArray)
                        {
                            JArray layoutArray = (JArray)jItems["layout"];
                            if (layoutArray == null) continue;
                            foreach (JObject layoutItem in layoutArray)
                            {
                                string label = (string)layoutItem["template"]?["label"];
                                JObject jData = (JObject)layoutItem["data"];
                                if (jData == null || string.IsNullOrEmpty(label)) continue;
                                switch (label)
                                {
                                    case "Doc Number":
                                        if (!string.IsNullOrEmpty(order.DocNumber))
                                            jData["value"] = order.DocNumber;
                                        break;
                                    case "ROA Reason":
                                        if (!string.IsNullOrEmpty(order.ROAReason))
                                            jData["value"] = order.ROAReason;
                                        break;

                                    case "Salesman Code":
                                        if (!string.IsNullOrEmpty(order.SalesmanCode))
                                            jData["value"] = order.SalesmanCode;
                                        break;

                                    case "Customer Code":
                                        if (!string.IsNullOrEmpty(order.CustomerCode))
                                            jData["value"] = order.CustomerCode;
                                        break;

                                    case "Address":
                                        if (!string.IsNullOrEmpty(order.Address))
                                            jData["value"] = order.Address;
                                        break;

                                    case "Tax Number":
                                        if (!string.IsNullOrEmpty(order.TaxNumber))
                                            jData["value"] = order.TaxNumber;
                                        break;

                                    case "Doc Create Time":
                                        if (!string.IsNullOrEmpty(order.DocCreateTime) &&
                                            DateTime.TryParseExact(order.DocCreateTime, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime createTime))
                                        {
                                            jData["value"] = createTime.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
                                        }
                                        break;

                                    case "Attactment":
                                        if (!string.IsNullOrEmpty(order.Attactment))
                                            jData["value"] = $"{order.DocNumber}|{order.Attactment}";
                                        break;

                                    case "Doc Status":
                                        if (!string.IsNullOrEmpty(order.DocStatus))
                                            jData["value"] = order.DocStatus;
                                        break;

                                    case "ROA Datetime":
                                        jData["value"] = order.ROADatetime.ToString("dd/MM/yyyy");
                                        break;

                                    case "Salesman Name":
                                        if (!string.IsNullOrEmpty(order.SalesmanName))
                                            jData["value"] = order.SalesmanName;
                                        break;

                                    case "Customer Name":
                                        if (!string.IsNullOrEmpty(order.CustomerName))
                                            jData["value"] = order.CustomerName;
                                        break;

                                    case "Telephone Number":
                                        if (!string.IsNullOrEmpty(order.TelephoneNumber))
                                            jData["value"] = order.TelephoneNumber;
                                        break;

                                    case "Grand Total":
                                        jData["value"] = order.Total_GrandTotal.ToString("N3");
                                        break;

                                    case "Doc Date":
                                        if (!string.IsNullOrEmpty(order.DocDate) &&
                                            DateTime.TryParseExact(order.DocDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime docDate))
                                        {
                                            jData["value"] = docDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
                                        }
                                        break;
                                }
                            } 
                        }
                        MAdvanceForm = JsonConvert.SerializeObject(jsonAdvanceForm);
                    }
                    #endregion

                    #region insertmemo 
                    TRNMemo Memo = new TRNMemo();
                    ViewEmployee viewCREATOR = new ViewEmployee();
                    ViewEmployee viewREQUESTOR = new ViewEmployee();
                    if (!string.IsNullOrEmpty(Creator))
                    {
                        viewCREATOR = db.Database.SqlQuery<ViewEmployee>("Select * from dbo.ViewEmployee").Where(x => x.Email == Creator).FirstOrDefault();
                        if (viewCREATOR == null)
                        {
                            logpala.LogInfo("ReceivedDataCreateDocument", "ไม่พบ Creator Email : " + Creator);
                            throw new Exception("ไม่พบ Creator Email : " + Creator);
                        }
                    }
                    else
                    {
                        viewCREATOR = db.Database.SqlQuery<ViewEmployee>("Select * from dbo.ViewEmployee").Where(x => x.Email == order.Requester).FirstOrDefault();
                        if (viewCREATOR == null)
                        {
                            logpala.LogInfo("ReceivedDataCreateDocument", "ไม่พบ Creator Email : " + order.Requester);
                            throw new Exception("ไม่พบ Creator Email : " + order.Requester);
                        }
                    }
                    if (!string.IsNullOrEmpty(Requestor))
                    {
                        viewREQUESTOR = db.Database.SqlQuery<ViewEmployee>("Select * from dbo.ViewEmployee").Where(x => x.Email == Requestor).FirstOrDefault();
                        if (viewREQUESTOR == null)
                        {
                            logpala.LogInfo("ReceivedDataCreateDocument", "ไม่พบ Creator Email : " + Requestor);
                            throw new Exception("ไม่พบ Requestor Email : " + Requestor);
                        }
                    }
                    else
                    {
                        viewREQUESTOR = db.Database.SqlQuery<ViewEmployee>("Select * from dbo.ViewEmployee").Where(x => x.Email == order.Requester).FirstOrDefault();
                        if (viewREQUESTOR == null)
                        {
                            logpala.LogInfo("ReceivedDataCreateDocument", "ไม่พบ Creator Email : " + order.Requester);
                            throw new Exception("ไม่พบ Requestor Email : " + order.Requester);
                        }
                    }
                    Memo.StatusName = "Draft";
                    Memo.CreatedDate = DateTime.Now;
                    Memo.CreatedBy = viewCREATOR.NameEn;
                    Memo.CreatorId = viewCREATOR.EmployeeId;
                    Memo.CNameTh = viewCREATOR.NameTh;
                    Memo.CNameEn = viewCREATOR.NameEn;
                    Memo.CPositionId = viewCREATOR.PositionId;
                    Memo.CPositionTh = viewCREATOR.PositionNameTh;
                    Memo.CPositionEn = viewCREATOR.PositionNameEn;
                    Memo.CDepartmentId = viewCREATOR.DepartmentId;
                    Memo.CDepartmentTh = viewCREATOR.DepartmentNameTh;
                    Memo.CDepartmentEn = viewCREATOR.DepartmentNameEn;
                    Memo.CcPerson = sTemplate.CcId;

                    Memo.RequesterId = viewREQUESTOR.EmployeeId;
                    Memo.RNameTh = viewREQUESTOR.NameTh;
                    Memo.RNameEn = viewREQUESTOR.NameEn;
                    Memo.RPositionId = viewREQUESTOR.PositionId;
                    Memo.RPositionTh = viewREQUESTOR.PositionNameTh;
                    Memo.RPositionEn = viewREQUESTOR.PositionNameEn;
                    Memo.RDepartmentId = viewREQUESTOR.DepartmentId;
                    Memo.RDepartmentTh = viewREQUESTOR.DepartmentNameTh;
                    Memo.RDepartmentEn = viewREQUESTOR.DepartmentNameEn;

                    Memo.ModifiedDate = DateTime.Now;
                    Memo.ModifiedBy = "Administrator";
                    Memo.TemplateId = sTemplate.TemplateId;
                    Memo.TemplateName = sTemplate.TemplateName;
                    Memo.GroupTemplateName = sTemplate.GroupTemplateName;
                    Memo.RequestDate = DateTime.Now;
                    Memo.CompanyId = Company.CompanyId;
                    Memo.CompanyName = Company.NameEn;

                    Memo.MAdvancveForm = MAdvanceForm;
                    Memo.TAdvanceForm = sTemplate.AdvanceForm;
                    Memo.MemoSubject = order.Company + " : " + order.DocNumber;
                    Memo.TemplateSubject = order.Company + " : " + order.DocNumber;
                    Memo.TemplateDetail = guids;
                    Memo.ProjectID = 0;
                    Memo.DocumentCode = GenControlRunning(viewREQUESTOR, sTemplate.DocumentCode, Memo, db);
                    Memo.DocumentNo = Memo.DocumentCode;
                    db.TRNMemoes.Add(Memo);
                    db.SaveChanges();
                    #endregion

                    #region lineapprove
                    ViewEmployee fristemp = new ViewEmployee();
                    if (order.Items != null && order.Items.Count > 0)
                    {
                        int sequence = 1;
                        foreach (var itemapprove in order.Items)
                        {
                            if (!string.IsNullOrEmpty(itemapprove.Email))
                            {
                                var emp = db.Database.SqlQuery<ViewEmployee>("Select * from dbo.ViewEmployee").Where(x => x.Email == itemapprove.Email).FirstOrDefault();
                                if (emp == null)
                                {
                                    throw new Exception($"ไม่พบ approver Email: {itemapprove.Email}");
                                    logpala.LogInfo("ReceivedDataCreateDocument", $"ไม่พบ approver Email: {itemapprove.Email}");
                                }
                                if (sequence == 1)
                                {
                                    fristemp = emp;
                                }
                                TRNLineApprove approve = new TRNLineApprove();
                                approve.LineApproveId = 0;
                                approve.MemoId = Memo.MemoId;
                                approve.Seq = sequence;
                                approve.EmployeeId = emp.EmployeeId;
                                approve.EmployeeCode = emp.EmployeeCode;
                                approve.NameTh = emp.NameTh;
                                approve.NameEn = emp.NameEn;
                                approve.PositionTH = emp.PositionNameTh;
                                approve.PositionEN = emp.PositionNameEn;
                                approve.SignatureId = 2019;
                                approve.SignatureTh = "อนุมัติ";
                                approve.SignatureEn = "Approved";
                                approve.IsActive = true;
                                db.TRNLineApproves.Add(approve);
                                db.SaveChanges();
                                sequence++;
                            }
                        }
                    }
                    #endregion

                    if (fristemp != null)
                    {
                        var memo = db.TRNMemoes.Where(x => x.MemoId == Memo.MemoId).FirstOrDefault();
                        memo.StatusName = "Wait for Approve";
                        memo.PersonWaitingId = fristemp.EmployeeId;
                        memo.PersonWaiting = fristemp.NameEn;
                        memo.CurrentApprovalLevel = 1;

                        #region History
                        TRNActionHistory his = new TRNActionHistory();
                        his.MemoId = Memo.MemoId;
                        his.ActorId = fristemp.EmployeeId;
                        his.ActorName = fristemp.NameTh;
                        his.StartDate = DateTime.Now;
                        his.ActionProcess = "submit";
                        his.ActionDate = DateTime.Now;
                        his.ActionStatus = "Draft";
                        his.SignatureId = 0;
                        his.Platform = "web";
                        his.ActorNameTh = fristemp.NameTh;
                        his.ActorNameEn = fristemp.NameEn;
                        his.ActorPositionId = fristemp.PositionId;
                        his.ActorPositionNameTh = fristemp.PositionNameTh;
                        his.ActorPositionNameEn = fristemp.PositionNameEn;
                        his.ActorDepartmentId = fristemp.DepartmentId;
                        his.ActorDepartmentNameTh = fristemp.DepartmentNameTh;
                        his.ActorDepartmentNameEn = fristemp.DepartmentNameEn;
                        his.HAdvancveForm = Memo.MAdvancveForm;
                        db.TRNActionHistories.Add(his);
                        #endregion
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                logpala.LogInfo("ReceivedDataCreateDocument", "เกิดข้อผิดพลาดฟังก์ชัน insertMemo : " + ex.StackTrace);
                throw ex;
            }
        }
        public static string GenControlRunning(ViewEmployee Emp, string DocumentCode, TRNMemo objTRNMemo, WolfApproveModel db)
        {
            string TempCode = DocumentCode;
            String sPrefixDocNo = $"{TempCode}-{DateTime.Now.Year.ToString()}-";
            int iRunning = 1;
            List<TRNMemo> temp = db.TRNMemoes.Where(a => a.DocumentNo.ToUpper().Contains(sPrefixDocNo.ToUpper())).ToList();
            if (temp.Count > 0)
            {
                String sLastDocumentNo = temp.OrderBy(a => a.DocumentNo).Last().DocumentNo;
                if (!String.IsNullOrEmpty(sLastDocumentNo))
                {
                    List<String> list_LastDocumentNo = sLastDocumentNo.Split('-').ToList();

                    if (list_LastDocumentNo.Count >= 3)
                    {
                        iRunning = checkDataIntIsNull(list_LastDocumentNo[list_LastDocumentNo.Count - 1]) + 1;
                    }
                }
            }

            String sDocumentNo = $"{sPrefixDocNo}{iRunning.ToString().PadLeft(6, '0')}";


            try
            {

                var mstMasterDataList = db.MSTMasterDatas.Where(a => a.MasterType == "DocNo").ToList();

                if (mstMasterDataList != null)
                    if (mstMasterDataList.Count() > 0)
                    {
                        var getCompany = db.MSTCompanies.Where(a => a.CompanyId == objTRNMemo.CompanyId).ToList();
                        var getDepartment = db.MSTDepartments.Where(a => a.DepartmentId == Emp.DepartmentId).ToList();
                        var getDivision = db.MSTDivisions.Where(a => a.DivisionId == Emp.DivisionId).ToList();

                        string CompanyCode = "";
                        string DepartmentCode = "";
                        string DivisionCode = "";
                        if (getCompany != null)
                            if (!string.IsNullOrWhiteSpace(getCompany.First().CompanyCode)) CompanyCode = getCompany.First().CompanyCode;
                        if (DepartmentCode != null)
                            if (!string.IsNullOrWhiteSpace(getDepartment.First().DepartmentCode)) DepartmentCode = getDepartment.First().DepartmentCode;
                        if (DivisionCode != null)
                        {
                            if (getDivision.Count > 0)
                                if (!string.IsNullOrWhiteSpace(getDivision.First().DivisionCode)) DivisionCode = getDivision.First().DivisionCode;
                        }
                        foreach (var getMaster in mstMasterDataList)
                        {
                            if (!string.IsNullOrWhiteSpace(getMaster.Value2))
                            {
                                var Tid_array = getMaster.Value2.Split('|');
                                string FixDoc = getMaster.Value1;
                                if (Tid_array.Count() > 0)
                                {
                                    if (Tid_array.Contains(objTRNMemo.TemplateId.ToString()))
                                    {
                                        sDocumentNo = DocNoGenerate(FixDoc, TempCode, CompanyCode, DepartmentCode, DivisionCode, db);
                                    }
                                }
                            }
                            else
                            {
                                string FixDoc = getMaster.Value1;
                                sDocumentNo = DocNoGenerate(FixDoc, TempCode, CompanyCode, DepartmentCode, DivisionCode, db);
                            }
                        }

                    }




            }
            catch (Exception ex) { }



            return sDocumentNo;
        }
        public static string DocNoGenerate(string FixDoc, string DocCode, string CCode, string DCode, string DSCode, WolfApproveModel db)
        {
            string sDocumentNo = "";
            int iRunning;
            if (!string.IsNullOrWhiteSpace(FixDoc))
            {
                string y4 = DateTime.Now.ToString("yyyy");
                string y2 = DateTime.Now.ToString("yy");
                string CompanyCode = CCode;
                string DepartmentCode = DCode;
                string DivisionCode = DSCode;
                string FixCode = FixDoc;
                FixCode = FixCode.Replace("[CompanyCode]", CompanyCode);
                FixCode = FixCode.Replace("[DepartmentCode]", DepartmentCode);
                FixCode = FixCode.Replace("[DocumentCode]", DocCode);
                FixCode = FixCode.Replace("[DivisionCode]", DivisionCode);

                FixCode = FixCode.Replace("[YYYY]", y4);
                FixCode = FixCode.Replace("[YY]", y2);
                sDocumentNo = FixCode;
                List<TRNMemo> tempfixDoc = db.TRNMemoes.Where(a => a.DocumentNo.ToUpper().Contains(sDocumentNo.ToUpper())).ToList();


                List<TRNMemo> tempfixDocByYear = db.TRNMemoes.ToList();

                tempfixDocByYear = tempfixDocByYear.FindAll(a => a.DocumentNo != ("Auto Generate") & Convert.ToDateTime(a.RequestDate).Year.ToString().Equals(y4)).ToList();
                if (tempfixDocByYear.Count > 0)
                {
                    tempfixDocByYear = tempfixDocByYear.OrderByDescending(a => a.MemoId).ToList();

                    String sLastDocumentNofix = tempfixDocByYear.First().DocumentNo;
                    if (!String.IsNullOrEmpty(sLastDocumentNofix))
                    {
                        List<String> list_LastDocumentNofix = sLastDocumentNofix.Split('-').ToList();
                        //Arm Edit 2020-05-18 Bug If Prefix have '-' will no running because list_LastDocumentNo.Count > 3

                        if (list_LastDocumentNofix.Count >= 3)
                        {
                            iRunning = checkDataIntIsNull(list_LastDocumentNofix[list_LastDocumentNofix.Count - 1]) + 1;
                            sDocumentNo = $"{sDocumentNo}-{iRunning.ToString().PadLeft(6, '0')}";
                        }
                    }
                }
                else
                {
                    sDocumentNo = $"{sDocumentNo}-{1.ToString().PadLeft(6, '0')}";

                }
            }
            return sDocumentNo;

        }
        public static int checkDataIntIsNull(object Input)
        {
            int Results = 0;
            if (Input != null)
                int.TryParse(Input.ToString().Replace(",", ""), out Results);

            return Results;
        }
        public void SendOrderApprovalConfirmatioon(WolfApprove.Model.TRNMemo memo, WolfApprove.Model.WolfApproveModel context)
        {
            var logpala = new AddonLog();
            var his = context.Database.SqlQuery<TRNActionHistory>("Select * from dbo.TRNActionHistory").Where(x => x.MemoId == memo.MemoId).LastOrDefault();
            var com = context.Database.SqlQuery<MSTCompany>("Select * from dbo.MSTCompany").Where(x => x.CompanyId == memo.CompanyId).FirstOrDefault();

            string DocNumber = string.Empty;
            string Comment = string.Empty;
            string CompanyCode = string.Empty;
            string DocumentStatus = memo.StatusName.Contains("Completed") ? "A" : "R";
            if (his != null)
            {
                Comment = !string.IsNullOrEmpty(his.Comment) ? his.Comment : string.Empty;
            }
            if (com != null)
            {
                CompanyCode = !string.IsNullOrEmpty(com.CompanyCode) ? com.CompanyCode : string.Empty;
            }

            JObject jsonAdvanceForm = JsonUtils.createJsonObject(memo.MAdvancveForm);
            JArray itemsArray = (JArray)jsonAdvanceForm["items"];
            foreach (JObject jItems in itemsArray)
            {
                JArray jLayoutArray = (JArray)jItems["layout"];
                foreach (JObject layoutItem in jLayoutArray)
                {
                    string label = layoutItem["template"]?["label"]?.ToString();
                    JObject jData = (JObject)layoutItem["data"];

                    if (label == "Doc Number" && jData != null)
                    {
                        DocNumber = jData["value"]?.ToString();
                        break;
                    }
                }
            }
            var OrderApprovalConfirmation = new OrderApprovalConfirmationModel
            {
                Company = CompanyCode,
                DocumentNumber = DocNumber,
                DocumentStatus = DocumentStatus,
                DocumentReason = Comment
            };
            logpala.LogInfo("SubmitOrderApprovalConfirmatioon", $"OrderApprovalConfirmation : {JsonConvert.SerializeObject(OrderApprovalConfirmation)}");
            CallApi(OrderApprovalConfirmation, memo);

        }
        public void CallApi(OrderApprovalConfirmationModel data, WolfApprove.Model.TRNMemo memo)
        {
            var logpala = new AddonLog();
            ApiResult resultObj = new ApiResult();
            string apiUrl = "https://isp.ismartsales.com/spider/apiqa/OrderApprovalConfirmation";
            string apiKey = "ca03na188ame03u1d78620de67282882a84";

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);

                string json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    var response = client.PostAsync(apiUrl, content).GetAwaiter().GetResult();
                    var result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    logpala.LogInfo("SubmitOrderApprovalConfirmatioon", $"Status : {response.StatusCode}");
                    logpala.LogInfo("SubmitOrderApprovalConfirmatioon", ($"Response : {result}"));
                    resultObj = JsonConvert.DeserializeObject<ApiResult>(result);
                    InsertLog(memo, data, resultObj);
                }
                catch (TaskCanceledException ex)
                {
                    var resultObjs = new ApiResult()
                    {
                        status = 408,
                        message = ex.Message
                    };
                    InsertLog(memo, data, resultObjs);
                    logpala.LogInfo("SubmitOrderApprovalConfirmatioon", "⏱ Timeout or request was canceled: " + ex.StackTrace);
                }
                catch (HttpRequestException ex)
                {
                    var resultObjs = new ApiResult()
                    {
                        status = 503,
                        message = ex.Message
                    };
                    InsertLog(memo, data, resultObjs);
                    logpala.LogInfo("SubmitOrderApprovalConfirmatioon", "🌐 HTTP error: " + ex.StackTrace);
                }
                catch (Exception ex)
                {
                    var resultObjs = new ApiResult()
                    {
                        status = 500,
                        message = ex.Message
                    };
                    InsertLog(memo, data, resultObjs);
                    logpala.LogInfo("SubmitOrderApprovalConfirmatioon", "💥 Unexpected error: " + ex.StackTrace);
                }
            }
        }
        public void InsertLog(WolfApprove.Model.TRNMemo memo, OrderApprovalConfirmationModel data, ApiResult resultObj)
        {
            #region GetConectionString
            var ConnectionToCenter = config["_ConnectionToCenter"];
            string Username = "wolf01@ltkintertrading.com";
            string ConectionStringCustomer = string.Empty;
            string strAccount = Username.Contains("@") ? Username.Substring(Username.IndexOf("@")) : string.Empty;
            using (SqlConnection myConnection = new SqlConnection(ConnectionToCenter))
            {
                string oString = "SELECT * FROM ContactUS WHERE Account = @Account";
                SqlCommand oCmd = new SqlCommand(oString, myConnection);

                oCmd.Parameters.Add("@Account", SqlDbType.NVarChar, 256).Value = strAccount;

                myConnection.Open();
                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    while (oReader.Read())
                    {
                        ConectionStringCustomer = oReader["ConnectionString"].ToString();
                    }
                }
            }
            #endregion

            string query = @"
            INSERT INTO [dbo].[LogOrderApprovalConfirmation]
            ([Memoid], [DocumentNo], [DocumentNumber], [DocumentStatus], [DocumentReason], [Status], [CompletedDate], [Message])
            VALUES (@Memoid, @DocumentNo, @DocumentNumber, @DocumentStatus, @DocumentReason, @Status, @CompletedDate, @Message)";

            using (var connection = new SqlConnection(ConectionStringCustomer))
            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Memoid", memo.MemoId);
                command.Parameters.AddWithValue("@DocumentNo", memo.DocumentNo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@DocumentNumber", data.DocumentNumber ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@DocumentStatus", data.DocumentStatus ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@DocumentReason", data.DocumentReason ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Status", resultObj.status);
                if (data.DocumentStatus == "A")
                {
                    command.Parameters.AddWithValue("@CompletedDate", memo.CompletedDate.HasValue ? memo.CompletedDate.Value : DateTime.Now);
                }
                else
                {
                    command.Parameters.AddWithValue("@CompletedDate", (object)DBNull.Value);
                }
                command.Parameters.AddWithValue("@Message", resultObj.message ?? (object)DBNull.Value);

                connection.Open();
                command.ExecuteNonQuery();
            }
        }
        public class ApiResult
        {
            public int status { get; set; }
            public string message { get; set; }
        }
    }
}