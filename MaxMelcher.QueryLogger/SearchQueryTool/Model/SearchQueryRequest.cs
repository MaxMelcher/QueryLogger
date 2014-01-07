using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SearchQueryTool.Model
{
    public class SearchQueryRequest : SearchRequest
    {
        public bool? EnableStemming { get; set; }
        public bool? EnablePhonetic { get; set; }
        public bool? EnableNicknames { get; set; }
        public bool? TrimDuplicates { get; set; }
        public bool? EnableFql { get; set; }
        public bool? EnableQueryRules { get; set; }
        public bool? ProcessBestBets { get; set; }
        public bool? ByPassResultTypes { get; set; }
        public bool? ProcessPersonalFavorites { get; set; }
        public bool? GenerateBlockRankLog { get; set; }
        public bool? IncludeRankDetail { get; set; }
        public int? StartRow { get; set; }
        public int? RowLimit { get; set; }
        public int? RowsPerPage { get; set; }
        public string SelectProperties { get; set; }
        public string Refiners { get; set; }
        public string RefinementFilters { get; set; }
        public string HitHighlightedProperties { get; set; }
        public string RankingModelId { get; set; }
        public string SortList { get; set; }
        public string Culture { get; set; }
        public string SourceId { get; set; }
        public string HiddenConstraints { get; set; }
        public string ResultsUrl { get; set; }
        public string QueryTag { get; set; }
        public string CollapseSpecifiation { get; set; }
        public string QueryTemplate { get; set; }
        public long? TrimDuplicatesIncludeId { get; set; }
        public string ClientType { get; set; }

        public override Uri GenerateHttpGetUri()
        {
            string sharepointSiteUrl = this.SharePointSiteUrl;

            StringBuilder uriBuilder = new StringBuilder();

            if (!String.IsNullOrWhiteSpace(sharepointSiteUrl))
            {
                uriBuilder.Append(sharepointSiteUrl);

                if (!sharepointSiteUrl.EndsWith("/"))
                    uriBuilder.Append("/");
            }

            uriBuilder.AppendFormat("_api/search/query?querytext='{0}'", UrlEncode(this.QueryText));

            if (this.EnableStemming == true)
                uriBuilder.Append("&enablestemming=true");
            else if (this.EnableStemming == false)
                uriBuilder.Append("&enablestemming=false");

            if (this.EnablePhonetic == true)
                uriBuilder.Append("&enablephonetic=true");
            else if (this.EnablePhonetic == false)
                uriBuilder.Append("&enablephonetic=false");

            if (this.EnableNicknames == true)
                uriBuilder.Append("&enablenicknames=true");
            if (this.EnableNicknames == false)
                uriBuilder.Append("&enablenicknames=false");

            if (this.TrimDuplicates == true)
                uriBuilder.Append("&trimduplicates=true");
            else if (this.TrimDuplicates == false)
                uriBuilder.Append("&trimduplicates=false");

            if (this.EnableFql == true)
                uriBuilder.Append("&enablefql=true");
            else if (this.EnableFql == false)
                uriBuilder.Append("&enablefql=false");

            if (this.EnableQueryRules == true)
                uriBuilder.Append("&enablequeryrules=true");
            else if (this.EnableQueryRules == false)
                uriBuilder.Append("&enablequeryrules=false");

            if (this.ProcessBestBets == true)
                uriBuilder.Append("&processbestbets=true");
            else if (this.ProcessBestBets == false)
                uriBuilder.Append("&processbestbets=false");

            if (this.ByPassResultTypes == true)
                uriBuilder.Append("&bypassresulttypes=true");
            else if (this.ByPassResultTypes == false)
                uriBuilder.Append("&bypassresulttypes=false");

            if (this.ProcessPersonalFavorites == true)
                uriBuilder.Append("&processpersonalfavorites=true");
            else if (this.ProcessPersonalFavorites == false)
                uriBuilder.Append("&processpersonalfavorites=false");

            if (this.GenerateBlockRankLog == true)
                uriBuilder.Append("&generateblockranklog=true");
            else if (this.GenerateBlockRankLog == false)
                uriBuilder.Append("&generateblockranklog=false");

            if (this.StartRow.HasValue && this.StartRow.Value > 0)
                uriBuilder.AppendFormat("&startrow={0}", this.StartRow.Value);

            if (this.RowsPerPage.HasValue)
                uriBuilder.AppendFormat("&rowsperpage={0}", this.RowsPerPage.Value);

            if (this.RowLimit.HasValue)
                uriBuilder.AppendFormat("&rowlimit={0}", this.RowLimit.Value);

            if (!String.IsNullOrEmpty(this.QueryTemplate))
                uriBuilder.AppendFormat("&querytemplate='{0}'", UrlEncode(this.QueryTemplate));

            SetRankDetailProperties();

            if (!String.IsNullOrEmpty(this.SelectProperties))
                uriBuilder.AppendFormat("&selectproperties='{0}'", UrlEncode(this.SelectProperties));

            if (!String.IsNullOrEmpty(this.Refiners))
                uriBuilder.AppendFormat("&refiners='{0}'", UrlEncode(this.Refiners));

            if (!String.IsNullOrEmpty(this.RefinementFilters))
                uriBuilder.AppendFormat("&refinementfilters='{0}'", UrlEncode(this.RefinementFilters));

            if (!String.IsNullOrEmpty(this.SortList))
                uriBuilder.AppendFormat("&sortlist='{0}'", UrlEncode(this.SortList));

            if (!String.IsNullOrEmpty(this.HitHighlightedProperties))
                uriBuilder.AppendFormat("&hithighlightedproperties='{0}'", UrlEncode(this.HitHighlightedProperties));

            if (!String.IsNullOrEmpty(this.RankingModelId))
                uriBuilder.AppendFormat("&rankingmodelid='{0}'", UrlEncode(this.RankingModelId));

            if (!String.IsNullOrEmpty(this.Culture))
                uriBuilder.AppendFormat("&culture={0}", UrlEncode(this.Culture));

            if (!String.IsNullOrEmpty(this.SourceId))
                uriBuilder.AppendFormat("&sourceid='{0}'", UrlEncode(this.SourceId));

            if (!String.IsNullOrEmpty(this.HiddenConstraints))
                uriBuilder.AppendFormat("&hiddenconstraints='{0}'", UrlEncode(this.HiddenConstraints));

            if (!String.IsNullOrEmpty(this.ResultsUrl))
                uriBuilder.AppendFormat("&resultsurl='{0}'", UrlEncode(this.ResultsUrl));

            if (!String.IsNullOrEmpty(this.QueryTag))
                uriBuilder.AppendFormat("&querytag='{0}'", UrlEncode(this.QueryTag));

            if (!String.IsNullOrEmpty(this.CollapseSpecifiation))
                uriBuilder.AppendFormat("&collapsespecification='{0}'", UrlEncode(this.CollapseSpecifiation));

            if (!String.IsNullOrEmpty(this.ClientType))
                uriBuilder.AppendFormat("&clienttype='{0}'", this.ClientType);

            if (this.TrimDuplicatesIncludeId.HasValue)
                uriBuilder.AppendFormat("&trimduplicatesincludeid={0}", this.TrimDuplicatesIncludeId.Value);

            if (this.AuthenticationType == AuthenticationType.Anonymous)
            {
                uriBuilder.Append("&QueryTemplatePropertiesUrl='spfile://webroot/queryparametertemplate.xml'");
            }

            return new Uri(uriBuilder.ToString());
        }

        private void SetRankDetailProperties()
        {
            if (this.IncludeRankDetail.HasValue && this.IncludeRankDetail.Value)
            {
                if (string.IsNullOrWhiteSpace(this.SelectProperties))
                {
                    this.SelectProperties = "RankDetail,Title,Path,Language";
                }
                else
                {
                    var props = this.SelectProperties.ToLower().Split(',').ToList();
                    if (!props.Contains("rankdetail")) props.Add("rankdetail");
                    if (!props.Contains("title")) props.Add("title");
                    if (!props.Contains("path")) props.Add("path");
                    if (!props.Contains("language")) props.Add("language");
                    this.SelectProperties = string.Join(",", props);
                }
            }
            //Todo why is this set to null?
            //else
            //{
            //    this.SelectProperties = null;
            //}
        }

        public override Uri GenerateHttpPostUri()
        {
            string restUri = this.SharePointSiteUrl;

            StringBuilder uriBuilder = new StringBuilder();

            if (!String.IsNullOrWhiteSpace(restUri))
            {
                uriBuilder.Append(restUri);

                if (!restUri.EndsWith("/"))
                    uriBuilder.Append("/");
            }

            uriBuilder.Append("_api/search/postquery");
            return new Uri(uriBuilder.ToString());
        }

        public override string GenerateHttpPostBodyPayload()
        {
            StringBuilder searchRequestBuilder = new StringBuilder();

            searchRequestBuilder.AppendFormat("{{'request': {{ 'Querytext':'{0}'", this.QueryText);

            if (this.EnableStemming == true)
                searchRequestBuilder.Append(", 'EnableStemming':true");
            else if (this.EnableStemming == false)
                searchRequestBuilder.Append(", 'EnableStemming':false");

            if (this.EnablePhonetic == true)
                searchRequestBuilder.Append(", 'EnablePhonetic':true");
            else if (this.EnablePhonetic == false)
                searchRequestBuilder.Append(", 'EnablePhonetic':false");

            if (this.EnableNicknames == true)
                searchRequestBuilder.Append(", 'EnableNicknames':true");
            if (this.EnableNicknames == false)
                searchRequestBuilder.Append(", 'EnableNicknames':false");

            if (this.TrimDuplicates == true)
                searchRequestBuilder.Append(", 'TrimDuplicates':true");
            else if (this.TrimDuplicates == false)
                searchRequestBuilder.Append(", 'TrimDuplicates':false");

            if (this.EnableFql == true)
                searchRequestBuilder.Append(", 'EnableFQL':true");
            else if (this.EnableFql == false)
                searchRequestBuilder.Append(", 'EnableFQL':false");

            if (this.ProcessBestBets == true)
                searchRequestBuilder.Append(", 'ProcessBestBets':true");
            else if (this.ProcessBestBets == false)
                searchRequestBuilder.Append(", 'ProcessBestBets':false");

            if (this.ByPassResultTypes == true)
                searchRequestBuilder.Append(", 'BypassResultTypes':true");
            else if (this.ByPassResultTypes == false)
                searchRequestBuilder.Append(", 'BypassResultTypes':false");

            if (this.EnableQueryRules == true)
                searchRequestBuilder.Append(", 'EnableQueryRules':true");
            else if (this.EnableQueryRules == false)
                searchRequestBuilder.Append(", 'EnableQueryRules':false");

            if (this.ProcessPersonalFavorites == true)
                searchRequestBuilder.Append(", 'ProcessPersonalFavorites':true");
            else if (this.ProcessPersonalFavorites == false)
                searchRequestBuilder.Append(", 'ProcessPersonalFavorites':false");

            if (this.GenerateBlockRankLog == true)
                searchRequestBuilder.Append(", 'GenerateBlockRankLog':true");
            else if (this.GenerateBlockRankLog == false)
                searchRequestBuilder.Append(", 'GenerateBlockRankLog':false");

            if (this.StartRow.HasValue && this.StartRow.Value > 0)
                searchRequestBuilder.AppendFormat(", 'StartRow':{0}", this.StartRow.Value);

            if (this.RowsPerPage.HasValue)
                searchRequestBuilder.AppendFormat(", 'RowsPerPage':{0}", this.RowsPerPage.Value);

            if (this.RowLimit.HasValue)
                searchRequestBuilder.AppendFormat(", 'RowLimit':{0}", this.RowLimit.Value);

            SetRankDetailProperties();

            if (!String.IsNullOrEmpty(this.SelectProperties))
            {
                var props = this.SelectProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                searchRequestBuilder.AppendFormat(", 'SelectProperties':{{'results':['{0}']}}", String.Join("','", props));
            }

            if (!String.IsNullOrEmpty(this.Refiners))
                searchRequestBuilder.AppendFormat(", 'Refiners':'{0}'", this.Refiners);

            if (!String.IsNullOrEmpty(this.RefinementFilters))
            {
                searchRequestBuilder.AppendFormat(", 'RefinementFilters':{{'results':['{0}']}}", this.RefinementFilters);
            }

            if (!String.IsNullOrEmpty(this.SortList))
            {
                var sorts = this.SortList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                List<string> sortsL = new List<string>();
                foreach (var sort in sorts)
                {
                    var parts = sort.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        if (!String.IsNullOrWhiteSpace(parts[0]) && !String.IsNullOrWhiteSpace(parts[1]))
                        {
                            string direction = "";
                            if (parts[1].ToLower() == "descending")
                                direction = "1";
                            else if (parts[1].ToLower() == "ascending")
                                direction = "0";

                            sortsL.Add(String.Format("{{'Property':'{0}','Direction':'{1}'}}", parts[0], direction));
                        }
                    }
                }
                searchRequestBuilder.AppendFormat(", 'SortList':{{'results':[{0}]}}", String.Join(",", sortsL));
            }

            if (!String.IsNullOrEmpty(this.HitHighlightedProperties))
            {
                var props = this.HitHighlightedProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                searchRequestBuilder.AppendFormat(", 'HitHighlightedProperties':{{'results':['{0}']}}", String.Join("','", props));
            }

            if (this.TrimDuplicatesIncludeId.HasValue)
                searchRequestBuilder.AppendFormat(", 'TrimDuplicatesIncludeId':'{0}'", this.TrimDuplicatesIncludeId.Value);

            if (!String.IsNullOrEmpty(this.QueryTemplate))
                searchRequestBuilder.AppendFormat(", 'QueryTemplate':'{0}'", this.QueryTemplate);

            if (!String.IsNullOrEmpty(this.RankingModelId))
                searchRequestBuilder.AppendFormat(", 'RankingModelId':'{0}'", this.RankingModelId);

            if (!String.IsNullOrEmpty(this.Culture))
                searchRequestBuilder.AppendFormat(", 'Culture':{0}", this.Culture);

            if (!String.IsNullOrEmpty(this.SourceId))
                searchRequestBuilder.AppendFormat(", 'SourceId':'{0}'", this.SourceId);

            if (!String.IsNullOrEmpty(this.HiddenConstraints))
                searchRequestBuilder.AppendFormat(", 'HiddenConstraints':'{0}'", this.HiddenConstraints);

            if (!String.IsNullOrEmpty(this.ResultsUrl))
                searchRequestBuilder.AppendFormat(", 'ResultsUrl':'{0}'", this.ResultsUrl);

            if (!String.IsNullOrEmpty(this.QueryTag))
                searchRequestBuilder.AppendFormat(", 'QueryTag':'{0}'", this.QueryTag);

            if (!String.IsNullOrEmpty(this.CollapseSpecifiation))
                searchRequestBuilder.AppendFormat(", 'CollapseSpecification':'{0}'", this.CollapseSpecifiation);

            if (!String.IsNullOrEmpty(this.ClientType))
                searchRequestBuilder.AppendFormat(", 'ClientType':'{0}'", this.ClientType);

            if (this.AuthenticationType == AuthenticationType.Anonymous)
            {
                searchRequestBuilder.Append(", 'QueryTemplatePropertiesUrl':'spfile://webroot/queryparametertemplate.xml'");
            }

            searchRequestBuilder.Append("} }");


            return searchRequestBuilder.ToString();
        }
    }
}
