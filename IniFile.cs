namespace IniCompiler
{
    using System;
    using System.Collections.Generic;

    public sealed class IniFile
    {
        private readonly List<string> sectionNames;

        private readonly Dictionary<string, Tuple<Section, int>> sections;

        public IniFile()
        {
            this.sectionNames = new List<string>();
            this.sections = new Dictionary<string, Tuple<Section, int>>();
        }

        public void AddSection(string sectionName, Section section)
        {
            this.sections.Add(sectionName, new Tuple<Section, int>(section, this.sectionNames.Count));
            this.sectionNames.Add(sectionName);
            this.sectionNames.Sort();
        }

        public object GetData(string sectionName, string fieldName)
        {
            var fields = this.sections[sectionName].Item1.Fields;
            foreach (var field in fields)
            {
                if (string.Equals(field.Name, fieldName))
                {
                    return field.Value;
                }
            }

            throw new Exception("Not found");
        }

        public IReadOnlyList<string> SectionNames => this.sectionNames;

        public IReadOnlyDictionary<string, Tuple<Section, int>> Sections => this.sections;
    }
}