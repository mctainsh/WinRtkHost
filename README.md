# WinRtkHost

Simple windows application to process RTK data and send to multiple NTRIP Casters

This project connects to LC29HDA, UM980 or UM982 RTK GNSS receiver to send RTK correction data to an NTRIP caster. The app wil automatically program the UM980/2 so there is no need to mess around with terminals or or the UPrecise software.

All up you it will cost about US$100 to make the station with GNSS receiver, antenna (If you have a windows PC). 

You can send to as many NTRIP casters as you want without the need for additional receivers or expensive splitters.

NOTE 1 : Although the sample setup is configures to send data to three RTK casters, if one of the casters fails to receive the message (blocks) the other will not be impacted.

NOTE 2 : ALthough LC29HDA, UM980 or UM982, I'll only talk about UM980 as the same applies unless stated otherwise.

This is a windows version of my ESP32 solution (I added this photo cos I like it but it doesn't have much to do with this project)
<img src="https://github.com/mctainsh/Esp32/blob/main/UM98RTKServer/Photos/TTGO-Display-S3/T-Display-S3-UM982_Boxed.jpg?raw=true" width="400" />

## Table of Contents 
 
- [Project Overview](#project-overview)
- [Hardware](#hardware)  
  - [Components](#components)
  - [Wiring Diagram](#wiring-diagram) 
- [Software](#software)  
  - [Features](#features)
  - [Key Mappings](#key-mappings) 
  - [Setup & Installation](#setup--installation)
  - [Usage](#usage)
- [License](#license)

## Project Overview

This project enables a Windows PC act as an RTK server sending RTK corrections to many NTRIP casters. Examples of these are be Onocoy, Rtk2Go or RtkDirect.

### Terms

| Name | Description |
| --- | --- |
| RTK Client | A device or software that receives RTK correction data from a server to improve positioning accuracy. |
| RTK Server | A server that processes and distributes RTK correction data to clients. (This project builds a RTK Server) |
| RTK Caster | A service that broadcasts RTK correction data over the internet using the NTRIP protocol. |

## Hardware 

### Components 
 
1. **UM980 with antenna** - Witte Intelligent WTRTK-982 high-precision positioning and orientation module. I got it from AliExpress for about A$220.00 [Not affiliate link. Find your own seller](https://www.aliexpress.com/item/1005007287184287.html)
 
2. **Windows PC** Connect to GPS receiver		
 
3. **Wires** - Connect to receiver via USB-C port or if using LC29HDA and FDDI

## Software 

### Features 

- Connected to UM980.
 
- Connects to Wifi.

- Programs the UM980 to send generate RTK correction data

- Sends correction data to all RTK Casters


### Config parameters 

Configuration is held in two locations 'WinRtkHost.exe.config' and 'ConfigX.txt' files parameters are set in the "Configure Wifi" web page

Note : You don't need to sign up to all three. Leave the CASTER ADDRESS blank to only use one or two casters. 

| Parameter | Usage | 
| --- | --- | 
| GPSReceiverType | Your WiFi network name. Note: Not all speed are supported by ESP32 |
| Password | Your Wifi password |
| CASTER 1 ADDRESS | Usually "ntrip.rtkdirect.com" |


| Row | Usage | 
| --- | --- | 
| 1 | Usually "rtk2go.com", ntrip.rtkdirect.com or  |
| 2 | Port usually 2101 |
| 3 | Mount point name |
| 4 | Create this with Rtk2Go signup |

WARNING :  Do not run without real credentials or your IP may be blocked!!

### Setup & Installation 

1. **Install VS Code** : Follow [Instructions](https://code.visualstudio.com/docs/setup/setup-overview)

2. **Install the PlatformIO IDE** : Download and install the [PlatformIO](https://platformio.org/install).
 
3. **Clone This Repository**

```bash
git clone https://github.com/mctainsh/Esp32.git
```

or just copy the files from
```
https://github.com/mctainsh/Esp32/tree/main/UM98RTKServer/UM98RTKServer
```
4. **Enable the TTGO T-Display header** : To use the TTGO T-Display-S3 with the TFT_eSPI library, you need to make the following changes to the User_Setup.h file in the library.

```
	.pio\libdeps\lilygo-t-display\TFT_eSPI\User_Setup_Select.h
	4.1. Comment out the default setup file
		//#include <User_Setup.h>           // Default setup is root library folder
	4.2. Uncomment the TTGO T-Display-S3 setup file
		#include <User_Setups/Setup206_LilyGo_T_Display_S3.h>     // For the LilyGo T-Display S3 based ESP32S3 with ST7789 170 x 320 TFT
	4.3. Add the following line to the start of the file
		#define DISABLE_ALL_LIBRARY_WARNINGS
```

### Configuration 

1. Create the accounts with [Oncony register](https://console.onocoy.com/auth/register/personal), [RtkDirect](https://cloud.rtkdirect.com/) or [RTK2GO](http://rtk2go.com/sample-page/new-reservation/)

2. Don't wire up anything to start with (We can let the smoke out of it later)

3. Upload the program to your ESP32. 

4. Power it up and check display for WIFI connection status.

5. Following instruction at [WifiManager](https://github.com/tzapu/WiFiManager) to connect your ESP32 to your WIFI.

6. Configure the RTK Servers you want to use in the "Configure Wifi" Screen.

7. Wire up the ESP32 to UM98x. Power it fom UM98x (Sometime the ESP32 doesn't output enough beans).

8. Review the status and logs through the web interface (http://x.x.x.x/i)

### Important

The T-Display-S3 will turn off it's display after about 30 seconds. This is OK, just press either button to turn it on again.

### Display

The display has several screens you can toggle through them by pressing one of the buttons.

The top line of the display shows the following

| Type | Usage | 
| --- | --- | 
| / | Rotating animation to show main loop is running |
| Title | Title of the page currently displayed |
| X | Connection state of RTK Server 3 |
| X | Connection state of RTK Server 2 |
| X | Connection state of RTK Server 1 |
| X | Connection state of WIFI |
| X | Connection State to UM98x |


### General
<img src="https://github.com/mctainsh/Esp32/blob/main/UM98RTKServer/Photos/TTGO-Display-S3/S3-Screen-Home.jpg?raw=true" width="300"/>

| Title | Meaning | 
| --- | --- | 
| Wi-Fi | Wifi IP address. | 
| Version | Software version | 
| Up time | How log it has been running. Max 76 days before the counter rolls over  | 
| Speed | Now many times to process is being checked per second | 

### GPS State
<img src="https://github.com/mctainsh/Esp32/blob/main/UM98RTKServer/Photos/TTGO-Display-S3/S3-Screen-GPS.jpg?raw=true" width="300"/>

| Title | Meaning | 
| --- | --- | 
| Type | Type of GPS device. Queried at startup | 
| Resets |  | 
| Packets | How many packets have been received | 
| Serial # | GPS module serial number | 
| Firmware | GPS module firmware verison | 

### RTK Server

<img src="https://github.com/mctainsh/Esp32/blob/main/UM98RTKServer/Photos/TTGO-Display-S3/S3-Screen-RTK.jpg?raw=true" width="300"/>

Only shows the state of the first two casters

| Title | Meaning | 
| --- | --- | 
| State | Connection state | 
| Reconnect | Number of time the connection was lost | 
| Sends | Number of packets sent | 
| μs | Microseconds per byte sent | 

### GPS Log

<img src="https://github.com/mctainsh/Esp32/blob/main/UM98RTKServer/Photos/TTGO-Display-S3/S3-Screen-Log-GPS.jpg?raw=true" width="300"/>


### First RTK Caster Log

<img src="https://github.com/mctainsh/Esp32/blob/main/UM98RTKServer/Photos/TTGO-Display-S3/S3-Screen-Log-C1.jpg?raw=true" width="300"/>

### Second RTK Caster Log

<img src="https://github.com/mctainsh/Esp32/blob/main/UM98RTKServer/Photos/TTGO-Display-S3/S3-Screen-Log-C2.jpg?raw=true" width="300"/>

### Third RTK Caster Log

<img src="https://github.com/mctainsh/Esp32/blob/main/UM98RTKServer/Photos/TTGO-Display-S3/S3-Screen-Log-C3.jpg?raw=true" width="300"/>

## TODO

1. Write instructions to install without compiling with PlatformIO (Using ESP32 Upload tool)

2. Make http sends in non-blocking to prevent one NTRIP server upsetting the others

3. Rework the TTGO T-Display code to make the display nicer (Currently optimized for larger S3)

4. Put each NTRIP server details on its own page

5. Make better looking STL

6. Build one using ESP32-S3 Mini board. Won't have display but will be very compact

## License 
This project is licensed under the GNU General Public License - see the [LICENSE](https://github.com/mctainsh/Esp32/blob/main/LICENSE)  file for details.

---


