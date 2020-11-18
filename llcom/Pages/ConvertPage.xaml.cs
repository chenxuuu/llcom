using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using c = System.Convert;

namespace llcom.Pages
{
    /// <summary>
    /// ConvertPage.xaml 的交互逻辑
    /// </summary>
    public partial class ConvertPage : Page
    {
        public ConvertPage()
        {
            InitializeComponent();
        }

        bool initial = false;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (initial)
                return;
            foreach (var i in converters)
                ConvertNamesComBox.Items.Add(i.Key);
            initial = true;
        }

        private void DoConvert()
        {
            byte[] row = Encoding.Default.GetBytes(RawTextBox.Text);
            for (int i = 0; i < ConvertJobsListBox.Items.Count; i++)
            {
                string name = ConvertJobsListBox.Items[i] as string;
                if (converters.ContainsKey(name))
                    row = converters[name](row);
            }
            ResultTextBox.Text = Encoding.Default.GetString(row);
        }

        private void RawTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DoConvert();
        }

        private void ConvertClearButton_Click(object sender, RoutedEventArgs e)
        {
            ConvertJobsListBox.Items.RemoveAt(ConvertJobsListBox.Items.Count - 1);
            DoConvert();
        }

        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            if (ConvertNamesComBox.SelectedItem == null)
                return;
            ConvertJobsListBox.Items.Add(ConvertNamesComBox.SelectedItem);
            DoConvert();
        }

        /// <summary>
        /// 转换器
        /// </summary>
        Dictionary<string, Func<byte[], byte[]>> converters = new Dictionary<string, Func<byte[], byte[]>>
        {
            ["String to Hex(with space)"] = (e) => Encoding.Default.GetBytes(BitConverter.ToString(e).Replace("-", " ")),
            ["String to Hex(without space)"] = (e) => Encoding.Default.GetBytes(BitConverter.ToString(e).Replace("-", "")),
            ["Hex to String"] = (e) => Hex2byte(Encoding.Default.GetString(e)),
            ["String to Base64"] = (e) => { try { return Encoding.Default.GetBytes(c.ToBase64String(e)); } catch (Exception ee) { return Encoding.Default.GetBytes(ee.Message); } },
            ["Base64 to String"] = (e) => { try { return c.FromBase64String(Encoding.Default.GetString(e)); } catch (Exception ee) { return Encoding.Default.GetBytes(ee.Message); } },
            ["URL encode"] = (e) => Encoding.Default.GetBytes(System.Web.HttpUtility.UrlEncode(Encoding.Default.GetString(e))),
            ["URL decode"] = (e) => Encoding.Default.GetBytes(System.Web.HttpUtility.UrlDecode(Encoding.Default.GetString(e))),
            ["HTML encode"] = (e) => Encoding.Default.GetBytes(System.Web.HttpUtility.HtmlEncode(Encoding.Default.GetString(e))),
            ["HTML decode"] = (e) => Encoding.Default.GetBytes(System.Web.HttpUtility.HtmlDecode(Encoding.Default.GetString(e))),
            ["String to Unicode"] = (e) => Encoding.Default.GetBytes(String2Unicode(Encoding.Default.GetString(e))),
            ["Unicode to String"] = (e) => Encoding.Default.GetBytes(Unicode2String(Encoding.Default.GetString(e))),
            ["String to MD5 (bytes)"] = (e) => MD5Encrypt(e),
            ["String to SHA-1 (bytes)"] = (e) => Sha1Encrypt(e),
            ["String to SHA-256 (bytes)"] = (e) => Sha256Encrypt(e),
            ["String to SHA-512 (bytes)"] = (e) => Sha512Encrypt(e),
        };

        public static byte[] Hex2byte(string mHex)
        {
            mHex = Regex.Replace(mHex, "[^0-9A-Fa-f]", "");
            if (mHex.Length % 2 != 0)
                mHex = mHex.Remove(mHex.Length - 1, 1);
            if (mHex.Length <= 0) return new byte[] { };
            byte[] vBytes = new byte[mHex.Length / 2];
            for (int i = 0; i < mHex.Length; i += 2)
                if (!byte.TryParse(mHex.Substring(i, 2), NumberStyles.HexNumber, null, out vBytes[i / 2]))
                    vBytes[i / 2] = 0;
            return vBytes;
        }

        public static byte[] MD5Encrypt(byte[] b)
        {
            MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();
            byte[] hashedDataBytes;
            hashedDataBytes = md5Hasher.ComputeHash(b);
            return hashedDataBytes;
        }
        public static byte[] Sha1Encrypt(byte[] b)
        {
            SHA1CryptoServiceProvider md5Hasher = new SHA1CryptoServiceProvider();
            byte[] hashedDataBytes;
            hashedDataBytes = md5Hasher.ComputeHash(b);
            return hashedDataBytes;
        }
        public static byte[] Sha256Encrypt(byte[] b)
        {
            SHA256CryptoServiceProvider md5Hasher = new SHA256CryptoServiceProvider();
            byte[] hashedDataBytes;
            hashedDataBytes = md5Hasher.ComputeHash(b);
            return hashedDataBytes;
        }
        public static byte[] Sha512Encrypt(byte[] b)
        {
            SHA512CryptoServiceProvider md5Hasher = new SHA512CryptoServiceProvider();
            byte[] hashedDataBytes;
            hashedDataBytes = md5Hasher.ComputeHash(b);
            return hashedDataBytes;
        }
        public static string String2Unicode(string source)
        {
            var bytes = Encoding.Unicode.GetBytes(source);
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < bytes.Length; i += 2)
            {
                stringBuilder.AppendFormat("\\u{0}{1}", bytes[i + 1].ToString("x").PadLeft(2, '0'), bytes[i].ToString("x").PadLeft(2, '0'));
            }
            return stringBuilder.ToString();
        }
        public static string Unicode2String(string source)
        {
            return new Regex(@"\\u([0-9a-fA-F]{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled).Replace(source, x => c.ToChar(c.ToUInt16(x.Result("$1"), 16)).ToString());
        }
    }
}
