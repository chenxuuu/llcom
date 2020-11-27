using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llcom.Tools
{
    class InputDialog
    {
        public static Tuple<bool, string> OpenDialog(string prompt, string defaultInput = "", string title = null)
        {
            InputDialogWindow dialog = new InputDialogWindow(prompt, defaultInput, title);
            bool ret = dialog.ShowDialog() ?? false;
            return Tuple.Create<bool, string>(ret, dialog.Value);
        }
    }
}
