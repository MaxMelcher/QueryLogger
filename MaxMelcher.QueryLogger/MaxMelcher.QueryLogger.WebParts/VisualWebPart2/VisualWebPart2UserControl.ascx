<%@ Assembly Name="$SharePoint.Project.AssemblyFullName$" %>
<%@ Assembly Name="Microsoft.Web.CommandUI, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> 
<%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="asp" Namespace="System.Web.UI" Assembly="System.Web.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" %>
<%@ Import Namespace="Microsoft.SharePoint" %> 
<%@ Register Tagprefix="WebPartPages" Namespace="Microsoft.SharePoint.WebPartPages" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="VisualWebPart2UserControl.ascx.cs" Inherits="MaxMelcher.QueryLogger.WebParts.VisualWebPart2.VisualWebPart2UserControl" %>


<head>
    <script src="/_layouts/15/MaxMelcher.QueryLogger.WebParts/jquery.js"></script>
    <script src="/_layouts/15/MaxMelcher.QueryLogger.WebParts/jquery.signalR-2.0.0.min.js"></script>
    <script src="/_layouts/15/MaxMelcher.QueryLogger.WebParts/mustache.js"></script>
    <script src="/_layouts/15/MaxMelcher.QueryLogger.WebParts/stream_table.min.js"></script>
    <script src="<%= HubUrlScript %>"></script>
</head>

<body>
    
    <script id="template" type="text/html">
        <tr>
            <td>{{record.Timestamp}}</td>
            <td>{{record.Process}}</td>
            <td>{{record.Thread}}</td>
            <td>{{record.Area}}</td>
            <td>{{record.Category}}</td>
            <td>{{record.EventID}}</td>
            <td>{{record.Level}}</td>
            <td>{{record.Message}}</td>
            <td>{{record.Correlation}}</td>
        </tr>
    </script>

    <script language="javascript">
        var connection;
        var hubProxy;
        var data = new Array();
        data.push({"Timestamp": "", "Message": "--- started ---"});

        var template = Mustache.compile($.trim($("#template").html()));

        var view = function (record, index) {
            return template({ record: record, index: index });
        };

        var options = {
            view: view,
        };

        var st;

        $(document).ready(function () {

            st = StreamTable('#table',
                             {
                                 view: view,
                                 per_page: 10,
                                 pagination: { span: 5, next_text: 'Next &rarr;', prev_text: '&larr; Previous' }
                             },
                             null
                             );

            connection = $.connection;
            connection.hub.url = "<%= HubUrlSignalr %>";

            hubProxy = connection.ulsHub;
            hubProxy.client.addMessage = function (message) {
                console.log(message);

                var array = $.makeArray(message);
               
                st.addData(array);
            };

            $.connection.hub.start()
                .done(function () { console.log('Now connected, connection ID=' + $.connection.hub.id); })
                .fail(function () { console.log('Could not Connect!'); });
        });

    </script>


    <table id="table">
        <thead>
            <tr>
                <th>Timestamp</th>
                <th>Process</th>
                <th>Thread</th>
                <th>Area</th>
                <th>Category</th>
                <th>EventID</th>
                <th>Level</th>
                <th>Message</th>
                <th>Correlation</th>
            </tr>
        </thead>
        <tbody>
        </tbody>

    </table>
    
    
</body>