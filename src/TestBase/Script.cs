namespace TestBase
{
    public class Script
    {
        public ScriptType Type { get; set; }
        public List<string>? Values { get; set; }

        public Script() { }

        public string Compact()
        {
            if (Values == null)
                return "";

            switch (this.Type)
            {
                case ScriptType.Inline:
                    if (Values is not null)
                        return string.Join(";", Values);
                    else
                        throw new InvalidOperationException();
                case ScriptType.File:
                    List<string> list = new List<string>();
                    foreach (var item in Values)
                    {
                        if (File.Exists(item))
                        {
                            using var sr = new StreamReader(item);
                            string data = sr.ReadToEnd();
                            list.Add(data);
                        }
                        else
                            throw new InvalidOperationException();
                    }
                    return string.Join(";", list);
                default:
                    throw new InvalidOperationException();
            }
        }
    }

    public enum ScriptType
    {
        Inline,
        File
    }
}
