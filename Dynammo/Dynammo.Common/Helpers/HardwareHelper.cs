/// -----------------------------------------------------------------
///  Dynamic MMO (Dynammo)
///  Dissertation project at University of Derby
/// -----------------------------------------------------------------
///  Written by Timothy Leonard
/// -----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Management;
using System.Security.Cryptography;

namespace Dynammo.Common
{

    /// <summary>
    ///     Contains some general use helper functions for working with specific parts of the hardware..
    /// </summary>
    public static class HardwareHelper
    {
        private static string g_stored_ip_address = "";

        /// <summary>
        ///     Gets a string that uniquely identifys this computer.
        /// </summary>
        /// <returns>Unique identifier of this computer.</returns>
        public static byte[] GenerateFingerprint()
        {
            string unhashed = "";

            // Use first network interface mac address.
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                unhashed += nic.GetPhysicalAddress().ToString() + "\n";
                break;
            }

            // Use first hard drive serial number.
            ManagementObjectSearcher    searcher          = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMedia");
            ManagementObjectCollection  managementObjects = searcher.Get();

            foreach (ManagementObject obj in managementObjects)
            {
                if (obj["SerialNumber"] != null && obj["Removable"] == null)
                {
                    unhashed += obj["SerialNumber"].ToString() + "\n";
                }
            }

            // Add in CPU ID.
            searcher          = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            managementObjects = searcher.Get();

            foreach (ManagementObject obj in managementObjects)
            {
                if (obj["ProcessorID"] != null)
                {
                    unhashed += obj["ProcessorID"].ToString() + "\n";
                }
            }

            // Hash the entire string.
            SHA1 hasher = new SHA1CryptoServiceProvider();
            byte[] hash = hasher.ComputeHash(StringHelper.StringToByteArray(unhashed));

            return hash;
        }

        /// <summary>
        ///     Generates a unique GUID byte array. This should be entirely unique.
        /// </summary>
        /// <returns>Unique GUID byte array.</returns>
        public static byte[] GenerateGUID()
        {
            Guid g = Guid.NewGuid();
            return g.ToByteArray();
        }

        /// <summary>
        ///     Gets the users local ip address in dotted form.
        /// </summary>
        /// <returns>Gets the users local IP address in dotted form.</returns>
        public static string GetLocalIPAddress()
        {
            if (g_stored_ip_address != "")
            {
                return g_stored_ip_address;
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            string last = "127.0.0.1";

            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    last = ip.ToString();
                }
            }

            g_stored_ip_address = last;
            return last;
        }
    
    }

}
