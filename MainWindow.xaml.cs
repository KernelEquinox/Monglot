using System;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Reflection;
using Microsoft.WindowsAPICodePack.Dialogs;
using CSJ2K;

namespace Monglot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Class variable definitions
        Random rand = new Random();
        int width, height, stride, bpp, counter;
        WriteableBitmap backupBuffer, buffer;
        byte[] rawData, tempData;
        string ext, termExt, transcoder, baseFile;
        string[] formats = { "bmp", "gif", "png", "tiff", "jpg", "jp2" };
        string outPath = Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\Documents\Monglot");

        public MainWindow()
        {
            // Make sure the output directory exists
            if (!Directory.Exists(outPath))
                Directory.CreateDirectory(outPath);

            // Include all referenced DLL files
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string resourceName = new AssemblyName(args.Name).Name + ".dll";
                string resource = Array.Find(this.GetType().Assembly.GetManifestResourceNames(), element => element.EndsWith(resourceName));

                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
                {
                    Byte[] assemblyData = new Byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    return Assembly.Load(assemblyData);
                }
            };
            InitializeComponent();
        }

        // Populates the transcoder dropdown once it loads
        private void cb_transcoder_Loaded(object sender, RoutedEventArgs e)
        {
            List<string> data = new List<string>();
            data.Add("Vernacular");
            data.Add("BMP");
            data.Add("GIF");
            data.Add("PNG");
            data.Add("TIFF");
            data.Add("JPG");
            data.Add("JPG 2000");
            data.Add("Random");
            var comboBox = sender as ComboBox;
            comboBox.ItemsSource = data;
            comboBox.SelectedIndex = 0;
        }

        // Seeds PRNG with first 32 bits of hashed input from tf_randomSeed textbox
        private void glitchSpeak_Click(object sender, RoutedEventArgs e)
        {
            HashAlgorithm hasher = new System.Security.Cryptography.SHA1Managed();
            var bytes = hasher.ComputeHash(Encoding.Unicode.GetBytes(tf_randomSeed.Text));
            var hash = string.Join("", bytes.Select(x => x.ToString("x2")).ToArray());
            rand = new Random(Convert.ToInt32(hash.Substring(0, 8), 16));
        }

        // Pokes the RNG
        private int pokeRNG(int max)
        {
            return rand.Next(0, max);
        }

        // Sets up the image for glitching, then knocks it back down
        private void eAffect_Click(object sender, RoutedEventArgs e)
        {
            transcoder = cb_transcoder.SelectedItem.ToString().ToLower();

            int iter = 1;
            if (bt_batch.IsChecked == true)
                Int32.TryParse(tf_numFrames.Text, out iter);
            
            if (baseFile == null)
                return;

            for (int i = 0; i < iter; i++)
            {
                tempData = rawData;
                termExt = getTranscoder(transcoder);
                int timestamp = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                string filename = outPath + @"\" + timestamp + (counter++).ToString("D3") + "." + termExt;
                doGlitch(filename);
                GC.Collect();
            }
            counter = 0;
        }

        // Glitches every file in a directory and saves the results to another directory
        private void folderBatch(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog inDlg = new CommonOpenFileDialog();
            CommonOpenFileDialog outDlg = new CommonOpenFileDialog();
            inDlg.IsFolderPicker = true;
            outDlg.IsFolderPicker = true;
            inDlg.Title = "INPUT Folder";
            outDlg.Title = "OUTPUT Folder";
            if (inDlg.ShowDialog() == CommonFileDialogResult.Ok &&
                outDlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string inPathGFD = inDlg.FileName;
                string outPathGFD = outDlg.FileName;
                foreach (string file in Directory.GetFiles(inPathGFD))
                {
                    ext = Path.GetExtension(file).Substring(1).ToLower();
                    switch(ext)
                    {
                        case "bmp":
                        case "gif":
                        case "png":
                        case "tif": case "tiff":
                        case "jpg": case "jpeg":
                        case "jp2": case "jpx": case "j2k":
                            transcoder = getTranscoder(transcoder);
                            string outFile = outPathGFD + @"\" + Path.GetFileName(file) + @"." + transcoder;
                            loadFile(file);
                            doGlitch(outFile);
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        
        // Shows the About dialog box
        private void About(object sender, RoutedEventArgs e)
        {
            About aboutDlg = new About();
            aboutDlg.ShowDialog();
        }

        // Opens the "Open" dialog box
        private void openFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".bmp";
            dlg.Filter  = "All images|*.bmp;*.gif;*.png;*.tif;*.tiff;*.jpg;*jpeg;*.jp2;*.jpx;*.j2k|" +
                "BMP|*.bmp|" +
                "GIF|*.gif|" +
                "PNG|*.png|" +
                "TIFF|*.tif;*.tiff|" +
                "JPG|*.jpg;*.jpeg|" +
                "JPG 2000|*.jp2;*.jpx;*.j2k";

            if (dlg.ShowDialog() == true)
            {
                baseFile = dlg.FileName;
                loadFile(baseFile);
            }
        }

        // Saves the current image displayed on screen to a file
        private void saveBuffer(object sender, RoutedEventArgs e)
        {
            if (baseFile == null)
                return;
            int timestamp = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            File.WriteAllBytes(outPath + @"\" + timestamp + "." + transcoder, tempData);
        }

        // Updates global variables with the loaded file's properties
        private void loadFile(string filename)
        {
            ext = transcoder = termExt = Path.GetExtension(filename).Substring(1).ToLower();
            rawData = File.ReadAllBytes(filename);
            backupBuffer = new WriteableBitmap(memDecode(rawData));
            buffer = new WriteableBitmap(backupBuffer);

            width = buffer.PixelWidth;
            height = buffer.PixelHeight;
            bpp = (buffer.Format.BitsPerPixel + 7) / 8;
            stride = width * bpp;
            image.Source = buffer;
        }

        // Determines the transcoder to use
        private string getTranscoder(string transcoder)
        {
            switch (transcoder)
            {
                case "vernacular":
                    switch (ext)
                    {
                        case "tif":
                            return "tiff";
                        case "jpeg":
                            return "jpg";
                        case "jpx": case "j2k":
                            return "jp2";
                        default:
                            return ext;
                    }
                case "jpg 2000":
                    return "jp2";
                case "random":
                    return formats[pokeRNG(6)];
                default:
                    return transcoder;
            }
        }

        // Picks which glitching method to utilize
        private void doGlitch(string outFile)
        {
            tempData = memEncode();
            switch (termExt)
            {
                case "bmp":
                    tempData = wordPad(tempData);
                    break;

                case "png":
                    int offset = pokeRNG(tempData.Length);
                    tempData[offset] = (byte)pokeRNG(256);
                    break;

                default:
                    simpleGlitch(tempData);
                    break;
            }
            if (bt_preserve.IsChecked == true)
                File.WriteAllBytes(outFile, tempData);
            buffer = new WriteableBitmap(memDecode(tempData));
            image.Source = buffer;
            GC.Collect();
        }

        // Performs the Wordpad glitch
        private byte[] wordPad(byte[] srcData)
        {
            int x = 0;
            byte[] dstData = new byte[srcData.Length * 2];
            for (int i = 0; i < srcData.Length; i++)
            {
                if (i < 54)
                    dstData[x++] = srcData[i];
                // 0A, 0B, 0D => 0D 0A
                else if (srcData[i] == 0xA || srcData[i] == 0xB || srcData[i] == 0xD)
                {
                    // Skip if already 0D 0A
                    if (srcData[i] == 0xA && srcData[i - 1] == 0xD)
                        continue;
                    dstData[x++] = 0xD;
                    dstData[x++] = 0xA;
                }
                else if (srcData[i] == 0x07)
                    dstData[x++] = 0x20;
                else
                    dstData[x++] = srcData[i];
            }
            Array.Resize(ref dstData, x);
            return dstData;
        }

        // General corruption shenanigans
        private byte[] simpleGlitch(byte[] srcData)
        {
            for (int i = 0x150; i < srcData.Length; i++)
            {
                int search, replace;
                if (bt_replace.IsChecked == true &&
                    Int32.TryParse(tf_from.Text, out search) &&
                    Int32.TryParse(tf_to.Text, out replace))
                {
                    if (srcData[i] == (byte)search)
                        srcData[i] = (byte)replace;
                }
                int chance = (int)sl_chance.Value;
                int conform = 100 - chance;
                if (conform == 0)
                    continue;
                if (i % (chance + 2) == 0)
                    if (pokeRNG(256) % (chance + 2) == 0)
                        srcData[i] = (byte)pokeRNG(256);
            }
            return srcData;
        }

        // Encodes an image to the specified format in memory
        private byte[] memEncode()
        {
            byte[] bytes = null;

            using (MemoryStream stream = new MemoryStream())
            {
                switch (termExt)
                {
                    case "bmp":
                        BmpBitmapEncoder bmpEncoder = new BmpBitmapEncoder();
                        bmpEncoder.Frames.Add(BitmapFrame.Create(backupBuffer));
                        bmpEncoder.Save(stream);
                        break;
                    case "jpg":
                        JpegBitmapEncoder jpgEncoder = new JpegBitmapEncoder();
                        jpgEncoder.Frames.Add(BitmapFrame.Create(backupBuffer));
                        jpgEncoder.Save(stream);
                        break;
                    case "gif":
                        GifBitmapEncoder gifEncoder = new GifBitmapEncoder();
                        gifEncoder.Frames.Add(BitmapFrame.Create(backupBuffer));
                        gifEncoder.Save(stream);
                        break;
                    case "png":
                        PngBitmapEncoder pngEncoder = new PngBitmapEncoder();
                        // pngEncoder.Interlace = PngInterlaceOption.On;
                        pngEncoder.Frames.Add(BitmapFrame.Create(backupBuffer));
                        pngEncoder.Save(stream);
                        break;
                    case "tiff":
                        TiffBitmapEncoder tiffEncoder = new TiffBitmapEncoder();
                        tiffEncoder.Frames.Add(BitmapFrame.Create(backupBuffer));
                        tiffEncoder.Save(stream);
                        break;
                    case "jp2":
                        bytes = new byte[height * stride];
                        backupBuffer.CopyPixels(bytes, stride, 0);
                        byte[] header = Encoding.ASCII.GetBytes("P6\n" + width + " " + height + "\n255\n");
                        stream.Write(header, 0, header.Length);
                        foreach (byte curByte in bytes)
                            stream.WriteByte(curByte);
                        bytes = J2kImage.ToBytes(J2kImage.CreateEncodableSource(stream));
                        return bytes;
                    default:
                        break;
                }
                bytes = stream.ToArray();
            }
            return bytes;
        }

        // Decodes an image in memory to show it on the screen
        private BitmapSource memDecode(byte[] bytes)
        {
            BitmapFrame frame;
            WriteableBitmap output;
            try
            {
                using (MemoryStream stream = new MemoryStream(bytes))
                {
                    switch (termExt)
                    {
                        case "gif":
                            GifBitmapDecoder gifDecoder = new GifBitmapDecoder(stream,
                                BitmapCreateOptions.IgnoreColorProfile |
                                BitmapCreateOptions.PreservePixelFormat,
                                BitmapCacheOption.OnLoad);
                            frame = gifDecoder.Frames.First();
                            break;
                        case "png":
                            PngBitmapDecoder pngDecoder = new PngBitmapDecoder(stream,
                                BitmapCreateOptions.IgnoreColorProfile |
                                BitmapCreateOptions.PreservePixelFormat,
                                BitmapCacheOption.OnLoad);
                            frame = pngDecoder.Frames.First();
                            break;
                        case "tiff":
                            TiffBitmapDecoder tiffDecoder = new TiffBitmapDecoder(stream,
                                BitmapCreateOptions.IgnoreColorProfile |
                                BitmapCreateOptions.PreservePixelFormat,
                                BitmapCacheOption.OnLoad);
                            frame = tiffDecoder.Frames.First();
                            break;
                        case "jpg":
                            JpegBitmapDecoder jpgDecoder = new JpegBitmapDecoder(stream,
                                BitmapCreateOptions.IgnoreColorProfile |
                                BitmapCreateOptions.PreservePixelFormat,
                                BitmapCacheOption.OnLoad);
                            frame = jpgDecoder.Frames.First();
                            break;
                        case "jp2":
                            var j2kImg = J2kImage.FromStream(stream);
                            output = j2kImg.As<WriteableBitmap>();
                            frame = BitmapFrame.Create(output);
                            break;
                        default:
                            BmpBitmapDecoder bmpDecoder = new BmpBitmapDecoder(stream,
                                BitmapCreateOptions.IgnoreColorProfile |
                                BitmapCreateOptions.PreservePixelFormat,
                                BitmapCacheOption.OnLoad);
                            frame = bmpDecoder.Frames.First();
                            break;
                    }
                    if (bpp < 4)
                        frame = BitmapFrame.Create(new FormatConvertedBitmap(frame, PixelFormats.Rgb24, null, 0));
                    else
                        frame = BitmapFrame.Create(new FormatConvertedBitmap(frame, PixelFormats.Rgba64, null, 0));
                    frame.Freeze();
                    return frame;
                }
            }
            catch
            {
                output = new WriteableBitmap(400, 400, 72, 72, PixelFormats.Bgr24, null);
                frame = BitmapFrame.Create(output);
                return frame;
            }
        }
    }
}