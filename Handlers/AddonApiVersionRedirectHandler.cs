using Addon.Extenstion;
using Addon.Models;
using Addon.Service;
using AddonDemo.Models.LTKAPI.Request;
using AddOnGs.Extenstion;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Results;
using WolfApprove.API2.Controllers.Bean;
using WolfApprove.API2.Extension;
using WolfApprove.Model.CustomClass;

namespace Addon.Handlers
{
    public class AddonApiVersionRedirectHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var logpala = new AddonLog();
                if (request.RequestUri.AbsolutePath.Contains("api/services/submitform"))
                {
                    var content = await request.Content.ReadAsStringAsync();
                    var _hook = JsonConvert.DeserializeObject<AddonFormModel.Form_Model>(content);
                    var context = DBContext.OpenConnection(_hook.memoPage.memoDetail.connectionString);
                    var MasterDocCode = context.MSTMasterDatas.Where(x => x.MasterType.ToLower() == "addonltk").FirstOrDefault();
                    if (MasterDocCode != null && !string.IsNullOrEmpty(MasterDocCode.Value1))
                    {
                        var resultFromMainService = await base.SendAsync(request, cancellationToken);
                        if (resultFromMainService.IsSuccessStatusCode)
                        {
                            var memo = context.TRNMemoes.Where(x => x.MemoId == _hook.memoPage.memoDetail.memoid).FirstOrDefault();
                            if (memo != null)
                            {
                                if (_hook.memoPage.memoDetail.template_code == MasterDocCode.Value1 && (memo.StatusName == Ext.Status._Completed || memo.StatusName == Ext.Status._Rejected) )
                                {
                                    logpala.LogInfo("SubmitOrderApprovalConfirmatioon", "========== Start SubmitOrderApprovalConfirmatioon ==========");
                                    try
                                    {
                                        var service = new CreateDocument();
                                        service.SendOrderApprovalConfirmatioon(memo, context);
                                        logpala.LogInfo("SubmitOrderApprovalConfirmatioon", "========== End SubmitOrderApprovalConfirmatioon ==========");
                                        return resultFromMainService;
                                    }
                                    catch (Exception ex)
                                    {
                                        var Response = new
                                        {
                                            Msg = ex.Message
                                        };
                                        logpala.LogInfo("ReceivedDataCreateDocument", $"Error : {ex.StackTrace}");
                                        logpala.LogInfo("SubmitOrderApprovalConfirmatioon", "========== End SubmitOrderApprovalConfirmatioon ==========");
                                        return request.CreateResponse(HttpStatusCode.InternalServerError, Response);
                                    }
                                }
                            }
                        }
                    }
                }
                if (request.RequestUri.AbsolutePath.Contains("api/AddOn/ReceivedDataCreateDocument"))
                {
                    try
                    {
                        logpala.LogInfo("ReceivedDataCreateDocument", "========== Start ReceivedDataCreateDocument ==========");
                        var content = await request.Content.ReadAsStringAsync();
                        var _hook = JsonConvert.DeserializeObject<ReceivedDataModel>(content);
                        var order = _hook.OrderApproval;
                        if (order != null)
                        {
                            logpala.LogInfo("ReceivedDataCreateDocument", "========== Start InsertMemo ==========", JsonConvert.SerializeObject(order));
                            logpala.LogInfo("ReceivedDataCreateDocument", $"Order_DocNumber : {order.DocNumber}");
                            var service = new CreateDocument();
                            try
                            {
                                service.insertMemo(order);
                            }
                            catch (Exception ex)
                            {
                                var responseEx = new
                                {
                                    ReturnCode = 500,
                                    Msg = "สร้างเอกสารไม่สำเร็จ!",
                                    Company = order.Company ?? "",
                                    DocumentNumber = order.DocNumber ?? ""
                                };
                                logpala.LogInfo("ReceivedDataCreateDocument", $"Error : {ex.StackTrace}");
                                logpala.LogInfo("ReceivedDataCreateDocument", "========== End InsertMemo ==========");
                                logpala.LogInfo("ReceivedDataCreateDocument", "========== End ReceivedDataCreateDocument ==========");
                                return request.CreateResponse(HttpStatusCode.BadRequest, responseEx);
                            }
                            var responseObj = new
                            {
                                ReturnCode = 0,
                                Msg = $"สร้างเอกสารสำเร็จ",
                                Company = order.Company ?? "",
                                DocumentNumber = order.DocNumber ?? ""
                            };
                            logpala.LogInfo("ReceivedDataCreateDocument", "========== End InsertMemo ==========");
                            logpala.LogInfo("ReceivedDataCreateDocument", "========== End ReceivedDataCreateDocument ==========");
                            return request.CreateResponse(HttpStatusCode.OK, responseObj);
                        }
                        else
                        {
                            var emptyResponse = new
                            {
                                ReturnCode = 404,
                                Msg = "ไม่พบข้อมูล!",
                                Company = string.Empty,
                                DocumentNumber = string.Empty
                            };
                            logpala.LogInfo("ReceivedDataCreateDocument", $"NotFound!", JsonConvert.SerializeObject(order));
                            logpala.LogInfo("ReceivedDataCreateDocument", "========== End ReceivedDataCreateDocument ==========");
                            return request.CreateResponse(HttpStatusCode.NotFound, emptyResponse);
                        }
                    }
                    catch (Exception ex)
                    {
                        var Response = new
                        {
                            ReturnCode = 500,
                            Msg = ex.Message,
                            Company = string.Empty,
                            DocumentNumber = string.Empty
                        };
                        logpala.LogInfo("ReceivedDataCreateDocument", $"Error : {ex.StackTrace}");
                        logpala.LogInfo("ReceivedDataCreateDocument", "========== End ReceivedDataCreateDocument ==========");
                        return request.CreateResponse(HttpStatusCode.InternalServerError, Response);
                    }
                }
                else
                {
                    return await base.SendAsync(request, cancellationToken);
                }

                return await base.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                Ext.ErrorLog(ex, "SendAsync");
                throw;
            }
        }
    }
}
