using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llcomTest
{
    [TestClass]
    public class Lua
    {
        [TestMethod]
        public void Basic()
        {
            using var env = new NLua.Lua();
            env.DoString("print('hello world!')");
        }
    }
}
