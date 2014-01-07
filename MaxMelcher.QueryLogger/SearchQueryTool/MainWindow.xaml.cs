using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;
using MaxMelcher.QueryLogger.Utils;
using Microsoft.AspNet.SignalR.Client;
using SearchQueryTool.Model;
using SearchQueryTool.Helpers;

namespace SearchQueryTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        enum SearchMethodType
        {
            Query,
            Suggest
        }

        private static string DefaultSharePointSiteUrl = "http://localhost";
        private static string ConnectionPropsXmlFileName = "connection-props.xml";

        private readonly SearchQueryRequest searchQueryRequest;
        private readonly SearchSuggestionsRequest searchSuggestionsRequest;

        public SafeObservable<SearchQueryDebug> ObservableQueryCollection
        {
            get { return _observableQueryCollection; }
            set { _observableQueryCollection = value; }
        }

        private object _locker = new object();

        private IHubProxy _hub;
        private HubConnection _hubConnection;
        private SafeObservable<SearchQueryDebug> _observableQueryCollection;

        public MainWindow()
        {
            searchQueryRequest = new SearchQueryRequest() { SharePointSiteUrl = DefaultSharePointSiteUrl };
            searchSuggestionsRequest = new SearchSuggestionsRequest() { SharePointSiteUrl = DefaultSharePointSiteUrl };
            _observableQueryCollection = new SafeObservable<SearchQueryDebug>(Dispatcher);

            InitializeComponent();
            InitializeControls();
        }

        private void InitializeControls()
        {
            SetCurrentWindowsUserIdentity();
            LoadConnectionPropertiesFromFile();
            UpdateRequestUriStringTextBlock();

            //Enable the cross acces to this collection elsewhere - see: http://stackoverflow.com/questions/14336750/upgrading-to-net-4-5-an-itemscontrol-is-inconsistent-with-its-items-source
            DebugGrid.ItemsSource = ObservableQueryCollection;
        }

        private SearchMethodType CurrentSearchMethodType
        {
            get
            {
                var selectedTabItem = SearchMethodTypeTabControl.SelectedItem as TabItem;
                if (selectedTabItem == this.QueryMethodTypeTabItem)
                {
                    // query
                    return SearchMethodType.Query;
                }
                else
                {
                    // suggest
                    return SearchMethodType.Suggest;
                }
            }
        }

        private HttpMethodType CurrentHttpMethodType
        {
            get
            {
                if (HttpGetMethodRadioButton == null) return HttpMethodType.Get; // default to HTTP Get

                return (HttpGetMethodRadioButton.IsChecked.Value ? HttpMethodType.Get : HttpMethodType.Post);
            }
        }

        #region Event Handlers

        #region Event Handlers for controls common to both query and suggestions

        /// <summary>
        /// Handles the Click event of the RunButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            SearchMethodType currentSelectedSearchMethodType = CurrentSearchMethodType;

            // fire off the query operation
            if (currentSelectedSearchMethodType == SearchMethodType.Query)
            {
                StartSearchQueryRequest();
            }
            else if (currentSelectedSearchMethodType == SearchMethodType.Suggest)
            {
                StartSearchSuggestionRequest();
            }
        }

        /// <summary>
        /// Handles the LostFocus event of the SharePointSiteUrlTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void SharePointSiteUrlTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            string url = this.SharePointSiteUrlTextBox.Text.Trim();

            try
            {
                Uri testUri = new Uri(url);
            }
            catch (Exception)
            {
                this.SharePointSiteUrlAlertImage.Visibility = System.Windows.Visibility.Visible;
                return;
            }

            this.SharePointSiteUrlAlertImage.Visibility = System.Windows.Visibility.Hidden;

            this.searchQueryRequest.SharePointSiteUrl = this.SharePointSiteUrlTextBox.Text.Trim();
            this.searchSuggestionsRequest.SharePointSiteUrl = this.SharePointSiteUrlTextBox.Text.Trim();

            UpdateRequestUriStringTextBlock();
        }

        /// <summary>
        /// Handles the SelectionChanged event of the SearchMethodTypeTabControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs" /> instance containing the event data.</param>
        private void SearchMethodTypeTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateRequestUriStringTextBlock();
        }

        /// <summary>
        /// Handles the SelectionChanged event of the AuthenticationTypeComboBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs" /> instance containing the event data.</param>
        private void AuthenticationTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedIndex = this.AuthenticationTypeComboBox.SelectedIndex;
            if (selectedIndex == 0) // use current user
            {
                if (this.AuthenticationUsernameTextBox != null)
                    this.AuthenticationUsernameTextBox.IsEnabled = false;

                if (this.AuthenticationPasswordTextBox != null)
                    this.AuthenticationPasswordTextBox.IsEnabled = false;

                if (this.AuthenticationMethodComboBox != null)
                {
                    this.AuthenticationMethodComboBox.SelectedIndex = 0;
                    this.AuthenticationMethodComboBox.IsEnabled = false;
                }

                this.SetCurrentWindowsUserIdentity();

                this.searchQueryRequest.AuthenticationType = AuthenticationType.CurrentUser;
                this.searchSuggestionsRequest.AuthenticationType = AuthenticationType.CurrentUser;

                if (this.UsernameAndPasswordTextBoxContainer != null)
                {
                    this.UsernameAndPasswordTextBoxContainer.Visibility = System.Windows.Visibility.Visible;
                }
                if (this.SPOLoginButtonContainer != null)
                {
                    this.SPOLoginButtonContainer.Visibility = System.Windows.Visibility.Hidden;
                }
                if (this.AuthenticationMethodComboBox != null)
                    this.AuthenticationMethodComboBox.IsEnabled = true;
            }
            else if (selectedIndex == 1)
            {
                this.AuthenticationMethodComboBox.IsEnabled = true;
                this.AuthenticationUsernameTextBox.IsEnabled = true;
                this.AuthenticationPasswordTextBox.IsEnabled = true;

                AuthenticationMethodComboBox_SelectionChanged(null, null);
                if (this.AuthenticationMethodComboBox != null)
                    this.AuthenticationMethodComboBox.IsEnabled = true;
            }
            else
            {
                //anonymous
                this.searchQueryRequest.AuthenticationType = AuthenticationType.Anonymous;
                this.searchSuggestionsRequest.AuthenticationType = AuthenticationType.Anonymous;
                if (this.AuthenticationMethodComboBox != null)
                    this.AuthenticationMethodComboBox.IsEnabled = false;
            }
        }

        private void AuthenticationMethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.AuthenticationTypeComboBox.SelectedIndex == 0)
            {
                return;
            }

            string dc = (this.AuthenticationMethodComboBox.SelectedItem as ComboBoxItem).DataContext as string;
            if (dc == "WinAuth")
            {
                this.searchQueryRequest.AuthenticationType = AuthenticationType.Windows;
                this.UsernameAndPasswordTextBoxContainer.Visibility = System.Windows.Visibility.Visible;
                this.SPOLoginButtonContainer.Visibility = System.Windows.Visibility.Hidden;
            }
            else if (dc == "FormsAuth")
            {
                this.searchQueryRequest.AuthenticationType = AuthenticationType.Forms;
                this.UsernameAndPasswordTextBoxContainer.Visibility = System.Windows.Visibility.Visible;
                this.SPOLoginButtonContainer.Visibility = System.Windows.Visibility.Hidden;
            }
            else if (dc == "SPOAuth")
            {
                this.searchQueryRequest.AuthenticationType = AuthenticationType.SPO;
                this.UsernameAndPasswordTextBoxContainer.Visibility = System.Windows.Visibility.Hidden;
                this.SPOLoginButtonContainer.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void AuthenticationUsernameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            this.searchQueryRequest.UserName = this.AuthenticationUsernameTextBox.Text;
        }

        private void AuthenticationPasswordTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            this.searchQueryRequest.SecurePassword = this.AuthenticationPasswordTextBox.SecurePassword;
            this.searchQueryRequest.Password = this.AuthenticationPasswordTextBox.Password;
        }

        private void LoginToSPOButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CookieCollection cc = SPOClientWebAuth.GetAuthenticatedCookies(this.searchQueryRequest.SharePointSiteUrl);
                if (cc == null)
                {
                    ShowMsgBox("Authentication cookie returned is null! Authentication failed. Please try again.");
                }

                this.searchQueryRequest.SPOAuthenticationCookie = cc;
                this.searchSuggestionsRequest.SPOAuthenticationCookie = cc;

                this.LoginToSPOButton.IsEnabled = false;
                this.LoggedinToSPOLabel.Visibility = System.Windows.Visibility.Visible;
            }
            catch (Exception ex)
            {
                ShowError(ex);
                ShowMsgBox("Failed to authenticate to SharePoint Online! Please try again.\n\n" + ex.Message);
            }
        }

        #endregion

        #region Event Handlers for Controls on the Search Query Tab

        /// <summary>
        /// Handles the Handler event of the SearchQueryTextBox_LostFocus control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void SearchQueryTextBox_LostFocus_Handler(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb != null)
            {
                string dataContext = (tb.DataContext as string) ?? "";
                switch (dataContext.ToLower())
                {
                    case "query": this.searchQueryRequest.QueryText = tb.Text.Trim(); break;
                    case "querytemplate": this.searchQueryRequest.QueryTemplate = tb.Text.Trim(); break;
                    case "selectproperties": this.searchQueryRequest.SelectProperties = tb.Text.Trim(); break;
                    case "refiners": this.searchQueryRequest.Refiners = tb.Text.Trim(); break;
                    case "refinementfilters": this.searchQueryRequest.RefinementFilters = tb.Text.Trim(); break;
                    case "trimduplicatesincludeid": this.searchQueryRequest.TrimDuplicatesIncludeId = TryConvertToLong(tb.Text.Trim()); break;
                    case "sortlist": this.searchQueryRequest.SortList = tb.Text.Trim(); break;
                    case "hithighlightedproperties": this.searchQueryRequest.HitHighlightedProperties = tb.Text.Trim(); break;
                    case "rankingmodelid": this.searchQueryRequest.RankingModelId = tb.Text.Trim(); break;
                    case "culture": this.searchQueryRequest.Culture = tb.Text.Trim(); break;
                    case "sourceid": this.searchQueryRequest.SourceId = tb.Text.Trim(); break;
                    case "hiddenconstraints": this.searchQueryRequest.HiddenConstraints = tb.Text.Trim(); break;
                    case "resultsurl": this.searchQueryRequest.ResultsUrl = tb.Text.Trim(); break;
                    case "querytag": this.searchQueryRequest.QueryTag = tb.Text.Trim(); break;
                    case "collapsespecifiation": this.searchQueryRequest.CollapseSpecifiation = tb.Text.Trim(); break;
                    case "startrow": this.searchQueryRequest.StartRow = TryConvertToInt(tb.Text.Trim()); break;
                    case "rows": this.searchQueryRequest.RowLimit = TryConvertToInt(tb.Text.Trim()); break;
                    case "rowsperpage": this.searchQueryRequest.RowsPerPage = TryConvertToInt(tb.Text.Trim()); break;

                    default: break;
                }

                UpdateRequestUriStringTextBlock();
            }
        }

        /// <summary>
        /// Handles the CheckChanged event of the SearchQueryCheckBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void SearchQueryCheckBox_CheckChanged(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if (cb != null)
            {
                string datacontext = (cb.DataContext as string) ?? "";
                switch (datacontext.ToLower())
                {
                    case "enablestemming": this.searchQueryRequest.EnableStemming = cb.IsChecked; break;
                    case "enablequeryrules": this.searchQueryRequest.EnableQueryRules = cb.IsChecked; break;
                    case "enablenicknames": this.searchQueryRequest.EnableNicknames = cb.IsChecked; break;
                    case "processbestbets": this.searchQueryRequest.ProcessBestBets = cb.IsChecked; break;
                    case "trimduplicates": this.searchQueryRequest.TrimDuplicates = cb.IsChecked; break;
                    case "enablefql": this.searchQueryRequest.EnableFql = cb.IsChecked; break;
                    case "enablephonetic": this.searchQueryRequest.EnablePhonetic = cb.IsChecked; break;
                    case "bypassresulttypes": this.searchQueryRequest.ByPassResultTypes = cb.IsChecked; break;
                    case "processpersonalfavorites": this.searchQueryRequest.ProcessPersonalFavorites = cb.IsChecked; break;
                    case "generateblockranklog": this.searchQueryRequest.GenerateBlockRankLog = cb.IsChecked; break;
                    case "includerankdetail": this.searchQueryRequest.IncludeRankDetail = cb.IsChecked; break;

                    default: break;
                }

                UpdateRequestUriStringTextBlock();
            }
        }

        /// <summary>
        /// Handles the SelectionChanged event of the QueryLogClientTypeComboBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs" /> instance containing the event data.</param>
        private void QueryLogClientTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedValue = (this.QueryLogClientTypeComboBox.SelectedItem as ComboBoxItem).DataContext as string;
            this.searchQueryRequest.ClientType = selectedValue;

            UpdateRequestUriStringTextBlock();
        }

        private void ResetCheckboxesButton_Click(object sender, RoutedEventArgs e)
        {
            this.searchQueryRequest.EnableStemming = null;
            this.EnableStemmingCheckBox.IsChecked = null;

            this.searchQueryRequest.EnableQueryRules = null;
            this.EnableQueryRulesCheckBox.IsChecked = null;

            this.searchQueryRequest.EnableNicknames = null;
            this.EnableNicknamesCheckBox.IsChecked = null;

            this.searchQueryRequest.ProcessBestBets = null;
            this.ProcessBestBetsCheckBox.IsChecked = null;

            this.searchQueryRequest.TrimDuplicates = null;
            this.TrimDuplicatesCheckBox.IsChecked = null;

            this.searchQueryRequest.EnableFql = null;
            this.EnableFqlCheckBox.IsChecked = null;

            this.searchQueryRequest.EnablePhonetic = null;
            this.EnablePhoneticCheckBox.IsChecked = null;

            this.searchQueryRequest.ByPassResultTypes = null;
            this.ByPassResultTypesCheckBox.IsChecked = null;

            this.searchQueryRequest.ProcessPersonalFavorites = null;
            this.ProcessPersonalFavoritesCheckBox.IsChecked = null;

            this.searchQueryRequest.GenerateBlockRankLog = null;
            this.GenerateBlockRankLogCheckBox.IsChecked = null;

            UpdateRequestUriStringTextBlock();
        }

        private void HttpMethodModeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            UpdateRequestUriStringTextBlock();
        }

        #endregion

        #region Event Handlers for Controls on the Search Suggestion Tab

        private void SearchSuggestionsTextBox_LostFocus_Handler(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb != null)
            {
                string dataContext = (tb.DataContext as string) ?? "";
                switch (dataContext.ToLower())
                {
                    case "query": this.searchSuggestionsRequest.QueryText = tb.Text.Trim(); break;
                    case "numberofquerysuggestions": this.searchSuggestionsRequest.NumberOfQuerySuggestions = TryConvertToInt(tb.Text.Trim()); break;
                    case "numberofresultsuggestions": this.searchSuggestionsRequest.NumberOfResultSuggestions = TryConvertToInt(tb.Text.Trim()); break;
                    case "suggestionsculture": this.searchSuggestionsRequest.Culture = TryConvertToInt(tb.Text.Trim()); break;

                    default: break;
                }

                UpdateRequestUriStringTextBlock();
            }
        }

        private void SearchSuggestionsCheckBox_CheckChanged(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if (cb != null)
            {
                string datacontext = (cb.DataContext as string) ?? "";
                switch (datacontext.ToLower())
                {
                    case "prequerysuggestions": this.searchSuggestionsRequest.PreQuerySuggestions = cb.IsChecked; break;
                    case "showpeoplenamesuggestions": this.searchSuggestionsRequest.ShowPeopleNameSuggestions = cb.IsChecked; break;
                    case "hithighlighting": this.searchSuggestionsRequest.HitHighlighting = cb.IsChecked; break;
                    case "capitalizefirstletters": this.searchSuggestionsRequest.CapitalizeFirstLetters = cb.IsChecked; break;

                    default: break;
                }

                UpdateRequestUriStringTextBlock();
            }
        }

        #endregion

        #region Event Handlers for Menu controls

        /// <summary>
        /// Handles the Click event of the PasteExampleButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void PasteExampleButton_Click(object sender, RoutedEventArgs e)
        {
            if (e.Source != null)
            {
                object d = (e.Source as Button).DataContext;
                if (d != null && d is String)
                {
                    string senderDatacontext = d as string;
                    if (!String.IsNullOrEmpty(senderDatacontext))
                    {
                        string exampleString = SampleStrings.GetExampleStringFor(senderDatacontext);
                        if (!String.IsNullOrEmpty(exampleString))
                        {
                            switch (senderDatacontext.ToLower())
                            {
                                case "selectproperties":
                                    this.SelectPropertiesTextBox.Text = exampleString;
                                    this.SelectPropertiesTextBox.Focus();
                                    break;
                                case "refiners":
                                    this.RefinersTextBox.Text = exampleString;
                                    this.RefinersTextBox.Focus();
                                    break;
                                case "refinementfilters":
                                    this.RefinementFiltersTextBox.Text = exampleString;
                                    this.RefinementFiltersTextBox.Focus();
                                    break;
                                case "sortlist":
                                    this.SortListTextBox.Text = exampleString;
                                    this.SortListTextBox.Focus();
                                    break;
                                default: break;
                            }
                        }
                    }
                }
            }
        }

        private void MenuSaveConnectionProperties_Click(object sender, RoutedEventArgs e)
        {
            XElement connectionPropsElm = new XElement("Connection-Props");
            connectionPropsElm.Add(new XElement("spsiteurl", this.SharePointSiteUrlTextBox.Text.Trim()));
            connectionPropsElm.Add(new XElement("timeout", this.WebRequestTimeoutTextBox.Text.Trim()));
            connectionPropsElm.Add(new XElement("accept", (this.AcceptJsonRadioButton.IsChecked == true ? "json" : "xml")));
            connectionPropsElm.Add(new XElement("httpmethod", (this.HttpGetMethodRadioButton.IsChecked == true ? "GET" : "POST")));

            try
            {
                string outputPath = System.IO.Path.Combine(Environment.CurrentDirectory, ConnectionPropsXmlFileName);
                using (FileStream fs = new FileStream(outputPath, FileMode.Create))
                {
                    connectionPropsElm.Save(fs);
                }
            }
            catch (Exception ex)
            {
                ShowMsgBox("Failed to save connection properties. Error:" + ex.Message);
            }
        }

        /// <summary>
        /// Handles the Click event of the MenuFileExit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void MenuFileExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Handles the Click event of the MenuAbout control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            new AboutBox().Show();
        }

        #endregion

        #endregion

        #region Request Building methods and Helpers

        /// <summary>
        /// Starts the search query request.
        /// </summary>
        private void StartSearchQueryRequest()
        {
            string queryText = this.QueryTextBox.Text.Trim();
            if (String.IsNullOrEmpty(queryText))
            {
                this.QueryTextBox.Focus();
                return;
            }

            try
            {
                this.searchQueryRequest.QueryText = queryText;
                UpdateRequestUriStringTextBlock();

                MarkRequestOperation(true, "Running");

                this.searchQueryRequest.HttpMethodType = CurrentHttpMethodType;
                this.searchQueryRequest.AcceptType = AcceptJsonRadioButton.IsChecked.Value ? AcceptType.Json : AcceptType.Xml;

                Task.Factory.StartNew<HttpRequestResponsePair>(() =>
                {
                    return HttpRequestRunner.RunWebRequest(this.searchQueryRequest);

                }, TaskCreationOptions.LongRunning)
                .ContinueWith((task) =>
                {
                    if (task.Exception != null)
                    {
                        ShowError(task.Exception);
                    }
                    else
                    {
                        var requestResponsePair = task.Result;
                        var request = requestResponsePair.Item1;

                        using (var response = requestResponsePair.Item2)
                        {
                            using (var responseStream = response.GetResponseStream())
                            {
                                using (var reader = new StreamReader(responseStream))
                                {
                                    var content = reader.ReadToEnd();

                                    NameValueCollection requestHeaders = new NameValueCollection();
                                    foreach (var header in request.Headers.AllKeys)
                                    {
                                        requestHeaders.Add(header, request.Headers[header]);
                                    }

                                    NameValueCollection responseHeaders = new NameValueCollection();
                                    foreach (var header in response.Headers.AllKeys)
                                    {
                                        responseHeaders.Add(header, response.Headers[header]);
                                    }

                                    string requestContent = "";
                                    if (request.Method == "POST")
                                    {
                                        requestContent = requestResponsePair.Item3;
                                    }

                                    var searchResults = new SearchQueryResult()
                                    {
                                        RequestUri = request.RequestUri,
                                        RequestMethod = request.Method,
                                        RequestContent = requestContent,
                                        ContentType = response.ContentType,
                                        ResponseContent = content,
                                        RequestHeaders = requestHeaders,
                                        ResponseHeaders = responseHeaders,
                                        StatusCode = response.StatusCode,
                                        StatusDescription = response.StatusDescription,
                                        HttpProtocolVersion = response.ProtocolVersion.ToString()
                                    };
                                    searchResults.Process();

                                    // set the result
                                    SetStatsResult(searchResults);
                                    SetRawResult(searchResults);
                                    SetPrimaryQueryResultItems(searchResults);
                                    SetRefinementResultItems(searchResults);
                                    SetSecondaryQueryResultItems(searchResults);
                                }
                            }
                        }
                    }

                    MarkRequestOperation(false, "Ready");

                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception ex)
            {
                MarkRequestOperation(false, "Ready");
                ShowError(ex);
            }
        }



        /// <summary>
        /// Starts the search suggestion request.
        /// </summary>
        private void StartSearchSuggestionRequest()
        {
            string queryText = this.SuggestionsQueryTextBox.Text.Trim();
            if (String.IsNullOrEmpty(queryText))
            {
                this.SuggestionsQueryTextBox.Focus();
                return;
            }

            this.searchSuggestionsRequest.QueryText = queryText;
            UpdateRequestUriStringTextBlock();

            try
            {
                MarkRequestOperation(true, "Running");

                this.searchSuggestionsRequest.HttpMethodType = HttpMethodType.Get; // Only get is supported for suggestions
                this.searchSuggestionsRequest.AcceptType = AcceptJsonRadioButton.IsChecked.Value ? AcceptType.Json : AcceptType.Xml;

                Task.Factory.StartNew<HttpRequestResponsePair>(() =>
                {
                    return HttpRequestRunner.RunWebRequest(this.searchSuggestionsRequest);

                }, TaskCreationOptions.LongRunning)
                .ContinueWith((task) =>
                {
                    if (task.Exception != null)
                    {
                        ShowError(task.Exception);
                    }
                    else
                    {
                        var requestResponsePair = task.Result;
                        var request = requestResponsePair.Item1;

                        using (var response = requestResponsePair.Item2)
                        {
                            using (var responseStream = response.GetResponseStream())
                            {
                                using (var reader = new StreamReader(responseStream))
                                {
                                    var content = reader.ReadToEnd();

                                    NameValueCollection requestHeaders = new NameValueCollection();
                                    foreach (var header in request.Headers.AllKeys)
                                    {
                                        requestHeaders.Add(header, request.Headers[header]);
                                    }

                                    NameValueCollection responseHeaders = new NameValueCollection();
                                    foreach (var header in response.Headers.AllKeys)
                                    {
                                        responseHeaders.Add(header, response.Headers[header]);
                                    }

                                    var searchResults = new SearchSuggestionsResult()
                                    {
                                        RequestUri = request.RequestUri,
                                        RequestMethod = request.Method,
                                        ContentType = response.ContentType,
                                        ResponseContent = content,
                                        RequestHeaders = requestHeaders,
                                        ResponseHeaders = responseHeaders,
                                        StatusCode = response.StatusCode,
                                        StatusDescription = response.StatusDescription,
                                        HttpProtocolVersion = response.ProtocolVersion.ToString()
                                    };
                                    searchResults.Process();

                                    // set the result
                                    SetStatsResult(searchResults);
                                    SetRawResult(searchResults);
                                    SetSuggestionsResultItems(searchResults);
                                }
                            }
                        }
                    }

                    MarkRequestOperation(false, "Ready");

                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception ex)
            {
                // log
                MarkRequestOperation(false, "Ready");
                ShowError(ex);
            }
        }

        /// <summary>
        /// Creates and populates the the Statistics tab from data from the passed in <paramref name="searchResult"/>.
        /// This method is used for both query and suggestions.
        /// </summary>
        /// <param name="searchResult">The search result.</param>
        private void SetStatsResult(SearchResult searchResult)
        {
            ScrollViewer sv = new ScrollViewer();
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;

            TextBox tb = new TextBox()
            {
                BorderBrush = null,
                IsReadOnly = true,
                IsReadOnlyCaretVisible = false,
                TextWrapping = TextWrapping.WrapWithOverflow
            };

            tb.AppendText(String.Format("HTTP/{0} {1} {2}\n", searchResult.HttpProtocolVersion,
                                                              (int)searchResult.StatusCode,
                                                              searchResult.StatusDescription));
            if (searchResult.StatusCode != HttpStatusCode.OK)
            {
                tb.AppendText(searchResult.ResponseContent);
            }

            if (searchResult is SearchQueryResult)
            {
                var searchQueryResult = searchResult as SearchQueryResult;

                if (!String.IsNullOrEmpty(searchQueryResult.SerializedQuery))
                    tb.AppendText(String.Format("\tSerialized Query:\n{0}\n\n", searchQueryResult.SerializedQuery));

                if (!String.IsNullOrEmpty(searchQueryResult.QueryElapsedTime))
                    tb.AppendText(String.Format("\tElapsed Time (ms): {0}\n\n", searchQueryResult.QueryElapsedTime));

                if (searchQueryResult.TriggeredRules != null && searchQueryResult.TriggeredRules.Count > 0)
                {
                    tb.AppendText("\tTriggered Rules:\n");

                    foreach (var rule in searchQueryResult.TriggeredRules)
                    {
                        tb.AppendText(String.Format("\t\tQuery Rule Id: {0}\n", rule));
                    }
                    tb.AppendText("\n");
                }

                if (searchQueryResult.PrimaryQueryResult != null)
                {
                    tb.AppendText("\tPrimary Query Results:\n");
                    tb.AppendText(String.Format("\t\tTotal Rows: {0}\n", searchQueryResult.PrimaryQueryResult.TotalRows));
                    tb.AppendText(String.Format("\t\tTotal Rows Including Duplicates: {0}\n", searchQueryResult.PrimaryQueryResult.TotalRowsIncludingDuplicates));
                    tb.AppendText(String.Format("\t\tQuery Id: {0}\n", searchQueryResult.PrimaryQueryResult.QueryId));
                    tb.AppendText(String.Format("\t\tQuery Rule Id: {0}\n", searchQueryResult.PrimaryQueryResult.QueryRuleId));
                }

                if (searchQueryResult.SecondaryQueryResults != null)
                {
                    tb.AppendText("\n");
                    tb.AppendText("\tSecondary Query Results:\n");

                    foreach (var sqr in searchQueryResult.SecondaryQueryResults)
                    {
                        tb.AppendText(String.Format("\t\tSecondary Query Result {0}\n", sqr.QueryRuleId));
                        tb.AppendText(String.Format("\t\tTotal Rows: {0}\n", sqr.TotalRows));
                        tb.AppendText(String.Format("\t\tTotal Rows Including Duplicates: {0}\n", sqr.TotalRowsIncludingDuplicates));
                        tb.AppendText(String.Format("\t\tQuery Id: {0}\n", sqr.QueryId));
                        tb.AppendText(String.Format("\t\tQuery Rule Id: {0}\n", sqr.QueryRuleId));
                    }
                }
            }

            sv.Content = tb;
            this.StatsResultTabItem.Content = sv;
        }

        /// <summary>
        /// Creates and populates the the Headers tab from data from the passed in <paramref name="requestHeaders" /> and <paramref name="responseHeaders" />.
        /// This method is used for both query and suggestions.
        /// </summary>
        /// <param name="searchResult">The search result.</param>
        private void SetRawResult(SearchResult searchResult)
        {
            ScrollViewer sv = new ScrollViewer();
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;

            TextBox tb = new TextBox()
            {
                BorderBrush = null,
                IsReadOnly = true,
                IsReadOnlyCaretVisible = false,
                FontSize = 12
            };

            tb.AppendText(String.Format("{0}\t{1}\tHTTP {2}\n\n", searchResult.RequestMethod, searchResult.RequestUri.ToString(), searchResult.HttpProtocolVersion));

            tb.AppendText("Request:" + Environment.NewLine);
            foreach (var header in searchResult.RequestHeaders.AllKeys)
            {
                tb.AppendText(String.Format("\t{0}: {1}{2}", header, searchResult.RequestHeaders[header], Environment.NewLine));
            }
            tb.AppendText("\n\t" + searchResult.RequestContent + "\n");
            tb.AppendText("\n\n");

            tb.AppendText("Response:\n");
            tb.AppendText(String.Format("\tHTTP/{0} {1} {2}\n", searchResult.HttpProtocolVersion.ToString(), (int)searchResult.StatusCode, searchResult.StatusDescription));
            foreach (var header in searchResult.ResponseHeaders.AllKeys)
            {
                tb.AppendText(String.Format("\t{0}: {1}{2}", header, searchResult.ResponseHeaders[header], Environment.NewLine));
            }

            tb.AppendText("\n\t" + searchResult.ResponseContent + "\n");

            sv.Content = tb;
            this.RawResultTabItem.Content = sv;
        }

        /// <summary>
        /// Creates and populates the the Primary results tab from data from the passed in <paramref name="searchResult"/>.
        /// This method is used for only query results.
        /// </summary>
        /// <param name="searchResult">The search result.</param>
        private void SetPrimaryQueryResultItems(SearchQueryResult searchResult)
        {
            ScrollViewer sv = new ScrollViewer();
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;

            if (searchResult.PrimaryQueryResult != null && searchResult.PrimaryQueryResult.TotalRows > 0)
            {
                int totalRows = searchResult.PrimaryQueryResult.TotalRows;

                StackPanel spTop = new StackPanel() { Orientation = Orientation.Vertical };

                int counter = 1;

                foreach (var resultItem in searchResult.PrimaryQueryResult.RelevantResults)
                {
                    StackPanel spEntry = new StackPanel() { Margin = new Thickness(25) };

                    string resultTitle;
                    if (resultItem.ContainsKey("Title"))
                        resultTitle = resultItem["Title"];
                    else if (resultItem.ContainsKey("title"))
                        resultTitle = resultItem["title"];
                    else if (resultItem.ContainsKey("DocId"))
                        resultTitle = String.Format("DocId: {0}", resultItem["DocId"]);
                    else
                        resultTitle = "";

                    TextBox titleTB = new TextBox()
                    {
                        IsReadOnly = true,
                        IsReadOnlyCaretVisible = false,
                        Text = String.Format("{0}. {1}", counter, resultTitle),
                        BorderBrush = null,
                        BorderThickness = new Thickness(0),
                        Foreground = Brushes.DarkBlue,
                        FontSize = 14
                    };
                    spEntry.Children.Add(titleTB);

                    //Always expand all entries
                    Expander propsExpander = new Expander() { IsExpanded = true, Header = "View" };
                    StackPanel spProps = new StackPanel();

                    var keys = resultItem.Keys.ToList();
                    keys.Sort();
                    foreach (string key in keys)
                    {
                        var val = resultItem[key];
                        DockPanel propdp = new DockPanel();
                        propdp.Children.Add
                            (
                                new TextBox()
                                {
                                    IsReadOnly = true,
                                    IsReadOnlyCaretVisible = false,
                                    Text = String.Format("{0}: ", key),
                                    BorderBrush = null,
                                    BorderThickness = new Thickness(0),
                                    Foreground = Brushes.DarkGreen,
                                    FontWeight = FontWeights.Bold,
                                    FontSize = 14
                                }
                            );

                        if (key.Equals("RankDetail", StringComparison.InvariantCultureIgnoreCase))
                        {
                            ResultItem item = new ResultItem
                                                  {
                                                      Xml = val,
                                                      Title = resultItem["title"],
                                                      Path = resultItem["path"],
                                                      Language = resultItem["language"]
                                                  };
                            var tb = new TextBox()
                                         {
                                             IsReadOnly = true,
                                             IsReadOnlyCaretVisible = true,
                                             Text = "Show rank details...",
                                             BorderBrush = null,
                                             BorderThickness = new Thickness(0),
                                             Foreground = Brushes.DodgerBlue,
                                             FontSize = 14,
                                             TextDecorations = TextDecorations.Underline,
                                             Cursor = Cursors.Hand,
                                             Background = Brushes.Transparent,
                                             DataContext = item
                                         };
                            tb.PreviewMouseLeftButtonUp += tb_MouseLeftButtonUp;
                            propdp.Children.Add(tb);
                        }
                        else
                        {
                            propdp.Children.Add
                                (
                                    new TextBox()
                                    {
                                        IsReadOnly = true,
                                        IsReadOnlyCaretVisible = true,
                                        Text = val,
                                        BorderBrush = null,
                                        BorderThickness = new Thickness(0),
                                        Foreground = Brushes.Green,
                                        FontSize = 14
                                    }
                                );

                        }
                        spProps.Children.Add(propdp);
                    }

                    propsExpander.Content = spProps;
                    spEntry.Children.Add(propsExpander);
                    spTop.Children.Add(spEntry);

                    //add an link to view all properties according to this article: http://blogs.technet.com/b/searchguys/archive/2013/12/11/how-to-all-managed-properties-of-a-document.aspx
                    AddViewAllPropertiesLink(resultItem, spEntry);

                    counter++;
                }

                sv.Content = spTop;
            }
            else
            {
                TextBox tb = new TextBox()
                {
                    BorderBrush = null,
                    BorderThickness = new Thickness(0),
                    IsReadOnly = true,
                    IsReadOnlyCaretVisible = false,
                    Text = "The query returned zero items!",
                    Margin = new Thickness(30)
                };
                sv.Content = tb;
            }

            this.PrimaryResultsTabItem.Content = sv;
        }

        private void AddViewAllPropertiesLink(ResulItem resultItem, StackPanel spEntry)
        {
            DockPanel propdp = new DockPanel();
            var tb = new TextBox()
            {
                IsReadOnly = true,
                IsReadOnlyCaretVisible = false,
                Text = String.Format("{0}: ", "View all Properties..."),
                BorderBrush = null,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.DodgerBlue,
                FontSize = 14,
                FontWeight = FontWeights.Bold,

            };

            tb.PreviewMouseLeftButtonUp += OpenPreviewAllProperties;

            propdp.Children.Add
                (
                    tb
                );

            spEntry.Children.Add(propdp);
        }

        private void OpenPreviewAllProperties(object sender, MouseButtonEventArgs e)
        {
            //Todo Open new Window
            //Query a new search with refiner: "ManagedProperties(filter=600/0/*)
            //Extract all Properties from refiner result
            //Query again with the select properties set
            //Open the new window and show the properties there
            
        }

        void tb_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            RankDetail rd = new RankDetail();
            rd.DataContext = tb.DataContext;

            rd.Show();
        }


        /// <summary>
        /// Creates and populates the the Refinement results tab from data from the passed in <paramref name="searchResult"/>.
        /// This method is used for only query results.
        /// </summary>
        /// <param name="searchResult">The search result.</param>
        private void SetRefinementResultItems(SearchQueryResult searchResult)
        {
            ScrollViewer sv = new ScrollViewer();
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;

            if (searchResult.PrimaryQueryResult != null && searchResult.PrimaryQueryResult.RefinerResults != null
                && searchResult.PrimaryQueryResult.RefinerResults.Count > 0)
            {
                StackPanel spTop = new StackPanel() { Orientation = Orientation.Vertical };

                int counter = 1;

                foreach (var refinerItem in searchResult.PrimaryQueryResult.RefinerResults)
                {
                    StackPanel spEntry = new StackPanel() { Margin = new Thickness(25) };
                    TextBox titleTB = new TextBox()
                    {
                        IsReadOnly = true,
                        IsReadOnlyCaretVisible = false,
                        Text = String.Format("{0}. {1}", counter, refinerItem.Name),
                        BorderBrush = null,
                        BorderThickness = new Thickness(0),
                        Foreground = Brushes.DarkBlue,
                        FontSize = 18
                    };
                    spEntry.Children.Add(titleTB);

                    Expander propsExpander = new Expander() { IsExpanded = false, Header = "View Entries" };
                    StackPanel spProps = new StackPanel();

                    foreach (var re in refinerItem)
                    {
                        DockPanel propdp = new DockPanel();
                        propdp.Children.Add
                            (
                                new TextBox()
                                {
                                    IsReadOnly = true,
                                    IsReadOnlyCaretVisible = false,
                                    Text = "Refinement Name:",
                                    BorderBrush = null,
                                    BorderThickness = new Thickness(0),
                                    Foreground = Brushes.DarkGreen,
                                    FontWeight = FontWeights.Normal,
                                    FontSize = 12
                                }
                            );
                        propdp.Children.Add
                            (
                                new TextBox()
                                {
                                    IsReadOnly = true,
                                    IsReadOnlyCaretVisible = false,
                                    Text = String.Format("{0}", re.Name.Replace("\n", " ")),
                                    BorderBrush = null,
                                    BorderThickness = new Thickness(0),
                                    Foreground = Brushes.DarkMagenta,
                                    FontWeight = FontWeights.Normal,
                                    FontSize = 12
                                }
                            );
                        spProps.Children.Add(propdp);

                        propdp = new DockPanel();
                        propdp.Children.Add
                            (
                                new TextBox()
                                {
                                    IsReadOnly = true,
                                    IsReadOnlyCaretVisible = false,
                                    Text = "Refinement Count:",
                                    BorderBrush = null,
                                    BorderThickness = new Thickness(0),
                                    Foreground = Brushes.DarkGreen,
                                    FontWeight = FontWeights.Normal,
                                    FontSize = 12
                                }
                            );
                        propdp.Children.Add
                            (
                                new TextBox()
                                {
                                    IsReadOnly = true,
                                    IsReadOnlyCaretVisible = false,
                                    Text = String.Format("{0}", re.Count),
                                    BorderBrush = null,
                                    BorderThickness = new Thickness(0),
                                    Foreground = Brushes.DarkMagenta,
                                    FontWeight = FontWeights.Normal,
                                    FontSize = 12
                                }
                            );
                        spProps.Children.Add(propdp);

                        propdp = new DockPanel();
                        propdp.Children.Add
                            (
                                new TextBox()
                                {
                                    IsReadOnly = true,
                                    IsReadOnlyCaretVisible = false,
                                    Text = "Refinement Token:",
                                    BorderBrush = null,
                                    BorderThickness = new Thickness(0),
                                    Foreground = Brushes.DarkGreen,
                                    FontWeight = FontWeights.Normal,
                                    FontSize = 12
                                }
                            );
                        propdp.Children.Add
                            (
                                new TextBox()
                                {
                                    IsReadOnly = true,
                                    IsReadOnlyCaretVisible = false,
                                    Text = String.Format("{0}", re.Token.Replace("\n", " ")),
                                    BorderBrush = null,
                                    BorderThickness = new Thickness(0),
                                    Foreground = Brushes.DarkMagenta,
                                    FontWeight = FontWeights.Normal,
                                    FontSize = 12
                                }
                            );
                        spProps.Children.Add(propdp);

                        propdp = new DockPanel();
                        propdp.Children.Add
                            (
                                new TextBox()
                                {
                                    IsReadOnly = true,
                                    IsReadOnlyCaretVisible = false,
                                    Text = "Refinement Value:",
                                    BorderBrush = null,
                                    BorderThickness = new Thickness(0),
                                    Foreground = Brushes.DarkGreen,
                                    FontWeight = FontWeights.Normal,
                                    FontSize = 12
                                }
                            );
                        propdp.Children.Add
                            (
                                new TextBox()
                                {
                                    IsReadOnly = true,
                                    IsReadOnlyCaretVisible = false,
                                    Text = String.Format("{0}", re.Value.Replace("\n", " ")),
                                    BorderBrush = null,
                                    BorderThickness = new Thickness(0),
                                    Foreground = Brushes.DarkMagenta,
                                    FontWeight = FontWeights.Normal,
                                    FontSize = 12
                                }
                            );
                        spProps.Children.Add(propdp);

                        spProps.Children.Add(new Line() { Height = 18 });
                    }

                    propsExpander.Content = spProps;
                    spEntry.Children.Add(propsExpander);
                    spTop.Children.Add(spEntry);

                    counter++;
                }

                sv.Content = spTop;
            }

            this.RefinementResultsTabItem.Content = sv;
        }

        /// <summary>
        /// Creates and populates the the Secondary results tab from data from the passed in <paramref name="searchResult"/>.
        /// This method is used for only query results.
        /// </summary>
        /// <param name="searchResult">The search result.</param>
        private void SetSecondaryQueryResultItems(SearchQueryResult searchResult)
        {
            ScrollViewer sv = new ScrollViewer();
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;

            if (searchResult.SecondaryQueryResults != null && searchResult.SecondaryQueryResults.Count > 0)
            {
                StackPanel spTop = new StackPanel() { Orientation = Orientation.Vertical };

                int counter = 1;

                foreach (var sqr in searchResult.SecondaryQueryResults)
                {
                    if (sqr.RelevantResults == null || sqr.RelevantResults.Count == 0)
                        continue;

                    foreach (var resultItem in sqr.RelevantResults)
                    {
                        StackPanel spEntry = new StackPanel() { Margin = new Thickness(25) };

                        string resultTitle;
                        if (resultItem.ContainsKey("Title"))
                            resultTitle = resultItem["Title"];
                        else if (resultItem.ContainsKey("title"))
                            resultTitle = resultItem["title"];
                        else if (resultItem.ContainsKey("DocId"))
                            resultTitle = String.Format("DocId: {0}", resultItem["DocId"]);
                        else
                            resultTitle = "";

                        TextBox titleTB = new TextBox()
                        {
                            IsReadOnly = true,
                            IsReadOnlyCaretVisible = false,
                            Text = String.Format("{0}. {1}", counter, resultTitle),
                            BorderBrush = null,
                            BorderThickness = new Thickness(0),
                            Foreground = Brushes.DarkBlue,
                            FontSize = 14
                        };
                        spEntry.Children.Add(titleTB);

                        Expander propsExpander = new Expander() { IsExpanded = searchResult.SecondaryQueryResults.Count == 1, Header = "View" };
                        StackPanel spProps = new StackPanel();
                        foreach (var kv in resultItem)
                        {
                            DockPanel propdp = new DockPanel();
                            propdp.Children.Add
                                (
                                    new TextBox()
                                    {
                                        IsReadOnly = true,
                                        IsReadOnlyCaretVisible = false,
                                        Text = String.Format("{0}: ", kv.Key),
                                        BorderBrush = null,
                                        BorderThickness = new Thickness(0),
                                        Foreground = Brushes.DarkGreen,
                                        FontWeight = FontWeights.Bold,
                                        FontSize = 14
                                    }
                                );

                            propdp.Children.Add
                                (
                                    new TextBox()
                                    {
                                        IsReadOnly = true,
                                        IsReadOnlyCaretVisible = true,
                                        Text = String.Format("{0}", kv.Value),
                                        BorderBrush = null,
                                        BorderThickness = new Thickness(0),
                                        Foreground = Brushes.Green,
                                        FontSize = 14
                                    }
                                );

                            spProps.Children.Add(propdp);
                        }

                        propsExpander.Content = spProps;
                        spEntry.Children.Add(propsExpander);
                        spTop.Children.Add(spEntry);

                        counter++;
                    }
                }

                sv.Content = spTop;
            }

            this.SecondaryResultsTabItem.Content = sv;
        }

        /// <summary>
        /// Creates and populates the the Suggestion results tab from data from the passed in <paramref name="searchResult"/>.
        /// This method is used for only suggestion results.
        /// </summary>
        /// <param name="searchResult">The search result.</param>
        private void SetSuggestionsResultItems(SearchSuggestionsResult searchResult)
        {
            ScrollViewer sv = new ScrollViewer();
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;

            if (searchResult.SuggestionResults != null && searchResult.SuggestionResults.Count > 0)
            {
                StackPanel spTop = new StackPanel() { Orientation = Orientation.Vertical };

                int counter = 1;

                foreach (var resultITem in searchResult.SuggestionResults)
                {
                    StackPanel spEntry = new StackPanel() { Margin = new Thickness(25) };
                    TextBox queryTB = new TextBox()
                    {
                        IsReadOnly = true,
                        IsReadOnlyCaretVisible = false,
                        Text = String.Format("{0}. {1}", counter, resultITem.Query),
                        BorderBrush = null,
                        BorderThickness = new Thickness(0),
                        Foreground = Brushes.DarkBlue,
                        FontSize = 14
                    };

                    spEntry.Children.Add(queryTB);

                    TextBox isPersonalTB = new TextBox()
                    {
                        IsReadOnly = true,
                        IsReadOnlyCaretVisible = false,
                        Text = String.Format("IsPersonal: {0}", resultITem.IsPersonal),
                        BorderBrush = null,
                        BorderThickness = new Thickness(0),
                        Foreground = Brushes.Chocolate,
                        FontSize = 14
                    };

                    spEntry.Children.Add(isPersonalTB);


                    spTop.Children.Add(spEntry);

                    counter++;
                }

                sv.Content = spTop;
            }
            else
            {
                TextBox tb = new TextBox()
                {
                    BorderBrush = null,
                    BorderThickness = new Thickness(0),
                    IsReadOnly = true,
                    IsReadOnlyCaretVisible = false,
                    Text = "The query returned no items!",
                    Margin = new Thickness(30)
                };
                sv.Content = tb;
            }

            this.SuggestionResultsTabItem.Content = sv;
        }

        /// <summary>
        /// Updates the request URI string text block.
        /// </summary>
        private void UpdateRequestUriStringTextBlock()
        {
            try
            {
                if (this.SharePointSiteUrlTextBox != null)
                {
                    this.searchQueryRequest.SharePointSiteUrl = this.SharePointSiteUrlTextBox.Text.Trim();
                    this.searchSuggestionsRequest.SharePointSiteUrl = this.SharePointSiteUrlTextBox.Text.Trim();
                }

                var searchMethodType = CurrentSearchMethodType;
                var httpMethodType = CurrentHttpMethodType;
                if (searchMethodType == SearchMethodType.Query)
                {
                    if (httpMethodType == HttpMethodType.Get)
                        this.RequestUriStringTextBox.Text = this.searchQueryRequest.GenerateHttpGetUri().ToString();
                    else if (httpMethodType == HttpMethodType.Post)
                        this.RequestUriStringTextBox.Text = this.searchQueryRequest.GenerateHttpPostUri().ToString();
                }
                else if (searchMethodType == SearchMethodType.Suggest)
                {
                    this.RequestUriStringTextBox.Text = this.searchSuggestionsRequest.GenerateHttpGetUri().ToString();
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }

            this.RequestUriStringTextBox.Visibility = System.Windows.Visibility.Visible;
        }

        /// <summary>
        /// Marks the request operation by disabling or enabling controls.
        /// </summary>
        /// <param name="starting">if set to <c>true</c> [starting].</param>
        /// <param name="status">The status.</param>
        private void MarkRequestOperation(bool starting, string status)
        {
            this.RunButton.IsEnabled = !starting;

            if (starting)
            {
                this.ClearResultTabs();
                this.QueryGroupBox.IsEnabled = false;
                this.ConnectionGroupBox.IsEnabled = false;
            }
            else
            {
                this.QueryGroupBox.IsEnabled = true;
                this.ConnectionGroupBox.IsEnabled = true;
            }

            this.ProgressBar.Visibility = starting ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
            Duration duration = new Duration(TimeSpan.FromSeconds(30));
            DoubleAnimation doubleanimation = new DoubleAnimation(100.0, duration);

            if (starting)
                this.ProgressBar.BeginAnimation(ProgressBar.ValueProperty, doubleanimation);
            else
                this.ProgressBar.BeginAnimation(ProgressBar.ValueProperty, null);

            this.StateBarTextBlock.Text = status;
        }

        /// <summary>
        /// Clears the result tabs.
        /// </summary>
        private void ClearResultTabs()
        {
            this.StatsResultTabItem.Content = null;
            this.RawResultTabItem.Content = null;
            this.PrimaryResultsTabItem.Content = null;
            this.RefinementResultsTabItem.Content = null;
            this.SecondaryResultsTabItem.Content = null;
            this.SuggestionResultsTabItem.Content = null;
        }

        /// <summary>
        /// Shows the error.
        /// </summary>
        /// <param name="error">The error.</param>
        private void ShowError(Exception error)
        {
            this.ClearResultTabs();

            ScrollViewer sv = new ScrollViewer();
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;

            TextBox tb = new TextBox();
            tb.BorderBrush = null;
            tb.IsReadOnly = true;
            tb.TextWrapping = TextWrapping.Wrap;
            tb.IsReadOnlyCaretVisible = false;

            if (error != null)
            {
                tb.AppendText(error.Message + Environment.NewLine + Environment.NewLine);
            }

            Exception inner = error.InnerException;
            while (inner != null)
            {
                tb.AppendText(inner.Message + Environment.NewLine + Environment.NewLine);
                inner = inner.InnerException;
            }

            sv.Content = tb;
            this.StatsResultTabItem.Content = sv;
        }

        private void ShowMsgBox(string message)
        {
            MessageBox.Show(message);
        }

        private void LoadConnectionPropertiesFromFile()
        {
            try
            {
                string connectionPropFilePath = System.IO.Path.Combine(Environment.CurrentDirectory, ConnectionPropsXmlFileName);
                if (File.Exists(connectionPropFilePath))
                {
                    var connectionPropsElm = XElement.Load(connectionPropFilePath);
                    if (connectionPropsElm != null && connectionPropsElm.HasElements)
                    {
                        var spsiteurl = (string)connectionPropsElm.Element("spsiteurl");
                        if (!String.IsNullOrEmpty(spsiteurl))
                        {
                            this.SharePointSiteUrlTextBox.Text = spsiteurl;
                        }

                        var timeout = (string)connectionPropsElm.Element("timeout");
                        if (!String.IsNullOrEmpty(timeout))
                        {
                            this.WebRequestTimeoutTextBox.Text = timeout;
                        }

                        var accept = (string)connectionPropsElm.Element("accept");
                        if (!String.IsNullOrEmpty(accept))
                        {
                            if (accept.ToLower() == "json")
                                this.AcceptJsonRadioButton.IsChecked = true;
                            else if (accept.ToLower() == "xml")
                                this.AcceptXmlRadioButton.IsChecked = true;
                        }

                        var httpmethod = (string)connectionPropsElm.Element("httpmethod");
                        if (!String.IsNullOrEmpty(httpmethod))
                        {
                            if (httpmethod.ToLower() == "get")
                                this.HttpGetMethodRadioButton.IsChecked = true;
                            else if (httpmethod.ToLower() == "post")
                                this.HttpPostMethodRadioButton.IsChecked = true;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                ShowMsgBox("Failed to read connection properties. Error:" + ex.Message);
            }
        }

        /// <summary>
        /// Sets the impersonation level selections.
        /// </summary>
        private void SetCurrentWindowsUserIdentity()
        {
            WindowsIdentity currentWindowsIdentity = WindowsIdentity.GetCurrent();
            if (currentWindowsIdentity != null)
            {
                if (this.AuthenticationUsernameTextBox != null
                    && String.IsNullOrWhiteSpace(this.AuthenticationUsernameTextBox.Text))
                    this.AuthenticationUsernameTextBox.Text = currentWindowsIdentity.Name;
            }
        }

        /// <summary>
        /// Adds the copy command.
        /// </summary>
        /// <param name="control">The control.</param>
        private void AddCopyCommand(Control control)
        {
            ContextMenu cm = new ContextMenu();
            control.ContextMenu = cm;

            MenuItem mi = new MenuItem();
            mi.Command = ApplicationCommands.Copy;
            mi.CommandTarget = control;
            mi.Header = ApplicationCommands.Copy.Text;
            cm.Items.Add(mi);

            CommandBinding copyCmdBinding = new CommandBinding();
            copyCmdBinding.Command = ApplicationCommands.Copy;
            copyCmdBinding.Executed += CopyCmdBinding_Executed;
            copyCmdBinding.CanExecute += CopyCmdBinding_CanExecute;
            control.CommandBindings.Add(copyCmdBinding);
        }

        /// <summary>
        /// Handles the Executed event of the CopyCmdBinding control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ExecutedRoutedEventArgs" /> instance containing the event data.</param>
        private void CopyCmdBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            // Set text to clip board 
            if (sender is TreeViewItem)
            {
                Clipboard.SetText((string)(sender as TreeViewItem).Header);
            }
        }

        /// <summary>
        /// Handles the CanExecute event of the CopyCmdBinding control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CanExecuteRoutedEventArgs" /> instance containing the event data.</param>
        private void CopyCmdBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            //Check for text 
            if (sender is TreeViewItem)
            {
                TreeViewItem tvi = sender as TreeViewItem;
                if (tvi.Header != null)
                {
                    e.CanExecute = true;
                }
                else
                {
                    e.CanExecute = false;
                }
            }
        }

        /// <summary>
        /// Tries the convert to int.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        private static int? TryConvertToInt(string text)
        {
            int num = 0;
            if (Int32.TryParse(text, out num))
            {
                return num;
            }

            return null;
        }

        /// <summary>
        /// Tries the convert to long.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        private static long? TryConvertToLong(string text)
        {
            long num = 0;
            if (Int64.TryParse(text, out num))
            {
                return num;
            }

            return null;
        }

        #endregion

        private void ConnectToSignalR_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                _hubConnection = new HubConnection(SignalRHubUrlTextBox.Text);

                _hub = _hubConnection.CreateHubProxy("UlsHub");

                _hubConnection.StateChanged += change =>
                {
                    if (change.NewState == ConnectionState.Disconnected)
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            SignalRUrlImage.ToolTip = "Disconnected";
                            SignalRUrlImage.Source = new BitmapImage(new Uri("Images/alert_icon.png", UriKind.Relative));
                            SignalRUrlImage.Visibility = Visibility.Visible;
                            DebugTabItem.IsEnabled = false;
                        }));
                    }
                    else if (change.NewState == ConnectionState.Connected)
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            SignalRUrlImage.ToolTip = "Connected";
                            SignalRUrlImage.Source = new BitmapImage(new Uri("Images/connected_icon.png", UriKind.Relative));
                            SignalRUrlImage.Visibility = Visibility.Visible;
                            DebugTabItem.IsEnabled = true;
                        }));
                    }
                    else if (change.NewState == ConnectionState.Reconnecting)
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            SignalRUrlImage.ToolTip = "Reconnecting";
                            SignalRUrlImage.Source = new BitmapImage(new Uri("Images/reconnect_icon.png", UriKind.Relative));
                            SignalRUrlImage.Visibility = Visibility.Visible;
                            DebugTabItem.IsEnabled = false;
                        }));
                    }
                };

                _hub.On<LogEntry>("addSearchQuery", ProcessQueryLogEntry);

                _hubConnection.Start().Wait();
            }
            catch (Exception ex)
            {
                ShowMsgBox("Could not connect to signalr hub: " + ex.Message);
            }
        }

        public void LogMessageToFile(string msg)
        {
            System.IO.StreamWriter sw = System.IO.File.AppendText(
                "c:\\log.txt");
            try
            {
                string logLine = System.String.Format(
                    "{0}.", msg);
                sw.WriteLine(logLine);
            }
            finally
            {
                sw.Close();
            }
        }


        private void ProcessQueryLogEntry(LogEntry logEntry)
        {

            try
            {
                Regex firstQuery = new Regex("^Microsoft.Office.Server.Search.Query.Ims.ImsQueryInternal : New request: Query text '(.*)', Query template '(.*)'; HiddenConstraints:(.*); SiteSubscriptionId: (.*)");
                Regex personalResults = new Regex("^Microsoft.Office.Server.Search.Query.Pipeline.Processing.QueryRouterEvaluator : QueryRouterEvaluator: Received (.*) PersonalFavoriteResults.*");
                Regex relevantResults = new Regex("^Microsoft.Office.Server.Search.Query.Pipeline.Processing.QueryRouterEvaluator : QueryRouterEvaluator: Received (.*) RelevantResults results.*");
                Regex refinementResults = new Regex("^Microsoft.Office.Server.Search.Query.Pipeline.Processing.QueryRouterEvaluator : QueryRouterEvaluator: Received (.*) RefinementResults.*");
                Regex boundVariables = new Regex("^QueryClassifierEvaluator : (.*)$");

                //TODO simplify this logic
                if (logEntry.Message.Contains("Max Melcher"))
                    LogMessageToFile(logEntry.Message);

                if (firstQuery.IsMatch(logEntry.Message))
                {
                    var query = firstQuery.Match(logEntry.Message).Groups[1].Value;
                    var queryTemplate = firstQuery.Match(logEntry.Message).Groups[2].Value;
                    var hiddenConstraints = firstQuery.Match(logEntry.Message).Groups[3].Value;
                    var siteSubscriptionId = firstQuery.Match(logEntry.Message).Groups[4].Value;
                    if (!string.IsNullOrEmpty(query))
                    {
                        SearchQueryDebug debug = new SearchQueryDebug(logEntry.Correlation, Dispatcher);
                        debug.Query = string.Format("{0}", query);

                        if (!string.IsNullOrEmpty(queryTemplate))
                        {
                            debug.Template = string.Format("{0}", queryTemplate);
                        }

                        if (!string.IsNullOrWhiteSpace(hiddenConstraints))
                        {
                            debug.HiddenConstraint = string.Format("{0}", hiddenConstraints);
                        }

                        if (!string.IsNullOrEmpty(siteSubscriptionId))
                        {
                            debug.SiteSubscriptionId = string.Format("{0}", siteSubscriptionId);
                        }

                        ObservableQueryCollection.Add(debug);
                    }
                }
                else
                {
                    //locking? 
                    SearchQueryDebug debug = ObservableQueryCollection.FirstOrDefault(d => d.Correlation == logEntry.Correlation);
                    if (debug != null)
                    {
                        if (logEntry.Message.StartsWith("QueryTemplateHelper: "))
                        {
                            try
                            {
                                Monitor.Enter(_locker);
                                string queryTemplateHelper = logEntry.Message.Replace("QueryTemplateHelper: ", "");
                                debug.QueryTemplateHelper.Add(queryTemplateHelper);
                            }
                            finally
                            {
                                Monitor.Exit(_locker);
                            }
                        }
                        else if (logEntry.Category == "Linguistic Processing")
                        {
                            try
                            {
                                Monitor.Enter(_locker);
                                if (logEntry.Message.StartsWith("Microsoft.Ceres.ContentEngine.NlpEvaluators.QuerySuggestionEvaluator"))
                                {
                                    debug.QuerySuggestion = logEntry.Message.Replace("Microsoft.Ceres.ContentEngine.NlpEvaluators.QuerySuggestionEvaluator: ", "");
                                }
                                else if (string.IsNullOrEmpty(debug.QueryExpanded1))
                                {
                                    debug.QueryExpanded1 = logEntry.Message.Replace("Microsoft.Ceres.ContentEngine.NlpEvaluators.Tokenizer.QueryWordBreakerProducer: ", "");
                                }
                                else if (logEntry.Message.StartsWith("..."))
                                {
                                    debug.QueryExpanded3 += logEntry.Message.TrimStart(new char[] { '.' });
                                }
                                else if (string.IsNullOrEmpty(debug.QueryExpanded2))
                                {
                                    debug.QueryExpanded2 = logEntry.Message.Replace("Microsoft.Ceres.ContentEngine.NlpEvaluators.Tokenizer.QueryWordBreakerProducer: ", "").TrimEnd(new char[] { '.' });
                                }

                            }
                            finally
                            {
                                Monitor.Exit(_locker);
                            }
                        }
                        else if (boundVariables.IsMatch(logEntry.Message))
                        {
                            string value = boundVariables.Match(logEntry.Message).Groups[1].Value;
                            debug.BoundVariables.Add(value);
                        }
                        else if (relevantResults.IsMatch(logEntry.Message))
                        {
                            debug.RelevantResults = relevantResults.Match(logEntry.Message).Groups[1].Value;
                        }
                        else if (refinementResults.IsMatch(logEntry.Message))
                        {
                            debug.RefinerResults = refinementResults.Match(logEntry.Message).Groups[1].Value;
                        }
                    }
                    else if (ObservableQueryCollection.Any(q => logEntry.Message.Contains(q.Correlation))) //child correlation, this can be a very expensive query... 
                    {
                        if (logEntry.Message.StartsWith("Microsoft.Office.Server.Search.Query.Pipeline.Processing.QueryRouterEvaluator : QueryRouterEvaluator: "))
                        {
                            debug = ObservableQueryCollection.FirstOrDefault(q => logEntry.Message.Contains(q.Correlation));
                            if (debug != null) debug.PersonalResults = personalResults.Match(logEntry.Message).Groups[1].Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void QueryDoubleClick(object sender, MouseButtonEventArgs e)
        {
            QueryTextBox.Text = "Max Melcher";
        }
    }
}
