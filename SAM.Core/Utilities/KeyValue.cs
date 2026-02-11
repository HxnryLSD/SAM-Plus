/* Copyright (c) 2024-2026 Rick (rick 'at' gibbed 'dot' us)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

namespace SAM.Core.Utilities;

using SAM.Core.Services;

/// <summary>
/// KeyValue parser for Steam binary format files.
/// </summary>
internal class KeyValue
{
    private static readonly KeyValue _Invalid = new();
    public string Name = "<root>";
    public KeyValueType Type = KeyValueType.None;
    public object? Value;
    public bool Valid;
    public List<KeyValue>? Children = null;

    public KeyValue this[string key]
    {
        get
        {
            if (Children == null)
            {
                return _Invalid;
            }

            var child = Children.SingleOrDefault(
                c => string.Compare(c.Name, key, StringComparison.InvariantCultureIgnoreCase) == 0);

            return child ?? _Invalid;
        }
    }

    public string AsString(string defaultValue)
    {
        if (Valid == false)
        {
            return defaultValue;
        }

        if (Value == null)
        {
            return defaultValue;
        }

        return Value.ToString() ?? defaultValue;
    }

    public int AsInteger(int defaultValue)
    {
        if (Valid == false)
        {
            return defaultValue;
        }

        return Type switch
        {
            KeyValueType.String or KeyValueType.WideString => int.TryParse((string?)Value, out int value) ? value : defaultValue,
            KeyValueType.Int32 => (int)(Value ?? defaultValue),
            KeyValueType.Float32 => (int)((float)(Value ?? (float)defaultValue)),
            KeyValueType.UInt64 => (int)((ulong)(Value ?? (ulong)defaultValue) & 0xFFFFFFFF),
            _ => defaultValue
        };
    }

    public float AsFloat(float defaultValue)
    {
        if (Valid == false)
        {
            return defaultValue;
        }

        return Type switch
        {
            KeyValueType.String or KeyValueType.WideString => float.TryParse((string?)Value, out float value) ? value : defaultValue,
            KeyValueType.Int32 => (int)(Value ?? (int)defaultValue),
            KeyValueType.Float32 => (float)(Value ?? defaultValue),
            KeyValueType.UInt64 => (ulong)(Value ?? (ulong)defaultValue) & 0xFFFFFFFF,
            _ => defaultValue
        };
    }

    public bool AsBoolean(bool defaultValue)
    {
        if (Valid == false)
        {
            return defaultValue;
        }

        return Type switch
        {
            KeyValueType.String or KeyValueType.WideString => int.TryParse((string?)Value, out int value) ? value != 0 : defaultValue,
            KeyValueType.Int32 => ((int)(Value ?? 0)) != 0,
            KeyValueType.Float32 => ((int)((float)(Value ?? 0f))) != 0,
            KeyValueType.UInt64 => ((ulong)(Value ?? 0UL)) != 0,
            _ => defaultValue
        };
    }

    public override string ToString()
    {
        if (Valid == false)
        {
            return "<invalid>";
        }

        if (Type == KeyValueType.None)
        {
            return Name;
        }

        return $"{Name} = {Value}";
    }

    public static KeyValue? LoadAsBinary(string path)
    {
        if (File.Exists(path) == false)
        {
            return null;
        }

        try
        {
            using var input = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            KeyValue kv = new();
            return kv.ReadAsBinary(input) == false ? null : kv;
        }
        catch (Exception ex)
        {
            Log.Debug($"Failed to load KeyValue from {path}: {ex.Message}");
            return null;
        }
    }

    public bool ReadAsBinary(Stream input)
    {
        Children = [];
        try
        {
            while (true)
            {
                var type = (KeyValueType)input.ReadValueU8();

                if (type == KeyValueType.End)
                {
                    break;
                }

                KeyValue current = new()
                {
                    Type = type,
                    Name = input.ReadStringUnicode(),
                };

                switch (type)
                {
                    case KeyValueType.None:
                        current.ReadAsBinary(input);
                        break;

                    case KeyValueType.String:
                        current.Valid = true;
                        current.Value = input.ReadStringUnicode();
                        break;

                    case KeyValueType.WideString:
                        throw new FormatException("wstring is unsupported");

                    case KeyValueType.Int32:
                        current.Valid = true;
                        current.Value = input.ReadValueS32();
                        break;

                    case KeyValueType.UInt64:
                        current.Valid = true;
                        current.Value = input.ReadValueU64();
                        break;

                    case KeyValueType.Float32:
                        current.Valid = true;
                        current.Value = input.ReadValueF32();
                        break;

                    case KeyValueType.Color:
                        current.Valid = true;
                        current.Value = input.ReadValueU32();
                        break;

                    case KeyValueType.Pointer:
                        current.Valid = true;
                        current.Value = input.ReadValueU32();
                        break;

                    default:
                        throw new FormatException();
                }

                if (input.Position >= input.Length)
                {
                    throw new FormatException();
                }

                Children.Add(current);
            }

            Valid = true;
            return input.Position == input.Length;
        }
        catch (Exception ex)
        {
            Log.Debug($"Failed to parse KeyValue binary: {ex.Message}");
            return false;
        }
    }
}