using System.ComponentModel;

namespace llcom.Model;

public class ToSendData : INotifyPropertyChanged
{
    public static event EventHandler? DataChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    private int _id;
    private string _text = "";
    private bool _hex;
    private string _commit = "";
    private string _recvScriptPath = "";
    private string _recvScriptPara = "";

    public int id
    {
        get => _id;
        set => _id = value;
    }

    public string text
    {
        get => _text;
        set
        {
            _text = value;
            DataChanged?.Invoke(0, EventArgs.Empty);
        }
    }

    public bool hex
    {
        get => _hex;
        set
        {
            _hex = value;
            DataChanged?.Invoke(0, EventArgs.Empty);
        }
    }

    public string commit
    {
        get => _commit;
        set
        {
            _commit = value;
            DataChanged?.Invoke(0, EventArgs.Empty);
        }
    }

    public string recvScriptPath
    {
        get => _recvScriptPath;
        set
        {
            _recvScriptPath = value;
            DataChanged?.Invoke(0, EventArgs.Empty);
            OnPropertyChanged(nameof(recvScriptPath));
        }
    }

    public string recvScriptPara
    {
        get => _recvScriptPara;
        set
        {
            _recvScriptPara = value;
            DataChanged?.Invoke(0, EventArgs.Empty);
            OnPropertyChanged(nameof(recvScriptPara));
        }
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
