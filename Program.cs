namespace IniCompiler
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public static class Program
    {
        internal static void Main(string[] args)
        {
            var fooSection = new Section();
            fooSection.Fields.Add(new Field("FieldA", ScalarTypes.Int32, false, 25L));
            fooSection.Fields.Add(new Field("FieldB", ScalarTypes.String, false, "FooBar"));

            var barSection = new Section();

            barSection.Fields.Add(new Field("FieldA", ScalarTypes.Int64, false, 100L));
            barSection.Fields.Add(new Field("FieldB", ScalarTypes.Message, false, "Foo"));
            barSection.Fields.Add(new Field("FieldC", ScalarTypes.Message, true, new List<string> { "Foo", "Baz"} ));

            var bazSection = new Section();

            bazSection.Fields.Add(new Field("FieldA", ScalarTypes.Int64, false, 100L));
            bazSection.Fields.Add(new Field("FieldB", ScalarTypes.Int64, true, new List<long> { 1000, 2000 }));

            var iniFile = new IniFile();
            iniFile.AddSection("Foo", fooSection);
            iniFile.AddSection("Bar", barSection);
            iniFile.AddSection("Baz", bazSection);

            SerializeIniFile(args[0], iniFile);
        }

        public static void SerializeIniFile(string outputBinaryFilePath, IniFile iniFile)
        {
            const long HeaderOffset = 16;

            const long PointerSize = 8;

            var sectionProcessList = new List<Tuple<long, string>>();
            var sectionPositionDict = new Dictionary<string, long>();

            using (var stream = new FileStream(outputBinaryFilePath, FileMode.Create, FileAccess.ReadWrite))
            {
                var sectionCount = (long)iniFile.Sections.Count;

                stream.Write(new ReadOnlySpan<byte>(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x42, 0x49, 0x4e, 0x49 })); // 0000BINI
                SerializeInt64(stream, sectionCount);

                long offset = HeaderOffset + sectionCount * PointerSize * 2;
                stream.Position = offset;

                var arr = new long[sectionCount];

                for (var i = 0; i < sectionCount; ++i)
                {
                    var sectionName = iniFile.SectionNames[i];
                    arr[i] = offset;
                    offset += SerializeStringBulk(stream, sectionName);
                }

                stream.Position = HeaderOffset;

                for (var i = 0; i < sectionCount; ++i)
                {
                    SerializeInt64(stream, arr[i]);
                }

                var sectionPointerOffset = HeaderOffset + sectionCount * PointerSize;

                for (var i = 0; i < sectionCount; ++i)
                {
                    sectionProcessList.Add(new Tuple<long, string>(sectionPointerOffset + 8 * i, iniFile.SectionNames[i]));
                }

                stream.Position = offset;

                for (var i = 0; i < sectionCount; ++i)
                {
                    var sectionName = iniFile.SectionNames[i];
                    sectionPositionDict.Add(sectionName, offset);

                    var section = iniFile.Sections[sectionName].Item1;

                    var fieldCount = section.Fields.Count;
                    offset += fieldCount * 8;

                    SerializeInt64(stream, fieldCount);
                    offset += 8;

                    for (var j = 0; j < fieldCount; ++j)
                    {
                        var field = section.Fields[j];
                        var data = iniFile.GetData(sectionName, field.Name);

                        if (field.IsRepeated)
                        {
                            var comeBackPosition = stream.Position;
                            var beginningOfListPosition = SerializePrimitiveTypeList(stream, field.Type, data, sectionProcessList, ref offset);
                            stream.Position = comeBackPosition;
                            SerializeInt64(stream, beginningOfListPosition);
                        }
                        else
                        {
                            SerializeScalarType(stream, data, field.Type, sectionProcessList, ref offset);
                        }
                    }

                    stream.Position = offset;
                }
            }
        }

        internal static void SerializeScalarType(Stream stream, object data, ScalarTypes fieldType, List<Tuple<long, string>> sectionProcessList, ref long offset)
        {
            switch (fieldType)
            {
                case ScalarTypes.Double:
                case ScalarTypes.Float:
                    SerializeDouble(stream, (double)data);
                    break;
                case ScalarTypes.Bool:
                case ScalarTypes.Int32:
                case ScalarTypes.UInt32:
                case ScalarTypes.Int64:
                    SerializeInt64(stream, (long)data);
                    break;
                case ScalarTypes.UInt64:
                    SerializeUInt64(stream, (ulong)data);
                    break;
                case ScalarTypes.String:
                {
                    var comeBackPosition = stream.Position;
                    var beginningOfStringPosition = SerializeString(stream, (string)data, ref offset);
                    stream.Position = comeBackPosition;
                    SerializeInt64(stream, beginningOfStringPosition);
                    break;
                }
                case ScalarTypes.Message:
                    sectionProcessList.Add(new Tuple<long, string>(stream.Position, (string)data));
                    SerializeInt64(stream, 0xDEADBEEF);
                    break;
            }
        }

        internal static void SerializeUInt64(Stream stream, ulong data)
        {
            unsafe
            {
                stream.Write(new ReadOnlySpan<byte>(&data, sizeof(ulong)));
            }
        }

        internal static void SerializeInt64(Stream stream, long data)
        {
            unsafe
            {
                stream.Write(new ReadOnlySpan<byte>(&data, sizeof(long)));
            }
        }

        internal static void SerializeDouble(Stream stream, double data)
        {
            unsafe
            {
                stream.Write(new ReadOnlySpan<byte>(&data, sizeof(double)));
            }
        }

        internal static long SerializeStringBulk(Stream stream, string data)
        {
            const long PointerSize = 8;
            var totalBytes = data.Length * 2;

            SerializeInt64(stream, data.Length);

            unsafe
            {
                fixed (char* s = data)
                {
                    stream.Write(new ReadOnlySpan<byte>(s, totalBytes));
                }
            }

            return PointerSize + totalBytes;
        }

        internal static long SerializeString(Stream stream, string data, ref long offset)
        {
            long beginningOfStringPosition = offset;
            stream.Position = offset;

            SerializeInt64(stream, data.Length);

            unsafe
            {
                fixed (char* s = data)
                {
                    stream.Write(new ReadOnlySpan<byte>(s, data.Length * 2));
                }
            }

            offset = stream.Position;
            return beginningOfStringPosition;
        }

        internal static long SerializePrimitiveTypeList(Stream stream, ScalarTypes fieldType, object data, List<Tuple<long, string>> sectionProcessList, ref long offset)
        {
            var beginningOfListPosition = offset;
            stream.Position = offset;

            switch (fieldType)
            {
                case ScalarTypes.Bool:
                case ScalarTypes.Int32:
                case ScalarTypes.UInt32:
                case ScalarTypes.Int64:
                { 
                    var list = (List<long>)data;
                    SerializeInt64(stream, list.Count);

                    foreach (var t in list)
                    {
                        SerializeInt64(stream, t);
                    }

                    break;
                }

                case ScalarTypes.UInt64:
                {
                    var list = (List<ulong>)data;
                    SerializeInt64(stream, list.Count);

                    foreach (var t in list)
                    {
                        SerializeUInt64(stream, t);
                    }

                    break;
                }

                case ScalarTypes.Float:
                case ScalarTypes.Double:
                {
                    var list = (List<double>)data;
                    SerializeInt64(stream, list.Count);

                    foreach (var t in list)
                    {
                        SerializeDouble(stream, t);
                    }

                    break;
                }

                case ScalarTypes.String:
                {
                    var list = (List<string>)data;

                    var count = list.Count;

                    SerializeInt64(stream, count);
                    var comeBackPosition = stream.Position;

                    offset = stream.Position + count * 8;

                    var rvaList = new List<long>(count);

                    foreach (var t in list)
                    {
                        rvaList.Add(offset);
                        offset += SerializeStringBulk(stream, t);
                    }

                    stream.Position = comeBackPosition;

                    foreach (var t in rvaList)
                    {
                        SerializeInt64(stream, t);
                    }

                    break;
                }

                case ScalarTypes.Message:
                {
                    var list = (List<string>)data;
                    SerializeInt64(stream, list.Count);

                    foreach (var t in list)
                    {
                        sectionProcessList.Add(new Tuple<long, string>(stream.Position, t));
                        SerializeInt64(stream, 0xDEADBEEF);
                    }

                    break;
                }
            }

            offset = stream.Position;
            return beginningOfListPosition;
        }
    }
}