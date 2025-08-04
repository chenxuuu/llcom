using System.Text;
using LLCOM.Models;
using LLCOM.Services;
using StringHelper = LLCOM.Services.StringHelper;

namespace llcomTest;

[TestClass]
public class StringHelperTest
{
    [TestMethod]
    public void GenerateHexStringTest()
    {
        byte[] data = [0x01, 0x02, 0x03, 0x04];
        var s = StringHelper.GenerateHexString(data);
        Assert.AreEqual("01 02 03 04 ", s);
    }
    
    [TestMethod]
    public void GenerateEncodedStringTest()
    {
        byte[] data = "0123"u8.ToArray();
        var s = StringHelper.GenerateString(data,Encoding.UTF8);
        Assert.AreEqual("0123", s);
    }
    
    [TestMethod]
    public void GenerateEncodedStringReadableTest()
    {
        byte[] data = "0123\n"u8.ToArray();
        var s = StringHelper.GenerateString(data,Encoding.UTF8);
        Assert.AreEqual("0123\u240a\n", s);
    }
    
    [TestMethod]
    public void GenerateEncodedStringReadableTest2()
    {
        byte[] data = "0123\r\n"u8.ToArray();
        var s = StringHelper.GenerateString(data,Encoding.UTF8);
        Assert.AreEqual("0123\u240d\u240a\r\n", s);
    }
    
    [TestMethod]
    public void GenerateEncodedStringChineseTest()
    {
        byte[] data = "你好"u8.ToArray();
        var s = StringHelper.GenerateString(data,Encoding.UTF8);
        Assert.AreEqual("你好", s);
    }
    
    [TestMethod]
    public void GenerateEncodedStringChineseReadableTest()
    {
        byte[] data = "你\r\n好"u8.ToArray();
        var s = StringHelper.GenerateString(data,Encoding.UTF8);
        Assert.AreEqual("你\u240d\u240a\r\n好", s);
    }
    
    [TestMethod]
    public void GenerateEncodedStringChineseGb2312Test()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        byte[] data = [0xC4, 0xE3, 0xBA, 0xC3];
        var s = StringHelper.GenerateString(data,Encoding.GetEncoding("GB2312"));
        Assert.AreEqual("你好", s);
    }
    
    [TestMethod]
    public void GenerateEncodedStringChineseGb2312Test2()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        byte[] data = [0xC4, 0xE3, 0x0a, 0xBA, 0xC3];
        var s = StringHelper.GenerateString(data,Encoding.GetEncoding("GB2312"));
        Assert.AreEqual("你\n好", s);
    }
    
    [TestMethod]
    public void GenerateEncodedStringReadableCutTest()
    {
        byte[] data = "0123\n\u0000abcd"u8.ToArray();
        var s = StringHelper.GenerateString(data,Encoding.UTF8);
        Assert.AreEqual("0123\u240a\n\u2400abcd", s);
    }
    
    [TestMethod]
    public void GenerateEncodedStringCutTest()
    {
        byte[] data = "0123\n\u0000abcd"u8.ToArray();
        var s = StringHelper.GenerateString(data,Encoding.UTF8, false);
        Assert.AreEqual("0123\nabcd", s);
    }

    [TestMethod]
    public void CheckUtf8LengthFull()
    {
        var data = "你好"u8.ToArray();
        var validLength = StringHelper.GetCompleteUtf8Length(data);
        Assert.AreEqual(6, validLength);
    }
    [TestMethod]
    public void CheckUtf8LengthCut()
    {
        var data = "你好"u8.ToArray().Take(5).ToArray();
        var validLength = StringHelper.GetCompleteUtf8Length(data);
        Assert.AreEqual(3, validLength);
    }
    [TestMethod]
    public void CheckUtf8LengthCutAdd()
    {
        var data = "你好"u8.ToArray().Take(5).ToList();//5
        data.AddRange("你好"u8.ToArray());//6
        var validLength = StringHelper.GetCompleteUtf8Length(data.ToArray());
        Assert.AreEqual(11, validLength);
        
        data.AddRange("你好"u8.ToArray().Take(5).ToList());//3
        validLength = StringHelper.GetCompleteUtf8Length(data.ToArray());
        Assert.AreEqual(14, validLength);
    }
}