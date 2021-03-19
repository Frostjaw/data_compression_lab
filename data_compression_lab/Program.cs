using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace data_compression_lab
{
    class Program
    {
        const string INPUT_FILE_NAME = "input.bmp";
        const string OUTPUT_FILE_NAME = "output.bmp";
        const string COMPRESSED_FILE_NAME = "compressedFile";
        const int BLOCK_SIZE = 16;
        const byte COMP_DIFF_MAX = 32;
        const int DIFF_MAX = 64;

        private static int _blocksCount = 0;
        private static int _discreteBlocksTempImageWidth = 0;
        private static List<int> _discreteBlocksIndexes = new List<int>();
        private static List<int> _continuousBlocksIndexes = new List<int>();
        private static bool _isDeflateEnabled = true;
        private static bool _isJpegEnabled = false;

        static void Main(string[] args)
        {
            var pathToFile = $"{Directory.GetCurrentDirectory()}\\{INPUT_FILE_NAME}";

            try
            {
                var image = new Bitmap(pathToFile);

                var rawBlocks = SplitImageIntoBlocks(image);

                List<Bitmap> discreteBlocks, continuousBlocks;
                (discreteBlocks, continuousBlocks) = ClassifyBlocks(rawBlocks);

                if (_isDeflateEnabled && _isJpegEnabled)
                {
                    var compressedDiscreteBlocks = DeflateEncode(discreteBlocks);
                    var compressedContinuousBlocks = JpegEncode(continuousBlocks);

                    var compressedCombinedBlocks = new byte[compressedContinuousBlocks.Length + 1][];
                    compressedCombinedBlocks[0] = compressedDiscreteBlocks;

                    for (var i = 0; i < compressedContinuousBlocks.Length; i++)
                    {
                        compressedCombinedBlocks[i + 1] = compressedContinuousBlocks[i];
                    }

                    WriteToBinaryFile(COMPRESSED_FILE_NAME, compressedCombinedBlocks);

                    var compressedFileSize = new FileInfo(COMPRESSED_FILE_NAME).Length;
                    var fileSize = new FileInfo(INPUT_FILE_NAME).Length;
                    var compressionRatio = (decimal)fileSize / compressedFileSize;
                    var stringBuilder = new StringBuilder();
                    stringBuilder.Append($"File size in bytes: {fileSize}");
                    stringBuilder.Append(Environment.NewLine);
                    stringBuilder.Append($"Compressed file size in bytes: {compressedFileSize}");
                    stringBuilder.Append(Environment.NewLine);
                    stringBuilder.Append($"Compression ratio: {compressionRatio:f3}");
                    Console.WriteLine(stringBuilder.ToString());

                    var decompressedCombinedBlocks = ReadFromBinaryFile<byte[][]>(COMPRESSED_FILE_NAME);

                    compressedDiscreteBlocks = decompressedCombinedBlocks[0];
                    compressedContinuousBlocks = new byte[decompressedCombinedBlocks.Length - 1][];
                    for (var i = 0; i < decompressedCombinedBlocks.Length - 1; i++)
                    {
                        compressedContinuousBlocks[i] = decompressedCombinedBlocks[i + 1];
                    }

                    var decompressedDiscreteBlocks = DeflateDecode(compressedDiscreteBlocks);
                    var decompressedContinuousBlocks = JpegDecode(compressedContinuousBlocks);

                    var blocks = CombineBlocks(decompressedDiscreteBlocks, decompressedContinuousBlocks);
                    var decodedImage = CreateImageFromBlocks(blocks);

                    decodedImage.Save(OUTPUT_FILE_NAME, ImageFormat.Bmp);

                    return;
                }

                if (_isDeflateEnabled && !_isJpegEnabled)
                {
                    var compressedDiscreteBlocks = DeflateEncode(discreteBlocks);
                    var compressedContinuousBlocks = PixelsAveragingEncode(continuousBlocks);

                    var compressedCombinedBlocks = new byte[][] 
                    {
                        compressedDiscreteBlocks,
                        compressedContinuousBlocks
                    };
                    WriteToBinaryFile(COMPRESSED_FILE_NAME, compressedCombinedBlocks);

                    var compressedFileSize = new FileInfo(COMPRESSED_FILE_NAME).Length;
                    var fileSize = new FileInfo(INPUT_FILE_NAME).Length;
                    var compressionRatio = (decimal)fileSize / compressedFileSize;
                    var stringBuilder = new StringBuilder();
                    stringBuilder.Append($"File size in bytes: {fileSize}");
                    stringBuilder.Append(Environment.NewLine);
                    stringBuilder.Append($"Compressed file size in bytes: {compressedFileSize}");
                    stringBuilder.Append(Environment.NewLine);
                    stringBuilder.Append($"Compression ratio: {compressionRatio:f3}");
                    Console.WriteLine(stringBuilder.ToString());

                    var decompressedCombinedBlocks = ReadFromBinaryFile<byte[][]>(COMPRESSED_FILE_NAME);

                    compressedDiscreteBlocks = decompressedCombinedBlocks[0];
                    compressedContinuousBlocks = decompressedCombinedBlocks[1];

                    var decompressedDiscreteBlocks = DeflateDecode(compressedDiscreteBlocks);
                    var decompressedContinuousBlocks = PixelsAveragingDecode(compressedContinuousBlocks);

                    var blocks = CombineBlocks(decompressedDiscreteBlocks, decompressedContinuousBlocks);
                    var decodedImage = CreateImageFromBlocks(blocks);
                    decodedImage.Save(OUTPUT_FILE_NAME, ImageFormat.Bmp);

                    return;
                }

                if (!_isDeflateEnabled && _isJpegEnabled)
                {
                    var compressedDiscreteBlocks = RleEncode(discreteBlocks);
                    var compressedContinuousBlocks = JpegEncode(continuousBlocks);

                    var compressedCombinedBlocks = new byte[compressedDiscreteBlocks.Length + compressedContinuousBlocks.Length][];
                    compressedDiscreteBlocks.CopyTo(compressedCombinedBlocks, 0);
                    compressedContinuousBlocks.CopyTo(compressedCombinedBlocks, compressedDiscreteBlocks.Length);

                    WriteToBinaryFile(COMPRESSED_FILE_NAME, compressedCombinedBlocks);

                    var compressedFileSize = new FileInfo(COMPRESSED_FILE_NAME).Length;
                    var fileSize = new FileInfo(INPUT_FILE_NAME).Length;
                    var compressionRatio = (decimal)fileSize / compressedFileSize;
                    var stringBuilder = new StringBuilder();
                    stringBuilder.Append($"File size in bytes: {fileSize}");
                    stringBuilder.Append(Environment.NewLine);
                    stringBuilder.Append($"Compressed file size in bytes: {compressedFileSize}");
                    stringBuilder.Append(Environment.NewLine);
                    stringBuilder.Append($"Compression ratio: {compressionRatio:f3}");
                    Console.WriteLine(stringBuilder.ToString());

                    var decompressedCombinedBlocks = ReadFromBinaryFile<byte[][]>(COMPRESSED_FILE_NAME);

                    compressedDiscreteBlocks = new byte[BLOCK_SIZE][];
                    for (var i = 0; i < compressedDiscreteBlocks.Length; i++)
                    {
                        compressedDiscreteBlocks[i] = decompressedCombinedBlocks[i];
                    }

                    compressedContinuousBlocks = new byte[decompressedCombinedBlocks.Length - BLOCK_SIZE][];
                    for (var i = BLOCK_SIZE; i < compressedContinuousBlocks.Length + BLOCK_SIZE; i++)
                    {
                        compressedContinuousBlocks[i - BLOCK_SIZE] = decompressedCombinedBlocks[i];
                    }

                    var decompressedDiscreteBlocks = RleDecode(compressedDiscreteBlocks);
                    var decompressedContinuousBlocks = JpegDecode(compressedContinuousBlocks);

                    var blocks = CombineBlocks(decompressedDiscreteBlocks, decompressedContinuousBlocks);
                    var decodedImage = CreateImageFromBlocks(blocks);
                    decodedImage.Save(OUTPUT_FILE_NAME, ImageFormat.Bmp);

                    return;
                }

                if (!_isDeflateEnabled && !_isJpegEnabled)
                {
                    var compressedDiscreteBlocks = RleEncode(discreteBlocks);
                    var compressedContinuousBlocks = PixelsAveragingEncode(continuousBlocks);

                    var compressedCombinedBlocks = new byte[compressedDiscreteBlocks.Length + 1][];
                    Array.Copy(compressedDiscreteBlocks, compressedCombinedBlocks, compressedDiscreteBlocks.Length);
                    compressedCombinedBlocks[compressedDiscreteBlocks.Length] = compressedContinuousBlocks;

                    WriteToBinaryFile(COMPRESSED_FILE_NAME, compressedCombinedBlocks);

                    var compressedFileSize = new FileInfo(COMPRESSED_FILE_NAME).Length;
                    var fileSize = new FileInfo(INPUT_FILE_NAME).Length;
                    var compressionRatio = (decimal)fileSize / compressedFileSize;
                    var stringBuilder = new StringBuilder();
                    stringBuilder.Append($"File size in bytes: {fileSize}");
                    stringBuilder.Append(Environment.NewLine);
                    stringBuilder.Append($"Compressed file size in bytes: {compressedFileSize}");
                    stringBuilder.Append(Environment.NewLine);
                    stringBuilder.Append($"Compression ratio: {compressionRatio:f3}");
                    Console.WriteLine(stringBuilder.ToString());

                    var decompressedCombinedBlocks = ReadFromBinaryFile<byte[][]>(COMPRESSED_FILE_NAME);

                    compressedDiscreteBlocks = new byte[decompressedCombinedBlocks.Length - 1][];
                    Array.Copy(decompressedCombinedBlocks, compressedDiscreteBlocks, decompressedCombinedBlocks.Length - 1);

                    compressedContinuousBlocks = decompressedCombinedBlocks[decompressedCombinedBlocks.Length - 1];

                    var decompressedDiscreteBlocks = RleDecode(compressedDiscreteBlocks);
                    var decompressedContinuousBlocks = PixelsAveragingDecode(compressedContinuousBlocks);

                    var blocks = CombineBlocks(decompressedDiscreteBlocks, decompressedContinuousBlocks);
                    var decodedImage = CreateImageFromBlocks(blocks);

                    decodedImage.Save(OUTPUT_FILE_NAME, ImageFormat.Bmp);

                    return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        private static List<Bitmap> SplitImageIntoBlocks(Bitmap image)
        {
            try
            {
                var blocks = new List<Bitmap>();

                for (int y = 0; y < image.Height / BLOCK_SIZE; y++)
                {
                    for (int x = 0; x < image.Width / BLOCK_SIZE; x++)
                    {
                        var block = new Bitmap(BLOCK_SIZE, BLOCK_SIZE);

                        using (var graphics = Graphics.FromImage(block))
                        {
                            graphics.DrawImage(image, new Rectangle(0, 0, BLOCK_SIZE, BLOCK_SIZE), new Rectangle(x * BLOCK_SIZE, y * BLOCK_SIZE, BLOCK_SIZE, BLOCK_SIZE), GraphicsUnit.Pixel);
                        }

                        blocks.Add(block);
                    }
                }

                _blocksCount = blocks.Count;

                return blocks;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return default;
            }

        }

        private static List<Bitmap> CombineBlocks(List<Bitmap> discreteBlocks, List<Bitmap> continuousBlocks)
        {
            var outputArray = new Bitmap[_blocksCount];

            for (var i = 0; i < discreteBlocks.Count; i++)
            {
                var index = _discreteBlocksIndexes[i];
                outputArray[index] = discreteBlocks[i];
            }

            for (var i = 0; i < continuousBlocks.Count; i++)
            {
                var index = _continuousBlocksIndexes[i];
                outputArray[index] = continuousBlocks[i];
            }

            return outputArray.ToList();
        }

        private static Bitmap CreateImageFromBlocks(List<Bitmap> blocks)
        {
            var tempImage = new Bitmap(960, 960);

            using (var graphics = Graphics.FromImage(tempImage))
            {
                graphics.Clear(Color.Black);

                var yOffset = 0;
                for (var y = 0; y < 60; y++)
                {
                    var xOffset = 0;
                    for (var x = 0; x < 60; x++)
                    {
                        var block = blocks[y * 60 + x];
                        graphics.DrawImage(block, new Rectangle(xOffset, yOffset, block.Width, block.Height));
                        xOffset += block.Width;
                    }

                    yOffset += BLOCK_SIZE;
                }
            }

            return tempImage;
        }

        private static (List<Bitmap>, List<Bitmap>) ClassifyBlocks(List<Bitmap> blocks)
        {
            var discreteBlocks = new List<Bitmap>();
            var continuousBlocks = new List<Bitmap>();

            foreach (var (block, index) in blocks.Select((block, index) => (block, index)))
            {
                bool flag = false;
                for (int y = 0; y < BLOCK_SIZE; y++)
                {
                    if (flag == true) break;
                    for (int x = 0; x < BLOCK_SIZE; x++)
                    {
                        if (flag == true) break;

                        Color currentPixel;
                        Color nextPixel;
                        Color bottomPixel;
                        int currentRCompDiff;
                        int currentGCompDiff;
                        int currentBCompDiff;
                        int currentDiff;
                        switch (y)
                        {
                            // последняя строка
                            case BLOCK_SIZE - 1:
                                // последний пиксель в строке
                                if (x == BLOCK_SIZE - 1)
                                {
                                    break;
                                }
                                currentPixel = block.GetPixel(x, y);
                                nextPixel = block.GetPixel(x + 1, y);

                                // условие 1
                                currentRCompDiff = Math.Abs(currentPixel.R - nextPixel.R);
                                currentGCompDiff = Math.Abs(currentPixel.G - nextPixel.G);
                                currentBCompDiff = Math.Abs(currentPixel.B - nextPixel.B);
                                if ((currentRCompDiff >= COMP_DIFF_MAX) &&
                                    (currentGCompDiff >= COMP_DIFF_MAX) &&
                                    (currentBCompDiff >= COMP_DIFF_MAX))
                                {
                                    flag = true;
                                    break;
                                }

                                // условие 2
                                currentDiff = Math.Abs(currentPixel.R + currentPixel.G + currentPixel.B -
                                    nextPixel.R - nextPixel.G - nextPixel.B);
                                if (currentDiff >= DIFF_MAX)
                                {
                                    flag = true;
                                    break;
                                }

                                break;
                            // остальные строки
                            default:
                                // последний пиксель в строке
                                if (x == BLOCK_SIZE - 1)
                                {
                                    currentPixel = block.GetPixel(x, y);
                                    bottomPixel = block.GetPixel(x, y + 1);

                                    // условие 1
                                    currentRCompDiff = Math.Abs(currentPixel.R - bottomPixel.R);
                                    currentGCompDiff = Math.Abs(currentPixel.G - bottomPixel.G);
                                    currentBCompDiff = Math.Abs(currentPixel.B - bottomPixel.B);
                                    if ((currentRCompDiff >= COMP_DIFF_MAX) &&
                                        (currentGCompDiff >= COMP_DIFF_MAX) &&
                                        (currentBCompDiff >= COMP_DIFF_MAX))
                                    {
                                        flag = true;
                                        break;
                                    }

                                    // условие 2
                                    currentDiff = Math.Abs(currentPixel.R + currentPixel.G + currentPixel.B -
                                        bottomPixel.R - bottomPixel.G - bottomPixel.B);
                                    if (currentDiff >= DIFF_MAX)
                                    {
                                        flag = true;
                                        break;
                                    }

                                    break;
                                }

                                currentPixel = block.GetPixel(x, y);
                                nextPixel = block.GetPixel(x + 1, y);

                                // условие 1
                                currentRCompDiff = Math.Abs(currentPixel.R - nextPixel.R);
                                currentGCompDiff = Math.Abs(currentPixel.G - nextPixel.G);
                                currentBCompDiff = Math.Abs(currentPixel.B - nextPixel.B);
                                if ((currentRCompDiff >= COMP_DIFF_MAX) &&
                                    (currentGCompDiff >= COMP_DIFF_MAX) &&
                                    (currentBCompDiff >= COMP_DIFF_MAX))
                                {
                                    flag = true;
                                    break;
                                }

                                // условие 2
                                currentDiff = Math.Abs(currentPixel.R + currentPixel.G + currentPixel.B -
                                    nextPixel.R - nextPixel.G - nextPixel.B);
                                if (currentDiff >= DIFF_MAX)
                                {
                                    flag = true;
                                    break;
                                }

                                bottomPixel = block.GetPixel(x, y + 1);

                                // условие 1
                                currentRCompDiff = Math.Abs(currentPixel.R - bottomPixel.R);
                                currentGCompDiff = Math.Abs(currentPixel.G - bottomPixel.G);
                                currentBCompDiff = Math.Abs(currentPixel.B - bottomPixel.B);
                                if ((currentRCompDiff >= COMP_DIFF_MAX) &&
                                    (currentGCompDiff >= COMP_DIFF_MAX) &&
                                    (currentBCompDiff >= COMP_DIFF_MAX))
                                {
                                    flag = true;
                                    break;
                                }

                                // условие 2
                                currentDiff = Math.Abs(currentPixel.R + currentPixel.G + currentPixel.B -
                                    bottomPixel.R - bottomPixel.G - bottomPixel.B);
                                if (currentDiff >= DIFF_MAX)
                                {
                                    flag = true;
                                    break;
                                }

                                break;
                        }
                    }
                }
                // блок дискретно-тоновый
                if (flag == true)
                {
                    discreteBlocks.Add(block);
                    _discreteBlocksIndexes.Add(index);
                    continue;
                };

                continuousBlocks.Add(block);
                _continuousBlocksIndexes.Add(index);
            }

            return (discreteBlocks, continuousBlocks);
        }

        private static byte[][] RleEncode(List<Bitmap> blocks)
        {
            try
            {
                _discreteBlocksTempImageWidth = BLOCK_SIZE * blocks.Count;

                var outputArray = new byte[BLOCK_SIZE][];

                var tempImage = new Bitmap(_discreteBlocksTempImageWidth, BLOCK_SIZE);

                using (var graphics = Graphics.FromImage(tempImage))
                {
                    graphics.Clear(Color.Black);

                    var offset = 0;

                    foreach (var block in blocks)
                    {
                        graphics.DrawImage(block, new Rectangle(offset, 0, block.Width, block.Height));
                        offset += block.Width;
                    }
                }

                byte[] countBytes;
                for (int y = 0; y < tempImage.Height; y++)
                {
                    var count = 1;
                    var rowOutput = new List<byte>();

                    var currentPixel = tempImage.GetPixel(0, y);

                    for (int x = 1; x < tempImage.Width; x++)
                    {
                        var nextPixel = tempImage.GetPixel(x, y);

                        if (currentPixel == nextPixel)
                        {
                            count++;
                        }
                        else
                        {
                            countBytes = BitConverter.GetBytes(count);
                            foreach (var curByte in countBytes)
                            {
                                rowOutput.Add(curByte);
                            }

                            rowOutput.Add(currentPixel.R);
                            rowOutput.Add(currentPixel.G);
                            rowOutput.Add(currentPixel.B);

                            currentPixel = nextPixel;
                            count = 1;
                        }
                    }

                    countBytes = BitConverter.GetBytes(count);
                    foreach (var curByte in countBytes)
                    {
                        rowOutput.Add(curByte);
                    }

                    rowOutput.Add(currentPixel.R);
                    rowOutput.Add(currentPixel.G);
                    rowOutput.Add(currentPixel.B);

                    outputArray[y] = rowOutput.ToArray();
                }

                return outputArray;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return default;
            }
        }

        private static List<Bitmap> RleDecode(byte[][] compressedBitmap)
        {
            try
            {
                var tempImage = new Bitmap(_discreteBlocksTempImageWidth, BLOCK_SIZE);

                for (int y = 0; y < BLOCK_SIZE; y++)
                {
                    var row = compressedBitmap[y];
                    var xIndex = 0;

                    for (var i = 0; i < row.Length; i += 7)
                    {
                        byte[] bytes = { row[i], row[i + 1], row[i + 2], row[i + 3] };
                        var sequenceLength = BitConverter.ToInt32(bytes);

                        for (var j = 0; j < sequenceLength; j++)
                        {
                            var pixel = Color.FromArgb(row[i + 4], row[i + 5], row[i + 6]);
                            tempImage.SetPixel(xIndex, y, pixel);
                            xIndex++;
                        }
                    }
                }

                var blocks = new List<Bitmap>();

                for (var y = 0; y < tempImage.Height / BLOCK_SIZE; y++)
                {
                    for (var x = 0; x < tempImage.Width / BLOCK_SIZE; x++)
                    {
                        var block = new Bitmap(BLOCK_SIZE, BLOCK_SIZE);

                        using (var graphics = Graphics.FromImage(block))
                        {
                            graphics.DrawImage(tempImage, new Rectangle(0, 0, BLOCK_SIZE, BLOCK_SIZE), new Rectangle(x * BLOCK_SIZE, y * BLOCK_SIZE, BLOCK_SIZE, BLOCK_SIZE), GraphicsUnit.Pixel);
                        }

                        blocks.Add(block);
                    }
                }

                return blocks;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return default;
            }
        }

        private static byte[] PixelsAveragingEncode(List<Bitmap> blocks)
        {
            var outputList = new List<byte>();

            foreach (var block in blocks)
            {
                var r = 0;
                var g = 0;
                var b = 0;
                var total = 0;

                for (var y = 0; y < block.Height; y++)
                {
                    for (int x = 0; x < block.Width; x++)
                    {
                        Color color = block.GetPixel(x, y);

                        r += color.R;
                        g += color.G;
                        b += color.B;

                        total++;
                    }
                }

                r = (int)Math.Round((float)r / total);
                g = (int)Math.Round((float)g / total);
                b = (int)Math.Round((float)b / total);

                outputList.Add(Convert.ToByte(r));
                outputList.Add(Convert.ToByte(g));
                outputList.Add(Convert.ToByte(b));
            }

            return outputList.ToArray();
        }

        private static List<Bitmap> PixelsAveragingDecode(byte[] data)
        {
            var outputList = new List<Bitmap>();

            for (var i = 0; i < data.Length; i += 3)
            {
                var block = new Bitmap(BLOCK_SIZE, BLOCK_SIZE);

                for (var y = 0; y < block.Height; y++)
                {
                    for (int x = 0; x < block.Width; x++)
                    {
                        var pixel = Color.FromArgb(data[i], data[i + 1], data[i + 2]);
                        block.SetPixel(x, y, pixel);
                    }
                }

                outputList.Add(block);
            }

            return outputList;
        }

        private static byte[][] JpegEncode(List<Bitmap> blocks)
        {
            var outputList = new List<byte[]>();

            foreach (var block in blocks)
            {
                using var memoryStream = new MemoryStream();

                var jpgEncoder = GetEncoder(ImageFormat.Jpeg);

                var myEncoderParameters = new EncoderParameters(1);

                var myEncoderParameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 50L);
                myEncoderParameters.Param[0] = myEncoderParameter;
                block.Save(memoryStream, jpgEncoder, myEncoderParameters);

                outputList.Add(memoryStream.ToArray());
            }

            return outputList.ToArray();
        }

        private static List<Bitmap> JpegDecode(byte[][] data)
        {
            var outputList = new List<Bitmap>();

            for (var i = 0; i< data.Length; i++)
            {
                using var memoryStream = new MemoryStream(data[i]);
                var block = new Bitmap(memoryStream);
                var tempBlock = new Bitmap(block);
                outputList.Add(tempBlock);
            }

            return outputList;
        }

        private static byte[] DeflateEncode(List<Bitmap> blocks)
        {
            _discreteBlocksTempImageWidth = BLOCK_SIZE * blocks.Count;

            var tempImage = new Bitmap(_discreteBlocksTempImageWidth, BLOCK_SIZE);

            using (var graphics = Graphics.FromImage(tempImage))
            {
                graphics.Clear(Color.Black);

                var offset = 0;

                foreach (var block in blocks)
                {
                    graphics.DrawImage(block, new Rectangle(offset, 0, block.Width, block.Height));
                    offset += block.Width;
                }
            }

            using var memoryStream = new MemoryStream();
            tempImage.Save(memoryStream, ImageFormat.Bmp);
            var data = memoryStream.ToArray();

            return DeflateCompress(data);
        }

        private static List<Bitmap> DeflateDecode(byte[] data)
        {
            var decodedData = DeflateDecompress(data);
            using var memoryStream = new MemoryStream(decodedData);
            var tempImage = new Bitmap(memoryStream);

            var blocks = new List<Bitmap>();

            for (var y = 0; y < tempImage.Height / BLOCK_SIZE; y++)
            {
                for (var x = 0; x < tempImage.Width / BLOCK_SIZE; x++)
                {
                    var block = new Bitmap(BLOCK_SIZE, BLOCK_SIZE);

                    using (var graphics = Graphics.FromImage(block))
                    {
                        graphics.DrawImage(tempImage, new Rectangle(0, 0, BLOCK_SIZE, BLOCK_SIZE), new Rectangle(x * BLOCK_SIZE, y * BLOCK_SIZE, BLOCK_SIZE, BLOCK_SIZE), GraphicsUnit.Pixel);
                    }

                    blocks.Add(block);
                }
            }

            return blocks;
        }

        private static byte[] DeflateCompress(byte[] data)
        {
            var output = new MemoryStream();
            using (var dstream = new DeflateStream(output, CompressionMode.Compress))
            {
                dstream.Write(data, 0, data.Length);
            }

            return output.ToArray();
        }

        private static byte[] DeflateDecompress(byte[] data)
        {
            var input = new MemoryStream(data);
            var output = new MemoryStream();
            using (var dstream = new DeflateStream(input, CompressionMode.Decompress))
            {
                dstream.CopyTo(output);
            }
            return output.ToArray();
        }

        private static void WriteToBinaryFile<T>(string filePath, T objectToWrite, bool append = false)
        {
            using Stream stream = File.Open(filePath, append ? FileMode.Append : FileMode.Create);
            var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            binaryFormatter.Serialize(stream, objectToWrite);
        }

        public static T ReadFromBinaryFile<T>(string filePath)
        {
            using Stream stream = File.Open(filePath, FileMode.Open);
            var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            return (T)binaryFormatter.Deserialize(stream);
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}
