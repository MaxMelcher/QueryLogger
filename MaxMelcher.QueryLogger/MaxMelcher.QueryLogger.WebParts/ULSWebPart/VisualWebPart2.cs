using System;
using System.ComponentModel;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;

namespace MaxMelcher.QueryLogger.WebParts.VisualWebPart2
{
    [ToolboxItemAttribute(false)]
    public class VisualWebPart2 : WebPart
    {
        // Visual Studio might automatically update this path when you change the Visual Web Part project item.
        private const string _ascxPath = @"~/_CONTROLTEMPLATES/15/MaxMelcher.QueryLogger.WebParts/ULSWebPart/VisualWebPart2UserControl.ascx";

        protected override void CreateChildControls()
        {
            VisualWebPart2UserControl control = (VisualWebPart2UserControl)Page.LoadControl(_ascxPath);
            control.HubUrl = HubUrl;
            Controls.Add(control);
        }

        [WebBrowsable(true),
        WebDisplayName("Hub Url"),
        WebDescription("Enter the hub url here, w/o /signalr"),
        Personalizable(PersonalizationScope.Shared),
        Category("SignalR"), DefaultValue("http://sharepoint2013:8080/")]
        public string HubUrl { get; set; }

    }
}
