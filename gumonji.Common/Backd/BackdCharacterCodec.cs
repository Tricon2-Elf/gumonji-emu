using System.Globalization;
using System.Numerics;
using System.Text;
using gumonji.Common.DAL.Entities;

namespace gumonji.Common.Backd;

/// <summary>Codec for the original line-oriented character document. No Unicode conversion of user text.</summary>
public static class BackdCharacterCodec
{
    public const int MaximumPayloadLength = 262144;
    private static readonly string[] Colors =
        ["black", "white", "red", "lisa", "pink", "grape", "blue", "skyblue", "green",
         "muscat", "yellow", "orange", "chocolate", "pearlblue", "cream", "gray", "beige"];
    private static readonly string[] Categories = ["", "animal", "plant", "egg", "seed", "item"];

    private sealed record Field<T>(string Name, Action<T, byte[]> Read, Func<T, byte[]?> Write);
    private static readonly Field<BackdCharacter>[] CharacterFields =
    [
        Number<BackdCharacter, uint>("owner_uid", x => x.OwnerUserId, (x, v) => x.OwnerUserId = v),
        Bytes<BackdCharacter>("nickname", x => x.Nickname, (x, v) => x.Nickname = v, 127, true),
        Bytes<BackdCharacter>("mark_name", x => x.MarkName, (x, v) => x.MarkName = v, 31, true),
        Color<BackdCharacter>("colortype", x => x.ColorType, (x, v) => x.ColorType = v),
        Number<BackdCharacter, int>("eyetype", x => x.EyeType, (x, v) => x.EyeType = v),
        Number<BackdCharacter, int>("subtype", x => x.Subtype, (x, v) => x.Subtype = v),
        Number<BackdCharacter, byte>("favcolor", x => x.FavoriteColor, (x, v) => x.FavoriteColor = v),
        Number<BackdCharacter, int>("editlevel", x => x.EditLevel, (x, v) => x.EditLevel = v),
        Number<BackdCharacter, int>("novice", x => x.Novice, (x, v) => x.Novice = v),
        Number<BackdCharacter, int>("login_count", x => x.LoginCount, (x, v) => x.LoginCount = v),
        Number<BackdCharacter, int>("move_count", x => x.MoveCount, (x, v) => x.MoveCount = v),
        Number<BackdCharacter, long>("consumption", x => x.Consumption, (x, v) => x.Consumption = v),
        Number<BackdCharacter, uint>("clock_count", x => x.ClockCount, (x, v) => x.ClockCount = v),
        Bytes<BackdCharacter>("myportal_server", x => x.PortalServer, (x, v) => x.PortalServer = v, 127, false),
        Number<BackdCharacter, uint>("last_gumolot_time", x => x.LastGumolotTime, (x, v) => x.LastGumolotTime = v),
        Number<BackdCharacter, uint>("last_login_time", x => x.LastLoginTime, (x, v) => x.LastLoginTime = v),
        Number<BackdCharacter, long>("when_last_save_gumo_lltime", x => x.LastSaveGameTime, (x, v) => x.LastSaveGameTime = v),
        Number<BackdCharacter, long>("total_exchanged_money", x => x.TotalExchangedMoney, (x, v) => x.TotalExchangedMoney = v),
        Number<BackdCharacter, long>("total_exchanged_count", x => x.TotalExchangedCount, (x, v) => x.TotalExchangedCount = v),
        Number<BackdCharacter, uint>("login_total_sec", x => x.LoginTotalSeconds, (x, v) => x.LoginTotalSeconds = v),
        Number<BackdCharacter, uint>("create_unixtime", x => x.CreatedUnixTime, (x, v) => x.CreatedUnixTime = v),
    ];
    private static readonly Field<BackdCharacterItem>[] ItemFields =
    [
        Number<BackdCharacterItem, int>("subtype", x => x.Subtype, (x, v) => x.Subtype = v),
        Color<BackdCharacterItem>("colortype", x => x.ColorType, (x, v) => x.ColorType = v),
        Bytes<BackdCharacterItem>("create_id", x => x.CreateId, (x, v) => x.CreateId = v, 47, false),
        Number<BackdCharacterItem, uint>("sticker_by_uidnum", x => x.StickerUserId, (x, v) => x.StickerUserId = v),
        Bytes<BackdCharacterItem>("family_name", x => x.FamilyName, (x, v) => x.FamilyName = v, 63, false, remainder: true),
        Number<BackdCharacterItem, long>("when_created_gumo_lltime", x => x.CreatedGameTime, (x, v) => x.CreatedGameTime = v),
        Number<BackdCharacterItem, uint>("generation", x => x.Generation, (x, v) => x.Generation = v),
        Number<BackdCharacterItem, uint>("fert", x => x.Fertilizer, (x, v) => x.Fertilizer = v),
        Number<BackdCharacterItem, uint>("water", x => x.Water, (x, v) => x.Water = v),
        Number<BackdCharacterItem, uint>("c6h4", x => x.C6H4, (x, v) => x.C6H4 = v),
        Number<BackdCharacterItem, uint>("sio2", x => x.SiO2, (x, v) => x.SiO2 = v),
        Number<BackdCharacterItem, uint>("caco3", x => x.CaCO3, (x, v) => x.CaCO3 = v),
        Number<BackdCharacterItem, int>("price", x => x.Price, (x, v) => x.Price = v),
    ];

    // Nullable fields preserve omitted directives and the original loader's defaulting behavior.
    private static Field<T> Number<T, V>(string name, Func<T, V?> get, Action<T, V> set)
        where V : struct, INumberBase<V> =>
        new(name, (x, value) => set(x, Parse<V>(value)),
            x => get(x) is { } value ? Ascii(value.ToString(null, CultureInfo.InvariantCulture)) : null);

    private static Field<T> Bytes<T>(string name, Func<T, byte[]?> get, Action<T, byte[]> set,
        int maximum, bool escaped, bool remainder = false) =>
        new(name, (x, value) => set(x, Bounded(escaped ? Unescape(value) : value, maximum)),
            x => get(x) is { } value ? WriteBytes(value, maximum, escaped, remainder) : null);

    private static byte[] WriteBytes(byte[] value, int maximum, bool escaped, bool remainder)
    {
        Bounded(value, maximum);
        if ((!remainder && value.AsSpan().Contains((byte)32)) ||
            (!escaped && (value.AsSpan().Contains((byte)0) || value.AsSpan().Contains((byte)10))))
            throw new InvalidDataException("Character string cannot be represented in this directive.");
        return escaped ? Escape(value) : value;
    }

    private static Field<T> Color<T>(string name, Func<T, int?> get, Action<T, int> set) =>
        new(name, (x, value) => set(x, ParseColor(value)),
            x => get(x) is { } value ? Ascii(value >= 0 && value < Colors.Length
                ? Colors[value] : value.ToString(CultureInfo.InvariantCulture)) : null);

    public static BackdCharacter Decode(long userId, ReadOnlySpan<byte> payload)
    {
        if (payload.Length is 0 or > MaximumPayloadLength || payload.Contains((byte)0))
            throw new InvalidDataException("Invalid Backd character document length or embedded NUL.");
        var lines = SplitLines(payload);
        if (lines.Count > 10000 || !Token(lines[0], 0).AsSpan().SequenceEqual("=character"u8))
            throw new InvalidDataException("Backd character document must start with =character.");
        var character = new BackdCharacter { UserId = userId };
        foreach (var line in lines.Skip(1))
        {
            if (line.Length == 0) continue;
            var name = Encoding.Latin1.GetString(Token(line, 0));
            var field = CharacterFields.FirstOrDefault(x => "=" + x.Name == name);
            if (field is not null)
            {
                field.Read(character, Token(line, 1));
                continue;
            }
            switch (name)
            {
                case "=iparam":
                    var index = Index(Token(line, 1), 4);
                    character.Parameters.RemoveAll(x => x.Index == index);
                    character.Parameters.Add(new() { UserId = userId, Index = index, Value = Parse<int>(Token(line, 2)) });
                    break;
                case "=equipflag":
                    var slot = Index(Token(line, 1), 48);
                    character.Equipment.RemoveAll(x => x.Slot == slot);
                    // Original stores a byte but prints it as a signed integer.
                    character.Equipment.Add(new() { UserId = userId, Slot = slot,
                        Value = unchecked((byte)Parse<int>(Token(line, 2))) });
                    break;
                case "=ex":
                    var experienceIndex = Index(Token(line, 1), 300);
                    var categoryName = AsciiText(Token(line, 2));
                    var category = Array.FindIndex(Categories, x => x.Equals(categoryName, StringComparison.OrdinalIgnoreCase));
                    if (category <= 0) throw new InvalidDataException("Unknown experience category.");
                    character.Experiences.RemoveAll(x => x.Index == experienceIndex);
                    character.Experiences.Add(new() { UserId = userId, Index = experienceIndex,
                        Category = (byte)category, Type = Parse<byte>(Token(line, 3)),
                        Subtype = Parse<byte>(Token(line, 4)), Color = Parse<byte>(Token(line, 5)) });
                    break;
                case "=item":
                    ReadItem(character, line);
                    break;
                default:
                    // Preserve unknown directives and noncanonical comments individually, not as a document BLOB.
                    if (name is not "#" || !IsCanonicalComment(line))
                        character.Extensions.Add(new() { UserId = userId, Index = character.Extensions.Count, Line = line });
                    break;
            }
        }
        return character;
    }

    private static void ReadItem(BackdCharacter character, byte[] line)
    {
        var slot = Index(Token(line, 1), 48);
        var name = Encoding.Latin1.GetString(Token(line, 2));
        var item = character.Items.SingleOrDefault(x => x.Slot == slot);
        if (name == "type")
        {
            var type = AsciiText(Token(line, 3));
            if (type.Length == 0) throw new InvalidDataException("Missing item type.");
            if (item is not null) character.Items.Remove(item);
            character.Items.Add(new() { UserId = character.UserId, Slot = slot, TypeName = type });
            return;
        }
        if (item is null) throw new InvalidDataException("Item type must precede its attributes.");
        var field = ItemFields.FirstOrDefault(x => x.Name == name);
        if (field is not null)
        {
            field.Read(item, name == "family_name" ? Remainder(line, 3) : Token(line, 3));
            return;
        }
        if (name is "iparam" or "secret_iparam")
        {
            var index = Index(Token(line, 3), 8);
            var secret = name == "secret_iparam";
            item.Parameters.RemoveAll(x => x.Index == index && x.Secret == secret);
            item.Parameters.Add(new() { UserId = character.UserId, Slot = slot, Index = index,
                Secret = secret, Value = Parse<int>(Token(line, 4)) });
        }
        else if (name == "comment")
        {
            var index = Index(Token(line, 3), 8);
            item.Comments.RemoveAll(x => x.Index == index);
            item.Comments.Add(new() { UserId = character.UserId, Slot = slot, Index = index,
                Text = Bounded(Unescape(Remainder(line, 4)), 63) });
        }
        else character.Extensions.Add(new() { UserId = character.UserId,
            Index = character.Extensions.Count, Line = line });
    }

    public static byte[] Encode(BackdCharacter character)
    {
        using var output = new MemoryStream();
        void Line(string prefix, byte[] value)
        {
            output.Write(Ascii(prefix));
            output.Write(value);
            output.WriteByte(10);
            if (output.Length > MaximumPayloadLength) throw new InvalidDataException("Backd character document is too large.");
        }
        Line("=character info file", []);
        foreach (var field in CharacterFields)
            if (field.Write(character) is { } value) Line("=" + field.Name + " ", value);
        foreach (var parameter in character.Parameters.OrderBy(x => x.Index))
        {
            CheckIndex(parameter.Index, 4);
            Line($"=iparam {parameter.Index} ", Decimal(parameter.Value));
        }
        Line("# here follows item info", []);
        foreach (var item in character.Items.OrderBy(x => x.Slot))
        {
            CheckIndex(item.Slot, 48);
            var typeName = Ascii(item.TypeName);
            if (typeName.Length == 0 || typeName.Any(b => b <= 32))
                throw new InvalidDataException("Item type must be a nonempty protocol identifier.");
            Line($"=item {item.Slot} type ", typeName);
            // The template resolver needs type, subtype and color in this order.
            foreach (var field in ItemFields.Take(2))
                if (field.Write(item) is { } value) Line($"=item {item.Slot} {field.Name} ", value);
            foreach (var parameter in item.Parameters.OrderBy(x => x.Secret).ThenBy(x => x.Index))
            {
                CheckIndex(parameter.Index, 8);
                Line($"=item {item.Slot} {(parameter.Secret ? "secret_iparam" : "iparam")} {parameter.Index} ",
                    Decimal(parameter.Value));
            }
            foreach (var comment in item.Comments.OrderBy(x => x.Index))
            {
                CheckIndex(comment.Index, 8);
                Line($"=item {item.Slot} comment {comment.Index} ", Escape(Bounded(comment.Text, 63)));
            }
            foreach (var field in ItemFields.Skip(2))
                if (field.Write(item) is { } value) Line($"=item {item.Slot} {field.Name} ", value);
        }
        foreach (var equipment in character.Equipment.OrderBy(x => x.Slot))
        {
            CheckIndex(equipment.Slot, 48);
            Line($"=equipflag {equipment.Slot} ", Decimal(unchecked((sbyte)equipment.Value)));
        }
        Line("# follows experience\n", []);
        foreach (var experience in character.Experiences.OrderBy(x => x.Index))
        {
            CheckIndex(experience.Index, 300);
            if (experience.Category is 0 or > 5) throw new InvalidDataException("Invalid experience category.");
            Line($"=ex {experience.Index} {Categories[experience.Category]} ",
                Ascii($"{experience.Type} {experience.Subtype} {experience.Color}"));
        }
        foreach (var extension in character.Extensions.OrderBy(x => x.Index))
        {
            if (extension.Line.AsSpan().Contains((byte)10) || extension.Line.AsSpan().Contains((byte)0))
                throw new InvalidDataException("Extension must be a single non-NUL line.");
            Line("", extension.Line);
        }
        // Original writer does not append LF or NUL after the final comment.
        output.Write("# end of character info file"u8);
        if (output.Length > MaximumPayloadLength) throw new InvalidDataException("Backd character document is too large.");
        return output.ToArray();
    }

    private static List<byte[]> SplitLines(ReadOnlySpan<byte> source)
    {
        var lines = new List<byte[]>();
        while (true)
        {
            var lf = source.IndexOf((byte)10);
            if (lf < 0) { lines.Add(source.ToArray()); return lines; }
            lines.Add(source[..lf].ToArray());
            source = source[(lf + 1)..];
        }
    }
    private static byte[] Remainder(byte[] line, int token)
    {
        var start = 0;
        for (var i = 0; i < token; i++)
        {
            var space = line.AsSpan(start).IndexOf((byte)32);
            if (space < 0) throw new InvalidDataException("Missing character directive token.");
            start += space + 1;
        }
        return line[start..];
    }
    private static byte[] Token(byte[] line, int token)
    {
        var rest = Remainder(line, token);
        var space = rest.AsSpan().IndexOf((byte)32);
        return space < 0 ? rest : rest[..space];
    }
    private static bool IsCanonicalComment(byte[] line) =>
        line.AsSpan().SequenceEqual("# here follows item info"u8) ||
        line.AsSpan().SequenceEqual("# follows experience"u8) ||
        line.AsSpan().SequenceEqual("# end of character info file"u8);

    private static V Parse<V>(byte[] value) where V : struct, INumberBase<V> =>
        V.TryParse(AsciiText(value), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result : throw new InvalidDataException("Invalid character integer.");
    private static int Index(byte[] value, int count) { var index = Parse<int>(value); CheckIndex(index, count); return index; }
    private static void CheckIndex(int value, int count)
    {
        if (value < 0 || value >= count) throw new InvalidDataException("Character array index is out of range.");
    }
    private static int ParseColor(byte[] value)
    {
        var name = AsciiText(value);
        var index = Array.FindIndex(Colors, x => x.Equals(name, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index : Parse<int>(value);
    }
    private static byte[] Bounded(byte[] value, int maximum) => value.Length <= maximum
        ? value : throw new InvalidDataException("Character string exceeds its original byte limit.");
    private static byte[] Decimal<T>(T value) where T : IFormattable => Ascii(value.ToString(null, CultureInfo.InvariantCulture));
    private static byte[] Ascii(string value)
    {
        if (value.Any(x => x > 127)) throw new InvalidDataException("Protocol identifier must be ASCII.");
        return Encoding.ASCII.GetBytes(value);
    }
    private static string AsciiText(byte[] value)
    {
        if (value.Any(x => x > 127)) throw new InvalidDataException("Protocol identifier must be ASCII.");
        return Encoding.ASCII.GetString(value);
    }

    private static byte[] Escape(byte[] value)
    {
        using var output = new MemoryStream();
        foreach (var b in value)
        {
            var suffix = b switch { 0 => '0', 10 => 'n', 13 => 'r', 26 => 'Z',
                34 => '"', 39 => '\'', 92 => '\\', _ => '\0' };
            if (suffix != '\0') { output.WriteByte(92); output.WriteByte((byte)suffix); }
            else output.WriteByte(b);
        }
        return output.ToArray();
    }
    private static byte[] Unescape(byte[] value)
    {
        using var output = new MemoryStream();
        for (var i = 0; i < value.Length; i++)
        {
            var b = value[i];
            if (b == 92)
            {
                if (++i == value.Length) throw new InvalidDataException("Truncated character escape.");
                b = value[i] switch { (byte)'0' => 0, (byte)'n' => 10, (byte)'r' => 13,
                    (byte)'Z' => 26, 34 => 34, 39 => 39, 92 => 92,
                    _ => throw new InvalidDataException("Unknown character escape.") };
            }
            output.WriteByte(b);
        }
        return output.ToArray();
    }
}
