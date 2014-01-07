using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SearchQueryTool.Helpers;

namespace SearchQueryTool
{
    /// <summary>
    /// Interaction logic for PropertiesDetail.xaml
    /// </summary>
    public partial class PropertiesDetail : Window
    {
        public ResultItem Item { get; set; }

        PropertiesDetail(ResultItem item)
        {
            InitializeComponent();
            Item = item;
        }
    }
}
