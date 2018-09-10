namespace IniCompiler
{
    using System.Collections.Generic;

    public sealed class Section
    {
        public Section()
        {
            this.Fields = new List<Field>();
        }

        public List<Field> Fields { get; }
    }
}