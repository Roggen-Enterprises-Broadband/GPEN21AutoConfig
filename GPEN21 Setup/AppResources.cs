using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

// Stores general use functions that did not make sense to add to the MainWindow class.
namespace GPEN21_Setup
{
    class AppResources
    {
        // Class to hold general use methods.
        public static bool isNumeric(string str)
        {
            // Determines whether or not a string is numeric.
            // Only worries about whole numbers since decimals are not important in the scope of this program.

            bool retVal = true;

            for (int i = 0; i < str.Length; i++)
            {
                if ((str[i] < 48) || (str[i] > 57))
                {
                    retVal = false;
                }
            }

            return (retVal);
        }

        public static bool isNumeric(char ch)
        {
            // Determines whether or not a char is numeric.
            // Does not worry about the '.' or '-' characters since they are not important in the scope of this program.
            bool retVal = false;

            switch(ch)
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    retVal = true;
                    break;
            }

            return (retVal);
        }

        public static string makeNumeric(string str)
        {
            // Makes a string numeric by removing the non-numeric characters.
            string retVal = "";

            for (int i = 0; i < str.Length; i++)
            {
                if (AppResources.isNumeric(str[i]))
                {
                    retVal += str[i];
                }
            }

            return (retVal);
        }

        public static int power(int baseNum, int exp)
        {
            // Simple power function that only deals with whole unsigned exponents.
            // Non whole signed exponents are not important in the scope of this program.
            if (exp == 0)
            {
                return (1);
            }
            else
            {
                return (baseNum * power(baseNum, (exp - 1)));
            }
        }

        public static int strToInt(string str)
        {
            // Converts a string object to an int datatype.
            // Only deals with whole unsigned numbers.
            // Non whole signed numbers are not important in the scope of this program.
            int retVal   = 0;
            int position = (str.Length - 1);

            for (int i = 0; i < str.Length; i++)
            {
                retVal += ((str[i] - 48) * power(10, position));
                position--;
            }

            return (retVal);
        }

        public static string[] appendArray(string[] array, string newItem)
        {
            // Appends a new element in an array to the end of the array.

            // Instantiates an array with a length one element greater than the sent array.
            string[] retVal = new string[array.Length + 1];

            // Iterates through the array.
            for (int i = 0; i < array.Length; i++)
            {
                // Sets the value of the return array at an index to the value of the sent array at the same index.
                retVal[i] = array[i];
            }

            retVal[retVal.Length - 1] = newItem;

            return (retVal);
        }

        public static string[] split(string str, char delim)
        {
            // Splits the sent string into an array of strings based on the delimiter.

            string[] retVal = new string[0];
            string holder   = "";

            // Iterates through the sent string.
            for (int i = 0; i < str.Length; i++)
            {
                // Determines whether or not the char is a delimiter.
                if (str[i] == delim)
                {
                    // Adds the section of text in the holder to the return array, then clears the holder.
                    retVal = AppResources.appendArray(retVal, holder);
                    holder = "";
                }
                else
                {
                    // Adds chars to the holder string.
                    holder += str[i];
                }
            }

            //Sets the last value in the return array to be equal to the new value.
            retVal = AppResources.appendArray(retVal, holder);

            return (retVal);
        }

        public static string formatMACAddress(string str)
        {
            // Formats the mac address that is extracted from the GPEN21. (Adds the ':' character every two characters to the mac address.)
            string retVal = "";

            for (int i = 0; i < str.Length; i++)
            {
                if ((((i + 1) % 2) == 0) && (i != (str.Length - 1)))
                {
                    retVal += (str[i] + ":");
                }
                else
                {
                    retVal += str[i];
                }
            }

            return (retVal);
        }

        public static void writeLogFile(string content)
        {
            // Writes a the log to a file.
            if (!Directory.Exists(".\\logs"))
            {
                Directory.CreateDirectory(".\\logs");
            }

            StreamWriter ostream = File.AppendText(".\\logs\\log.txt");

            ostream.WriteLine(content);
            ostream.Close();
        }

        public static string addPreceedingZeros(string num, int digitsRequired)
        {
            // Adds a specified number of zeros before a set of chars.
            // I don't think this is used anywhere anymore.
            if (num.Length < digitsRequired)
            {
                for (int i = 0; (i < (digitsRequired - num.Length)); i++)
                {
                    num = ('0' + num);
                }
            }

            return (num);
        }

        public static string[] correctIP(string ip)
        {
            // Corrects the format of an IP address. (Removes '.' characters that are next to each other.)
            // I don't think this method is nearly as robust as I wanted it to be, I ran out of time and got busy doing other things.
            // I never came back to it.
            string[] retVal = new string[2] { "", "0"};

            for (int i = 0; i < ip.Length; i++)
            {
                if ((ip[i] == '.') && (i < (ip.Length - 1)))
                {
                    if (ip[i + 1] != '.')
                    {
                        retVal[0] += ip[i];
                    }
                    else
                    {
                        retVal[1] = "1";
                    }
                }
                else
                {
                    retVal[0] += ip[i];
                }
            }

            int dotCount = 0;

            for (int i = 0; i < retVal[0].Length; i++)
            {
                if (retVal[0][i] == '.')
                {
                    dotCount++;
                }
            }

            if (retVal[0].Length > 0)
            {
                if ((retVal[0][retVal[0].Length - 1] == '.') && (dotCount > 3))
                {
                    retVal[0] = retVal[0].Substring(0, (retVal[0].Length - 1));
                }
            }

            return (retVal);
        }

        public static string calculateIP(string VLAN, string CPENetwork)
        {
            // Calculates the IP address of a GPEN21 based on the VLAN that it is being configured to use.
            int vlan         = AppResources.strToInt(VLAN);
            int cpeNetwork   = AppResources.strToInt(CPENetwork);
            int custIPv4Low  = 2;
            int custIPv4High = 1;

            for (int i = 1501; i < vlan; i++)
            {
                custIPv4Low += 32;

                if (custIPv4Low >= 255)
                {
                    custIPv4High++;
                    custIPv4Low = 2;
                }
            }

            return ("10." + CPENetwork.ToString() + "." + custIPv4High.ToString() + "." + custIPv4Low.ToString());
        }
    }
}
