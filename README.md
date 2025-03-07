# WinRtkHost

Simple OPEN SOURCE windows application to process RTK data and send to multiple NTRIP Casters. Also refered to as Defi mining.

This project connects a LC29HDA, UM980 or UM982 RTK GNSS receiver to send RTK correction data to NTRIP casters. The app wil automatically program the UM980 so there is no need to mess around with terminals or or the UPrecise software.

All up you it will cost about US$100 to make the station with GNSS receiver, antenna (If you have a windows PC).

You can send to as many NTRIP casters as you want without the need for additional receivers, antennas or splitters.

```
NOTE : It runs with LC29HDA, UM980 or UM982, I'll only talk about UM980 as the same applies unless stated otherwise. Also the LC29HDA isa very cheap option but the mining rewards are a bit shit.
```

## Table of Contents

- [Project Overview](#project-overview)
- [Hardware](#hardware)
  - [Components](#components)
  - [Software](#software)
  - [Features](#features)
  - [Key Mappings](#key-mappings)
  - [Setup & Installation](#setup--installation)
  - [Usage](#usage)
- [License](#license)

## Project Overview

This project enables a Windows PC act as an RTK server sending RTK corrections to many NTRIP casters. Examples of these are be Onocoy, Rtk2Go or RtkDirect.

### Terms

| Name       | Description                                                                                                |
| ---------- | ---------------------------------------------------------------------------------------------------------- |
| RTK Client | A device or software that receives RTK correction data from a server to improve positioning accuracy.      |
| RTK Server | A server that processes and distributes RTK correction data to clients. (This project builds a RTK Server) |
| RTK Caster | A service that broadcasts RTK correction data over the internet using the NTRIP protocol.                  |

## Hardware

This is the hardware I used. The links are not affiliate links and I don't recommend any of the suppliers. Shop arround and you will get a better deal and/or faster delivery.

### Components

1. **RTK Receiver** - One of the following

   1. **UM980 with antenna** - Witte Intelligent WTRTK-980 high-precision positioning and orientation module. I got it from AliExpress for about US$100 or $US167 with fancy antenna [https://vi.aliexpress.com/item/1005008328841504.html](https://vi.aliexpress.com/item/1005008328841504.html)
   2. **Quectel LC29HDA** - Cheapest US$40 (With antenna)  [https://vi.aliexpress.com/item/1005007513127879.html](https://vi.aliexpress.com/item/1005007513127879.html) The DA suffix is important. Also note with this one you will need an FTDI [https://vi.aliexpress.com/item/1005006445462581.html](https://vi.aliexpress.com/item/1005006445462581.html) and some wires. It is the cheapest but a bit fiddly. Also the mining rewards are less as you get fewer receiver bands.

2. **Windows PC** Connect to GPS receiver

3. **Wires** - Connect to receiver via USB-C port or if using LC29HDA and FDDI

## Software

### Features

- Connected to UM980.

- Connects to Wifi.

- Programs the UM980 to send generate RTK correction data

- Sends correction data to all RTK Casters

### Config parameters

Configuration is held in two locations 'WinRtkHost.exe.config' and 'ConfigX.txt' files.

Note : You need to signup to the NTRIP caster before you use start sending data or they may block your address for 48 hours.

Parameters for 'WinRtkHost.exe.config'

| Parameter |Usage    |
| -------- | ---------------- |
| GPSReceiverType    | Type of device. Valid values are  LC29H   UM980    UM982   |
| ComPort            | Serial port the device is connected to. If you don't know, leave this blank and the App with try the first one it finds. Sample values are COM1  COM12|
| BaseStationAddress | Leave empty if you don't have an EXACT location. IIf you do, record it as "Latitude Longitude Height" (Without quotes)                       |

#### CasterX.txt
Note : X is a number starting at 0.

Each file represents a different destination. Only add the ones you need but be sure they are numbered from zero sequencially.

| Row | Usage                                       |
| --- | ------------------------------------------- |
| 1   | Usually rtk2go.com, ntrip.rtkdirect.com or  |
| 2   | Port usually 2101                           |
| 3   | Mount point name. From caster web site      |
| 4   | Create this with signup on caster web site  |

WARNING :  Do not run without real credentials or your IP may be blocked!!

### Setup & Installation

1. **Download the files** : [Everything from Code button](https://github.com/mctainsh/WinRtkHost/tree/main) or just [Download only the application](https://github.com/mctainsh/WinRtkHost/tree/540277185d19ed3b363d4e9723451ba20e1afddc/ConsoleApp). 

2. **Place in folder** : Put the ConsoleApp files in a folder

3. **Sign up** : Create the accounts with [Oncony register](https://console.onocoy.com/auth/register/personal), [RtkDirect](https://cloud.rtkdirect.com/) or [RTK2GO](http://rtk2go.com/sample-page/new-reservation/)

4. **Configure** : Configure the device type and caster details above

5. **Run** : Run WinRtkHost

6. **Review logs** : Any errors will appear on the screen. Although more detailed logs are written to the RtkLogs folder

7. **Report issues / Enhancements** : I made this over a weekend. If anyone uses it I'll add some feature to make it more useful.

## TODO

1. Make some kind of web or GUI interface

2. Convert to a Windows service so it runs in background without user login

3. Add averaging routing to build up an exact device location

## Final thoughts
It runs does mine with LC29HDA, UM980 or UM982. But the LC29HDA is very cheap option and the mining rewards are a bit shit. To be fair all no one is actually paying you real money to mine when you can get the same data from rtk2go for free.

<table>
	<tr>
		<td>LC29HDA</td>
		<td>UM980</td>
	</tr>
	<tr>
		<td><img src="https://github.com/mctainsh/WinRtkHost/blob/main/Photos/LC29HDA.png?raw=true" width="300" /></td>
		<td><img src="https://github.com/mctainsh/WinRtkHost/blob/main/Photos/UM980.png?raw=true" width="300" /></td>
	</tr>
</table>

This project is a windows version of my ESP32 solution. The ESP32 version is still flakey but I added this photo cos I like it.

<img src="https://github.com/mctainsh/Esp32/blob/main/UM98RTKServer/Photos/TTGO-Display-S3/T-Display-S3-UM982_Boxed.jpg?raw=true" width="400" />

## License

This project is licensed under the GNU General Public License - see the [LICENSE](https://github.com/mctainsh/Esp32/blob/main/LICENSE)  file for details.

---
