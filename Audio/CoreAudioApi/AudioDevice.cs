using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Screna.Audio
{
    public class WasapiAudioDevice
    {
        #region MMDeviceEnumerator
        static IMMDeviceEnumerator realEnumerator;

        static WasapiAudioDevice() { realEnumerator = new MMDeviceEnumeratorComObject() as IMMDeviceEnumerator; }

        /// <summary>
        /// Enumerate Audio Endpoints
        /// </summary>
        /// <param name="dataFlow">Desired DataFlow</param>
        /// <param name="dwStateMask">State Mask</param>
        /// <returns>Device Collection</returns>
        internal static IEnumerable<WasapiAudioDevice> EnumerateAudioEndPoints(DataFlow dataFlow)
        {
            IMMDeviceCollection collection;
            int DeviceState_Active = 0x00000001;
            Marshal.ThrowExceptionForHR(realEnumerator.EnumAudioEndpoints(dataFlow, DeviceState_Active, out collection));

            int Count;
            Marshal.ThrowExceptionForHR(collection.GetCount(out Count));

            IMMDevice dev;
            for (int index = 0; index < Count; index++)
            {
                collection.Item(index, out dev);
                yield return new WasapiAudioDevice(dev);
            }
        }

        /// <summary>
        /// Get Default Endpoint
        /// </summary>
        /// <param name="dataFlow">Data Flow</param>
        /// <param name="role">Role</param>
        /// <returns>Device</returns>
        internal static WasapiAudioDevice GetDefaultAudioEndpoint(DataFlow dataFlow, Role role)
        {
            IMMDevice device = null;
            Marshal.ThrowExceptionForHR(((IMMDeviceEnumerator)realEnumerator).GetDefaultAudioEndpoint(dataFlow, role, out device));
            return new WasapiAudioDevice(device);
        }

        /// <summary>
        /// Get device by ID
        /// </summary>
        /// <param name="id">Device ID</param>
        /// <returns>Device</returns>
        public static WasapiAudioDevice Get(string id)
        {
            IMMDevice device = null;
            Marshal.ThrowExceptionForHR(((IMMDeviceEnumerator)realEnumerator).GetDevice(id, out device));
            return new WasapiAudioDevice(device);
        }
        #endregion

        readonly IMMDevice deviceInterface;

        static Guid IID_IAudioClient = new Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");

        #region Properties
        internal AudioClient AudioClient
        {
            get
            {
                object result;
                int ClsCtx_All = 0x1 | 0x2 | 0x4 | 0x10;
                Marshal.ThrowExceptionForHR(deviceInterface.Activate(ref IID_IAudioClient, ClsCtx_All, IntPtr.Zero, out result));
                return new AudioClient(result as IAudioClient);
            }
        }

        public string Name { get; }

        public string ID
        {
            get
            {
                string result;
                Marshal.ThrowExceptionForHR(deviceInterface.GetId(out result));
                return result;
            }
        }
        #endregion

        static readonly PropertyKey PKEY_Device_FriendlyName = new PropertyKey()
        {
            formatId = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"),
            propertyId = 14
        };

        internal WasapiAudioDevice(IMMDevice realDevice)
        {
            deviceInterface = realDevice;

            IPropertyStore propstore;
            int StorageAccessMode_Read = 0;

            Marshal.ThrowExceptionForHR(deviceInterface.OpenPropertyStore(StorageAccessMode_Read, out propstore));

            Name = new PropertyStore(propstore)[PKEY_Device_FriendlyName];
        }

        public override string ToString() => Name;
    }
}
