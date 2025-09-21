<%@ WebHandler Language="C#" Class="GComm.Integrations.QBD.QWCHandler" %>

using System;
using System.Web;
using System.Xml;
using System.IO;
using System.Web.SessionState;
namespace GComm.Integrations.QBD
{
    public class QWCHandler : IHttpHandler, IRequiresSessionState
    {

        public void ProcessRequest(HttpContext context)
        {
            var token = context.Request.QueryString["token"];
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }
            if (context.Session[token] != null)
            {
                context.Response.ContentType = "application/xml";
                string qwcFile = context.Server.MapPath("HTTPWebService.qwc");
                var fileData = File.ReadAllText(qwcFile);
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(fileData);
                var nodes = xmlDoc.GetElementsByTagName("UserName");
                nodes[0].InnerText = context.Session[token].ToString();
                var localPath = context.Request.Url.LocalPath;
                var domain = context.Request.Url.Host;
                localPath = localPath.Replace("QWCHandler.ashx", string.Empty);
                nodes = xmlDoc.GetElementsByTagName("AppURL");
                nodes[0].InnerText = string.Format("{0}{1}{2}WCWebService.asmx", context.Request.IsSecureConnection ? "https://" : "http://", domain, localPath);
                nodes = xmlDoc.GetElementsByTagName("AppSupport");
                nodes[0].InnerText = string.Format("{0}{1}{2}WCWebService.asmx?wsdl", context.Request.IsSecureConnection ? "https://" : "http://", domain, localPath);
                nodes = xmlDoc.GetElementsByTagName("OwnerID");
                nodes[0].InnerText = string.Format("{{{0}}}", Guid.NewGuid());
                nodes = xmlDoc.GetElementsByTagName("FileID");
                nodes[0].InnerText = string.Format("{{{0}}}", Guid.NewGuid());
                context.Response.Write(xmlDoc.OuterXml);
            }
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}