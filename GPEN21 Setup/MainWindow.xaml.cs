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
using System.IO;

namespace GPEN21_Setup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// []
    /// </summary>
    public partial class MainWindow : Window
    {
        // Class variables.
        // These variables are resources for the methods in this class.
        private string        username;
        private string        password;
        private string        physicalAddress;
        private string        hostname;
        private string[]      fieldData;
        private PowerShell    psMain;
        private PowerShell    psSecondary;
        private Thread        backgroundProcess;
        private bool          gpenDetected;
        private string        workingDirectory;
        private bool          defaultConfig;
        private bool          updateChecked;
        private bool          configurationProcessRunning;
        private string        log;

        //These are call back functions. They are required to call on objects/functions from the main class from a separate thread.
        private delegate void detect_gpen21_callback(int state);
        private delegate void logger_callback(string content);
        private delegate void messagebox_callback(string content);
        private delegate void increment_vlan_callback();
        private delegate void disable_user_input_callback();
        private delegate void enable_user_input_callback();

        public MainWindow()
        {
            // Constructor.
            InitializeComponent(); // Initializes GUI.

            this.log = "";

            // Each time the window opens, it reads from a file that contains all of the data that was in the text boxes last time the program was closed.
            // The data is then put back into the text boxes so that the user can pick up from where they left off without having to re-enter information that may be the as before.
            // Refer to line 149 in this code.
            if (Directory.Exists("C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\GPEN21_Setup") && File.Exists("C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\GPEN21_Setup\\data"))
            {
                string [] fieldData = AppResources.split(File.ReadAllText("C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\GPEN21_Setup\\data"), '\n');

                usernameTextBox.Text        = fieldData[0];
                passwordTextBox.Password    = fieldData[1];
                IPAddress.Text              = fieldData[2];
                PhysicalAddressTextBox.Text = fieldData[3];
                VLANTextBox.Text            = fieldData[4];
                POPIDTextBox.Text           = fieldData[5];
                POPMangTextBox.Text         = fieldData[6];
            }

            // This sets the log text area to be read only so that the user can only see and highlight the text in the log, not change it.
            LOG.IsReadOnly = true;

            // Instantiates the PowerShell Object that will be used in the background process.
            // The constant pings that are sent to the IP Address in the "IP Address" text box are done through this PowerShell Object.
            this.psSecondary = PowerShell.Create();

            // This signals whether or not the process that configures the GPEN 21 is running or not since it is ran in a separate thread
            // to prevent the UI from freezing while the GPEN 21 is being configured.
            // This signals whether or not the separate thread is still running.
            this.configurationProcessRunning = false;

            // GPEN21 Login Information.
            this.username         = usernameTextBox.Text;
            this.password         = passwordTextBox.Password;
            this.physicalAddress  = PhysicalAddressTextBox.Text;
            this.hostname         = IPAddress.Text;

            // This signals whether or not the ip address in the "IP Address" text box can be reached.
            this.gpenDetected = false;

            // This signals whether or not it will apply the default configuration or the configureation entered into the text boxes by the user.
            this.defaultConfig = false;

            // Sets the Current Working Directory as a string in the this.workingDirectory variable.
            this.workingDirectory = Environment.CurrentDirectory;

            // Initializes the background process the detects the GPEN21.
            // This process continuously pings the ip address entered into the "IP Address" text box.
            this.backgroundProcess = new Thread(() => {
                // Tells the Thread object to run the thread in the background so that it does not lock up the UI process.
                Thread.CurrentThread.IsBackground = true;

                // Executes the following method in a separate thread.
                // Refer to line 162 in this code.
                backgroundDetectGPEN21();
            });

            // Starts the background process that detects the GPEN21.
            this.backgroundProcess.Start();
            
            // Gets the current date and time.
            DateTime currentTime = DateTime.Now;

            // Writes the program startup information to the log.
            // Includes:
            //   start date, start time, running user, running machine name, and current working directory. 
            logger(
                "> "
                + AppResources.addPreceedingZeros(currentTime.Year.ToString(), 4)
                + AppResources.addPreceedingZeros(currentTime.Month.ToString(), 2)
                + AppResources.addPreceedingZeros(currentTime.Day.ToString(), 2)
                + AppResources.addPreceedingZeros(currentTime.Hour.ToString(), 2)
                + AppResources.addPreceedingZeros(currentTime.Minute.ToString(), 2)
                + AppResources.addPreceedingZeros(currentTime.Second.ToString(), 2)
                + " : Current User = " + Environment.UserName 
                + "\nMachine Name = " + Environment.MachineName 
                + "\nDate & Time = " + DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")
                + "\nCurrent Working Directory: \"" + Environment.CurrentDirectory + "\"\n");
        }

        ~MainWindow()
        {
            // Deconstructor.
            // Only executes when the application is terminated and resources are being deallocated.

            // This ensures that the background thread used to detect the GPEN 21 is stopped when the program terminates.
            // Otherwise it will keep running even after the program is closed.
            this.backgroundProcess.Abort();

            // This writes the field data that is stored in the text boxes at the time the program is closed to a file
            // so that the next time the program is started, it can put the data back into the text boxses to that the
            // user can pick up where they left off. Refer to line 63 in this code.
            if (Directory.Exists("C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\GPEN21_Setup"))
            {
                File.WriteAllText(("C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\GPEN21_Setup\\data"), (this.fieldData[0] + "\n" + this.fieldData[1] + "\n" + this.fieldData[2] + "\n" + this.fieldData[3] + "\n" + this.fieldData[4] + "\n" + this.fieldData[5] + "\n" + this.fieldData[6]));
            }
            else
            {
                Directory.CreateDirectory("C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\GPEN21_Setup");
                File.WriteAllText(("C:\\Users\\" + Environment.UserName + "\\AppData\\Local\\GPEN21_Setup\\data"), (this.fieldData[0] + "\n" + this.fieldData[1] + "\n" + this.fieldData[2] + "\n" + this.fieldData[3] + "\n" + this.fieldData[4] + "\n" + this.fieldData[5] + "\n" + this.fieldData[6]));
            }

            // Writes the log to a file so that historical data can be accessed in the future.
            AppResources.writeLogFile(this.log);
        }
        private void backgroundDetectGPEN21()
        {
            // Continuously pings the GPEN21 to determine whether or not it is connected to the network.
            // The hostname is pinged with one byte of data and a maximum timeout of 10ms.
            // Any device on the local network should respond within this amount of time.
            // This is meant to reduce the network utilization of the application and to decrease the amount of time the ping command takes to finish.
            // It should show 0Mbps in the task manager while it is running even though it is actively checking the connection.
            // Refer to line 111 in this code.

            // This is an infinite loop. It will run until the thread is aborted.
            while (true)
            {
                // Attempts to execute the block of code used to ping the ip address entered into the "IP Address" text box.
                try
                {
                    // This block of code will throw an error if it is unable to ping the address/hostname given to it.
                    if (!gpenConnected(this.psSecondary.AddScript("$pingout = (cmd /c ping " + this.hostname + " -n 1 -w 10 -l 1); $pingout").Invoke()[2].BaseObject.ToString(), this.hostname))
                    {
                        // Call back function. Refer to lines 46 and 205 of this code.
                        // Updates the UI in the main thread to reflect the Disconnected Status of the device.
                        // The value passed to the "detection" method is done at "{ 0 }" in the following line of code.
                        Dispatcher.BeginInvoke(new detect_gpen21_callback(detection), System.Windows.Threading.DispatcherPriority.Render, new object[] { 0 });
                    }
                    else
                    {
                        // Call back function. Refer to lines 46 and 205 of this code.
                        //Updates the UI in the main thread to reflect the Connected Status of the device.
                        // The value passed to the "detection" method is done at "{ 1 }" in the following line of code.
                        Dispatcher.BeginInvoke(new detect_gpen21_callback(detection), System.Windows.Threading.DispatcherPriority.Render, new object[] { 1 });
                    }
                }
                catch(Exception e)
                {
                    // If something goes wrong, the thread times out for five seconds.
                    // Sometimes the powershell object throws an error.
                    Thread.Sleep(5000);
                }

                // A one second wait produced the best connect/disconnect detection time.
                Thread.Sleep(1000);
            }
        }

        private void detection(int state)
        {
            // Updates the UI to reflect connection/disconnection status.

            switch (state)
            {
                case 0:
                    // The GPEN is not detected.
                    if (this.gpenDetected)
                    {
                        this.gpenDetected = false;

                        // Switches the image in the UI to the orange disconnected image with an X.
                        // The Uri Object cannot use relative paths.
                        // The current working directory has to be gotten and inserted into a string, then converted into a UriString object then passed to the Uri Object.
                        NotConnectedImage.Source         = new BitmapImage(new Uri(uriString: (Environment.CurrentDirectory + "\\Disconnected.png")));
                        ConnectivityNotification.Content = "Not Connected!";
                    }
                    break;
                case 1:
                    // The GPEN is detected.
                    if (!this.gpenDetected)
                    {
                        this.gpenDetected = true;

                        // Switches the image in the UI to the green connected image with a check mark.
                        // The Uri Object cannot use relative paths.
                        // The current working directory has to be gotten and inserted into a string, then converted into a UriString object then passed to the Uri Object.
                        NotConnectedImage.Source         = new BitmapImage(new Uri(uriString: (Environment.CurrentDirectory + "\\Connected.png")));
                        ConnectivityNotification.Content = "Connected!";
                    }
                    break;
            }
        }

        private void updateFieldData()
        {
            // Makes sure that the data stored in the array "fieldData" matches the data that is actually in the fields.
            if ((usernameTextBox != null) && (passwordTextBox != null) && (PhysicalAddressTextBox != null) && (IPAddress != null) && (VLANTextBox != null) && (POPIDTextBox != null) && (POPMangTextBox != null))
            {
                // The data is only updated if all of the above fields have been instantiated to avoid an error.
                // When the program is first starting up, this method will run before all of the fields have been fully instantiated and it will produce an error.
                this.fieldData = new string[7] { usernameTextBox.Text, passwordTextBox.Password, IPAddress.Text, PhysicalAddressTextBox.Text, VLANTextBox.Text, POPIDTextBox.Text, POPMangTextBox.Text };
            }
        }

        private void numeric_text_filter(object sender, KeyEventArgs e)
        {
            // Does not allow any characters other than numeric characters 0-9 to be entered into the text box.
            int keyboardKey = ((int)e.Key);

            // Determines whether or not the key pressed on the keyboard was one of the keys 0-9.
            // Keys 0-9 have ids 34-43 or 74-83 depending on whether or not the key was pressed on the numberpad or on the number row.
            if (!((keyboardKey >= 34) && (keyboardKey <= 43)) && !((keyboardKey >= 74) && (keyboardKey <= 83)))
            {
                // Some non numeric key presses need to be handeled.
                // If the tab button is pressed, which has an id of 3, the event is allowed to complete beyond this event handler.
                // Shift tab will also work.
                switch(keyboardKey)
                {
                    default:
                        // This tells the event handler to not process the default event for the sender object after this method finishes.
                        // Non digit characters except for the '.' character will not be printed into the text box.
                        e.Handled = true;
                        break;
                    case 3:
                        // This block executes when the tab key is pressed.
                        // This tells the event handler to process the default event after this method finishes executing.
                        // Allows the tab key to continue to function as default for the sender object. (Focus on next field.)
                        e.Handled = false;
                        break;
                    case 88:  // The switch statement will enter here if the user pressed the '.' key on the number pad since it is required to enter an IP Address.
                              // The switch cases will continue to the next automatically.
                    case 144: // The switch statement will enter here if the user pressed the '.' key on the keyboard (not the number pad) since it is required to enter an IP Address.
                        if ((sender as TextBox).Name.Equals("IPAddress"))
                        {
                            // If the sender object was the "IP Address" field, the default event is allowed to execute. (prints the '.' character into the text box.)
                            e.Handled = false;
                        }
                        else
                        {
                            // If the sender object was not the "IP Address" field, the default event is stopped from executing. (the '.' character is not printed into the text box.)
                            e.Handled = true;
                        }
                        break;
                }
            }

            // Updates the "fieldData" array to match the values in the text box fields.
            this.updateFieldData();
        }

        private void whitespace_change_event(object sender, TextChangedEventArgs e)
        {
            // This event handler executes on text change.
            // This event removes white space characters from the text in the text boxs that use this event handler.
            // Since TextBox Objects do not allow you to enter new line characters or tab characters, etc, this event handler
            // only needs to worry about removing the ' ' character from the text.

            TextBox senderObject    = sender as TextBox;
            string  noWhiteSpace    = "";
            int     selectionStart  = senderObject.SelectionStart;

            // Iterates through the text in the text box, and sets the noWhiteSpace string to be equal to the text in the text box without the space characters.
            for(int i = 0; i < senderObject.Text.Length; i++)
            {
                if (senderObject.Text[i] != 32)
                {
                    noWhiteSpace += senderObject.Text[i];
                }
            }

            // Sets the text in the text box to be equal to the noWhiteSpace string.
            // Moves the cursor in the text box to the end of the text in the text box so that the user can continue to type.
            senderObject.Text = noWhiteSpace;

            // Converts the text entered into the sender text box to an integer data type.
            // Will return 0 if the string entered is empty after the non numeric characters are removed.
            int enteredNumber = AppResources.strToInt(AppResources.makeNumeric(senderObject.Text));

            // Determines what to do based on the sender object's MaxLength attribute.
            switch (senderObject.MaxLength)
            {
                case 3: // Only executes if the sender object is the "POP Management #" text box.
                    if (((enteredNumber < 1) || (enteredNumber > 255)) && (senderObject.Text.Length > 0))
                    {
                        // Only executes if the entered number is less than 1, or greater than 255, and the text in the sender text box is not blank.
                        if (enteredNumber > 255)
                        {
                            // If the entered number is greater than 255, the number is changed to 255 which is the maximum number that the second octet in an IP address can be.
                            senderObject.Text = "255";
                        }
                        else if (enteredNumber < 1)
                        {
                            // If the entered number is less than 1, the number is changed to 1 since 1 is the first available POP management number available.
                            // The central office uses 0 in the second octet of it's IP range already, so a POP management number cannot be 0. This may change in the future.
                            // This was implemented to prevent unknowing techs from generating an IP that would interfere with the central office.
                            senderObject.Text = "1";
                        }
                    }
                    break;
                case 4: // Only executes if the sender object is the "VLAN" text box.
                    if (senderObject.Text.Length > 0)
                    {
                        // Only executes if the text in the VLAN text box is not blank.
                        if (((enteredNumber < 1501) || (enteredNumber > 3500)) && (senderObject.Text.Length == senderObject.MaxLength))
                        {
                            // Only executes if the number entered into the VLAN text box is greater than 1501, and less than 3500 since the customer VLANs start at 1501 and end at 3500.
                            // This prevents unknowing techs from setting a VLAN to a value that cannot be a customer connection.
                            if (enteredNumber > 3500)
                            {
                                // If the vlan entered is greater than 3500, the vlan is set to 3500 which is the maximum vlan number for a customer.
                                senderObject.Text = "3500";
                            }
                            else if (enteredNumber < 1501)
                            {
                                // If the vlan entered is less than 1501, the VLAN is set to 1501 which is the minimum vlan number for a customer.
                                senderObject.Text = "1501";
                            }
                        }
                    }
                    break;
            }

            // Updates the "fieldData" array to match the values in the text box fields.
            this.updateFieldData();

            // Moves the cursor in the text box to the end of the text in the text box.
            senderObject.SelectionStart  = selectionStart;
            senderObject.SelectionLength = 0;

            e.Handled = true;
        }

        private void make_uppercase_onchange(object sender, TextChangedEventArgs e)
        {
            TextBox senderObject = sender as TextBox;
            int selectionStart   = senderObject.SelectionStart;

            // Sets the text in the sender text box to be upper case.
            senderObject.Text = (sender as TextBox).Text.ToUpper();

            // Moves the cursor in the text box to the end of the text in the text box.
            senderObject.SelectionStart  = selectionStart;
            senderObject.SelectionLength = 0;

            // Updates the "fieldData" array to match the values in the text box fields.
            this.updateFieldData();

            // Prevents any default handler execution of the event.
            e.Handled = true;
        }

        private void update_username_onchange(object sender, TextChangedEventArgs e)
        {
            // Sets the username of this object equal to the text in the text box so that it can be accessed from a separate thread.
            this.username = (sender as TextBox).Text;

            // Updates the "fieldData" array to match the values in the text box fields.
            this.updateFieldData();
        }

        private void update_physicaladdress_onchange(object sender, TextChangedEventArgs e)
        {
            // Sets the physicalAddress of this object equal to the text in the text box so that it can be accessed from a separate thread.
            this.physicalAddress = (sender as TextBox).Text;

            // Updates the "fieldData" array to match the values in the text box fields.
            this.updateFieldData();
        }

        private void update_password_onchange(object sender, TextChangedEventArgs e)
        {
            // Sets the password of this object equal to the text in the text box so that it can be accessed from a separate thread.
            this.password = (sender as TextBox).Text;

            // Updates the "fieldData" array to match the values in the text box fields.
            this.updateFieldData();
        }

        private void update_ip_onchange(object sender, TextChangedEventArgs e)
        {
            // Prevents a user from entering two dots side by side in an IP address.
            // For example, entering two dots side by side.
            // Since the ip address in the "IP Address" field has changed if this event executes, it also resets the GPEN detected status to false, "Searching for IP...".

            TextBox senderObject = (sender as TextBox);
            int selectionStart   = senderObject.SelectionStart;

            // Updates the text in the sender object to be the corrected IP address.
            string[] correction         = AppResources.correctIP(senderObject.Text);

            // Sets the cursor position to be back where it was when it started.
            // You can enter a dot next to another in the middle of the IP address and the cursor will go back to where it was originally after the foul character was removed.
            senderObject.Text           = correction[0];
            senderObject.SelectionStart = (selectionStart - (AppResources.strToInt(correction[1])));

            // The GPEN detection status is reset.
            try
            {
                if (this.gpenDetected)
                {
                    // The orange circle with the X is displayed.
                    NotConnectedImage.Source = new BitmapImage(new Uri(uriString: (Environment.CurrentDirectory + "\\Disconnected.png")));
                    this.gpenDetected        = false;
                }
            }
            catch (Exception ex)
            {
                //Do Nothing. . .
            }

            // The detection status text is set to "Searching for IP..."
            ConnectivityNotification.Content = "Searching for IP. . .";

            // The variable that the detection thread uses to get the host name to ping is updated to match the IP entered into the text box.
            // Separate threads cannot access objects that exist only in the UI thread. They can however access objects that exist in the class that they belong to.
            this.hostname = senderObject.Text;

            // Updates the "fieldData" array to match the values in the text box fields.
            this.updateFieldData();
        }

        private void textbox_focus_event(object sender, EventArgs e)
        {
            // Highlights all of the text in a text box on focus.

            TextBox senderObject = (sender as TextBox);

            // Highlights the text in the sender text box.
            senderObject.SelectAll();
        }

        private void correct_vlan_lost_focus_event(object sender, EventArgs e)
        {
            // When the "VLAN" text box loses focus, the following code executes.

            TextBox senderObject   = sender as TextBox;
            string  noWhiteSpace   = "";
            int     selectionStart = senderObject.SelectionStart;

            if (senderObject.Text.Length > 0)
            {
                // Iterates through the text in the text box, and sets the noWhiteSpace string to be equal to the text in the text box without the space characters.
                for (int i = 0; i < senderObject.Text.Length; i++)
                {
                    if (senderObject.Text[i] != 32)
                    {
                        noWhiteSpace += senderObject.Text[i];
                    }
                }

                // Sets the text box text to be equal to the noWhiteSpace string.
                senderObject.Text = noWhiteSpace;

                int enteredNumber = AppResources.strToInt(AppResources.makeNumeric(senderObject.Text));

                // Only executes if the text in the VLAN text box is not blank.
                if (((enteredNumber < 1501) || (enteredNumber > 3500)) && (senderObject.Text.Length == senderObject.MaxLength))
                {
                    // Only executes if the number entered into the VLAN text box is greater than 1501, and less than 3500 since the customer VLANs start at 1501 and end at 3500.
                    // This prevents unknowing techs from setting a VLAN to a value that cannot be a customer connection.
                    if (enteredNumber > 3500)
                    {
                        // If the vlan entered is greater than 3500, the vlan is set to 3500 which is the maximum vlan number for a customer.
                        senderObject.Text = "3500";
                    }
                    else if (enteredNumber < 1501)
                    {
                        // If the vlan entered is less than 1501, the VLAN is set to 1501 which is the minimum vlan number for a customer.
                        senderObject.Text = "1501";
                    }
                }
            }

            // Updates the "fieldData" array to match the values in the text box fields.
            this.updateFieldData();

            // Moves the cursor in the text box to the end of the text in the text box.
            senderObject.SelectionStart  = selectionStart;
            senderObject.SelectionLength = 0;
        }

        private void update_physicaladdress_event(object sender, EventArgs e)
        {
            // Updates the "fieldData" array to match the values in the text box fields.
            this.updateFieldData();
        }

        private void default_change_event(object sender, EventArgs e)
        {
            // I am pretty sure this is not used at the moment.

            //If the box is checked, then "not isChecked" will be false, disabling the fields.
            //If the box is not checked then "not isChecked" will be true, enabling the fields.

            bool isChecked = (bool)(sender as CheckBox).IsChecked; //True if the check box is checked.
                                                                   //False if the check box is not checked.
            // Enables or disables the configuration fields based on whether or not the "Apply Default" check box is checked.
            // Checking the "Apply Default" check box will disable PhysicalAddressTextBox, VLANTextBox, POPIDTextBox, POPMangTextBox,
            // and set the defaultConfig variable to True signaling that the default configuration needs to be applied.
            PhysicalAddressTextBox.IsEnabled = !isChecked;
            VLANTextBox.IsEnabled            = !isChecked;
            POPIDTextBox.IsEnabled           = !isChecked;
            POPMangTextBox.IsEnabled         = !isChecked;
            this.defaultConfig               = isChecked;
        }

        private void update_password_event(object sender, EventArgs e)
        {
            // Sets the password of this object equal to the text in the text box so that it can be accessed from a separate thread.
            this.password = (sender as PasswordBox).Password;

            // Updates the "fieldData" array to match the values in the text box fields.
            this.updateFieldData();
        }

        private void logger(string content)
        {
            // Writes to the log which is currently a TextBox in the UI window.
            // Currently used for testing purposes.
            // Can be used like STDOUT in a console application.

            //Determines whether or not there is already stuff in the log.
            if (LOG.Text.ToString().Length > 0)
            {
                // If there is, then a new line is appended.
                LOG.Text += "\n";
            }

            // Sets log Text.
            LOG.Text += content;
            this.log = LOG.Text;

            // Scrolls the log text area to the end so that the newest logs are displayed.
            LOG.ScrollToEnd();
        }

        private void background_logger(string content)
        {
            // This call back method allows the "logger" method to update the log text area from a separate thread.
            // Separate threads cannot directly access objects in the UI thread so they have to invoke methods
            // in the main thread to access the UI objects while their own thread is executing.
            Dispatcher.BeginInvoke(new logger_callback(this.logger), System.Windows.Threading.DispatcherPriority.Render, new object[] { content });
        }

        private void background_messageBox(string content)
        {
            // This call back method allows a separate thread to access the MessageBox object.
            // The MessageBox object exists in the UI thread, so it cannot be directly accessed from a separate thread.
            // Separate threads have to invoke a method in the main thread to access the MessageBoxThread.
            Dispatcher.BeginInvoke(new messagebox_callback((string message) => {
                // Instead of using a pre-defined call back method, the method block of code is being passed to the Dispatcher.BeginInvoke method as a parameter.
                // This is done all the time in Javascript. Look up some javascript examples to get what is going on here.
                MessageBox.Show(message);
            }), System.Windows.Threading.DispatcherPriority.Render, new object[] { content });
        }

        private bool gpenConnected(string ping, string hostname)
        {
            // Pings the IP given and looks for "Reply from {IP}" in the output of the ping command to determine whether or not the IP is in the network.
            
            return (AppResources.split(ping, ':')[0].Equals("Reply from " + hostname));
        }

        private void incrementVLAN()
        {
            // Increments the VLAN number in the text box to the next value.
            // Used after each GPEN configuration successfully completes.
            // Not currently used because GPENs ended up being programmed in a random order.
            if ((VLANTextBox.Text.Length > 0) && AppResources.isNumeric(VLANTextBox.Text))
            {
                VLANTextBox.Text = (Int32.Parse(VLANTextBox.Text) + 1).ToString();
            }
            else
            {
                // If the input filters functioned properly, this error message should never be displayed.
                MessageBox.Show("Error: '" + VLANTextBox.Text + "' is not numeric.");
            }
        }

        private void Increment_VLAN(object sender, RoutedEventArgs e)
        {
            // I am not sure why this was used.
            // I am leaving it here until I can figure out why I created it.
            this.incrementVLAN();
        }

        private void background_increment_vlan()
        {
            // Allows the incrementVLAN() method to be used from a separate thread.
            // Objects in the UI thread cannot be accessed from another thread.
            // Separate threads have to invoke a method in the main thread to make changes to objects in the UI thread.
            Dispatcher.BeginInvoke(new increment_vlan_callback(this.incrementVLAN), System.Windows.Threading.DispatcherPriority.Render, new object[] { });
        }

        private void disable_user_input()
        {
            // Disables user input.
            // Used while the configuration is being applied by the configuration thread to prevent errors from occurring in the configuration thread.
            usernameTextBox.IsReadOnly        = true;
            passwordTextBox.IsEnabled         = false;
            PhysicalAddressTextBox.IsReadOnly = true;
            IPAddress.IsReadOnly              = true;
            VLANTextBox.IsReadOnly            = true;
            POPIDTextBox.IsReadOnly           = true;
            POPMangTextBox.IsReadOnly         = true;

            updateBox.IsEnabled           = false;
            GOButton.IsEnabled            = false;
            IncrementVLANButton.IsEnabled = false;
        }

        private void enable_user_input()
        {
            // Enables user input.
            // Allows the user to make changes to the fields only after the configuration thread has completed.
            usernameTextBox.IsReadOnly        = false;
            passwordTextBox.IsEnabled         = true;
            PhysicalAddressTextBox.IsReadOnly = false;
            IPAddress.IsReadOnly              = false;
            VLANTextBox.IsReadOnly            = false;
            POPIDTextBox.IsReadOnly           = false;
            POPMangTextBox.IsReadOnly         = false;

            updateBox.IsEnabled           = true;
            GOButton.IsEnabled            = true;
            IncrementVLANButton.IsEnabled = true;
        }

        private void background_disable_user_input()
        {
            // Allows the separate configuration thread to disable user input.
            // Objects that exist in the UI thread cannot be accessed by separate threads.
            // This call back allows the configuration thread thread to invoke the disable_user_input method in the main thread to make changes to the UI objects.
            Dispatcher.BeginInvoke(new disable_user_input_callback(this.disable_user_input), System.Windows.Threading.DispatcherPriority.Render, new object[] { });
        }

        private void background_enable_user_input()
        {
            // Allows the separate configuration thread to enable user input.
            // Objects that exist in the UI thread cannot be accessed by separate threads.
            // This call back allows the configuration thread thread to invoke the enable_user_input method in the main thread to make changes to the UI objects.
            Dispatcher.BeginInvoke(new enable_user_input_callback(this.enable_user_input), System.Windows.Threading.DispatcherPriority.Render, new object[] { });
        }

        private void Setup_GPEN21(object sender, RoutedEventArgs e)
        {
            // This function attempts to log into the IP address and get the mac address from the sys.b output from the IP over HTTP.
            // If it does not get anything, then the device is not a GPEN21 and this function fails.
            // If it does get a mac address back from the IP, then it is safe to assume that it is a GPEN21.

            this.updateChecked = (bool)updateBox.IsChecked;
            this.psMain        = PowerShell.Create(); // Instantiates the PowerShell Object that will be used in the configuration process.
                                                      // The PowerShell Object that is used in the detection thread cannot be used in the
                                                      // configuration thread as well because it will produce an error.
                                                      // The PowerShell Object only has one thread that it can use at a time.

            // The plain text containing the functions in the powershell script are read into the powershell object and executed
            // so that they can be accessed from the powershell object's runtime.
            this.psMain.AddScript(System.IO.File.ReadAllText(@".\GPEN21_Config.ps1")).Invoke();

            // The command to set the working directory of the powershell object is executed to set the working directory of the powershell object
            // to be the same as the working directory of this program.
            // Believe it or not, the powershell object will default to the C:\Windows\system32 working directory for some unknown reason instead
            // of using the working directory of the script itself.
            this.psMain.AddCommand("Set-Location")
                .AddParameter("Path", Environment.CurrentDirectory)
                .Invoke();

            // Adds the GPEN21-MAC function command to the powershell object, but it is not executed yet.
            this.psMain.AddCommand("GPEN21-MAC")
                .AddParameter("User", this.username)
                .AddParameter("Password", this.password)
                .AddParameter("Hostname", this.hostname);
            
            // Indicates that the program is getting the mac address of the GPEN in the log.
            this.logger("Getting MAC Address of GPEN at " + this.hostname + ". . .");

            // Attempts to begin the configuration process.
            try
            {
                // The GPEN21-MAC command is executed. See line 714 of this code.
                // If the username, password, and hostname were correct for the GPEN, it's mac address is printed to the stdout of the powershell object.
                // The stdout of the powershell object is extracted and stored in the gpenMac string. The stdout of the invoked command should only be the mac address of the GPEN.
                // This indicates whether or not the username, password, and hostname was correct before it attempts to make configuration changes to the GPEN at that hostname.
                string gpenMac = this.psMain.Invoke()[0].BaseObject.ToString();

                if (gpenMac.Length > 0)
                {
                    // If at this point the MAC Address length is greater than 0, then it is safe to assume that the device at the hostname is a
                    // GPEN21 and the username and password were correct. The program can continue.

                    if (this.updateChecked)
                    {
                        // If the "Install Update" check box is checked, the command to run the Update-GPEN21 function in the powershell script is added
                        // to the powershell object and executed to apply the update.

                        //Updates the log to indicate that it is updating the GPEN21.
                        this.background_logger("Updating GPEN21. . .");

                        this.psMain.AddCommand("Update-GPEN21")
                            .AddParameter("User", this.username)
                            .AddParameter("Password", this.password)
                            .AddParameter("Hostname", this.hostname)
                            .Invoke();
                    }

                    // Instantiates the configuration thread.
                    // The actual configuring is done in this thread.
                    Thread configurationThread = new Thread(() => {
                        //Indicates that the mac address of the GPEN21 at the indicated IP address in the log.
                        this.background_logger("MAC Address of GPEN21 at " + this.hostname + " = " + AppResources.formatMACAddress(gpenMac.Substring(1, gpenMac.Length - 2)) + ".");

                        // Creates an array with the field names in order.
                        string[] names = new string[] { "Physical Address", "VLAN (1501 - 3547)", "POP ID", "POP Management #" };
                        string errors  = "";

                        // Checks to make sure that all of the required fields are filled out.
                        for (int i = 3; (i < this.fieldData.Length) && !this.defaultConfig; i++)
                        {
                            if (this.fieldData[i].Length == 0)
                            {
                                // Loggs the name of the required field that was not field out.
                                if (errors.Length > 0)
                                {
                                    errors += "\n";
                                }

                                errors += "\"" + names[i - 3] + "\" is a required field.";
                            }
                        }

                        if (errors.Length > 0)
                        {
                            // Executes if there were errors.
                            // Displays the errors detected.
                            background_logger("Error!\n" + errors);
                            background_messageBox(errors);
                        }
                        else
                        {
                            // Sets the path to store the config file.
                            string configPath = "";

                            if (!this.defaultConfig)
                            {
                                // Executes only if the "Apply Default" checkbox is not checked.
                                // Generates the configuration file for the GPEN21 and stores it in the current working directory.

                                //Indicates that it is generating a configuration file in the log.
                                // The try block that this code is located in will error out if there are problems
                                // during the configuration process.
                                background_logger("Generating Configuration File. . .");

                                // Adds the command to execute the function from the powershell script to generate a configuration to the powershell object.
                                // The command is added with the data from the text boxes hard coded into it.
                                // Executes the command to generate a configuration with the indicated parameters.
                                // The stdout of the command execution is the path to the configuration file.
                                // The stdout of the command is extracted and stored in the "configPath" string.
                                configPath = this.psMain.AddScript("genConfig(\"" + this.fieldData[3] + "\"," + this.fieldData[4] + ",\"" + this.fieldData[5] + "\"," + this.fieldData[6] + ",\"" + gpenMac + "\");").Invoke()[0].BaseObject.ToString();
                            }
                            else
                            {
                                // Executes only if the "Apply Default" checkbox is checked.

                                // Indicates that the default configuration file is going to be uploaded.
                                // The try block that this code is located in will error out if there are problems
                                // during the configuration process.
                                background_logger("Uploading Default Configuration File. . .");
                                
                                // Sets the path to the default configuration file.
                                configPath = "./DEFAULT_CONFIG.swb";
                            }
                            
                            // Adds the command to apply the configuration file to the GPEN21.
                            // Executes the command.
                            this.psMain.AddCommand("RESTOREBACKUP-GPEN21")
                                .AddParameter("User", this.username)
                                .AddParameter("Password", this.password)
                                .AddParameter("Path", configPath)
                                .AddParameter("Hostname", this.hostname)
                                .Invoke();

                            // If this command executes without throwing any errors, the log indicates that the gpen21 configuration was generated, and saved in the configPath.
                            // The log also indicates that the GPEN21 was rebooted.
                            // The try block that this code is located in will error out if there are problems
                            // during the configuration process.
                            background_logger("The configuration file was successfully generated and saved at " + configPath + "\nRebooting GPEN21 at " + this.hostname + ". . .");

                            // The command to execute the function in the powershell script to reboot the GPEN21 is added to the powershell object.
                            // The command is executed.
                            this.psMain.AddCommand("Reboot-GPEN21")
                                .AddParameter("User", this.username)
                                .AddParameter("Password", this.password)
                                .AddParameter("Hostname", this.hostname)
                                .Invoke();

                            // If nothing errors out, the log indicates that the configuration has been completed.
                            // The try block that this code is located in will error out if there are problems
                            // during the configuration process.
                            background_logger("Configuration complete.");
                            
                            if (!this.defaultConfig)
                            {
                                // If the "Apply Default" checkbox is not checked.
                                // Log is updated to indicate the IP address that the GPEN21 was statically set too.
                                // The try block that this code is located in will error out if there are problems
                                // during the configuration process.
                                background_logger("GPEN21 IP statically set to " + AppResources.calculateIP(this.fieldData[4], this.fieldData[6]));

                                // Increments the vlan number in the VLAN text box.
                                // Currently, this behavior is disabled because the GPEN21s have been configured in random order.
                                //background_increment_vlan();
                            }
                            else
                            {
                                // If the "Apply Default" checkbox is checked.
                                // Log is updated to indicate that the GPEN was configured to use DHCP with the fallback IP.
                                // The try block that this code is located in will error out if there are problems
                                // during the configuration process.
                                background_logger("GPEN21 IP Address Aquisition set to DHCP with fallback. . .\nRefer to the DHCP server to find the IP address. . .");
                            }

                            // The log is updated to indicate that the configuration has been completed.
                            // The try block that this code is located in will error out if there are problems
                            // during the configuration process.
                            background_messageBox("Configuration Complete.");

                            background_logger("\r");
                        }

                        // Indicates that the configuration thread is no longer running.
                        // I don't think this is currently used.
                        this.configurationProcessRunning = false;
                    });

                    // Starts the configuration thread.
                    configurationThread.Start();
                }
                else
                {
                    // Displays an error notifying the user that the GPEN21 could not be connected to.
                    logger("Error! Verify the connection to the GPEN21.");
                    MessageBox.Show("Error! Verify the connection to the GPEN21.");
                }
            }
            catch (ArgumentOutOfRangeException exception)
            {
                // This should only execute if the GPEN was not able to be contacted.

                // If the try block stops because an error was thrown, the following information is added to the log.
                logger("ERROR: Unable to obtain a MAC Address. Please verify the GPEN21's IP Address.\n\nPossible Causes:\n\tThe GPEN21 is not connected to the network.\n\tThe GPEN21 is not using the IP Address " + this.hostname + ".\n\tThe GPEN21 has a password set on the admin login.");
                
                // If the try block stops because an error was thrown, a popup message is displayed on the screen to indicate possible causes for the error.
                MessageBox.Show("ERROR: Unable to obtain a MAC Address. Please verify the GPEN21's IP Address.\n\nPossible Causes:\n\tThe GPEN21 is not connected to the network.\n\tThe GPEN21 is not using the IP Address " + this.hostname + ".\n\tThe GPEN21 has a password set on the admin login.");
            }
        }

        private void vlan_text_change_event(object sender, TextChangedEventArgs e)
        {
            // Not used.
        }
    }
}
