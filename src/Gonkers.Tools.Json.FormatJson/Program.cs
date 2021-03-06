using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace Gonkers.JsonTools.FormatJson
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var rootCommand = new RootCommand("This tool will format JSON files. Useful for very large files that your IDE cannot format.")
            {
                // as of this writing System.CommandLine is in a piss poor state and does not implement default parameters properly.
                new Option<FileInfo>(new[] { "--input-file", "-i" }, "The path to the input JSON file to read.") { IsRequired = true },
                new Option<FileInfo>(new[] { "--output-file", "-o" }, "The path to the output JSON file to write. If not provided the input file will be replaced."),
                new Option<bool>(new[] {"--force", "-f"}, "Force overwriting the output file."),
                new Option<bool>(new[] {"--ascii", "-a"}, "Escape non-ASCII and HTML-sensitive characters")
            };

            rootCommand.Name = "format-json";
            rootCommand.Handler = CommandHandler.Create<FileInfo, FileInfo, bool, bool>(CliCommandHandler);
            await rootCommand.InvokeAsync(args);
        }

        static async Task<int> CliCommandHandler(FileInfo inputFile, FileInfo outputFile, bool force, bool ascii)
        {
            if (!inputFile.Exists)
            {
                Console.Error.WriteLine($"The input file '{inputFile.FullName}' was not found.");
                return 1;
            }

            if (outputFile is null)
                outputFile = inputFile;
            else if (outputFile.Exists && !force)
            {
                Console.Error.WriteLine($"The output file '{outputFile.FullName}' already exists.");
                return 2;
            }

            await FormatJsonFile(inputFile, outputFile, ascii);

            return 0;
        }

        static async Task FormatJsonFile(FileInfo inputFile, FileInfo outputFile, bool useAscii)
        {
            var tempFile = new FileInfo(Path.GetTempFileName());

            using (var inputStream = inputFile.OpenRead())
            using (var tempStream = tempFile.OpenWrite())
            using (var jsonWriter = new Utf8JsonWriter(tempStream, new JsonWriterOptions 
            { 
                Indented = true, 
                Encoder = useAscii ? null : JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }))
            {
                var jsonDoc = JsonDocument.Parse(inputStream);
                jsonDoc.WriteTo(jsonWriter);
                await jsonWriter.FlushAsync();
            }

            if (outputFile.Exists)
                outputFile.Delete();

            tempFile.MoveTo(outputFile.FullName);
        }
    }
}
