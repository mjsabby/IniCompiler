namespace IniCompiler
{
    public sealed class Field
    {
        public Field(string name, ScalarTypes type, bool isRepeated, object value)
        {
            this.Name = name;
            this.Type = type;
            this.IsRepeated = isRepeated;
            this.Value = value;
        }

        public string Name { get; }

        public ScalarTypes Type { get; }

        public bool IsRepeated { get; }

        public object Value { get; }
    }
}