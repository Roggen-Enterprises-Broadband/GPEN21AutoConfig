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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Management.Automation;
using System.Collections.ObjectModel;

namespace GPEN21_Setup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void Setup_GPEN21(object sender, RoutedEventArgs e)
        {
            PowerShell ps      = PowerShell.Create();

            ps.AddScript(System.IO.File.ReadAllText(@"C:\Users\ncabral\Documents\GPEN21 Files\GPEN21 Config.ps1")).Invoke();
            ps.AddCommand("GPEN21-Data")
                .AddParameter("User", "\"admin\"")
                .AddParameter("Password", "\"ou812100#\"")
                .AddParameter("Hostname", "\"192.168.1.89\"");

            MessageBox.Show(ps.Invoke()[0].Properties["Values"].Value.ToString());
        }
    }
}
