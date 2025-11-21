using HidSharp;

namespace AMInfinityBattery
{
    public static class LocalDevice
    {
        /// <summary>
        /// Gets the first matching connected device by Vendor ID and Product ID, with minimum feature count.
        /// </summary>
        public static HidDevice? Get(int vendorId, int productId, int minFeatureCount = 12)
        {
            var devices = DeviceList.Local
                .GetHidDevices(vendorID: vendorId, productID: productId)
                .ToArray();

            foreach(var device in devices)
                if (device.GetMaxFeatureReportLength() >= minFeatureCount)
                    return device;

            return null;
        }

        /// <summary>
        /// Gets akk connected HID devices, ordered by Vendor ID, Product ID, and Feature Report Length.
        /// </summary>
        public static HidDevice[] GetAllConnected()
        {
            return [.. DeviceList.Local.GetHidDevices()
                .OrderBy(d => d.VendorID)
                .ThenBy(d => d.ProductID)
                .ThenByDescending(d => d.GetMaxFeatureReportLength())];
        }
    }
}
