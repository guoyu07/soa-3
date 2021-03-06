﻿using ESB.Core.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;

namespace ESB.CallCenter.BasicService
{
    /// <summary>
    /// 异常处理服务
    /// </summary>
    [WebService(Namespace = "http://esb.com/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // 若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消注释以下行。 
    // [System.Web.Script.Services.ScriptService]
    public class ExceptionService : System.Web.Services.WebService
    {
        [WebMethod(Description = "获取到用户的异常信息")]
        public List<ExceptionCoreTb> GetAllExceptionByBusinessID(String bussinesID)
        {
            if (String.IsNullOrEmpty(bussinesID))
                return ExceptionCoreTb.FindAll();
            else
                return ExceptionCoreTb.FindAll(ExceptionCoreTb._.BusinessID, bussinesID);
        }

        [WebMethod(Description = "根据异常编码获取到异常信息")]
        public ExceptionCoreTb GetExceptionByID(String exceptionID)
        {
            return ExceptionCoreTb.FindByExceptionID(exceptionID);
        }

        [WebMethod(Description = "根据异常编码获取到异常信息")]
        public void DeleteExceptionByID(String exceptionID)
        {
            ExceptionCoreTb.FindByExceptionID(exceptionID).Delete();
        }
    }
}
