namespace AMInfinityBattery
{
    /// <summary>
    /// Feature command definitions for Angry Miao Infinity mouse dongle.
    /// Proprietary values determined by payload capture from official AM Master software via USBPcap.
    /// </summary>
    internal static class AMFeature
    {
        public const byte FeatureId_Initialize = 0x00;

        public const byte FeatureId_MouseInfo = 0xF7;

        private const byte ProprietaryCommand = 0xF7;

        private const int BufferLength = 65; // Feature Id + 64 bytes of payload.

        public static byte[] GetFeatureBuffer(byte featureId)
        {
            var buffer = new byte[BufferLength];
            buffer[0] = featureId;
            buffer[1] = ProprietaryCommand;
            // The rest of the buffer is initialized to 0 by default.
            return buffer;
        }
    }
}
