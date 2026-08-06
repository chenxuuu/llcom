using System.Collections.Generic;
using Newtonsoft.Json;

namespace llcom.Model
{
    /// <summary>
    /// 快捷发送区相关设置（Settings 分部类）。
    /// 共 10 页（quickSendList），每页可重命名（quickListName0~9），当前页由 quickSendSelect 决定。
    /// </summary>
    partial class Settings
    {
        public List<List<ToSendData>> quickSendList = new List<List<ToSendData>>();
        private int _quickSendSelect = -1;

        /// <summary>
        /// 当前选中的快捷发送列表编号
        /// </summary>
        public int quickSendSelect
        {
            get => _quickSendSelect;
            set { if (SetProperty(ref _quickSendSelect, value)) Save(); }
        }

        /// <summary>
        /// 当前选中的快捷发送列表数据。
        /// 注意：此属性是 quickSendList 的“视图”（getter 返回当前页引用），
        /// 必须 [JsonIgnore]——否则序列化时会与 quickSendList 字段重复写入 JSON，
        /// 反序列化时 Newtonsoft 的 ObjectCreationHandling.Auto 会向同一 List 追加两次，导致数据翻倍。
        /// </summary>
        [JsonIgnore]
        public List<ToSendData> quickSend
        {
            get
            {
                if (_quickSendSelect < 0 || _quickSendSelect > 10)
                    return new List<ToSendData>();
                if (quickSendList.Count <= 10)
                {
                    for (var i = 0; i < 10; i++)
                        quickSendList.Add(new List<ToSendData>());
                }
                return quickSendList[_quickSendSelect];
            }
            set
            {
                if (_quickSendSelect < 0 || _quickSendSelect > 10)
                    return;
                if (quickSendList.Count <= 10)
                {
                    for (var i = 0; i < 10; i++)
                        quickSendList.Add(new List<ToSendData>());
                }
                quickSendList[_quickSendSelect] = value;
                Save();
            }
        }

        private string _quickListName0 = "未命名0";
        public string quickListName0 { get => _quickListName0; set { if (SetProperty(ref _quickListName0, value)) Save(); } }

        private string _quickListName1 = "未命名1";
        public string quickListName1 { get => _quickListName1; set { if (SetProperty(ref _quickListName1, value)) Save(); } }

        private string _quickListName2 = "未命名2";
        public string quickListName2 { get => _quickListName2; set { if (SetProperty(ref _quickListName2, value)) Save(); } }

        private string _quickListName3 = "未命名3";
        public string quickListName3 { get => _quickListName3; set { if (SetProperty(ref _quickListName3, value)) Save(); } }

        private string _quickListName4 = "未命名4";
        public string quickListName4 { get => _quickListName4; set { if (SetProperty(ref _quickListName4, value)) Save(); } }

        private string _quickListName5 = "未命名5";
        public string quickListName5 { get => _quickListName5; set { if (SetProperty(ref _quickListName5, value)) Save(); } }

        private string _quickListName6 = "未命名6";
        public string quickListName6 { get => _quickListName6; set { if (SetProperty(ref _quickListName6, value)) Save(); } }

        private string _quickListName7 = "未命名7";
        public string quickListName7 { get => _quickListName7; set { if (SetProperty(ref _quickListName7, value)) Save(); } }

        private string _quickListName8 = "未命名8";
        public string quickListName8 { get => _quickListName8; set { if (SetProperty(ref _quickListName8, value)) Save(); } }

        private string _quickListName9 = "未命名9";
        public string quickListName9 { get => _quickListName9; set { if (SetProperty(ref _quickListName9, value)) Save(); } }

        public string GetQuickListNameNow()
        {
            return GetQuickListNameByIndex(_quickSendSelect);
        }

        /// <summary>
        /// 获取指定索引页的名称（0-9）
        /// </summary>
        public string GetQuickListNameByIndex(int index)
        {
            return index switch
            {
                0 => quickListName0,
                1 => quickListName1,
                2 => quickListName2,
                3 => quickListName3,
                4 => quickListName4,
                5 => quickListName5,
                6 => quickListName6,
                7 => quickListName7,
                8 => quickListName8,
                9 => quickListName9,
                _ => "??",
            };
        }

        public void SetQuickListNameNow(string name)
        {
            switch (_quickSendSelect)
            {
                case 0: quickListName0 = name; break;
                case 1: quickListName1 = name; break;
                case 2: quickListName2 = name; break;
                case 3: quickListName3 = name; break;
                case 4: quickListName4 = name; break;
                case 5: quickListName5 = name; break;
                case 6: quickListName6 = name; break;
                case 7: quickListName7 = name; break;
                case 8: quickListName8 = name; break;
                case 9: quickListName9 = name; break;
                default: break;
            }
        }
    }
}
