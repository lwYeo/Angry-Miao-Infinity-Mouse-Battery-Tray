using HidSharp;

namespace AMInfinityBattery
{
    public static class Reader
    {
        /// <summary>
        /// Gets the battery percentage of the mouse and dongle.
        /// </summary>
        public static (int? Mouse, int? Dongle) GetBattery(HidDevice device, int retryCount = 3)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            using var stream = device.Open();

            for (int attempt = 0; attempt < retryCount; attempt++)
            {
                // 1. Send the feature report request
                var reportBuffer = AMFeature.GetFeatureBuffer(AMFeature.FeatureId_Initialize);
                try { stream.SetFeature(reportBuffer); }
                catch { continue; } // Retry on failure

                Thread.Sleep(20); // Wait for device to populate the response.

                // 2. Read the feature report response
                var readBuffer = AMFeature.GetFeatureBuffer(AMFeature.FeatureId_MouseInfo);
                try { stream.GetFeature(readBuffer); }
                catch { continue; } // Retry on failure

                // 3. Validate report and extract battery percentage from the response
                if (readBuffer[0] != AMFeature.FeatureId_MouseInfo)
                    continue; // Retry on invalid report ID

                bool isMouseBattDisconnect = readBuffer[5] > 0;
                bool isDongleBattDisconnect = readBuffer[10] < 1;

                int? batteryPercentage = isMouseBattDisconnect ? null : readBuffer[3];
                int? donglePercentage = isDongleBattDisconnect ? null : readBuffer[11];

                return (batteryPercentage, donglePercentage);
            }

            return (null, null); // All attempts failed
        }
    }
}
