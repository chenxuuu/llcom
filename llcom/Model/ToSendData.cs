using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llcom.Model
{
    public class ToSendData
    {
        public static event EventHandler DataChanged;
        private int _id;
        private string _text;
        private bool _hex;
        private string _commit;
        public int id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
                DataChanged(0, EventArgs.Empty);
            }
        }
        public string text
        {
            get
            {
                return _text;
            }
            set
            {
                _text = value;
                DataChanged(0, EventArgs.Empty);
            }
        }
        public bool hex
        {
            get
            {
                return _hex;
            }
            set
            {
                _hex = value;
                DataChanged(0, EventArgs.Empty);
            }
        }

        public string commit
        {
            get
            {
                return _commit;
            }
            set
            {
                _commit = value;
                DataChanged(0, EventArgs.Empty);
            }
        }
    }
}
