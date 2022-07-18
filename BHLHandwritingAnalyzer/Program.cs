using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BHLHandwritingAnalyzer
{
    class Program
    {
        private static int _itemID = 0;

        static void Main(string[] args)
        {
            try
            {
                if (ReadCommandLineArguments())
                {
                    if (!Directory.Exists(Config.OutputFolder)) Directory.CreateDirectory(Config.OutputFolder);
                    if (!Directory.Exists(Config.OriginalFolder)) Directory.CreateDirectory(Config.OriginalFolder);
                    if (!Directory.Exists(Config.NewFolder)) Directory.CreateDirectory(Config.NewFolder);

                    // Get the Page IDs from BHL for the specified item
                    List<int> pageIDs = GetPageIDs(_itemID);

                    foreach (int pageID in pageIDs)
                    {
                        // Get original page text from BHL
                        GetOriginalText(pageID);

                        // Get original scientific names on the page from BHL
                        GetOriginalNames(pageID);

                        // Get new page text from Azure service (perform OCR on the page image)
                        Task.Delay(2000).Wait();    // Throttle the API requests sent to Azure
                        GetNewText(pageID).Wait();

                        // Use the gnfinder tool to extract scientific names from the new page text
                        GetNewNames(pageID);
                    }

                    // Compile complete lists of all names found in the item
                    GetCompleteOriginalNameList(_itemID, pageIDs);
                    GetCompleteNewNameList(_itemID, pageIDs);

                    Console.Write($"Analysis of item {_itemID} is complete.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the BHL page identifiers for the specified BHL Item identifier.
        /// </summary>
        private static List<int> GetPageIDs(int itemID)
        {
            List<int> pageIDs = new List<int>();

            // Get the item metadata from the BHL API
            string itemMetadataResponse =
                BHLApi3.GetItemMetadata(itemID, true, false, false, BHLApi3.ResponseFormat.Xml, Config.BhlApiKey);

            // Extract the page identifiers from the API response
            XDocument xml = XDocument.Parse(itemMetadataResponse);
            foreach (XElement page in xml.Root
                .Elements("Result")
                .Elements("Item")
                .Elements("Pages")
                .Elements("Page"))
            {
                string pageID = page.Element("PageID").Value;
                pageIDs.Add(Convert.ToInt32(pageID));
            }

            return pageIDs;
        }

        /// <summary>
        /// Get from BHL the text of the specified page.
        /// </summary>
        /// <param name="pageID"></param>
        static void GetOriginalText(int pageID)
        {
            string outputFile = string.Format("{0}\\{1}.txt", Config.OriginalFolder, pageID.ToString());
            string pageTextUrl = string.Format(Config.BhlPageTextUrl, pageID.ToString());
            string pageText = new WebClient().DownloadString(pageTextUrl);
            File.WriteAllText(outputFile, pageText, Encoding.UTF8);
        }

        /// <summary>
        /// Get from BHL the scientific names associated with the specified page.
        /// </summary>
        /// <param name="pageID"></param>
        static void GetOriginalNames(int pageID)
        {
            string outputFile = string.Format("{0}\\{1}_names.xml", Config.OriginalFolder, pageID.ToString());
            string pageMetadata = BHLApi3.GetPageMetadata(pageID, false, true, BHLApi3.ResponseFormat.Xml, Config.BhlApiKey);
            File.WriteAllText(outputFile, pageMetadata, Encoding.UTF8);
        }

        /// <summary>
        /// Compile a list of all scientific names found in the original text of the specified BHL item.
        /// </summary>
        /// <param name="itemID"></param>
        /// <param name="pageIDs"></param>
        static void GetCompleteOriginalNameList(int itemID, List<int> pageIDs)
        {
            List<string> outputLines = new List<string>();
            outputLines.Add("PageID\tName");

            foreach (int pageID in pageIDs)
            {
                string nameFile = string.Format("{0}\\{1}_names.xml", Config.OriginalFolder, pageID.ToString());

                if (File.Exists(nameFile))
                {
                    var xdoc = XDocument.Load(nameFile);
                    var root = xdoc.Root;
                    var names = root.Element("Result").Element("Page").Element("Names");

                    if (names.HasElements)
                    {
                        foreach (var name in names.Elements("Name"))
                        {
                            string nameFound = name.Element("NameFound").Value;
                            outputLines.Add(string.Format("{0}\t{1}", pageID.ToString(), nameFound));
                        }
                    }
                }
            }

            File.WriteAllLines(string.Format("{0}\\AllOriginalNames{1}.tsv", Config.OriginalFolder, itemID.ToString()),
                outputLines.ToArray());
        }

        /// <summary>
        /// Perform OCR analysis of the specified BHL page.
        /// </summary>
        /// <remarks>It is assumed that the page being analyzed is handwritten.</remarks>
        /// <param name="pageID">BHL identifier for the page</param>
        /// <returns>Text of the page</returns>
        static public async Task GetNewText(int pageID)
        {
            // Authenticate with the Azure service
            ComputerVisionClient client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(Config.SubscriptionKey))
              { Endpoint = Config.Endpoint };

            // Read text from URL
            string imageUrl = string.Format(Config.BhlPageImageUrl, pageID.ToString());
            var textHeaders = await client.ReadAsync(imageUrl);

            // After the request, get the operation location (operation ID)
            string operationLocation = textHeaders.OperationLocation;
            Thread.Sleep(2000);

            // Retrieve the URI where the extracted text will be stored from the Operation-Location header.
            // We only need the ID and not the full URL
            const int numberOfCharsInOperationId = 36;
            string operationId = operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);

            // Extract the text
            ReadOperationResult results;
            do
            {
                results = await client.GetReadResultAsync(Guid.Parse(operationId));
            }
            while ((results.Status == OperationStatusCodes.Running || results.Status == OperationStatusCodes.NotStarted));

            // Save the found text
            var textUrlFileResults = results.AnalyzeResult.ReadResults;
            StringBuilder sb = new StringBuilder();
            foreach (ReadResult page in textUrlFileResults)
            {
                foreach (Line line in page.Lines)
                {
                    sb.AppendLine(line.Text);
                }
            }

            File.WriteAllText(
                string.Format("{0}\\{1}.txt", Config.NewFolder, pageID.ToString()),
                sb.ToString());
        }

        /// <summary>
        /// Invoke the Global Names gnfinder tool to extract scientific names from the text of the specified page.
        /// </summary>
        /// <remarks>Compiled executables of the gnfinder tool can be found at https://github.com/gnames/gnfinder</remarks>
        /// <param name="pageID"></param>
        static void GetNewNames(int pageID)
        {
            // Execute the gnfinder utility to find names in the specified text
            Process process = new Process();

            string inputFile = string.Format("{0}\\{1}.txt", Config.NewFolder, pageID.ToString());
            string gnfinderCommand = string.Format($"/C gnfinder -v -f pretty {inputFile}");

            process.StartInfo = new ProcessStartInfo()
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "cmd.exe",
                Arguments = gnfinderCommand,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            process.Start();

            // Now read the value, parse to int and add 1 (from the original script)
            string gnfinderOut = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            File.WriteAllText(
                string.Format("{0}\\{1}_names.json", Config.NewFolder, pageID.ToString()),
                gnfinderOut);
        }

        /// <summary>
        /// Compile a list of all scientific names found in the new text of the specified BHL item.
        /// </summary>
        /// <param name="itemID"></param>
        /// <param name="pageIDs"></param>
        static void GetCompleteNewNameList(int itemID, List<int> pageIDs)
        {
            List<string> outputLines = new List<string>();
            outputLines.Add("PageID\tName");

            foreach (int pageID in pageIDs)
            {
                string nameFile = string.Format("{0}\\{1}_names.json", Config.NewFolder, pageID.ToString());

                try
                {
                    if (File.Exists(nameFile))
                    {
                        var json = File.ReadAllText(nameFile);
                        var jobj = JObject.Parse(json);

                        int totalNames = (int)(jobj["metadata"]["totalNames"] ?? 0);
                        if (totalNames > 0)
                        {
                            foreach (var name in jobj["names"])
                            {
                                var best = name["verification"]["bestResult"];
                                if (best != null)
                                {
                                    string nameValue = (string)best["matchedCanonicalFull"];
                                    if (nameValue != null) outputLines.Add(string.Format("{0}\t{1}", pageID.ToString(), nameValue));
                                }
                            }
                        }
                    }
                }
                catch (JsonReaderException jex)
                {
                    // No names to parse, or names unparsable
                }
            }

            File.WriteAllLines(string.Format("{0}\\AllNewNames{1}.tsv", Config.NewFolder, itemID.ToString()),
                outputLines.ToArray());
        }

        /// <summary>
        /// Read the Item ID from the command line.
        /// </summary>
        /// <returns></returns>
        private static bool ReadCommandLineArguments()
        {
            bool validArgs = false;

            string[] args = Environment.GetCommandLineArgs();
            switch (args.Length)
            {
                case 1:
                    Console.WriteLine("BHL Item ID is required.  Format is \"BHLHandwritingAnalyzer <ITEMID>\".");
                    break;
                case 2:
                    if (!int.TryParse(args[1], out _itemID))
                    {
                        Console.Write("Invalid Item ID.  Item ID must be a numeric integer value.  Example:  BHLHandwritingAnalyzer 1234");
                    }
                    else
                    {
                        validArgs = true;
                    }
                    break;
                default:
                    Console.WriteLine("Too many command line arguments.  Format is \"BHLHandwritingAnalyzer <ITEMID>\".");
                    break;
            }

            return validArgs;
        }
    }
}
