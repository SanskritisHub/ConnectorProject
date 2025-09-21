using System;
using System.Collections;
using System.Web.Services;
using System.Xml;
using System.Text.RegularExpressions;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Configuration;
using System.Linq;
using System.Net;
using GComm.Integrations.QBD.Request;
using System.Xml.Serialization;
using Newtonsoft.Json;
using System.Collections.Generic;
using log4net.Config;
using log4net;

namespace GComm.Integrations.QBD
{
	/// <summary>
	/// Web Service Namespace="http://developer.intuit.com/"
	/// Web Service Name="WCWebService"
	/// </summary>
	[WebService(
		 Namespace="http://developer.intuit.com/",
		 Name="WCWebService",
 		 Description="Web Service to integrate QBD")]
	public class WCWebService : WebService
	{
		#region GlobalVariables
		private const string NEWLINE = "\r\n";
		private const string USER_SEPARATOR = "_~_";
		private int count=0;
		private ArrayList req=new ArrayList();
		#endregion

		#region Constructor
		public WCWebService()
		{
			initEvLog();
		}
		#endregion

		#region WebMethods
        [WebMethod]
        public string getInteractiveURL(string wcTicket, string sessionID) 
		{
            return string.Empty;
        }

        [WebMethod]
        public string interactiveRejected(string wcTicket, string reason) 
		{
            return string.Empty;
        }

        [WebMethod]
        public string interactiveDone(string wcTicket) 
		{
            return string.Empty;
        }

        [WebMethod]
        public string serverVersion() 
		{
			StringBuilder evLogTxt = new StringBuilder();
			string serverVersion = "2.0.0.1";
            evLogTxt.AppendFormat("WebMethod: serverVersion() has been called by QBWebconnector{0}{1}", NEWLINE, NEWLINE);
			evLogTxt.Append("No Parameters required.");
			evLogTxt.AppendFormat("Returned: {0}", serverVersion);
            return serverVersion;
        }

        [WebMethod]
		public string clientVersion(string strVersion)
		{
			StringBuilder evLogTxt = new StringBuilder();
			evLogTxt.AppendFormat("WebMethod: clientVersion() has been called by QBWebconnector{0}{1}", NEWLINE, NEWLINE);
			evLogTxt.AppendFormat("Parameters received:{0}", NEWLINE);
			evLogTxt.AppendFormat("string strVersion = {0}{1}", strVersion, NEWLINE);
			evLogTxt.Append(NEWLINE);

			string retVal=null;
			double recommendedVersion  = 1.5;
			double supportedMinVersion = 1.0;
			double suppliedVersion=Convert.ToDouble(this.parseForVersion(strVersion));
			evLogTxt.AppendFormat("QBWebConnector version = {0}{1}", strVersion, NEWLINE);
			evLogTxt.AppendFormat("Recommended Version = {0}{1}",recommendedVersion, NEWLINE);
			evLogTxt.AppendFormat("Supported Minimum Version = {0}{1}", supportedMinVersion, NEWLINE);
			evLogTxt.AppendFormat("SuppliedVersion = {0}{1}", suppliedVersion, NEWLINE);
			if(suppliedVersion<recommendedVersion) 
			{
				retVal="W:We recommend that you upgrade your QBWebConnector";
			}
			else if(suppliedVersion<supportedMinVersion)
			{
				retVal="E:You need to upgrade your QBWebConnector";
			}
			evLogTxt.Append(NEWLINE);
			evLogTxt.AppendFormat("Return values: {0}", NEWLINE);
			evLogTxt.AppendFormat("string retVal = {0}", retVal);
			logEvent(evLogTxt.ToString());
			return retVal;
		}

		[WebMethod(EnableSession = true)]
		public string[] authenticate(string strUserName, string strPassword)
		{
			StringBuilder evLogTxt = new StringBuilder();
			evLogTxt.AppendFormat("WebMethod: authenticate() has been called by QBWebconnector{0}","\r\n\r\n");
			evLogTxt.AppendFormat("Parameters received:{0}", NEWLINE);
			evLogTxt.AppendFormat("string strUserName = {0}{1}",strUserName,NEWLINE);
			evLogTxt.AppendFormat("string strPassword = {0}{1}", strPassword, NEWLINE);
			evLogTxt.Append(NEWLINE);

			string[] authReturn = new string[2];
			authReturn[0]= Guid.NewGuid().ToString();
			var creds = ConfigurationManager.AppSettings["credentials"];
			var credList = creds.Split(new[] { USER_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries).Select(t => new NetworkCredential(t.Split(new[] { "_*_" }, StringSplitOptions.RemoveEmptyEntries).First().ToLower(), t.Split(new[] { "_*_" }, StringSplitOptions.RemoveEmptyEntries).Last())).ToList();
			var validUser = credList.SingleOrDefault(t => t.UserName == strUserName.ToLower());
			if(validUser == null)
            {
				authReturn[1] = "nvu";
            }
			if (strPassword.Trim().Equals(validUser.Password))
			{
				authReturn[1] = string.Format(string.Concat(this.Context.Request.Url.Scheme, "://", this.Context.Request.Url.Host, this.Context.Request.Url.LocalPath.Replace("/WCWebService.asmx/authenticate", "/QWCHandler.ashx?token={0}")), authReturn[0]);
				Session[authReturn[0]] = strUserName.ToLower();
			}
			// You could also return "none" to indicate there is no work to do
			// or a company filename in the format C:\full\path\to\company.qbw
			// based on your program logic and requirements.

			evLogTxt.Append(NEWLINE);
			evLogTxt.AppendFormat("Return values: {0}",NEWLINE);
			evLogTxt.AppendFormat("string[] authReturn[0] = {0}{1}",authReturn[0],NEWLINE);
			evLogTxt.AppendFormat("string[] authReturn[1] = {0}",authReturn[1]);
			logEvent(evLogTxt.ToString());
			return authReturn;
		}

		[ WebMethod(Description="This web method facilitates web service to handle connection error between QuickBooks and QBWebConnector",EnableSession=true) ]
		public string connectionError(string ticket, string hresult, string message)
		{
			if (Session["ce_counter"] == null) 
			{
				Session["ce_counter"] = 0;
			}

			StringBuilder evLogTxt = new StringBuilder();
			evLogTxt.AppendFormat("WebMethod: connectionError() has been called by QBWebconnector {0}{1}",NEWLINE,NEWLINE);
			evLogTxt.AppendFormat("Parameters received:",NEWLINE);
			evLogTxt.AppendFormat("string ticket = {0}{1}",ticket,NEWLINE);
			evLogTxt.AppendFormat("string hresult = {0}{1}", hresult, NEWLINE);
			evLogTxt.AppendFormat("string message = {0}{1}", message, NEWLINE);
			evLogTxt.Append(NEWLINE);
			
			string retVal=null;
			// 0x80040400 - QuickBooks found an error when parsing the provided XML text stream.

			const string QB_ERROR_WHEN_PARSING="0x80040400"; 
			// 0x80040401 - Could not access QuickBooks.  
			const string QB_COULDNT_ACCESS_QB="0x80040401";
			// 0x80040402 - Unexpected error. Check the qbsdklog.txt file for possible, additional information. 
			const string QB_UNEXPECTED_ERROR="0x80040402";
			
			if(hresult.Trim().Equals(QB_ERROR_WHEN_PARSING))
			{
				evLogTxt.AppendFormat("HRESULT = {0}{1}", hresult,NEWLINE);
				evLogTxt.AppendFormat("Message = {0}{1}",message,NEWLINE);
				retVal = "DONE";
			}
			else if(hresult.Trim().Equals(QB_COULDNT_ACCESS_QB))
			{
				evLogTxt.AppendFormat("HRESULT = {0}{1}",hresult,NEWLINE);
				evLogTxt.AppendFormat("Message = {0}{1}",message, NEWLINE);
				retVal = "DONE";
			}
			else if(hresult.Trim().Equals(QB_UNEXPECTED_ERROR))
			{
				evLogTxt.AppendFormat("HRESULT = {0}{1}", hresult, NEWLINE);
				evLogTxt.AppendFormat("Message = {0}{1}", message, NEWLINE);
				retVal = "DONE";
			}
			else 
			{ 
				// Depending on various hresults return different value 
				if((int)Session["ce_counter"]==0)
				{
					// Try again with this company file
					evLogTxt.AppendFormat("HRESULT = {0}{1}", hresult, NEWLINE);
					evLogTxt.AppendFormat("Message = {0}{1}", message, NEWLINE);
					evLogTxt.Append("Sending empty company file to try again.");
					retVal = "";
				}
				else
				{
					evLogTxt.AppendFormat("HRESULT = {0}{1}", hresult, NEWLINE);
					evLogTxt.AppendFormat("Message = {0}{1}", message, NEWLINE);
					evLogTxt.Append("Sending DONE to stop.");
					retVal = "DONE";
				}
			}
			evLogTxt.Append(NEWLINE);
			evLogTxt.AppendFormat("Return values: {0}",NEWLINE);
			evLogTxt.AppendFormat("string retVal = {0}{1}", retVal, NEWLINE);
			logEvent(evLogTxt.ToString());
			Session["ce_counter"] = ((int) Session["ce_counter"]) + 1;
			return retVal;
		}

		[ WebMethod(Description="This web method facilitates web service to send request XML to QuickBooks via QBWebConnector",EnableSession=true) ]
		public string sendRequestXML(string ticket, string strHCPResponse, string strCompanyFileName, string qbXMLCountry, int qbXMLMajorVers, int qbXMLMinorVers)
		{
			if (Session["counter"] == null) 
			{
				Session["counter"] = 0;
			}
			StringBuilder evLogTxt = new StringBuilder();
			evLogTxt.AppendFormat("WebMethod: sendRequestXML() has been called by QBWebconnector {0}{1}",NEWLINE, NEWLINE);
			evLogTxt.AppendFormat("Parameters received:{0}", NEWLINE);
			evLogTxt.AppendFormat("string ticket = {0}{1}", ticket, NEWLINE);
			evLogTxt.AppendFormat("string strHCPResponse = {0}{1}", strHCPResponse, NEWLINE);
			evLogTxt.AppendFormat("string strCompanyFileName = {0}{1}", strCompanyFileName, NEWLINE);
			evLogTxt.AppendFormat("string qbXMLCountry = {0}{1}", qbXMLCountry, NEWLINE);
			evLogTxt.AppendFormat("int qbXMLMajorVers = {0}{1}", qbXMLMajorVers, NEWLINE);
			evLogTxt.AppendFormat("int qbXMLMinorVers = {0}{1}", qbXMLMinorVers, NEWLINE);
			evLogTxt.Append(NEWLINE);

			var data = GetInventory();
			
			var req=buildRequest(data);
			string request = string.Empty;
			
			int total = req.Count;
			count=Convert.ToInt32(Session["counter"]);

            if (count < total)
            {
				request = req[count].ToString();
                evLogTxt.AppendFormat("sending request no = {0}{1}", (count + 1), NEWLINE);
                Session["counter"] = ((int)Session["counter"]) + 1;
            }
            else
            {
                count = 0;
                Session["counter"] = 0;
                request = string.Empty;
            }
			//request = "<?xml version=\"1.0\"?><?qbxml version=\"13.0\"?><QBXML><QBXMLMsgsRq onError=\"stopOnError\"><InvoiceModRq requestID=\"2S2WFP3W2WH\"><InvoiceMod><TxnID>2802-1634728599</TxnID><EditSequence>1634731760</EditSequence><CustomerRef><FullName>AUTOZONE VDP</FullName></CustomerRef><RefNumber>2S2WFP3W2WH</RefNumber><ShipAddress><Addr1>AUTOZONE LEXINGTON</Addr1><Addr2>77 RUSH ST</Addr2><City>LEXINGTON</City><State>TN</State><PostalCode>38351</PostalCode></ShipAddress><PONumber>potest2inv22</PONumber><FOB>BFL</FOB><ShipMethodRef><FullName>Federal Express</FullName></ShipMethodRef><InvoiceLineMod><TxnLineID>-1</TxnLineID><ItemRef><FullName>CLS1565-4</FullName></ItemRef><Quantity>1</Quantity><Rate>99.00</Rate><Other2>MPC50004DL-4</Other2></InvoiceLineMod><InvoiceLineMod><TxnLineID>-1</TxnLineID><ItemRef><FullName>TRACKING</FullName></ItemRef><Other2>784008062295</Other2></InvoiceLineMod></InvoiceMod><IncludeRetElement>RefNumber</IncludeRetElement><IncludeRetElement>EditSequence</IncludeRetElement><IncludeRetElement>TxnID</IncludeRetElement></InvoiceModRq></QBXMLMsgsRq></QBXML>";

			//request = "<?xml version=\"1.0\"?><?qbxml version=\"13.0\"?><QBXML><QBXMLMsgsRq onError=\"stopOnError\"><InvoiceAddRq requestID=\"2S2WFP3W2WH\"><InvoiceAdd><CustomerRef><FullName>AUTOZONE VDP</FullName></CustomerRef><RefNumber>2S2WFP3W2WH</RefNumber><ShipAddress><Addr1>AUTOZONE LEXINGTON</Addr1><Addr2>77 RUSH ST</Addr2><City>LEXINGTON</City><State>TN</State><PostalCode>38351</PostalCode></ShipAddress><PONumber>potest2inv22</PONumber><FOB>BFL</FOB><InvoiceLineAdd><ItemRef><FullName>CLS1808</FullName></ItemRef><Quantity>1</Quantity><Rate>31.63</Rate><Other2>C50016DL</Other2></InvoiceLineAdd><InvoiceLineAdd><ItemRef><FullName>CLS1933</FullName></ItemRef><Quantity>2</Quantity><Rate>28.12</Rate><Other2>C50054DL</Other2></InvoiceLineAdd><InvoiceLineAdd><ItemRef><FullName>TRACKING</FullName></ItemRef><Other2>784008062295</Other2></InvoiceLineAdd></InvoiceAdd><IncludeRetElement>RefNumber</IncludeRetElement><IncludeRetElement>EditSequence</IncludeRetElement><IncludeRetElement>TxnID</IncludeRetElement></InvoiceAddRq></QBXMLMsgsRq></QBXML>";
			evLogTxt.Append(NEWLINE);
			evLogTxt.AppendFormat("Return values: {0}",NEWLINE);
			evLogTxt.AppendFormat("string request = {0}{1}",request,NEWLINE);
			logEvent(evLogTxt.ToString());
			return request;
		}
		
		[ WebMethod(Description="This web method facilitates web service to receive response XML from QuickBooks via QBWebConnector",EnableSession=true) ]
		public int receiveResponseXML(string ticket, string response, string hresult, string message)
		{
			StringBuilder evLogTxt = new StringBuilder();
			evLogTxt.AppendFormat("WebMethod: receiveResponseXML() has been called by QBWebconnector{0}{1}", NEWLINE, NEWLINE);
			evLogTxt.AppendFormat("Parameters received:{0}", NEWLINE);
			evLogTxt.AppendFormat("string ticket = {0}{1}", ticket, NEWLINE);
			evLogTxt.AppendFormat("string response = {0}{1}", response, NEWLINE);
			evLogTxt.AppendFormat("string hresult = {0}{1}", hresult, NEWLINE);
			evLogTxt.AppendFormat("string message = ", message, NEWLINE);
			evLogTxt.AppendFormat(NEWLINE);

			int retVal=0;
			if(!hresult.ToString().Equals(""))
			{
				// if there is an error with response received, web service could also return a -ve int		
				evLogTxt.AppendFormat("HRESULT = {0}{1}", hresult, NEWLINE);
				evLogTxt.AppendFormat("Message = {0}{1}", message, NEWLINE);
				retVal =-101;
			}
			else
			{
				evLogTxt.AppendFormat("Length of response received = ", response.Length, NEWLINE);

                var req = buildResponse(response);
                if (req != null && req.Count > 0)
                {
                    int total = req.Count;
                    int count = Convert.ToInt32(Session["counter"]);

                    int percentage = (count * 100) / total;
                    if (percentage >= 100)
                    {
                        count = 0;
                        Session["counter"] = 0;
                    }
                    retVal = percentage;
                }
                else
                {
                    retVal = 0;
                }
			}
			evLogTxt.Append(NEWLINE);
			evLogTxt.AppendFormat("Return values: {0}", NEWLINE);
			evLogTxt.AppendFormat("int retVal= {0}{1}", retVal, NEWLINE);
			logEvent(evLogTxt.ToString());
			return retVal;
		}

		[WebMethod]
		public string getLastError(string ticket)
		{
			StringBuilder evLogTxt = new StringBuilder();
			evLogTxt.AppendFormat("WebMethod: getLastError() has been called by QBWebconnector{0}{1}", NEWLINE, NEWLINE);
			evLogTxt.AppendFormat("Parameters received:{0}", NEWLINE);
			evLogTxt.AppendFormat("string ticket = {0}{1}", ticket, NEWLINE);
			evLogTxt.Append(NEWLINE);

			int errorCode=0;
			string retVal=null;
			if(errorCode==-101)
			{
				retVal="QuickBooks was not running!";
			}
			else
			{
				retVal="Error!";
			}
			evLogTxt.Append(NEWLINE);
			evLogTxt.AppendFormat("Return values: {0}", NEWLINE);
			evLogTxt.AppendFormat("int retVal= {0}{1}", retVal, NEWLINE);
			logEvent(evLogTxt.ToString());
			return retVal;
		}

		[WebMethod]
		public string closeConnection(string ticket) 
		{
			StringBuilder evLogTxt = new StringBuilder();
			evLogTxt.AppendFormat("WebMethod: closeConnection() has been called by QBWebconnector{0}{1}", NEWLINE, NEWLINE);
			evLogTxt.AppendFormat("Parameters received:{0}", NEWLINE);
			evLogTxt.AppendFormat("string ticket = {0}{1}", ticket, NEWLINE);
			evLogTxt.Append(NEWLINE);
            string retVal = "OK";

            evLogTxt.Append(NEWLINE);
			evLogTxt.AppendFormat("Return values: {0}", NEWLINE);
			evLogTxt.AppendFormat("int retVal= {0}{1}", retVal, NEWLINE);
			logEvent(evLogTxt.ToString());
			return retVal;
		}

		private GetTrackingResponse GetInventory()
        {
			logEvent("Entering method GetInventory");
			try
			{
				ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback((a, b, c, d) => true);
				WebClient webClient = new WebClient();
				webClient.Headers.Add("Authorization", ConfigurationManager.AppSettings["FMIntegrationCredentials"]);
				var data = webClient.DownloadData(ConfigurationManager.AppSettings["FMAPIUrl"]);
				var jsonData = UTF8Encoding.UTF8.GetString(data);
				logEvent(string.Format("Got response: {1} from EndPoint :{0}", ConfigurationManager.AppSettings["FMAPIUrl"], jsonData));
				var trackingResponse = JsonConvert.DeserializeObject<GetTrackingResponse>(jsonData);
				logEvent("Exiting method GetInventory");
				return trackingResponse;
			}
            catch(Exception ex)
            {
				logEvent(string.Format("Error Occurred in GetInventory {0}", ex));
				throw;
            }
        }

		private AckResponse AcknowledgeInventory(string invoiceNumber, string txnNumber, string editSequence, string errorMessage)
        {
			logEvent(string.Format("Entering method AcknowledgeInventory with Invoice Number : {0}", invoiceNumber));
			try
			{
				ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback((a, b, c, d) => true);
				WebClient webClient = new WebClient();
				webClient.Headers.Add("Authorization", ConfigurationManager.AppSettings["FMIntegrationCredentials"]);
				List<AckOrder> ackOrders = new List<AckOrder>
				{   new AckOrder
					{
						InvoiceNumber = invoiceNumber,
						QBDTxnID = txnNumber,
						QBDEditSequence = editSequence,
						ErrorMessage = errorMessage
					}
				};
				var data = webClient.UploadString(ConfigurationManager.AppSettings["FMAPIUrl"], JsonConvert.SerializeObject(ackOrders));
				logEvent(string.Format("Got response: {1} from EndPoint :{0}", ConfigurationManager.AppSettings["FMAPIUrl"], data));
				logEvent("Exiting method AcknowledgeInventory");
				return JsonConvert.DeserializeObject<AckResponse>(data);
			}
			catch(Exception ex)
            {
				logEvent(string.Format("Error Occurred in AcknowledgeInventory {0}", ex));
				throw;
			}
        }

		#endregion

		#region UtilityMethods
		private void initEvLog()
		{
			try
			{
				XmlConfigurator.Configure();
			}
			catch{};
		}

		private void logEvent(string logText)
		{
			try
			{
				var logger = LogManager.GetLogger(typeof(WCWebService));
				if(logger != null && logger.IsInfoEnabled)
                {
					logger.Info(logText);
                }
			}
			catch{};
		}

		public ArrayList buildResponse(string response)
		{
			logEvent(string.Format("Called buildResponse with {0}", response));
			XmlDocument responseXmlDoc = new XmlDocument();
			responseXmlDoc.LoadXml(response);
			XmlNodeList invoiceAddRespList = responseXmlDoc.GetElementsByTagName("InvoiceAddRs");
			XmlNodeList invoiceModRespList = responseXmlDoc.GetElementsByTagName("InvoiceModRs");
			if ((invoiceAddRespList != null && invoiceAddRespList.Count == 1) || (invoiceModRespList != null && invoiceModRespList.Count == 1))
			{
				XmlNode responseNode = null;
				if (invoiceAddRespList.Count == 1)
				{
					responseNode = invoiceAddRespList.Item(0);
				} 
				else if(invoiceModRespList.Count == 1)
                {
					responseNode = invoiceModRespList.Item(0);
				}
				if (responseNode != null)
				{
					XmlAttributeCollection rsAttributes = responseNode.Attributes;
					string statusCode = rsAttributes.GetNamedItem("statusCode").Value;
					string statusSeverity = rsAttributes.GetNamedItem("statusSeverity").Value;
					string statusMessage = rsAttributes.GetNamedItem("statusMessage").Value;
					string requestID = rsAttributes.GetNamedItem("requestID").Value;

					//status code = 0 all OK, > 0 is warning
					if (Convert.ToInt32(statusCode) >= 0 && statusSeverity != "Error")
					{
						XmlNodeList invoiceList = responseNode.SelectNodes("//InvoiceRet");
						for (int i = 0; i < invoiceList.Count; i++)
						{
							XmlNode invoiceItem = invoiceList.Item(i);
							if (ProcessInvoiceResponse(invoiceItem))
							{
								var data = new ArrayList
								{
									true
								};
								return data;
							}
						}
					}
					else
					{
						var errorMessage = statusMessage;
						var ackResponse = AcknowledgeInventory(requestID, string.Empty, string.Empty, errorMessage);
					}
				}
			}
			return req;
		}

		bool ProcessInvoiceResponse(XmlNode invoiceResp)
		{
			if (invoiceResp == null) 
				return false;
			logEvent(string.Format("Called ProcessInvoiceResponse with Argument {0}", invoiceResp.OuterXml));
			var invoiceEditSequenceNode = invoiceResp.SelectSingleNode("./EditSequence");
			var invoiceTxnIDNode = invoiceResp.SelectSingleNode("./TxnID");
			var invoiceNumberNode = invoiceResp.SelectSingleNode("./RefNumber");
			if (invoiceNumberNode != null && !string.IsNullOrWhiteSpace(invoiceNumberNode.InnerText) && invoiceTxnIDNode != null && !string.IsNullOrWhiteSpace(invoiceTxnIDNode.InnerText) && invoiceEditSequenceNode != null && !string.IsNullOrWhiteSpace(invoiceEditSequenceNode.InnerText))
			{
				string txnID = invoiceTxnIDNode.InnerText;
				string editSequence = invoiceEditSequenceNode.InnerText;
				string txnNumber = invoiceNumberNode.InnerText;

				var ackResponse = AcknowledgeInventory(txnNumber, txnID, editSequence, string.Empty);
				if(ackResponse != null && ackResponse.Acknowledged.Any(t => t == txnNumber))
                {
					logEvent(string.Format("AcknowledgeInventory successful for {0}", txnNumber));
					return true;
                }
			}
			logEvent(string.Format("AcknowledgeInventory failed for {0}", invoiceResp.OuterXml));
			return false;
		}

		public ArrayList buildRequest(GetTrackingResponse data) 
		{
			string strRequestXML = string.Empty;
			XmlDocument inputXMLDoc = null;
			
			// InvoiceAddRq
			inputXMLDoc = new XmlDocument();
			inputXMLDoc.AppendChild(inputXMLDoc.CreateXmlDeclaration("1.0",null, null));
			inputXMLDoc.AppendChild(inputXMLDoc.CreateProcessingInstruction("qbxml", "version=\"13.0\""));
			
			var qbXML = inputXMLDoc.CreateElement("QBXML");
			inputXMLDoc.AppendChild(qbXML);
			QBXMLMsgsRq qBXMLMsgsRq = new QBXMLMsgsRq();
			qBXMLMsgsRq.onError = QBXMLMsgsRqOnError.stopOnError;
			if (data != null && data.Unacknowledged != null && data.Unacknowledged.Any())
			{
				List<InvoiceAddRqType> listAdd = new List<InvoiceAddRqType>();
				List<InvoiceModRqType> listMod = new List<InvoiceModRqType>();
				var recordsToSend = 10;
				int.TryParse(ConfigurationManager.AppSettings["RecordsToSendInBatch"], out recordsToSend);
				foreach (TrackingItem orderItem in data.Unacknowledged.Take(recordsToSend))
				{
					InvoiceAddRqType invoiceAddRq = new InvoiceAddRqType();
					InvoiceModRqType invoiceModRq = new InvoiceModRqType();
					if (!orderItem.InvoiceModified.GetValueOrDefault(false))
					{
						invoiceAddRq.requestID = orderItem.OrderRef;
						invoiceAddRq.InvoiceAdd = new InvoiceAdd
						{
							CustomerRef = new CustomerRef
							{
								FullName = orderItem.CustomerName
							},
							RefNumber = orderItem.OrderRef,
							PONumber = orderItem.CustomerPO,
							FOB = "BFL",
							Items = orderItem.Items.Select(t => new InvoiceLineAdd
							{
								Quantity = t.QuantityRequired.ToString(),
								Item = t.UnitCost,
								ItemElementName = ItemChoiceType5.Rate,
								ItemRef = new ItemRef
								{
									FullName = t.VendorPartNumber,
								},
								Other2 = t.CustomerSku,
							}).ToArray(),
						};
						if (orderItem.ShipTo != null)
						{
							invoiceAddRq.InvoiceAdd.ShipAddress = new ShipAddress
							{
								Addr1 = orderItem.ShipTo.Name,
								Addr2 = orderItem.ShipTo.Address,
								Addr3 = orderItem.ShipTo.Address2,
								City = orderItem.ShipTo.City,
								State = orderItem.ShipTo.StateCode,
								PostalCode = orderItem.ShipTo.ZipCode
							};
						}
						if (!string.IsNullOrWhiteSpace(orderItem.TrackingNumber))
						{
							var items = invoiceAddRq.InvoiceAdd.Items.Cast<InvoiceLineAdd>().ToList();
							items.Add(new InvoiceLineAdd
							{
								ItemRef = new ItemRef
								{
									FullName = "TRACKING",
								},
								Other2 = orderItem.TrackingNumber
							});
							invoiceAddRq.InvoiceAdd.Items = items.ToArray();
						}
						invoiceAddRq.IncludeRetElement = new string[] { "RefNumber", "TxnID", "EditSequence" };
						listAdd.Add(invoiceAddRq);
					}
                    else
                    {
						invoiceModRq.requestID = orderItem.OrderRef;
						invoiceModRq.InvoiceMod = new InvoiceMod
						{
							TxnID = orderItem.QBDTransactionID,
							EditSequence = orderItem.QBDEditSequence,
							CustomerRef = new CustomerRef
							{
								FullName = orderItem.CustomerName
							},
							RefNumber = orderItem.OrderRef,
							PONumber = orderItem.CustomerPO,
							FOB = "BFL",
							Items = orderItem.Items.Select(t => new InvoiceLineMod
							{
								TxnLineID = decimal.MinusOne.ToString(),
								Quantity = t.QuantityRequired.ToString(),
								Item = t.UnitCost,
								ItemElementName = ItemChoiceType8.Rate,
								ItemRef = new ItemRef
								{
									FullName = t.VendorPartNumber,
								},
								Other2 = t.CustomerSku,
							}).ToArray(),
						};
						if (orderItem.ShipTo != null)
						{
							invoiceModRq.InvoiceMod.ShipAddress = new ShipAddress
							{
								Addr1 = orderItem.ShipTo.Name,
								Addr2 = orderItem.ShipTo.Address,
								Addr3 = orderItem.ShipTo.Address2,
								City = orderItem.ShipTo.City,
								State = orderItem.ShipTo.StateCode,
								PostalCode = orderItem.ShipTo.ZipCode
							};
						}
						if (!string.IsNullOrWhiteSpace(orderItem.TrackingNumber))
						{
							var items = invoiceModRq.InvoiceMod.Items.Cast<InvoiceLineMod>().ToList();
							items.Add(new InvoiceLineMod
							{
								TxnLineID = decimal.MinusOne.ToString(),
								ItemRef = new ItemRef
								{
									FullName = "TRACKING",
								},
								Other2 = orderItem.TrackingNumber
							});
							invoiceModRq.InvoiceMod.Items = items.ToArray();
						}
						invoiceModRq.IncludeRetElement = new string[] { "RefNumber", "TxnID", "EditSequence" };
						listMod.Add(invoiceModRq);
					}
				}
				qBXMLMsgsRq.Items = listAdd.Any() ? listAdd.ToArray() : listMod.Any() ?listMod.ToArray() : new object[0];
				XmlSerializer xmlSerializer = new XmlSerializer(typeof(QBXMLMsgsRq));
				var ms = new MemoryStream();
				XmlDocument doc = new XmlDocument();
				xmlSerializer.Serialize(ms, qBXMLMsgsRq);
				ms.Seek(0L, SeekOrigin.Begin);
				doc.LoadXml(UTF8Encoding.UTF8.GetString(ms.ToArray()));
				qbXML.InnerXml = doc.DocumentElement.OuterXml;
			}
			strRequestXML = Regex.Replace(inputXMLDoc.OuterXml, @"(xmlns:?[^=]*=[""][^""]*[""])", string.Empty,
				RegexOptions.IgnoreCase | RegexOptions.Multiline);
			req.Add(strRequestXML);
			// Clean up
			strRequestXML="";
			inputXMLDoc=null;
			qbXML=null;

			return req;
		}

		private string parseForVersion(string input){
			// This method is created just to parse the first two version components
			// out of the standard four component version number:
			// <Major>.<Minor>.<Release>.<Build>
			// 
			// As long as you get the version in right format, you could use
			// any algorithm here. 
			string retVal="";
			string major="";
			string minor="";
			Regex version = new Regex(@"^(?<major>\d+)\.(?<minor>\d+)(\.\w+){0,2}$", RegexOptions.Compiled);
			Match versionMatch= version.Match(input);
			if (versionMatch.Success){
				major= versionMatch.Result("${major}");							
				minor= versionMatch.Result("${minor}");							
				retVal=major+"."+minor;
			}
			else{
				retVal=input;
			}
			return retVal;
		}
		
		#endregion

	}
}
