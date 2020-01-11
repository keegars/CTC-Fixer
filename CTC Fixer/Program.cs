using System;
using System.IO;
using System.Linq;
using CTC_Fixer.FileDefinition;

namespace CTC_Fixer
{
    internal static class Program
    {
        private static void Main()
        {
            var currentDirectory = Directory.GetCurrentDirectory();

            var ctcFiles = Directory.GetFiles(currentDirectory, "*.ctc", SearchOption.AllDirectories).ToList();

            Console.WriteLine($"Directory: {currentDirectory}");
            Console.WriteLine($"Files to edit: {ctcFiles.Count}");

            foreach (var file in ctcFiles)
            {
                var currentBytes = File.ReadAllBytes(file);

                try
                {
                    var newCTC = new CTC(currentBytes);


                    var newBytes = newCTC.GenerateIceborneFromOriginalBytes();

                    if (currentBytes.Length == newBytes.Length)
                    {
                        Console.WriteLine($"Skipping file: {file}");
                    }
                    else
                    {
                        File.Copy(file, file + "_old.old");
                        File.Delete(file);
                        File.WriteAllBytes(file, newBytes);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{e.Message}");
                }
              
            }
        }
    }
}