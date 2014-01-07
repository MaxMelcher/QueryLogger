<%@ Assembly Name="$SharePoint.Project.AssemblyFullName$" %>
<%@ Assembly Name="Microsoft.Web.CommandUI, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register TagPrefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register TagPrefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register TagPrefix="asp" Namespace="System.Web.UI" Assembly="System.Web.Extensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" %>
<%@ Import Namespace="Microsoft.SharePoint" %>
<%@ Register TagPrefix="WebPartPages" Namespace="Microsoft.SharePoint.WebPartPages" Assembly="Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="OthersAreSearchingUserControl.ascx.cs" Inherits="MaxMelcher.QueryLogger.WebParts.OthersAreSearching.OthersAreSearchingUserControl" %>

<script src="/_layouts/15/MaxMelcher.QueryLogger.WebParts/knockout-3.0.0.js"></script>
<script src="/_layouts/15/MaxMelcher.QueryLogger.WebParts/jquery-2.0.3.min.js"></script>
<script src="/_layouts/15/MaxMelcher.QueryLogger.WebParts/jquery.color-2.1.2.min.js"></script>
<script src="/_layouts/15/MaxMelcher.QueryLogger.WebParts/jquery.signalR-2.0.0.min.js"></script>
<script src="<%=HubUrlScript %>"></script>

<style type="text/css">
    
    #wrapper {
        height: 100px;
    }

</style>

<div id="wrapper">
    <div id="ranking" data-bind="foreach: {data: ranking, afterAdd: greenFadeIn, beforeRemove: redFadeOut}">
        <div class="rank">
            <a href="" data-bind="text: Query, attr: { href: href }"></a>(<span data-bind="text:Count"></span>)
        </div>
    </div>
</div>

<script type="text/javascript">

    var model;
    var connection;
    var hubProxy;

    var Query = function(query) {
        this.Query = query;
        this.Count = ko.observable(1);

        this.Count.subscribe(function() {
            model.refresh(true);
        });

        this.href = "#k=" + query;
    };
    
    var ViewModel = function () {
        var self = this;
        
        //contains all entries
        self.list = ko.observableArray([]);

        //a fake refresh option when the count changes
        self.refresh = ko.observable(false);

        //a sorted list
        this.ranking = ko.computed(function () {
            if (!self.list())
                return [];

            //create a dependency to refresh
            self.refresh(false);

            //sort the array
            self.list().sort(function (left, right) { return left.Count() == right.Count() ? 0 : (left.Count() > right.Count() ? -1 : 1); });
            
            //return the first n elements
            return this.list().slice(0, 5);
        }, this);


        self.greenFadeIn = function (element, index, data) {
            if (element.nodeType === 1) {
                $(element).css({ opacity: 0.0, visibility: "visible" }).animate({ opacity: 1.0, backgroundColor: 'green'}, 500).animate({ backgroundColor: 'white' }, 1000);
            }
        };
        
        self.redFadeOut = function (element, index, data) {
            if (element.nodeType === 1) {
                $(element).animate({ opacity: 0.0, backgroundColor: 'red' }, 1000).fadeOut();
            }
        };

        self.AddSearchQuery = function(query) {
           var existing = ko.utils.arrayFilter(self.list(), function (entry) {
                return entry.Query == query;
           });
            
           if (existing.length == 0) {
               self.list.push(new Query(query));
           } else {
               var count = existing[0].Count() + 1;  
               existing[0].Count(count);
           }
        };
    };
    
    $(document).ready(function () {
        try {
            model = new ViewModel();
            ko.applyBindings(model, document.getElementById('wrapper'));

            connection = $.connection;
            connection.hub.url = "<%= HubUrlSignalr %>";

            hubProxy = connection.ulsHub;
            var regexQuery = new RegExp("^.*New request: Query text '(.*)', Query template '.*");
            hubProxy.client.addSearchQuery = function (query) {

                if (query.Message.match(regexQuery)) {
                    console.log(RegExp.$1);
                    model.AddSearchQuery(RegExp.$1);
                }
            };

            $.connection.hub.start()
                .done(function () { console.log('Now connected, connection ID=' + $.connection.hub.id); })
                .fail(function () { console.log('Could not Connect!'); });
        } catch (exception)
             {
            console.error(exception);
        }
    });



</script>