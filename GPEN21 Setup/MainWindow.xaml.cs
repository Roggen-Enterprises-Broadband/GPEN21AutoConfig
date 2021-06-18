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
using System.Threading;

namespace GPEN21_Setup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// []
    /// </summary>
    public partial class MainWindow : Window
    {
        private string                     username;
        private string                     password;
        private string                     hostname;
        private PowerShell                 ps;
        private System.Windows.Forms.Timer timer1;

        public MainWindow()
        {
            InitializeComponent();

            this.ps       = PowerShell.Create();
            this.username = "admin";
            this.password = "";
            this.hostname = "192.168.1.89";

            this.timer1          = new System.Windows.Forms.Timer();
            this.timer1.Tick    += new EventHandler(TimerEventProcessor);
            this.timer1.Interval = 30000;
            this.timer1.Start();
        }

        private void TimerEventProcessor(Object eventObject, EventArgs eventArgs)
        {
            detectGpen();
        }

        private void detectGpen()
        {
            if (!gpenConnected(ps.AddScript("$pingout = (cmd /c ping " + hostname + " -n 1); $pingout").Invoke()[2].BaseObject.ToString(), hostname))
            {
                log("GPEN not connected.");
                NotConnectedImage.Source = new BitmapImage(new Uri(@"C:\Users\ncabral\Documents\GPEN21 Files\GPEN21 Setup\Disconnected.png"));
                ConnectivityNotification.Content = "GPEN21 Not Connected!";
            }
            else
            {
                NotConnectedImage.Source = new BitmapImage(new Uri(@"C:\Users\ncabral\Documents\GPEN21 Files\GPEN21 Setup\Connected.png"));
                ConnectivityNotification.Content = "GPEN21 Connected!";
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void log(string content)
        {
            if (LOG.Text.ToString().Length > 0)
            {
                LOG.Text += "\n";
            }

            LOG.Text += content;
        }

        private string[] appendArray(string[] array, string newItem)
        {
            string[] retVal = new string[array.Length + 1];

            for (int i = 0; i < array.Length; i++)
            {
                retVal[i] = array[i];
            }

            retVal[retVal.Length - 1] = newItem;

            return (retVal);
        }

        private string[] split(string str, char delim)
        {
            string[] retVal = new string[0];
            string   holder = "";

            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == delim)
                {
                    retVal = appendArray(retVal, holder);
                    holder = "";
                }
                else
                {
                    holder += str[i];
                }
            }

            retVal = appendArray(retVal, holder);

            return (retVal);
        }

        private Boolean gpenConnected(string ping, string hostname)
        {
            return (split(ping, ':')[0].Equals("Reply from " + hostname));
        }

        private void Setup_GPEN21(object sender, RoutedEventArgs e)
        {
            ps.AddScript(System.IO.File.ReadAllText(@".\GPEN21 Config.ps1")).Invoke();
            ps.AddCommand("GPEN21-MAC")
                .AddParameter("User", username)
                .AddParameter("Password", password)
                .AddParameter("Hostname", hostname);

            log("Getting MAC Address of GPEN at " + hostname + ". . .");

            string gpenMac = ps.Invoke()[0].BaseObject.ToString();

            if (gpenMac.Length > 0)
            {
                log("MAC Address of GPEN21 at " + hostname + " is " + gpenMac + ".");
                string[] names = new string[] { "VLAN (1501 - 3547)", "POP Management #", "POP ID" };
                string[] userInput = new string[] { VLANTextBox.Text, POPMangTextBox.Text, POPIDTextBox.Text.ToUpper() };
                string   errors = "";

                for (int i = 0; i < userInput.Length; i++)
                {
                    if (userInput[i].Length == 0)
                    {
                        if (errors.Length > 0)
                        {
                            errors += "\n";
                        }

                        errors += "\"" + names[i] + "\" is a required field.";
                    }
                }

                if (errors.Length > 0)
                {
                    log("Error!\n" + errors);
                    MessageBox.Show(errors);
                }
                else
                {
                    log("Generating Configuration File. . .");
                    string configPath = ps.AddScript("genConfig(" + userInput[0] + ", " + userInput[1] + ",\"" + userInput[2] + "\",\"" + gpenMac + "\");").Invoke()[0].BaseObject.ToString();
                    ps.AddCommand("RESTOREBACKUP-GPEN21")
                        .AddParameter("User", username)
                        .AddParameter("Password", password)
                        .AddParameter("Path", configPath)
                        .AddParameter("Hostname", hostname)
                        .Invoke();

                    log("The configuration file was successfully generated and saved at " + configPath + "\nRebooting GPEN21 at " + hostname + ". . .");

                    ps.AddCommand("Reboot-GPEN21")
                        .AddParameter("User", username)
                        .AddParameter("Password", password)
                        .AddParameter("Hostname", hostname)
                        .Invoke();
                    
                    log("Configuration complete.");
                }
            }
            else
            {
                log("Error! Verify the connection to the GPEN21.");
                MessageBox.Show("Error! Verify the connection to the GPEN21.");
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            detectGpen();
        }
    }
}
