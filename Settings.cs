using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Micro.WinForms;

namespace FRESH {
    public static class Settings {
        public static readonly ConfigTable configFeeds = new ConfigTable(@"feeds.cfg");
        public static readonly List<Feed> feeds = new List<Feed>();
        public static readonly Options options = new Options(@"options.cfg");

        static void Load<T>(ConfigTable table, List<T> list) where T : IExposable, new() {
            table.Load();
            list.Clear();
            list.AddRange(table.data.Select(a => {
                var s = new T();
                for (int i = 0; i < s.RealValuesCount && i < a.Length; i++)
                    s.ParseAndSet(a[i], i, false);
                return s;
            }));
        }
        static void Save<T>(ConfigTable table, List<T> list, IEnumerable<T> newValues = null) where T : IExposable, new() {
            var raw = table.data;
            if (newValues != null) {
                list.Clear();
                list.AddRange(newValues);
            }
            raw.Clear();
            foreach (var entry in list)
                raw.Add(entry.TextValues(false).Select(o => o?.ToString() ?? "").ToArray());
            table.Save();
        }
        public static void LoadFeeds() {
            Load(configFeeds, feeds);
            feeds.ForEach(f => f.LoadCache());
        }
        public static void SaveFeeds(IEnumerable<Feed> newValues = null) {
            Save(configFeeds, feeds, newValues);
            Directory.CreateDirectory(Program.CacheDir);
            feeds.ForEach(f => f.SaveCache());
        }
    }


    public class ConfigTable : IEnumerable<string[]> {
        public const char BACKSLASH = '\\';
        //public static readonly Regex rgxLine = new Regex(@"\s+");
        public readonly string path;
        public readonly List<string[]> data;

        public ConfigTable(string name) {
            path = Path.Combine(Program.AppDir, name);
            data = new List<string[]>();
        }
        public void Load() {
            var f = File.Open(path, FileMode.OpenOrCreate, FileAccess.Read);
            var r = new StreamReader(f);
            data.Clear();
            string l;
            while ((l = r.ReadLine()) != null)
                data.Add(l.Split('\t').Select(p => Unescape(p)).ToArray());
            //data.Add(rgxLine.Split(l).Select(p => Unescape(p)).ToArray());
            r.Close();
            f.Dispose();
        }
        public void Save() {
            var f = File.Open(path, FileMode.Create, FileAccess.Write);
            var w = new StreamWriter(f);
            foreach (var l in data)
                w.WriteLine(string.Join("\t", l.Select(p => Escape(p)).ToArray()));
            w.Flush();
            w.Close();
            f.Dispose();
        }
        public static IEnumerable<char> Escape(IEnumerable<char> input) {
            foreach (var c in input) {
                switch (c) {
                    case BACKSLASH: yield return BACKSLASH; yield return BACKSLASH; break;
                    case '/': yield return BACKSLASH; yield return c; break;
                    case ' ': yield return BACKSLASH; yield return 's'; break;
                    case '|': yield return BACKSLASH; yield return 'p'; break;
                    case '\a': yield return BACKSLASH; yield return 'a'; break;
                    case '\b': yield return BACKSLASH; yield return 'b'; break;
                    case '\f': yield return BACKSLASH; yield return 'f'; break;
                    case '\n': yield return BACKSLASH; yield return 'n'; break;
                    case '\r': yield return BACKSLASH; yield return 'r'; break;
                    case '\t': yield return BACKSLASH; yield return 't'; break;
                    case '\v': yield return BACKSLASH; yield return 'v'; break;
                    default: yield return c; break;
                }
            }
        }
        public static IEnumerable<char> Unescape(IEnumerable<char> input) {
            var e = input.GetEnumerator();
            while (e.MoveNext()) {
                char c1 = e.Current;
                if (c1 == BACKSLASH) {
                    if (!e.MoveNext())
                        throw new FormatException();
                    char c2 = e.Current;
                    switch (c2) {
                        case BACKSLASH: yield return BACKSLASH; break;
                        case '/': yield return c2; break;
                        case 's': yield return ' '; break;
                        case 'p': yield return '|'; break;
                        case 'a': yield return '\a'; break;
                        case 'b': yield return '\b'; break;
                        case 'f': yield return '\f'; break;
                        case 'n': yield return '\n'; break;
                        case 'r': yield return '\r'; break;
                        case 't': yield return '\t'; break;
                        case 'v': yield return '\v'; break;
                        default: throw new FormatException();
                    }
                    continue;
                }
                yield return c1;
            }
        }
        public static string Escape(string txt)
            => new string(Escape((IEnumerable<char>)txt).ToArray());
        public static string Unescape(string txt)
            => new string(Unescape((IEnumerable<char>)txt).ToArray());

        public IEnumerator<string[]> GetEnumerator()
            => data.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
            => data.GetEnumerator();
    }
    

    public class Options {
        const string SECTION = "General";
        public readonly ValueWrapper
            defaultSoundEffectPath,
            maxElementsPerFeed,
            disableNotification,
            disableSound,
            lastFeedCheck;
        public readonly INIFile file;
        readonly ValueWrapper[] bindings;

        public Options(string name) {
            file = new INIFile(Path.Combine(Program.AppDir, name));
            bindings = new[] {
                defaultSoundEffectPath = new ValueWrapper(TypeCode.String,   nameof(defaultSoundEffectPath), ""),
                maxElementsPerFeed     = new ValueWrapper(TypeCode.Int32,    nameof(maxElementsPerFeed),     0),
                disableNotification    = new ValueWrapper(TypeCode.Boolean,  nameof(disableNotification),    false),
                disableSound           = new ValueWrapper(TypeCode.Boolean,  nameof(disableSound),           false),
                lastFeedCheck          = new ValueWrapper(TypeCode.DateTime, nameof(lastFeedCheck),          Program.FastNow),
            };
        }
        public void Load() {
            foreach (var b in bindings)
                b.value = file.Read(SECTION, b.name, TypeCode.String);
        }
        public void Save() {
            foreach (var b in bindings)
                file.Write(SECTION, b.name, b.value);
        }
    }

    public class ValueWrapper {
        public readonly TypeCode type;
        public readonly string name;
        public readonly object @default;
        public object value {
            get => _v == null ? @default : _v;
            set => _v = value == null ? @default : Convert.ChangeType(value, type);
        }
        object _v;

        public ValueWrapper(TypeCode type, string name, object @default) {
            this.type = type;
            this.name = name;
            this.@default = _v = @default;
        }
        public T cast<T>()
            => (T)value;
    }
}
