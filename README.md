# IniCompiler
Compiles an INI file into binary data

# Fundamentals

1. Concept of sections. Each section is given a strong type. Each section can contain the following:

* 3 Main Wire Types: `64-bit integer`, `64-bit floating point value`, `64-bit pointer`
* `Primitives`: `bool`, `int8`, `uint8`, `int16`, `uint16`, `int32`, `uint32`, `int64`, `uint64` -> `uint64`
* `single`, `double` -> `double`
* `string` -> `8-byte pointer` to length-prefixed utf16 data
* `object` -> `8-byte pointer` to another section (described by name)
* `map<K, V>` -> sorted `list<K>` followed by `list<V>` where `K` must be either a `primitive type` or a `string`. `V` must be a `primitive type`, `string`, or `object`, and cannot be a `list<>`.
* `set<K>` -> sorted `list<K>`
* `list<Primitives>`
* `list<string>`
* `list<object>`
* NOT Permitted: `list<list<object>`, but `list<someObject>` where `someObject` is `list<object>` permitted.

# Overall Layout

1. 8-bytes of Magic Number: 0x00, 0x00, 0x00, 0x00, 0x42, 0x49, 0x4E, 0x49
2. Number Of Sections (8-bytes)
3. Pointers to Section Names (Number Of Sections * 8-bytes) [NOTE: Parallel array with 4.]
4. Pointers to Section Offsets (Number Of Sections * 8-bytes) [NOTE: Parallel array with 3.]
4. Section Data

## Section

1. Number Of Fields
2. Field Data (Number of Fields * 8-bytes) [NOTE: Data can be inline in the case of `primitive types`, or pointers in the case of `object`, `string`, `list<>`]