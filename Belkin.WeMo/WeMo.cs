using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UPNPLib;

namespace Belkin.WeMo
{
    /// <summary>
    /// TODO : Add future functionality for controlling WeMo sensors
    /// </summary>
    public class WeMoSensor : WeMoDevice
    {
        public WeMoSensor(UPnPDevice device)
        {
            this.Device = device;
        }
    }

    /// <summary>
    /// Exposes functionality to control a WeMo outlet switch (On/Off)
    /// </summary>
    public class WeMoSwitch : WeMoDevice
    {
        const string COMMAND_OFF = @"<?xml version=""1.0"" encoding=""utf-8""?><s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"" s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/""><s:Body><u:SetBinaryState xmlns:u=""urn:Belkin:service:basicevent:1""><BinaryState>0</BinaryState></u:SetBinaryState></s:Body></s:Envelope>";
        const string COMMAND_ON = @"<?xml version=""1.0"" encoding=""utf-8""?><s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"" s:encodingStyle=""http://schemas.xmlsoap.org/soap/encoding/""><s:Body><u:SetBinaryState xmlns:u=""urn:Belkin:service:basicevent:1""><BinaryState>1</BinaryState></u:SetBinaryState></s:Body></s:Envelope>";
        
        /// <summary>
        /// Create abstraction layer on UPnPDevice
        /// </summary>
        /// <param name="device"></param>
        public WeMoSwitch(UPnPDevice device)
        {
            this.Device = device;
        }

        /// <summary>
        /// Send command to underlying device to turn on
        /// </summary>
        /// <returns></returns>
        public void On()
        {
            SendCommand(COMMAND_ON); 
        }

        /// <summary>
        /// Send command to underlying device to turn off
        /// </summary>
        /// <returns></returns>
        public void Off()
        {
            SendCommand(COMMAND_OFF);
        }

        /// <summary>
        /// Sends one of the pre-fabricated SOAP messages to the WeMo switch by IP/PORT using HTTP POST
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private void SendCommand(string command)
        {
            //
            //  Pull presentation URL from device and extract IP and PORT
            //
            string port = new Uri(Device.PresentationURL).Port.ToString();
            string baseUrl = new Uri(Device.PresentationURL).DnsSafeHost.ToString();
            
            //
            //  Build new target URL including the basicevent1 path
            //
            string targetUrl = "http://" + baseUrl + ":" + port + "/upnp/control/basicevent1";

            //
            //  Create the packet and payload to send to the endpoint to get the switch to process the command
            //
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(targetUrl);
            request.Method = "POST";
            request.Headers.Add("SOAPAction", "\"urn:Belkin:service:basicevent:1#SetBinaryState\"");
            request.ContentType = @"text/xml; charset=""utf-8""";
            request.KeepAlive = false;
            Byte[] bytes = UTF8Encoding.ASCII.GetBytes(command);
            request.ContentLength = bytes.Length;
            using(Stream stream = request.GetRequestStream())
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Close();
                request.GetResponse();
            }
            
            //
            //  HACK: If we don't abort the result the device holds on to the connection sometimes and prevents other commands from being received
            //
            request.Abort();
        }
    }

    /// <summary>
    /// Base class used to find and reference the WeMo devices
    /// </summary>
    public class WeMoDevice
    {
        /// <summary>
        /// The possible types of WeMo devices that can be detected
        /// </summary>
        public enum WeMoDeviceType
        {
            Switch,
            Sensor,
            Unknown
        }
        
        /// <summary>
        /// Returns the friendly name of the device (ex. WeMo Modem)
        /// </summary>
        public string Name
        {
            get
            {
                return this.Device.FriendlyName;
            }
        }

        /// <summary>
        /// Reference to the underlying UPNP device this object abstract from
        /// </summary>
        public UPnPDevice Device { set; get; }

        /// <summary>
        /// Searches network for WeMo devices and returns all that are found or none
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public static WeMoDevice GetDevice(string Name)
        {
            List<WeMoDevice> devices = WeMoDevice.GetDevices();
            return devices.Where(a => a.Name == Name).FirstOrDefault();
        }

        /// <summary>
        /// Queries UPnP for Belkin WeMo devices and returns a reference to them
        /// </summary>
        /// <returns>A collection of located WeMo devices</returns>
        public static List<WeMoDevice> GetDevices()
        {
            UPnPDeviceFinder finder = new UPnPDeviceFinder();
            List<WeMoDevice> foundDevices = new List<WeMoDevice>();
            
            //
            //  Query all UPNP root devices
            //
            string deviceType = "upnp:rootdevice";
            UPnPDevices devices = finder.FindByType(deviceType, 1);

            //
            //  Iterate devices and create proper parent types based on WeMo device type and store in collection
            //
            foreach (UPnPDevice device in devices)
            {
                if (device.Type.StartsWith("urn:Belkin:"))
                {
                    switch (GetDeviceType(device))
                    {
                        case WeMoDeviceType.Switch :
                            WeMoSwitch wemoSwitch = new WeMoSwitch( device );
                            foundDevices.Add(wemoSwitch);
                            break;

                        case WeMoDeviceType.Sensor :
                            WeMoSensor wemoSensor = new WeMoSensor( device );
                            foundDevices.Add(wemoSensor);
                            break;
                        default:
                            // TODO: Decide what to do with non Sensor/Switch devices
                            break;
                            
                    }
                }
            }

            return foundDevices;
        }

        /// <summary>
        /// Returns the specific WeMo device type from the UPnPDevice object
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public static WeMoDeviceType GetDeviceType(UPnPDevice device)
        {
            if (device.Type.Contains("controllee"))
            {
                return WeMoDeviceType.Switch;
            }
            else if (device.Type.Contains("sensor"))
            {
                return WeMoDeviceType.Sensor;
            }
            else
            {
                return WeMoDeviceType.Unknown;
            }
        }
    }
}
