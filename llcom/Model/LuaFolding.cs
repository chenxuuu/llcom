using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llcom.Model
{
    //从这里抄的：
    //https://github.com/gmlwns2000/Symphony/blob/master/Symphony/UI/Settings/Sound/LuaFoldingStragey.cs
    //没写开源协议，不过也没关系吧反正这个项目也是开源的
    public class LuaFolding
	{
        public void UpdateFoldings(FoldingManager manager, TextDocument document)
        {
            int firstErrorOffset;
            IEnumerable<NewFolding> newFoldings = CreateNewFoldings(document, out firstErrorOffset);
            manager.UpdateFoldings(newFoldings, firstErrorOffset);
        }

        /// <summary>
        /// Create <see cref="NewFolding"/>s for the specified document.
        /// </summary>
        public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
        {
            firstErrorOffset = -1;
            return CreateNewFoldings(document);
        }

        /// <summary>
        /// Create <see cref="NewFolding"/>s for the specified document.
        /// </summary>
        public IEnumerable<NewFolding> CreateNewFoldings(ITextSource document)
        {
            List<NewFolding> newFoldings = new List<NewFolding>();

            Stack<int> startOffsets = new Stack<int>();

            List<string> Openings = new List<string>();
            Openings.Add("function");
            Openings.Add("if");
            Openings.Add("while");
            Openings.Add("for");
            Openings.Add("do");

            List<string> Endings = new List<string>();
            Endings.Add("end");

            string block = "";
            int lastNewLine = -1;
            bool comment = false;
            bool longComment = false;

            for (int i = 0; i < document.TextLength; i++)
            {
                char c = document.GetCharAt(i);

                if (c == '\n' || c == '\r')
                {
                    comment = false;
                }
                else
                {
                    if (c != '\t' && c != '+' && c != '-' && c != ';' && c != ' ' && c != ',')
                    {
                        block += c;
                    }

                    if (document.TextLength - i >= 2 && document.GetText(i, 2) == "--")
                    {
                        comment = true;
                    }
                    if (document.TextLength - i >= 4)
                    {
                        string str = document.GetText(i, 4);

                        if (str == "--[[")
                        {
                            longComment = true;
                        }
                        else if (str == "]]--")
                        {
                            longComment = false;
                        }
                    }
                }

                if ((c == '\n' || c == '=' || c == ',' || c == ' ' || i == document.TextLength - 1) && !comment && !longComment)
                {
                    foreach (string op in Openings)
                    {
                        if (block == op)
                        {
                            startOffsets.Push(i - op.Length);
                        }
                    }

                    foreach (string end in Endings)
                    {
                        if (block == end)
                        {
                            if (startOffsets.Count > 0)
                            {
                                int startOffset = startOffsets.Pop();

                                if (startOffset < lastNewLine)
                                {
                                    if (i == document.TextLength - 1)
                                    {
                                        newFoldings.Add(new NewFolding(startOffset, i + 1));
                                    }
                                    else
                                    {
                                        if (c == '\n')
                                        {
                                            newFoldings.Add(new NewFolding(startOffset, i - 1));
                                        }
                                        else
                                        {
                                            newFoldings.Add(new NewFolding(startOffset, i));
                                        }
                                    }
                                }
                            }
                        }
                    }
                    block = "";

                    if (c == '\n' || c == '\r')
                    {
                        lastNewLine = i;
                    }
                }
            }

            newFoldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
            return newFoldings;
        }
    }
}
