using System;
using System.Runtime.InteropServices;

// PropertyStore is used here just for retrieving String Properties
namespace Screna.Audio
{
    [StructLayout(LayoutKind.Sequential)]
    struct PropString
    {
        short vt, wReserved1, wReserved2, wReserved3;
        IntPtr pointerValue;

        public string Value { get { return Marshal.PtrToStringUni(pointerValue); } }
    }

    struct PropertyKey
    {
        public Guid formatId;
        public int propertyId;

        public override bool Equals(object obj)
        {
            if (!(obj is PropertyKey)) return false;

            var PKey = (PropertyKey)obj;

            return PKey.formatId == formatId && PKey.propertyId == propertyId;
        }
    }

    /// <summary>
    /// Property Store class, only supports reading properties at the moment.
    /// </summary>
    class PropertyStore
    {
        readonly IPropertyStore storeInterface;

        int Count
        {
            get
            {
                int result;
                Marshal.ThrowExceptionForHR(storeInterface.GetCount(out result));
                return result;
            }
        }

        /// <summary>
        /// Indexer by guid
        /// </summary>
        /// <param name="key">Property Key</param>
        /// <returns>Property or null if not found</returns>
        public string this[PropertyKey key]
        {
            get
            {
                PropString result;
                for (int i = 0; i < Count; i++)
                {
                    PropertyKey ikey = Get(i);

                    if (ikey.Equals(key))
                    {
                        Marshal.ThrowExceptionForHR(storeInterface.GetValue(ref ikey, out result));
                        return result.Value;
                    }
                }

                return "Unknown";
            }
        }

        /// <summary>
        /// Gets property key at sepecified index
        /// </summary>
        /// <param name="index">Index</param>
        /// <returns>Property key</returns>
        PropertyKey Get(int index)
        {
            PropertyKey key;
            Marshal.ThrowExceptionForHR(storeInterface.GetAt(index, out key));
            return key;
        }

        /// <summary>
        /// Creates a new property store
        /// </summary>
        /// <param name="store">IPropertyStore COM interface</param>
        public PropertyStore(IPropertyStore store) { storeInterface = store; }
    }
}

