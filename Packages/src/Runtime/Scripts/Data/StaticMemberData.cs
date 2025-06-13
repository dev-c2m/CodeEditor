using System;

namespace C2M.CodeEditor
{
    [Serializable]
    public class StaticMemberData
    {
        public string MainKeyword;
        public string SubKeyword;

        public string GetMainKeyword()
        {
            return MainKeyword.Trim();
        }

        public string GetSubKeyword()
        {
            return SubKeyword.Trim();
        }
    }
}
