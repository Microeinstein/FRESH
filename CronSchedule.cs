using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace FRESH {
    public class CronSchedule : ICloneable {
        public const string
            S_ANY        = "*",
            S_LIST       = ",",
            S_RANGE      = "-",
            S_JUMP       = "/",
            EVERY_MINUTE = "* * * * *";
        public const char
            C_ANY   = '*',
            C_LIST  = ',',
            C_RANGE = '-',
            C_JUMP  = '/',
            C_PARTS = ' ';
        static readonly char[] splitPARTS = new char[] {C_PARTS};
        public static readonly Regex
            rgxValidNumeric = new Regex(@"[0-9*,/-]+"),
            rgxValidNames = new Regex(@"[0-9a-z*,/-]+"),
            rgxMaybeValid = new Regex(
                $@"^{rgxValidNumeric}(?: {rgxValidNumeric})?(?: {rgxValidNumeric})?(?: {rgxValidNames})?(?: {rgxValidNames})?$",
                RegexOptions.IgnoreCase
            );
        public event EventHandler Changed;
        const int TPARTS   = 5,
                  WEEKDAYS = 7;
        static readonly int[] limits = new int[] { 0,59,  0,23,  1,31,  1,12,  0,WEEKDAYS };

        public string Expression { get; protected set; } = EVERY_MINUTE;
        public bool DOW_AsFilter { get; private set; } = false;
        public readonly ReadOnlyCollection<int> Minutes, Hours, DaysOfMonth, Months, DaysOfWeek;
        readonly List<int>[] times = new List<int>[TPARTS];

        public CronSchedule(string exp = null) {
            for (int i = 0; i < times.Length; i++)
                times[i] = new List<int>();
            Minutes     = times[0].AsReadOnly();
            Hours       = times[1].AsReadOnly();
            DaysOfMonth = times[2].AsReadOnly();
            Months      = times[3].AsReadOnly();
            DaysOfWeek  = times[4].AsReadOnly();
            Change(exp ?? Expression);
        }
        public CronSchedule(CronSchedule c) {
            for (int i = 0; i < times.Length; i++)
                times[i] = new List<int>(c.times[i]);
            Expression = c.Expression;
            Minutes     = times[0].AsReadOnly();
            Hours       = times[1].AsReadOnly();
            DaysOfMonth = times[2].AsReadOnly();
            Months      = times[3].AsReadOnly();
            DaysOfWeek  = times[4].AsReadOnly();
        }
        public object Clone()
            => new CronSchedule(this);
        public static bool IsValid(string expression)
            => tryGenerate(expression, null, out _);
        public bool IsTime(DateTime instant) {
            var simple = Minutes.Contains(instant.Minute)
                      && Hours.Contains(instant.Hour)
                      && Months.Contains(instant.Month);
            if (!simple)
                return false;
            bool d1() => DaysOfMonth.Contains(instant.Day);
            bool d2() => DaysOfWeek.Contains((int)instant.DayOfWeek);
            return DOW_AsFilter ? (d1() && d2()) : (d1() || d2());
        }

        /// <summary>
        /// Warning: infinite sequence, also this may not find the next time under special circumstances (combinations of month/week days).
        /// <para>Use [System.Linq.<see cref="System.Linq.Enumerable"/>] methods to take a specific part of it.</para>
        /// </summary>
        /// <param name="start">The starting point of the sequence.</param>
        /// <returns></returns>
        public IEnumerable<DateTime> NextTimes(DateTime? start = null) {
            start = start ?? DateTime.Now;
            int[,] time = new int[2,4];
            var monthDays = new List<int>();
            int year = start.Value.Year;
            bool cOver, cUnch;

            int month() => time[1,3];
            void initPart(int t, int n, out bool overflow, out bool unchanged, List<int> custom = null) {
                overflow = false;
                custom = custom ?? times[t];
                int i = custom.FindIndex(v => v >= n);
                if (i < 0) {
                    i = 0;
                    overflow = true;
                }
                time[0,t] = i;
                time[1,t] = custom[i];
                unchanged = time[1,t] == n;
            }
            void initDays() {
                monthDays.Clear();
                var m = month();
                monthDays.AddRange(Enumerable.Range(1, DateTime.DaysInMonth(year, m)).Where(
                    d => {
                        bool d1() => DaysOfMonth.Contains(d);
                        bool d2() => DaysOfWeek.Contains((int)(new DateTime(year, m, d).DayOfWeek));
                        return DOW_AsFilter ? (d1() && d2()) : (d1() || d2());
                    }
                ));
            }
            DateTime build()
                => new DateTime(year, time[1,3], time[1,2], time[1,1], time[1,0], 0);
            bool incrPart(int t, List<int> custom = null) {
                int i = ++time[0,t];
                bool c;
                custom = custom ?? times[t];
                if (c = (i >= custom.Count))
                    i = time[0,t] = 0;
                time[1,t] = custom[i];
                return c;
            }
            void incr(int t) {
                bool ovr = false;
                if (t <= 3)
                    ovr = incrPart(t, t == 2 ? monthDays : null);
                else
                    year++;
                if (ovr)
                    incr(t + 1);
                if (t == 3) {
                    initDays();
                    time[0,2] = -1;
                    incrPart(2, monthDays);
                }
            }
            void init(int t) {
                if (t < 0)
                    return;
                int v = 0;
                switch (t) {
                    case 0: v = start.Value.Minute; break;
                    case 1: v = start.Value.Hour; break;
                    case 2: v = start.Value.Day; break;
                    case 3: v = start.Value.Month; break;
                }
                initPart(t, v, out cOver, out cUnch, (t == 2 ? monthDays : null));
                if (cOver)
                    incr(t + 1);
                if (t == 3)
                    initDays();
                if (cOver || !cUnch) {
                    if (t > 0) initPart(0, 0, out _, out _);
                    if (t > 1) initPart(1, 0, out _, out _);
                    if (t > 2) initPart(2, 1, out _, out _, monthDays);
                    return;
                }
                init(t - 1);
            }

            init(3);
            while (true) {
                yield return build();
                incr(0);
            }
        }

        static bool tryGenerate(string exp, List<int>[] times, out bool dow_as_filter) {
            dow_as_filter = false;

            if (!rgxMaybeValid.IsMatch(exp))
                return false;

            void add(int ti, int v)
                => ((List<int>)times?.GetValue(ti))?.Add(ti == TPARTS - 1 ? (v % WEEKDAYS) : v);
            IEnumerable<int> genRange(int n, int m, int mod) {
                if (n > m) //non-standard
                    m += mod;
                return Enumerable.Range(n, m - n + 1).Select(v => v % mod);
            }

            string[] parts, list, jumped, ranged;
            List<string> months, days;
            parts = exp.Split(splitPARTS, StringSplitOptions.RemoveEmptyEntries);

            bool jump = false,
                 range = false;
            int i, j, fd, a, b, s, min, max, len = Math.Min(parts.Length, TPARTS);
            months = len <= 3
                ? null
                : DateTimeFormatInfo.CurrentInfo.AbbreviatedMonthNames.Select(aa => aa.ToLower()).ToList();
            days = len <= 4
                ? null
                : DateTimeFormatInfo.CurrentInfo.AbbreviatedDayNames.Select(aa => aa.ToLower()).ToList();
            fd = (int)DateTimeFormatInfo.CurrentInfo.FirstDayOfWeek;
            
            bool toInt(int ti, string v, out int ret) {
                ret = -1;
                if (v == S_ANY)
                    return true;
                switch (ti) {
                    case 3: ret = months.IndexOf(v.ToLower()); break;
                    case 4: ret = days.IndexOf(v.ToLower()); break;
                }
                if (ret < 0)
                    return int.TryParse(v, out ret);
                else {
                    //if (ti == 4) //v is a name
                    //    ret = (ret - fd) % (max + 1);
                    return true;
                }
            }

            dow_as_filter = parts.Length == 5;
            for (i = 0; i < TPARTS; i++) {
                min = limits[i * 2];
                max = i == 3 ? DateTime.DaysInMonth(4, 2) : limits[(i * 2) + 1];

                //parts[i] not defined, so is considered "*"
                if (i >= parts.Length) {
                    foreach (var n in genRange(min, max, max + 1))
                        add(i, n);
                    continue;
                }

                list = parts[i].Split(C_LIST);
                for (j = 0; j < list.Length; j++) {
                    if (list[j].Length == 0) //1-4,,6
                        return false;
                }

                for (j = 0; j < list.Length; j++) {
                    jumped = list[j].Split(C_JUMP);
                    jump = jumped.Length > 1;
                    s = jump ? int.Parse(jumped[1]) : 1; // <-- jump
                    if (s < 1) //error
                        s = 1;
                        //return false;
                    ranged = jumped[0].Split(C_RANGE);
                    range = ranged.Length > 1;
                    if (!toInt(i, ranged[0], out a)) // <-- a
                        return false;
                    if (range) {
                        if (!toInt(i, ranged[1], out b)) // <-- b
                            return false;
                    } else
                        b = jump ? max : a;
                    if (i == 2) //0 0 * * mon-fri
                        dow_as_filter &= a == -1;
                    if (a == -1) {
                        a = min;
                        b = max;
                    } else if (a < min || a > max || b < min || b > max)
                        return false;
                    //a = Math.Min(Math.Max(min, a), max);
                    //b = Math.Min(Math.Max(min, b), max);
                    /* jump = defined ? jump : 1
                     * a = initial
                     * b = defined ? b : (jump ? max : a)
                     * > a+jump*0, a+jump*1, a+jump*2, ..., <= b
                     */
                    foreach (var n in genRange(a, b, max + 1).Where((_, p) => (p - s) % s == 0))
                        add(i, n);
                }
            }

            for (i = 0; i < times.Length; i++) {
                var cpy = times[i].Distinct().OrderBy(n => n).ToArray();
                times[i].Clear();
                times[i].AddRange(cpy);
            }
            return true;
        }
        public bool TryChange(string exp) {
            bool b, dowaf;
            clean();
            if (b = tryGenerate(exp, times, out dowaf)) {
                Expression = exp;
                DOW_AsFilter = dowaf;
                Changed?.Invoke(this, null);
            } else {
                clean();
                tryGenerate(Expression, times, out dowaf);
                DOW_AsFilter = dowaf;
            }
            return b;
        } 
        public void Change(string exp) {
            bool dowaf;
            clean();
            if (!tryGenerate(exp, times, out dowaf)) {
                clean();
                tryGenerate(Expression, times, out dowaf);
                DOW_AsFilter = dowaf;
                throw new FormatException();
            }
            Expression = exp;
            DOW_AsFilter = dowaf;
            Changed?.Invoke(this, null);
        }
        void clean() {
            foreach (var set in times)
                set.Clear();
        }

        public override string ToString()
            => Expression;

        public static implicit operator string(CronSchedule a)
            => a.Expression;
    }
}
