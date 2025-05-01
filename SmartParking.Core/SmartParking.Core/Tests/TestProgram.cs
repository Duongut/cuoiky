using System;
using System.Threading.Tasks;

namespace SmartParking.Core.Tests
{
    public class TestProgram
    {
        public static async Task Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: dotnet run --project SmartParking.Core -- test <image_path>");
                return;
            }

            string command = args[0].ToLower();

            switch (command)
            {
                case "test-license-plate":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Usage: dotnet run --project SmartParking.Core -- test-license-plate <image_path>");
                        return;
                    }
                    
                    string imagePath = args[1];
                    var licensePlateTest = new LicensePlateServiceTest();
                    await licensePlateTest.TestLicensePlateRecognition(imagePath);
                    break;
                
                default:
                    Console.WriteLine($"Unknown command: {command}");
                    Console.WriteLine("Available commands: test-license-plate");
                    break;
            }
        }
    }
}
