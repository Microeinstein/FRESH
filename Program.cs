#define CRON_DEBUG
#undef CRON_DEBUG
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.ServiceModel.Syndication;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using Micro.WinForms;
using Microsoft.Win32;

namespace FRESH {
    //Program, Main, new Worker, load, Settings.Load,

    static class Program {
        public const string
            YES = "Yes",
            NO = "No",
            NAME = nameof(FRESH),
            REG_KEY_STARTUP = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
            REG_ENTRY_NAME  = NAME,
            ARG_STARTUP     = "toggleStartup",
            SYSTEM_SND_FEED = @"%systemroot%\Media\Windows Notify.wav";
        const int
            WM_SETREDRAW = 11,
            WM_USER = 0x0400,
            TTM_SETTITLEW = WM_USER + 33;

        internal static readonly ListEditorIcons icons = new ListEditorIcons() {
            Add        = Properties.Resources.plus_circle,
            Remove     = Properties.Resources.cross_circle,
            Duplicate  = Properties.Resources.document_copy,
            MoveUp     = Properties.Resources.arrow_090,
            MoveDown   = Properties.Resources.arrow_270,
            MoveTop    = Properties.Resources.arrow_stop_090,
            MoveBottom = Properties.Resources.arrow_stop_270,
            Export     = Properties.Resources.application_export,
            Import     = Properties.Resources.application_import,
        };
        internal static readonly Random rand = new Random();
        internal static Worker worker;
        internal static readonly HttpClient http = new HttpClient();
        internal static readonly PropertyInfo
            ToolTipHandle = typeof(ToolTip).GetProperty("Handle", BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.NonPublic);
        internal static readonly FieldInfo
            NotifyIconWindow = typeof(NotifyIcon).GetField("window", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly TimeSpan LocalUtcOffset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
        public static DateTime FastNow
            => DateTime.UtcNow + LocalUtcOffset;
        public static readonly string
            AppDir   = AppDomain.CurrentDomain.BaseDirectory,
            ExecPath = Application.ExecutablePath,
            CacheDir = Path.Combine(AppDir, "cache");
        public static readonly bool
            Is64Bit    = Environment.Is64BitOperatingSystem,
            IsWoW      = Is64Bit && !Environment.Is64BitProcess,
            IsElevated = WindowsIdentity.GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid);
        public static bool RUNNING = false;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int wMsg, bool wParam, int lParam);
        /*[DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, string lParam);*/

        public static void SuspendDrawing(this Control parent) {
            SendMessage(parent.Handle, WM_SETREDRAW, false, 0);
        }
        public static void ResumeDrawing(this Control parent) {
            SendMessage(parent.Handle, WM_SETREDRAW, true, 0);
            parent.Refresh();
        }
        public static Bitmap ResizeImage(Image image, int width, int height) {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            //destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage)) {
                graphics.CompositingMode    = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode  = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode      = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode    = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes()) {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
        /*public static void SetTitle(NotifyIcon ni, Image image, string title) {
            //var handle = (IntPtr)ToolTipHandle.GetValue(tt);
            var handle = ((NativeWindow)NotifyIconWindow.GetValue(ni)).Handle;
            using (var min = new MemoryStream())
            using (var mout = new MemoryStream()) {
                Icon icon = null;
                if (image != null) {
                    image?.Save(min, ImageFormat.Png);
                    ImagingHelper.ConvertToIcon(min, mout);
                    icon = new Icon(mout);
                }
                SendMessage(handle, TTM_SETTITLEW, icon?.Handle ?? IntPtr.Zero, title.Length <= 100 ? title : title.Substring(0, 100));
                icon?.Dispose();
            }
        }*/
        public static string BoolToString(this ref bool b, bool forUI)
            => forUI ? (b ? YES : NO) : (b ? "1" : "0");
        public static bool StringToBool(this string s, bool fromUI)
            => fromUI ? s == YES : s == "1";
        public static string EscapeCommand(string cmd) {
            var sb = new StringBuilder(cmd);
            for (int i = 0; i < sb.Length; i++) {
                char c = sb[i];
                switch (c) {
                    case '^':
                    case '"':
                        sb.Insert(i, c);
                        i++;
                        break;
                    case '\\':
                    case '&':
                    case '|':
                    case '>':
                    case '<':
                        sb.Insert(i, '^');
                        i++;
                        break;
                }
            }
            return sb.ToString();
            /*return cmd
                .Replace("^", "^^")
                .Replace("\"", "\"\"")
                .Replace("\\", "^\\")
                .Replace("&", "^&")
                .Replace("|", "^|")
                .Replace(">", "^>")
                .Replace("<", "^<");*/
        }
        public static bool GetStartup(bool machineLevel, bool checkValue = false) {
            var regRunKey = (machineLevel ? Registry.LocalMachine : Registry.CurrentUser).OpenSubKey(REG_KEY_STARTUP, false);
            var regValue = regRunKey.GetValue(REG_ENTRY_NAME, null);
            return regValue == null ? false : (checkValue ? (string)regValue == ExecPath : true);
        }
        public static void SetStartup(bool machineLevel, bool value) {
            bool writable = !machineLevel || IsElevated;
            if (writable) {
                var regRunKey = (machineLevel ? Registry.LocalMachine : Registry.CurrentUser).OpenSubKey(REG_KEY_STARTUP, writable);
                if (value)
                    regRunKey.SetValue(REG_ENTRY_NAME, ExecPath, RegistryValueKind.String);
                else
                    regRunKey.DeleteValue(REG_ENTRY_NAME, false);
                regRunKey.Close();
            } else
                Process.Start(new ProcessStartInfo(ExecPath, ARG_STARTUP) { Verb = "runas" });
        }
        public static void ToggleStartup(bool machineLevel = false)
            => SetStartup(machineLevel, !GetStartup(machineLevel));
        public static Exception HTTPGet(string url, out HttpResponseMessage res) {
            res = null;
            try {
                var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.ConnectionClose = true;
                res = http.SendAsync(req, HttpCompletionOption.ResponseContentRead).Result;
                return null;
            } catch (Exception ex) {
                return ex;
            }
        }
        public static HttpResponseMessage Download(string url) {
            var ex = HTTPGet(url, out var res);
            if (ex != null)
                throw ex;
            if (!res.IsSuccessStatusCode)
                throw new HttpRequestException($"Request not successful ({(int)res.StatusCode})");
            return res;
        }
        public static string DownloadString(string url)
            => Download(url).Content.ReadAsStringAsync().Result;
        public static Stream DownloadData(string url)
            => Download(url).Content.ReadAsStreamAsync().Result;
        public static void ReadFeed(string url, out string xml, out SyndicationFeed feed) {
            xml = DownloadString(url);
            ReadFeed(xml, out feed);
        }
        public static void ReadFeed(string xml, out SyndicationFeed feed) {
            using (var str = new StringReader(xml))
            using (var reader = XmlReader.Create(str)) {
                feed = SyndicationFeed.Load(reader);
                reader.Close();
            }
        }
        public static void StreamPouring(this Stream readFrom, Stream writeTo, int bufferSize) {
            //Forgot Stream.CopyTo()...
            int len;
            byte[] buffer = new byte[bufferSize];
            while ((len = readFrom.Read(buffer, 0, bufferSize)) > 0)
                writeTo.Write(buffer, 0, len);
        }
        public static string RandomHex(int bytes) {
            var b = new byte[bytes];
            rand.NextBytes(b);
            return string.Concat(b.Select(n => $"{n:x2}"));
        }
        public static string[] CommandToExeArgs(string cmd, bool parseEscape = true) {
            var ret = new string[2];
            bool escape = false,
                 quotes = false;
            var sb = new StringBuilder();
            foreach (var c in cmd) {
                if (escape) {
                    sb.Append(c);
                    escape = false;
                    continue;
                }
                if (parseEscape && c == '\\') {
                    escape = true;
                    continue;
                }
                if (c == '"') {
                    sb.Append(c);
                    quotes = !quotes;
                    if (!quotes)
                        break;
                    continue;
                }
                if (!quotes && c == ' ')
                    break;
                sb.Append(c);
            }
            ret[0] = sb.ToString();
            ret[1] = cmd.Substring(sb.Length, cmd.Length - sb.Length).Trim();
            return ret;
        }

        [STAThread]
        static void Main() {
            RUNNING = true;

#if CRON_DEBUG
            var START_DATE = new DateTime(2019, 5, 1);
            //var c = new CronSchedule("* 99 * * *"); //error
            //var c = new CronSchedule("0 12 5 2,4 1-2"); //every (5,monday-thusday)
            //var c = new CronSchedule("0 0 1 6 lun");
            var c = new CronSchedule("0 0 * 1 ven-lun");
            var a = c.NextTimes(START_DATE).Take(20).ToArray();
            //int i = 0;
            foreach (var d in a) {
                Console.WriteLine($"\"{d}\",");
                //if (++i % 4 == 0)
                //    Console.WriteLine();
            }
#else
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && args[1] == ARG_STARTUP) {
                ToggleStartup();
                return;
            }
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new FormSettings());
            Application.Run(worker = new Worker());
            DisposeStatic();
#endif
        }

        static void DisposeStatic() {
            Worker.sysSndFeed?.Dispose();
        }
    }
}
