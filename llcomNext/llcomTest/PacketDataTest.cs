using System.Text;
using LLCOM.Models;

namespace llcomTest;

[TestClass]
public class PacketDataTest
{
    [TestMethod]
    public void GenerateHexStringTest()
    {
        byte[] data = [0x01, 0x02, 0x03, 0x04];
        var s = PacketData.GenerateHexString(data);
        Assert.AreEqual("01 02 03 04 ", s);
    }
    
    [TestMethod]
    public void GenerateEncodedStringTest()
    {
        byte[] data = "0123"u8.ToArray();
        var s = PacketData.GenerateString(data,Encoding.UTF8);
        Assert.AreEqual("0123", s);
    }
    
    [TestMethod]
    public void GenerateEncodedStringReadableTest()
    {
        byte[] data = "0123\n"u8.ToArray();
        var s = PacketData.GenerateString(data,Encoding.UTF8);
        Assert.AreEqual("0123\u240a\n", s);
    }
    
    [TestMethod]
    public void GenerateEncodedStringReadableTest2()
    {
        byte[] data = "0123\r\n"u8.ToArray();
        var s = PacketData.GenerateString(data,Encoding.UTF8);
        Assert.AreEqual("0123\u240d\u240a\r\n", s);
    }
    
    [TestMethod]
    public void GenerateEncodedStringChineseTest()
    {
        byte[] data = "你好"u8.ToArray();
        var s = PacketData.GenerateString(data,Encoding.UTF8);
        Assert.AreEqual("你好", s);
    }
    
    [TestMethod]
    public void GenerateEncodedStringChineseReadableTest()
    {
        byte[] data = "你\r\n好"u8.ToArray();
        var s = PacketData.GenerateString(data,Encoding.UTF8);
        Assert.AreEqual("你\u240d\u240a\r\n好", s);
    }
    
    [TestMethod]
    public void GenerateEncodedStringChineseGb2312Test()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        byte[] data = [0xC4, 0xE3, 0xBA, 0xC3];
        var s = PacketData.GenerateString(data,Encoding.GetEncoding("GB2312"));
        Assert.AreEqual("你好", s);
    }
    
    [TestMethod]
    public void GenerateEncodedStringChineseGb2312Test2()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        byte[] data = [0xC4, 0xE3, 0x0a, 0xBA, 0xC3];
        var s = PacketData.GenerateString(data,Encoding.GetEncoding("GB2312"));
        Assert.AreEqual("你\n好", s);
    }
    
    [TestMethod]
    public void GenerateEncodedStringReadableCutTest()
    {
        byte[] data = "0123\n\u0000abcd"u8.ToArray();
        var s = PacketData.GenerateString(data,Encoding.UTF8);
        Assert.AreEqual("0123\u240a\n\u2400abcd", s);
    }
    
    [TestMethod]
    public void GenerateEncodedStringCutTest()
    {
        byte[] data = "0123\n\u0000abcd"u8.ToArray();
        var s = PacketData.GenerateString(data,Encoding.UTF8, false);
        Assert.AreEqual("0123\nabcd", s);
    }
}