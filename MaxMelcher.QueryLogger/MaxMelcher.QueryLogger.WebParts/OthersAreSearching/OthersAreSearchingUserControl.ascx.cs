using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;

namespace MaxMelcher.QueryLogger.WebParts.OthersAreSearching
{
    public partial class OthersAreSearchingUserControl : UserControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }

        public string HubUrlScript
        {
            get
            {
                return HubUrl + "/signalr/hubs";
            } 
        }

        public string HubUrlSignalr
        {
            get { return HubUrl + "/signalr"; }
        }

        public string HubUrl { get; set; }
    }
}
