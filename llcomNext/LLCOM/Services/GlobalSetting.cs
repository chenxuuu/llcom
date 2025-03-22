using System;
using System.IO;
using System.Threading.Tasks;
using SQLite;

namespace LLCOM.Services;

public class GlobalSetting
{
    public class Setting
    {
        [PrimaryKey]
        public string Key { get; set; }
        public string Value { get; set; }
    }
    
    private static SQLiteAsyncConnection? _db = null;
    
    /// <summary>
    /// 初始化全局设置的数据库
    /// </summary>
    public static void Initialize(string dbFileName = "setting.db")
    {
        if(_db is not null)
            return;
        // Initialize the global setting
        // 指定路径的sqlite数据库
        var dbPath = Path.Combine(Utils.AppPath, dbFileName);
        //检查数据库是否存在
        if (!File.Exists(dbPath))
        {
            //创建数据库
            using var dbNew = new SQLiteConnection(dbPath);
            //创建表
            dbNew.CreateTable<Setting>();
        }
        //创建数据库连接
        _db = new SQLiteAsyncConnection(dbPath);
    }
    
    private static async Task _update(string key, string value)
    {
        if (_db is null)
            throw new Exception("GlobalSetting not initialized");
        await _db.InsertOrReplaceAsync(new Setting
        {
            Key = key,
            Value = value
        });
    }
    
    private static async Task<string?> _get(string key)
    {
        if (_db is null)
            throw new Exception("GlobalSetting not initialized");
        var setting = await _db.Table<Setting>().Where(s => s.Key == key).FirstOrDefaultAsync();
        return setting?.Value;
    }
    
    public static async Task Set<T>(string key, T value) => await _update(key, value!.ToString()!);
    
    public static async Task<string> Get(string key, string defaultValue = "") => await _get(key) ?? defaultValue;
    public static async Task<int> Get(string key, int defaultValue = 0) => int.TryParse(await _get(key), out var result) ? result : defaultValue;
    public static async Task<bool> Get(string key, bool defaultValue = false) => bool.TryParse(await _get(key), out var result) ? result : defaultValue;
    public static async Task<double> Get(string key, double defaultValue = 0) => double.TryParse(await _get(key), out var result) ? result : defaultValue;
    public static async Task<long> Get(string key, long defaultValue = 0) => long.TryParse(await _get(key), out var result) ? result : defaultValue;
    
}