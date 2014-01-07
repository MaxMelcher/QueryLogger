using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;

namespace MaxMelcher.QueryLogger.WebParts.VisualWebPart2
{
    public partial class VisualWebPart2UserControl : UserControl
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

        public string HubUrl { get; set; }

        public string HubUrlSignalr
        {
            get
            {
                return HubUrl + "/signalr";
            }
            
        }
    }
}
