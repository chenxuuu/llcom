using LLCOM.Services;

namespace llcomTest;

[TestClass]
public class GlobalSettingTest
{
    [TestMethod]
    public void InitializeTest()
    {
        GlobalSetting.Initialize("test.db");
    }
    
    [TestMethod]
    public async Task UpdateTest()
    {
        GlobalSetting.Initialize("test.db");
        await GlobalSetting.Set("test", "test");
    }
    
    [TestMethod]
    public async Task GetTest()
    {
        GlobalSetting.Initialize("test.db");
        await GlobalSetting.Set("GetTest","test123");
        var value = await GlobalSetting.Get("GetTest","default");
        Assert.AreEqual("test123", value);
    }
    
    [TestMethod]
    public async Task GetDefaultTest()
    {
        GlobalSetting.Initialize("test.db");
        var value = await GlobalSetting.Get("GetDefaultTest","default");
        Assert.AreEqual("default", value);
    }
}