#When installing curl, add the path to the working directory for curl in the PATH variable in order to make it work from the console.
#    Instructions:
#        1. Press WIN+PAUSE|BREAK
#        2. Click "Advanced system settings"
#        3. Click "Environment Variables..."
#        4. Under the "System variables" section click the "Path" variable.
#        5. Click "New".
#        6. In the text box, type or paste the path to the curl working directory.

#For some reason Invoke-Webrequest is aliased as curl in powershell.
#That alias needs to be removed to use the actual curl.
$sysB = $null;

if (Test-Path Alias:\curl)
{
    Remove-Item alias:curl;
}

function StringToHex($i)
{
    $r = ""

    $i.ToCharArray() | foreach-object -process {
        $r += '{0:X}' -f [int][char]$_
    }

    return $r
}

function remCloseChrs($string)
{
    return ($string.Substring(1, ($string.length - 2)));
}

function makeHash($string)
{
    $hashTable = $null;
    
    if (($string[0] -eq '{') -and ($string[$string.length - 1] -eq '}'))
    {
        $hashTable     = @{};
        $parameterList = remCloseChrs($string);
        $parameterList = $parameterList.split(",")

        for ($i = 0; ($i -lt $parameterList.Length); $i++)
        {
            $data  = $parameterList[$i];
            $data  = $data.split(":");
            $hashTable.add($data[0], $data[1]);
        }
    }

    return $hashTable;
}

#Reboots the specified GPEN21.
function Reboot-GPEN21
{
    param(
        $User,
        $Password = "",
        $Hostname
    );

    $u = ($User + ":" + $Password);

    curl --digest -s -u $u -d "$" ($Hostname + "/reboot");
}

#Updates a GPEN21 from a bin file.
function Update-GPEN21
{
    param(
        $User,
        $Password = "",
        $Path,
        $Hostname
    );

    $u = ($User + ":" + $Password);
    $f = ("`"f=@" + $Path + "`"");

    curl --digest -s -u $u -F $f ($Hostname + "/upgrade");
    Reboot-GPEN21 -User $User -Password $Password -Host $Hostname;
}

#Restores Backup to a GPEN21.
function RESTOREBACKUP-GPEN21
{
    param(
        $User,
        $Password = "",
        $Path,
        $Hostname
    );

    $u = ($User + ":" + $Password);
    $f = ("`"f=@" + $Path + "`"");

    curl --digest -s -u $u -F $f ($Hostname + "/backup.swb");
    Reboot-GPEN21 -User $User -Password $Password -Host $Hostname;
}

function genConfig($vals)
{
    [int]$vlanID            = $vals[0] -as [int];
    [int]$CPENetwork        = $vals[1] -as [int];
    [string]$POPID          = $vals[2] -as [string];
    [string]$MACAddress     = $vals[3] -as [string];
    $custIPv4low            = 2;
    $custIPv4hi             = 0;
    $IncrementToNextNetwork = 32;
    $i                      = 1501;

    while ($i -lt $vlanID)
    {
        $i++;
        $custIPv4low = $custIPv4low + $IncrementToNextNetwork; #increment the IPs that we are using to the next network

        if($custIPv4low -ge 255) #When we go above 255 we need to increment the next higher parts of the IP and reset the lower parts of the IP
        {
            $custIPv4hi++;
            $custIPv4low = 2;
        }
    }

    $vlanIDHEX = ('{0:X}' -f $vlanID).PadLeft(4,"0");
    $vlanIDHEX = $vlanIDHEX.ToLower();
    $cpeName   = $POPID + "Customer" + $vlanID;
    $cpeName   = StringToHex($cpeName);
    $cpeName   = $cpeName.ToLower();
    
    $gpenIP = ('{0:X}' -f $custIPv4low).PadLeft(2, "0") + ('{0:X}' -f $custIPv4hi).PadLeft(2, "0") + ('{0:X}' -f $CPENetwork).PadLeft(2, "0") + ('{0:X}' -f 10).PadLeft(2, "0");
    $gpenIP = $gpenIP.ToLower();

    $vlanIDHEX = ('{0:X}' -f $vlanID).PadLeft(4,"0");
    $vlanIDHEX = $vlanIDHEX.ToLower();

    $cpeName = $POPID + "Customer" + $vlanID;
    $cpeName = StringToHex($cpeName);
    $cpeName = $cpeName.ToLower();

    $outputFile = "./" + $POPID + "CustomerGPEN-" + $vlanID + ".swb";
    $retVal     = "./" + $POPID + "CustomerGPEN-" + $vlanID + ".swb";

    "test" | Add-Content -Path $outputFile;
    Clear-Content -Path $outputFile;

    ".pwd.b:{i01:'6f7538313231303023'},sys.b:{i05:'" + $cpeName + "',i12:0x06,i08:0x06,i21:0x00,i09:0x" + $gpenIP + "7203310a,i0a:0x00,i0d:0x01,i0e:0x00,i0f:0x00,i13:0x07,i14:0x01,i1c:0x02,i17:0x00,i19:0x00,i1a:0x00,i1b:0x03e7,i20:0x00},link.b:{i01:0x07,i0c:0x00,i02:0x07,i03:0x07,i04:0x07,i05:[0x00,0x00,0x00],i0a:['506f727431','506f727432','53465031']},fwd.b:{i10:0x00,i11:0x00,i15:[0x01,0x01,0x02],i17:[0x02,0x02,0x01],i18:[0x" + $vlanIDHEX + ",0x03e7,0x" + $vlanIDHEX + "],i19:0x03,i1a:[0x00,0x00,0x00],i1b:0x00,i1d:[0x00,0x00,0x00],i1e:[0x00,0x00,0x00]},vlan.b:[{i01:0x03e7,i02:0x06,i03:0x00},{i01:0x" + $vlanIDHEX + ",i02:0x05,i03:0x00}],host.b:[],snmp.b:{i01:0x00,i02:'72746562626F75383132',i03:'" + $cpeName + "',i04:'4B45'},acl.b:[{i01:0x01,i02:'000000000000',i03:'ffffffffffff',i04:" + $MACAddress + ",i05:'ffffffffffff',i06:0x00,i07:0x00,i08:0x00,i09:0x08,i0a:0x00,i0b:0x00,i0c:0x00,i0d:0x00,i0e:0x00,i0f:0x00,i10:0x00,i11:0x40,i12:0x01,i13:0x00,i14:0x00,i15:0x00,i16:0x08,i17:0x00,i18:0x00}]" | Add-Content -Path $outputFile;

    return ($retVal);
}

function GPEN21-Data
{
    param(
        $User,
        $Password,
        $Hostname
    );

    $retVal = $null;
    $u      = ($User + ":" + $Password);
    $h      = ($Hostname + "/sys.b");

    $retVal = makeHash(curl --digest -s -u $u $h);

    return ($retVal);
}

#$ip         = "192.168.1.91";
#$username   = "admin";
#$password   = "";
#$GPEN21Info = ISGPEN -User $username -Password $password -Hostname $ip;
#
#if ($GPEN21Info -eq $null)
#{
#    ECHO ($ip + " is not a GPEN21.");
#}
#else
#{
#	$name = genConfig(1502, 37, "HR", $GPEN21Info["i03"]);
#	RESTOREBACKUP-GPEN21 -User $username -Password $password -Path $name -Host $ip;
# 	Reboot-GPEN21
#}
#
#Update-GPEN21 -User "admin" -Path "H:\Documents\GPEN21 Files\Updates\Firmware Versions\swos-gpen21-2.14.bin" -Host "192.168.1.87"
#
#while (!(Test-Connection -ComputerName 192.168.1.87 -Quiet))
#{
#    ECHO "Restarting . . .";
#}
#
#RESTOREBACKUP-GPEN21 -User "admin" -Path "C:\Users\ncabral\Downloads\backup_test.swb" -Hostname "192.168.1.87";