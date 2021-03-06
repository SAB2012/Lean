﻿/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Core;
using QuantConnect.Logging;

namespace QuantConnect 
{
    /// <summary>
    /// Compression class manages the opening and extraction of compressed files (zip, tar, tar.gz).
    /// </summary>
    /// <remarks>QuantConnect's data library is stored in zip format locally on the hard drive.</remarks>
    public static class Compression
    {
        /// <summary>
        /// Create a zip file of the supplied file names and string data source
        /// </summary>
        /// <param name="zipPath">Output location to save the file.</param>
        /// <param name="filenamesAndData">File names and data in a dictionary format.</param>
        /// <returns>True on successfully creating the zip file.</returns>
        public static bool ZipData(string zipPath, Dictionary<string, string> filenamesAndData)
        {
            var success = true;
            var buffer = new byte[4096];

            try
            {
                //Create our output
                using (var stream = new ZipOutputStream(File.Create(zipPath)))
                {
                    foreach (var filename in filenamesAndData.Keys)
                    {
                        //Create the space in the zip file:
                        var entry = new ZipEntry(filename);
                        //Get a Byte[] of the file data:
                        var file = Encoding.Default.GetBytes(filenamesAndData[filename]);
                        stream.PutNextEntry(entry);

                        using (var ms = new MemoryStream(file))
                        {
                            int sourceBytes;
                            do
                            {
                                sourceBytes = ms.Read(buffer, 0, buffer.Length);
                                stream.Write(buffer, 0, sourceBytes);
                            }
                            while (sourceBytes > 0);
                        }
                    } // End For Each File.

                    //Close stream:
                    stream.Finish();
                    stream.Close();
                } // End Using
            }
            catch (Exception err)
            {
                Log.Error("QC.Data.ZipData(): " + err.Message);
                success = false;
            }
            return success;
        }

        /// <summary>
        /// Create a zip file of the supplied file names and data using a byte array
        /// </summary>
        /// <param name="zipPath">Output location to save the file.</param>
        /// <param name="filenamesAndData">File names and data in a dictionary format.</param>
        /// <returns>True on successfully saving the file</returns>
        public static bool ZipData(string zipPath, IReadOnlyDictionary<string, byte[]> filenamesAndData)
        {
            var success = true;
            var buffer = new byte[4096];

            try
            {
                //Create our output
                using (var stream = new ZipOutputStream(File.Create(zipPath)))
                {
                    foreach (var filename in filenamesAndData.Keys)
                    {
                        //Create the space in the zip file:
                        var entry = new ZipEntry(filename);
                        //Get a Byte[] of the file data:
                        var file = filenamesAndData[filename];
                        stream.PutNextEntry(entry);

                        using (var ms = new MemoryStream(file))
                        {
                            int sourceBytes;
                            do
                            {
                                sourceBytes = ms.Read(buffer, 0, buffer.Length);
                                stream.Write(buffer, 0, sourceBytes);
                            }
                            while (sourceBytes > 0);
                        }
                    } // End For Each File.

                    //Close stream:
                    stream.Finish();
                    stream.Close();
                } // End Using
            }
            catch (Exception err)
            {
                Log.Error("QC.Data.ZipData(): " + err.Message);
                success = false;
            }
            return success;
        }

        /// <summary>
        /// Uncompress zip data byte array into a dictionary string array of filename-contents.
        /// </summary>
        /// <param name="zipData">Byte data array of zip compressed information</param>
        /// <returns>Uncompressed dictionary string-sting of files in the zip</returns>
        public static Dictionary<string, string> UnzipData(byte[] zipData)
        {
            // Initialize:
            var data = new Dictionary<string, string>();

            try
            {
                using (var ms = new MemoryStream(zipData))
                {
                    //Read out the zipped data into a string, save in array:
                    using (var zipStream = new ZipInputStream(ms))
                    {
                        while (true)
                        {
                            //Get the next file
                            var entry = zipStream.GetNextEntry();

                            if (entry != null)
                            {
                                //Read the file into buffer:
                                var buffer = new byte[entry.Size];
                                zipStream.Read(buffer, 0, (int)entry.Size);

                                //Save into array:
                                data.Add(entry.Name, buffer.GetString());
                            }
                            else
                            {
                                break;
                            }
                        }
                    } // End Zip Stream.
                } // End Using Memory Stream

            }
            catch (Exception err)
            {
                Log.Error("Data.UnzipData(): " + err.Message);
            }
            return data;
        }

        /// <summary>
        /// Compress a given file and delete the original file. Automatically rename the file to name.zip.
        /// </summary>
        /// <param name="textPath">Path of the original file</param>
        /// <param name="deleteOriginal">Boolean flag to delete the original file after completion</param>
        /// <returns>String path for the new zip file</returns>
        public static string Zip(string textPath, bool deleteOriginal = true)
        {
            var zipPath = "";

            try
            {
                var buffer = new byte[4096];
                zipPath = textPath.Replace(".csv", ".zip");
                zipPath = zipPath.Replace(".txt", ".zip");
                //Open the zip:
                using (var stream = new ZipOutputStream(File.Create(zipPath)))
                {
                    //Zip the text file.
                    var entry = new ZipEntry(Path.GetFileName(textPath));
                    stream.PutNextEntry(entry);

                    using (var fs = File.OpenRead(textPath))
                    {
                        int sourceBytes;
                        do
                        {
                            sourceBytes = fs.Read(buffer, 0, buffer.Length);
                            stream.Write(buffer, 0, sourceBytes);
                        }
                        while (sourceBytes > 0);
                    }
                    //Close stream:
                    stream.Finish();
                    stream.Close();
                }
                //Delete the old text file:
                if (deleteOriginal) File.Delete(textPath);
            }
            catch (Exception err)
            {
                Log.Error("QC.Data.Zip(): " + err.Message);
            }
            return zipPath;
        } // End Zip:

        public static void Zip(string data, string zipPath, string zipEntry)
        {
            using (var stream = new ZipOutputStream(File.Create(zipPath)))
            {
                var entry = new ZipEntry(zipEntry);
                stream.PutNextEntry(entry);
                var buffer = new byte[4096];
                using (var dataReader = new MemoryStream(Encoding.Default.GetBytes(data)))
                {
                    int sourceBytes;
                    do
                    {
                        sourceBytes = dataReader.Read(buffer, 0, buffer.Length);
                        stream.Write(buffer, 0, sourceBytes);
                    }
                    while (sourceBytes > 0);
                }
            }
        }

        /// <summary>
        /// Zips all files specified to a new zip at the destination path
        /// </summary>
        public static void ZipFiles(string destination, IEnumerable<string> files)
        {
            try
            {
                using (var zipStream = new ZipOutputStream(File.Create(destination)))
                {
                    var buffer = new byte[4096];
                    foreach (var file in files)
                    {
                        if (!File.Exists(file))
                        {
                            Log.Trace("ZipFiles(): File does not exist: " + file);
                            continue;
                        }

                        var entry = new ZipEntry(Path.GetFileName(file));
                        zipStream.PutNextEntry(entry);
                        using (var fstream = File.OpenRead(file))
                        {
                            StreamUtils.Copy(fstream, zipStream, buffer);
                        }
                    }
                }
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
        }

        /// <summary>
        /// Streams a local zip file using a streamreader.
        /// Important: the caller must call Dispose() on the returned ZipFile instance.
        /// </summary>
        /// <param name="filename">Location of the original zip file</param>
        /// <param name="zip">The ZipFile instance to be returned to the caller</param>
        /// <returns>Stream reader of the first file contents in the zip file</returns>
        public static StreamReader Unzip(string filename, out Ionic.Zip.ZipFile zip)
        {
            StreamReader reader = null;
            zip = null;

            try
            {
                if (File.Exists(filename))
                {
                    try
                    {
                        zip = new Ionic.Zip.ZipFile(filename);

                        reader = new StreamReader(zip[0].OpenReader());
                    }
                    catch (Exception err)
                    {
                        Log.Error("QC.Data.Unzip(1): " + err.Message);
                        if (zip != null) zip.Dispose();
                        if (reader != null) reader.Close();
                    }
                }
                else
                {
                    Log.Error("Data.UnZip(2): File doesn't exist: " + filename);
                }
            }
            catch (Exception err)
            {
                Log.Error("Data.UnZip(3): " + filename + " >> " + err.Message);
            }
            return reader;
        } // End UnZip

        /// <summary>
        /// Streams each line from the first zip entry in the specified zip file
        /// </summary>
        /// <param name="filename">The zip file path to stream</param>
        /// <returns>An enumerable containing each line from the first unzipped entry</returns>
        public static IEnumerable<string> ReadLines(string filename)
        {
            if (!File.Exists(filename))
            {
                Log.Error("Compression.ReadFirstZipEntry(): File does not exist: " + filename);
                return null;
            }

            try
            {
                return ReadLinesImpl(filename);
            }
            catch (Exception err)
            {
                Log.Error(err);
            }
            return null;
        }

        private static IEnumerable<string> ReadLinesImpl(string filename)
        {
            using (var zip = Ionic.Zip.ZipFile.Read(filename))
            {
                var entry = zip[0];
                using (var entryReader = new StreamReader(entry.OpenReader()))
                {
                    while (!entryReader.EndOfStream)
                    {
                        yield return entryReader.ReadLine();
                    }
                }
            }
        }

        /// <summary>
        /// Unzip a local file and return its contents via streamreader:
        /// </summary>
        public static StreamReader UnzipStream(Stream zipstream)
        {
            StreamReader reader = null;
            try
            {
                //Initialise:                    
                MemoryStream file;

                //If file exists, open a zip stream for it.
                using (var zipStream = new ZipInputStream(zipstream))
                {
                    //Read the file entry into buffer:
                    var entry = zipStream.GetNextEntry();
                    var buffer = new byte[entry.Size];
                    zipStream.Read(buffer, 0, (int)entry.Size);

                    //Load the buffer into a memory stream.
                    file = new MemoryStream(buffer);
                }

                //Open the memory stream with a stream reader.
                reader = new StreamReader(file);
            }
            catch (Exception err)
            {
                Log.Error(err, "Data.UnZip(): Stream >> " + err.Message);
            }

            return reader;
        } // End UnZip

        /// <summary>
        /// Unzip a local file and return its contents via streamreader to a local the same location as the ZIP.
        /// </summary>
        /// <param name="zipFile">Location of the zip on the HD</param>
        /// <returns>List of unzipped file names</returns>
        public static List<string> UnzipToFolder(string zipFile)
        {
            //1. Initialize:
            var files = new List<string>();
            var outFolder = zipFile.Substring(0, zipFile.LastIndexOf(Path.DirectorySeparatorChar));
            ZipFile zf = null;

            try
            {
                var fs = File.OpenRead(zipFile);
                zf = new ZipFile(fs);

                foreach (ZipEntry zipEntry in zf)
                {
                    //Ignore Directories
                    if (!zipEntry.IsFile) continue;

                    //Remove the folder from the entry
                    var entryFileName = Path.GetFileName(zipEntry.Name);
                    if (entryFileName == null) continue;

                    var buffer = new byte[4096];     // 4K is optimum
                    var zipStream = zf.GetInputStream(zipEntry);

                    // Manipulate the output filename here as desired.
                    var fullZipToPath = Path.Combine(outFolder, entryFileName);

                    //Save the file name for later:
                    files.Add(fullZipToPath);
                    //Log.Trace("Data.UnzipToFolder(): Input File: " + zipFile + ", Output Directory: " + fullZipToPath); 

                    //Copy the data in buffer chunks
                    using (var streamWriter = File.Create(fullZipToPath))
                    {
                        StreamUtils.Copy(zipStream, streamWriter, buffer);
                    }
                }
            }
            finally
            {
                if (zf != null)
                {
                    zf.IsStreamOwner = true; // Makes close also shut the underlying stream
                    zf.Close(); // Ensure we release resources
                }
            }
            return files;
        } // End UnZip

        /// <summary>
        /// Extracts all file from a zip archive and copies them to a destination folder.
        /// </summary>
        /// <param name="source">The source zip file.</param>
        /// <param name="destination">The destination folder to extract the file to.</param>
        public static void UnTarFiles(string source, string destination)
        {
            var inStream = File.OpenRead(source);
            var tarArchive = TarArchive.CreateInputTarArchive(inStream);
            tarArchive.ExtractContents(destination);
            tarArchive.Close();
            inStream.Close();
        }

        /// <summary>
        /// Extract tar.gz files to disk
        /// </summary>
        /// <param name="source">Tar.gz source file</param>
        /// <param name="destination">Location folder to unzip to</param>
        public static void UnTarGzFiles(string source, string destination)
        {
            var inStream = File.OpenRead(source);
            var gzipStream = new GZipInputStream(inStream);
            var tarArchive = TarArchive.CreateInputTarArchive(gzipStream);
            tarArchive.ExtractContents(destination);
            tarArchive.Close();
            gzipStream.Close();
            inStream.Close();
        }

        /// <summary>
        /// Creates the entry name for a QC zip data file
        /// </summary>
        public static string CreateZipEntryName(string symbol, SecurityType securityType, DateTime date, Resolution resolution)
        {
            if (resolution == Resolution.Hour || resolution == Resolution.Daily)
            {
                return symbol + ".csv";
            }
            if (securityType == SecurityType.Forex)
            {
                return String.Format("{0}_{1}_{2}_quote.csv", date.ToString(DateFormat.EightCharacter), symbol.ToLower(), resolution.ToString().ToLower());
            }
            return String.Format("{0}_{1}_{2}_trade.csv", date.ToString(DateFormat.EightCharacter), symbol.ToLower(), resolution.ToString().ToLower());
        }

        /// <summary>
        /// Creates the zip file name for a QC zip data file
        /// </summary>
        public static string CreateZipFileName(string symbol, SecurityType securityType, DateTime date, Resolution resolution)
        {
            if (resolution == Resolution.Hour || resolution == Resolution.Daily)
            {
                return symbol + ".zip";
            }

            var zipFileName = date.ToString(DateFormat.EightCharacter);
            if (securityType == SecurityType.Forex)
            {
                return zipFileName + "_quote.zip";
            }
            return zipFileName + "_trade.zip";
        }
    } // End OS Class
} // End QC Namespace
