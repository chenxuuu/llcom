using System.Text;
using LLCOM.Models;

namespace llcomTest;

[TestClass]
public class PacketDataTest
{
    [TestMethod]
    public void GenerateHexStringTest()
    {
        var packetData = new PacketData
        {
            Data = [0x01, 0x02, 0x03, 0x04]
        };
        packetData.GenerateHexString();
        Assert.AreEqual("01 02 03 04 ", packetData.String);
    }
    
    [TestMethod]
    public void GenerateEncodedStringTest()
    {
        var packetData = new PacketData
        {
            Data = "0123"u8.ToArray()
        };
        packetData.GenerateString(Encoding.UTF8);
        Assert.AreEqual("0123", packetData.String);
    }
    
    [TestMethod]
    public void GenerateEncodedStringReadableTest()
    {
        var packetData = new PacketData
        {
            Data = "0123\n"u8.ToArray()
        };
        packetData.GenerateString(Encoding.UTF8);
        Assert.AreEqual("0123\u240a\n", packetData.String);
    }
    
    [TestMethod]
    public void GenerateEncodedStringReadableTest2()
    {
        var packetData = new PacketData
        {
            Data = "0123\r\n"u8.ToArray()
        };
        packetData.GenerateString(Encoding.UTF8);
        Assert.AreEqual("0123\u240d\u240a\r\n", packetData.String);
    }
    
    [TestMethod]
    public void GenerateEncodedStringChineseTest()
    {
        var packetData = new PacketData
        {
            Data = "你好"u8.ToArray()
        };
        packetData.GenerateString(Encoding.UTF8);
        Assert.AreEqual("你好", packetData.String);
    }
    
    [TestMethod]
    public void GenerateEncodedStringChineseReadableTest()
    {
        var packetData = new PacketData
        {
            Data = "你\r\n好"u8.ToArray()
        };
        packetData.GenerateString(Encoding.UTF8);
        Assert.AreEqual("你\u240d\u240a\r\n好", packetData.String);
    }
    
    [TestMethod]
    public void GenerateEncodedStringChineseGb2312Test()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var packetData = new PacketData
        {
            Data = [0xC4, 0xE3, 0xBA, 0xC3]
        };
        packetData.GenerateString(Encoding.GetEncoding("GB2312"));
        Assert.AreEqual("你好", packetData.String);
    }
    
    [TestMethod]
    public void GenerateEncodedStringChineseGb2312Test2()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var packetData = new PacketData
        {
            Data = [0xC4, 0xE3, 0x0a, 0xBA, 0xC3]
        };
        packetData.GenerateString(Encoding.GetEncoding("GB2312"));
        Assert.AreEqual("你\n好", packetData.String);
    }
    
    [TestMethod]
    public void GenerateEncodedStringReadableCutTest()
    {
        var packetData = new PacketData
        {
            Data = "0123\n\u0000abcd"u8.ToArray()
        };
        packetData.GenerateString(Encoding.UTF8);
        Assert.AreEqual("0123\u240a\n\u2400abcd", packetData.String);
    }
    
    [TestMethod]
    public void GenerateEncodedStringCutTest()
    {
        var packetData = new PacketData
        {
            Data = "0123\n\u0000abcd"u8.ToArray()
        };
        packetData.GenerateString(Encoding.UTF8, false);
        Assert.AreEqual("0123\nabcd", packetData.String);
    }
}