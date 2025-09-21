using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GComm.Integrations.QBD
{
    public class TrackingItem
    {
        [JsonProperty(PropertyName = "sc_ship_id")]
        public int ScShipId { get; set; }
        [JsonProperty(PropertyName = "order_ref")]
        public string OrderRef { get; set; }
        [JsonProperty(PropertyName = "invoice_modified")]
        public bool? InvoiceModified { get; set; }
        [JsonProperty(PropertyName = "tracking_num")]
        public string TrackingNumber { get; set; }
        [JsonProperty(PropertyName = "service")]
        public string Service { get; set; }
        [JsonProperty(PropertyName = "customer_name")]
        public string CustomerName { get; set; }
        [JsonProperty(PropertyName = "shipper")]
        public string Shipper { get; set; }
        [JsonProperty(PropertyName = "shipping_cost")]
        public string ShippingCost { get; set; }
        [JsonProperty(PropertyName = "customer_po")]
        public string CustomerPO { get; set; }
        [JsonProperty(PropertyName = "shipto")]
        public AddressItem ShipTo { get; set; }
        [JsonProperty(PropertyName = "items")]
        public List<LineItem> Items { get; set; }
        [JsonProperty(PropertyName = "qbd_txn_id")]
        public string QBDTransactionID { get; set; }
        [JsonProperty(PropertyName = "qbd_edit_sequence_num")]
        public string QBDEditSequence { get; set; }
    }

    public class AddressItem
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "address")]
        public string Address { get; set; }
        [JsonProperty(PropertyName = "address2")]
        public string Address2 { get; set; }
        [JsonProperty(PropertyName = "zipcode")]
        public string ZipCode { get; set; }
        [JsonProperty(PropertyName = "city")]
        public string City { get; set; }
        [JsonProperty(PropertyName = "state")]
        public string StateCode { get; set; }
    }

    public class LineItem
    {
        [JsonIgnore]
        public int TxnLineID { get; set; }
        [JsonProperty(PropertyName = "unit_cost")]
        public string UnitCost { get; set; }
        [JsonProperty(PropertyName = "customer_sku")]
        public string CustomerSku { get; set; }
        [JsonProperty(PropertyName = "vendor_linecode")]
        public string VendorLineCode { get; set; }
        [JsonProperty(PropertyName = "qtyreq")]
        public int QuantityRequired { get; set; }
        [JsonProperty(PropertyName = "vendor_partno")]
        public string VendorPartNumber { get; set; }
    }

    public class GetTrackingResponse
    {
        [JsonProperty("unacknowledged")]
        public List<TrackingItem> Unacknowledged { get; set; }
        [JsonProperty("acknowledged")]
        public List<TrackingItem> Acknowledged { get; set; }
    }

    public class AckResponse
    {
        [JsonProperty("unacknowledged")]
        public List<string> Unacknowledged { get; set; }
        [JsonProperty("acknowledged")]
        public List<string> Acknowledged { get; set; }
    }
    
    public class AckOrder
    {
        [JsonProperty(PropertyName = "order_ref")]
        public string InvoiceNumber { get; set; }
        [JsonProperty(PropertyName = "QBD_TxnID")]
        public string QBDTxnID { get; set; }
        [JsonProperty(PropertyName = "QBD_EditSequenceNumber")]
        public string QBDEditSequence { get; set; }
        [JsonProperty(PropertyName = "err_msg")]
        public string ErrorMessage { get; set; }
    }
}