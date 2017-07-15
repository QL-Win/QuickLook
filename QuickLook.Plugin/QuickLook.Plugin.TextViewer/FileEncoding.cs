#region *   License     *

/*
    SimpleHelpers - FileEncoding   

    Copyright © 2014 Khalid Salomão

    Permission is hereby granted, free of charge, to any person
    obtaining a copy of this software and associated documentation
    files (the “Software”), to deal in the Software without
    restriction, including without limitation the rights to use,
    copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the
    Software is furnished to do so, subject to the following
    conditions:

    The above copyright notice and this permission notice shall be
    included in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
    OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
    NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
    HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
    WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
    FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
    OTHER DEALINGS IN THE SOFTWARE. 

    License: http://www.opensource.org/licenses/mit-license.php
    Website: https://github.com/khalidsalomao/SimpleHelpers.Net
 */

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Ude;

namespace QuickLook.Plugin.TextViewer
{
    public class FileEncoding
    {
        private const int DefaultBufferSize = 128 * 1024;

        private readonly Dictionary<string, int> _encodingFrequency =
            new Dictionary<string, int>(StringComparer.Ordinal);

        private readonly CharsetDetector _ude = new CharsetDetector();
        private bool _started;


        /// <summary>
        ///     If the detection has reached a decision.
        /// </summary>
        /// <value>The done.</value>
        public bool Done { get; set; }

        /// <summary>
        ///     Detected encoding name.
        /// </summary>
        public string EncodingName { get; set; }

        /// <summary>
        ///     If the data contains textual data.
        /// </summary>
        public bool IsText { get; set; }

        /// <summary>
        ///     If the file or data has any mark indicating encoding information (byte order mark).
        /// </summary>
        public bool HasByteOrderMark { get; set; }

        /// <summary>
        ///     Tries to detect the file encoding.
        /// </summary>
        /// <param name="inputFilename">The input filename.</param>
        /// <param name="defaultIfNotDetected">The default encoding if none was detected.</param>
        /// <returns></returns>
        public static Encoding DetectFileEncoding(string inputFilename, Encoding defaultIfNotDetected = null)
        {
            using (var stream = new FileStream(inputFilename, FileMode.Open,
                FileAccess.Read, FileShare.ReadWrite | FileShare.Delete,
                DefaultBufferSize))
            {
                return DetectFileEncoding(stream) ?? defaultIfNotDetected;
            }
        }

        /// <summary>
        ///     Tries to detect the file encoding.
        /// </summary>
        /// <param name="inputStream">The input stream.</param>
        /// <param name="defaultIfNotDetected">The default encoding if none was detected.</param>
        /// <returns></returns>
        public static Encoding DetectFileEncoding(Stream inputStream, Encoding defaultIfNotDetected = null)
        {
            var det = new FileEncoding();
            det.Detect(inputStream);
            return det.Complete() ?? defaultIfNotDetected;
        }

        /// <summary>
        ///     Tries to detect the file encoding.
        /// </summary>
        /// <param name="inputData">The input data.</param>
        /// <param name="start">The start.</param>
        /// <param name="count">The count.</param>
        /// <param name="defaultIfNotDetected">The default encoding if none was detected.</param>
        /// <returns></returns>
        public static Encoding DetectFileEncoding(byte[] inputData, int start, int count,
            Encoding defaultIfNotDetected = null)
        {
            var det = new FileEncoding();
            det.Detect(inputData, start, count);
            return det.Complete() ?? defaultIfNotDetected;
        }

        /// <summary>
        ///     Tries to load file content with the correct encoding.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="defaultValue">The default value if unable to load file content.</param>
        /// <returns>File content</returns>
        public static string TryLoadFile(string filename, string defaultValue = "")
        {
            try
            {
                if (File.Exists(filename))
                {
                    // enable file encoding detection
                    var encoding = DetectFileEncoding(filename);
                    // Load data based on parameters
                    return File.ReadAllText(filename, encoding);
                }
            }
            catch
            {
                // ignored
            }
            return defaultValue;
        }

        /// <summary>
        ///     Detects if contains textual data.
        /// </summary>
        /// <param name="rawData">The raw data.</param>
        public static bool CheckForTextualData(byte[] rawData)
        {
            return CheckForTextualData(rawData, 0, rawData.Length);
        }

        /// <summary>
        ///     Detects if contains textual data.
        /// </summary>
        /// <param name="rawData">The raw data.</param>
        /// <param name="start">The start.</param>
        /// <param name="count">The count.</param>
        public static bool CheckForTextualData(byte[] rawData, int start, int count)
        {
            if (rawData.Length < count || count < 4 || start + 1 >= count)
                return true;

            if (CheckForByteOrderMark(rawData, start))
                return true;

            // http://stackoverflow.com/questions/910873/how-can-i-determine-if-a-file-is-binary-or-text-in-c
            // http://www.gnu.org/software/diffutils/manual/html_node/Binary.html
            // count the number od null bytes sequences
            // considering only sequeces of 2 0s: "\0\0" or control characters below 10
            var nullSequences = 0;
            var controlSequences = 0;
            for (var i = start + 1; i < count; i++)
                if (rawData[i - 1] == 0 && rawData[i] == 0)
                {
                    if (++nullSequences > 1)
                        break;
                }
                else if (rawData[i - 1] == 0 && rawData[i] < 10)
                {
                    ++controlSequences;
                }

            // is text if there is no null byte sequences or less than 10% of the buffer has control caracteres
            return nullSequences == 0 && controlSequences <= rawData.Length / 10;
        }

        /// <summary>
        ///     Detects if data has bytes order mark to indicate its encoding for textual data.
        /// </summary>
        /// <param name="rawData">The raw data.</param>
        /// <param name="start">The start.</param>
        /// <returns></returns>
        private static bool CheckForByteOrderMark(byte[] rawData, int start = 0)
        {
            if (rawData.Length - start < 4)
                return false;
            // Detect encoding correctly (from Rick Strahl's blog)
            // http://www.west-wind.com/weblog/posts/2007/Nov/28/Detecting-Text-Encoding-for-StreamReader
            if (rawData[start] == 0xef && rawData[start + 1] == 0xbb && rawData[start + 2] == 0xbf)
                return true;
            if (rawData[start] == 0xfe && rawData[start + 1] == 0xff)
                return true;
            if (rawData[start] == 0 && rawData[start + 1] == 0 && rawData[start + 2] == 0xfe &&
                rawData[start + 3] == 0xff)
                return true;
            if (rawData[start] == 0x2b && rawData[start + 1] == 0x2f && rawData[start + 2] == 0x76)
                return true;
            return false;
        }

        /// <summary>
        ///     Resets this instance.
        /// </summary>
        public void Reset()
        {
            _started = false;
            Done = false;
            HasByteOrderMark = false;
            _encodingFrequency.Clear();
            _ude.Reset();
            EncodingName = null;
        }

        /// <summary>
        ///     Detects the encoding of textual data of the specified input data.
        ///     <para />
        ///     Only the stream first 1Mb will be analysed.
        /// </summary>
        /// <param name="inputData">The input data.</param>
        /// <returns>Detected encoding name</returns>
        public string Detect(Stream inputData)
        {
            return Detect(inputData, 1 * 1024 * 1024);
        }

        /// <summary>
        ///     Detects the encoding of textual data of the specified input data.
        /// </summary>
        /// <param name="inputData">The input data.</param>
        /// <param name="maxSize">
        ///     Size in byte of analysed data, if you want to analysed only a sample. Use 0 to read all stream
        ///     data.
        /// </param>
        /// <param name="bufferSize">Size of the buffer for the stream read.</param>
        /// <returns>Detected encoding name</returns>
        /// <exception cref="ArgumentOutOfRangeException">bufferSize parameter cannot be 0 or less.</exception>
        public string Detect(Stream inputData, int maxSize, int bufferSize = 16 * 1024)
        {
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), @"Buffer size cannot be 0 or less.");
            var maxIterations = maxSize <= 0 ? int.MaxValue : maxSize / bufferSize;
            var i = 0;
            var buffer = new byte[bufferSize];
            while (i++ < maxIterations)
            {
                var sz = inputData.Read(buffer, 0, buffer.Length);
                if (sz <= 0)
                    break;
                Detect(buffer, 0, sz);
                if (Done)
                    break;
            }
            Complete();
            return EncodingName;
        }

        /// <summary>
        ///     Detects the encoding of textual data of the specified input data.
        /// </summary>
        /// <param name="inputData">The input data.</param>
        /// <param name="start">The start.</param>
        /// <param name="count">The count.</param>
        /// <returns>Detected encoding name</returns>
        public string Detect(byte[] inputData, int start, int count)
        {
            if (Done)
                return EncodingName;
            if (!_started)
            {
                Reset();
                _started = true;
                if (!CheckForTextualData(inputData, start, count))
                {
                    IsText = false;
                    Done = true;
                    return EncodingName;
                }
                HasByteOrderMark = CheckForByteOrderMark(inputData, start);
                IsText = true;
            }

            // execute charset detector                
            _ude.Feed(inputData, start, count);
            _ude.DataEnd();
            if (_ude.IsDone() && !string.IsNullOrEmpty(_ude.Charset))
            {
                IncrementFrequency(_ude.Charset);
                Done = true;
                return EncodingName;
            }

            // singular buffer detection
            var singleUde = new CharsetDetector();
            const int udeFeedSize = 4 * 1024;
            var step = count - start < udeFeedSize ? count - start : udeFeedSize;
            for (var pos = start; pos < count; pos += step)
            {
                singleUde.Reset();
                if (pos + step > count)
                    singleUde.Feed(inputData, pos, count - pos);
                else
                    singleUde.Feed(inputData, pos, step);
                singleUde.DataEnd();
                // update encoding frequency
                if (singleUde.Confidence > 0.3 && !string.IsNullOrEmpty(singleUde.Charset))
                    IncrementFrequency(singleUde.Charset);
            }
            // vote for best encoding
            EncodingName = GetCurrentEncoding();
            // update current encoding name
            return EncodingName;
        }

        /// <summary>
        ///     Finalize detection phase and gets detected encoding name.
        /// </summary>
        /// <returns></returns>
        public Encoding Complete()
        {
            Done = true;
            _ude.DataEnd();
            if (_ude.IsDone() && !string.IsNullOrEmpty(_ude.Charset))
                EncodingName = _ude.Charset;
            // vote for best encoding
            EncodingName = GetCurrentEncoding();
            // check result
            if (!string.IsNullOrEmpty(EncodingName))
                return Encoding.GetEncoding(EncodingName);
            return null;
        }

        private void IncrementFrequency(string charset)
        {
            int currentCount;
            _encodingFrequency.TryGetValue(charset, out currentCount);
            _encodingFrequency[charset] = ++currentCount;
        }

        private string GetCurrentEncoding()
        {
            if (_encodingFrequency.Count == 0)
                return null;
            // ASCII should be the last option, since other encodings often has ASCII included...
            return _encodingFrequency
                .OrderByDescending(i => i.Value * (i.Key != "ASCII" ? 1 : 0))
                .FirstOrDefault().Key;
        }
    }
}