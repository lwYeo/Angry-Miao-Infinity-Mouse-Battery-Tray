using AMInfinityBattery;

var device = LocalDevice.Get(Constants.VendorId, Constants.ProductId);
if (device == null)
{
    Console.WriteLine("Device not found.");
    return;
}

var (mouse, dongle) = Reader.GetBattery(device);

Console.WriteLine($"Mouse: {mouse?.ToString() ?? "--"}%");
Console.WriteLine($"Dongle: {dongle?.ToString() ?? "--"}%");

Console.WriteLine("Press any key to list all connected HID devices...");
Console.ReadKey();

var connectedDevices = LocalDevice.GetAllConnected();

foreach (var item in connectedDevices)
    Console.WriteLine($"Vendor ID: {item.VendorID:x4} Product ID: {item.ProductID:x4} Feature Count: {item.GetMaxFeatureReportLength():000} Product Name: {item.GetProductName()}");

Console.WriteLine("Press any key to exit...");
Console.ReadKey();