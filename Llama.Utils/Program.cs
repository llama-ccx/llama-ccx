using System;
using System.IO;
using Llama.Core.License;

namespace Llama.Utils
{
    internal class Program
    {
        static void Main()
        {
            var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
            var jsonFilePath = Path.Combine(projectDir, "data.json");
            var binaryFilePath = Path.Combine(projectDir, "data.bin");

            Console.WriteLine($"Project directory: {projectDir}");
            Console.WriteLine($"Binary file path: {binaryFilePath}");

            try
            {
                var binary = License.SerializeJsonToBinary(jsonFilePath);
                License.SerializeBinaryToFile(binaryFilePath, binary);
                Console.WriteLine("data.bin created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating data.bin: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }
}
