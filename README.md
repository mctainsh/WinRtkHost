# WinRtkHost

Simple OPEN SOURCE windows application to process RTK data and send to multiple NTRIP Casters. Also referred to as Defi mining.

This project connects a ByNav M20, LC29HDA, UM980 or UM982 RTK GNSS receiver to send RTK correction data to NTRIP casters. The app wil automatically program the GPS device so there is no need to mess around with terminals or or the UPrecise software.

All up you it will cost less than US$200 to make the station with GNSS receiver, antenna (If you have a windows PC).

You can send to as many NTRIP casters as you want without the need for additional receivers, antennas or splitters. (Note: You cannot send to Geodnet as they only allow their own expensive propitiatory devices)

```
NOTE : It runs with M20, LC29HDA, UM980 or UM982, I'll only talk about M20 as the same applies unless stated otherwise. Also the LC29HDA is a very cheap option but the mining rewards are a bit shit.
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

This is the hardware I used. The links are not affiliate links and I don't recommend any of the suppliers. Shop around and you will get a better deal and/or faster delivery.

### Components

1. **RTK Receiver** - One of the following

   1. **Bynav M20** - The best option for support and packaging at a good price ($228) and fast delivery [https://gnss.store/unicore-gnss-modules/247-401-elt0222.html](https://gnss.store/unicore-gnss-modules/247-401-elt0222.html)
   
   2. **UM980** - A very reasonable priced option but it does not include a package and requires a fan as it runs very hot. Unpackaged from AliExpress for about US$100 or $US167 with fancy antenna [https://vi.aliexpress.com/item/1005008328841504.html](https://vi.aliexpress.com/item/1005008328841504.html)

   3. **Quectel LC29HDA** - Cheapest US$40 (With antenna)  [https://vi.aliexpress.com/item/1005007513127879.html](https://vi.aliexpress.com/item/1005007513127879.html) The DA suffix is important. Also note with this one you will need an FTDI [https://vi.aliexpress.com/item/1005006445462581.html](https://vi.aliexpress.com/item/1005006445462581.html) and some wires. It is the cheapest but a bit fiddly. Also the mining rewards are less as you get fewer receiver bands.

2. **Windows PC** Connect to GPS receiver

3. **Wires** - Connect to receiver via USB-C port or if using LC29HDA and FDDI

## Software

### Features

- Connects to M20, LC29HDA, UM980 or UM982.

- Connects to Wifi.

- Programs the M20 to send generate RTK correction data

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

Each file represents a different destination. Only add the ones you need but be sure they are numbered from zero sequentially.

| Row | Usage                                       |
| --- | ------------------------------------------- |
| 1   | Usually servers.onocoy.com, rtk2go.com or ntrip.rtkdirect.com  |
| 2   | Port usually 2101                           |
| 3   | Mount point name. From caster web site      |
| 4   | Create this with signup on caster web site  |

WARNING :  Do not run without real credentials or your IP may be blocked (Particularly for rtk2go.com)!!

## Setup & Installation

There is two different ways to run the application. EIther as a console app or windows service. Console app is easy just download and run. The windows service requires you to install the service which is tricker to setup. I'd recommend starting with the console app then creating windows service once everything is working.

FIles can be downloaded from the `AA-Latest` folder [Here](https://1drv.ms/f/s!Avrf6GYUWqyFhtRJyiwprtYICXZggA)

### Console App

1. **Download the files** :  Open the `AA-Latest` folder and Download `ConsoleApp` 

2. **Place in folder** : Unzip the ConsoleApp files

3. **Sign up** : Create the accounts with [Oncony register](https://console.onocoy.com/auth/register/personal), [RtkDirect](https://cloud.rtkdirect.com/) or [RTK2GO](http://rtk2go.com/sample-page/new-reservation/)

4. **Configure** : Configure the device type and caster details above

5. **Run** : Run WinRtkHost

6. **Review logs** : Any errors will appear on the screen. Although more detailed logs are written to the RtkLogs folder

7. **Report issues / Enhancements** : I made this over a weekend. If anyone uses it I'll add some feature to make it more useful.

### Windows Service

1. **Download the files** : Open the `AA-Latest` folder and Download `WinRtkHostService` 

2. **Place in folder** : Create a folder under `C:\Program Files\SecureHub\WinRtkHostService`. Unzip downloaded folder this folder.

3. **Sign up** : Create the accounts with [Oncony register](https://console.onocoy.com/auth/register/personal), [RtkDirect](https://cloud.rtkdirect.com/) or [RTK2GO](http://rtk2go.com/sample-page/new-reservation/)

4. **Configure** : Configure the device type and caster details above

5. **Setup Windows Service** : Right click on `C:\Program Files\SecureHub\WinRtkHostService\CreateService(Run as Administrator).bat` and run as administrator. This will configure and start the service

6. **Review logs** : Services run in the background even when you are not logged into your computer so there is no user interface. The logs can be found at `C:\Program Files\SecureHub\WinRtkHostService\Logs`

7. **Report issues / Enhancements** : I made this over a weekend. If anyone uses it I'll add some feature to make it more useful.

## TODO

1. Make some kind of web or GUI interface

2. Convert to a Windows service so it runs in background without user login

3. Add averaging routing to build up an exact device location

## Final thoughts
It does mine with M20, LC29HDA, UM980 or UM982. But the LC29HDA is very cheap option and the mining rewards are a bit shit. To be fair no one is actually paying you real money to mine when you can get the same data from rtk2go for free. Geodnet did pay about $10 per week but you have to buy their miner. Selling miners to pay rewards seems like someone is going to be left holding the bag, but I could be wrong (I usually am).

<table>
	<tr>
		<td>M20</td>
		<td>LC29HDA</td>
		<td>UM980</td>
	</tr>
	<tr>
		<td><img src="https://github.com/mctainsh/WinRtkHost/blob/main/Photos/M20.png?raw=true" width="300" /></td>
		<td><img src="https://github.com/mctainsh/WinRtkHost/blob/main/Photos/LC29HDA.png?raw=true" width="300" /></td>
		<td><img src="https://github.com/mctainsh/WinRtkHost/blob/main/Photos/UM980.png?raw=true" width="300" /></td>
	</tr>
</table>

This project is a windows version of my ESP32 solution. The ESP32 version is still flakey but I added this photo cos I like it.

<img src="https://github.com/mctainsh/Esp32/blob/main/UM98RTKServer/Photos/TTGO-Display-S3/T-Display-S3-UM982_Boxed.jpg?raw=true" width="400" />

## License

This project is licensed under the GNU General Public License - see the [LICENSE](https://github.com/mctainsh/Esp32/blob/main/LICENSE)  file for details.

---
