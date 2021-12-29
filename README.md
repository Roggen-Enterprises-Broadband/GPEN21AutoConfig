# GPEN21AutoConfig
Auto configures a GPEN21.
This was built to assist us in deploying lots of GPEN21s as FTTH/B/P ONTs

Bascially there are two programs being run. A Powershell script that is able to make the changes to a GPEN21 and a graphical UI built in C# that invokes the Powershell script.

Password is changed from -blank- to "password"

Port1 becomes the "internet access port", the port enables untagged access to that VLAN and no other VLANs.
Port2 becomes an access port for VLAN 999 which is the Managment VLAN for the GPEN21
SFP1 becomes a trunk port for the configured VLAN and VLAN 999. 

Note: for compatibility reasons SFP1 is hard set to 1G Full Duplex. 

SNMP is setup for Roggen Enterprises default SNMP settings, but not enabled (we will try to get around to removing that someday)

The MAC address of the GPEN21 is copied and placed in an ACL rule to prevent access to the managment interface from Port1.

The Identity is written in the System Tab as well as a static IP address for the GPEN21 that is based on a /27 being assigned to the customer. 

Our hope is to someday make this program more flexible to fit more network deployments, but we figure that getting the code out there even though it's very specific to our use case would be better then keeping it until we perfected the code for every use case. 

To use the "GPEN21 Setup.exe" program.

1. Create a folder that will act as the root directory for the program.
	a. Place the "GPEN21 Setup.exe" file in the directory.
		• This program will automatically switch its working
		  directory to whever the executed file is located.

	b. Place the "GPEN21_Config.ps1" powershell script in the same directory.
	c. Place the "DEFAULT_CONFIG.swb" file in the same directory.
	d. Place the "curl.exe" file in the same directory.
	e. Place the "Connected.png" image in the same directory.
	f. Place the "Disconnected.png" image in the same directory.

2. Create a folder in the directory called "Firmware".
	a. Place the swos-gpen21-[version].bin file in this folder.
	b. You will need to modify the "GPEN21_Config.ps1" powershell
	  script to upload the specific version you want to upload from this directory.

3. Create a folder in the directory called "logs"
	a. you do not need to put anything in this directory. The "GPEN21 Setup.exe" program 	   	   		   will create the log by itself.
