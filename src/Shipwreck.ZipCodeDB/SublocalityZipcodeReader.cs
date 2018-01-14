using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Shipwreck.ZipCodeDB
{
    public class SublocalityZipcodeReader : LocalityZipCodeReader
    {
        private sealed class SublocalityEntry
        {
            private static readonly Regex _PointyKana = new Regex(@"\<(.*)ｦﾉｿﾞｸ\>$");
            private static readonly Regex _Pointy = new Regex(@"「(.*)を除く」$");

            private static readonly Regex _Number = new Regex(@"([０-９]+)～([０-９]+)");
            private static readonly Regex _NumberKana = new Regex(@"([0-9]+)\-([0-9]+)");

            public SublocalityEntry(string name, string kana)
            {
                Name = name;
                Kana = kana;

                var pcm = _Pointy.Match(name);
                var pkm = _PointyKana.Match(kana);

                if (pcm.Success && pkm.Success)
                {
                    ExceptFor = pcm.Groups[1].Value;
                    ExceptForKana = pkm.Groups[1].Value;
                    Name = name.Substring(0, pcm.Index);
                    Kana = kana.Substring(0, pkm.Index);
                }
            }

            public SublocalityEntry(string name, string kan, string exceptFor, string exceptForKana)
            {
                Name = name;
                Kana = Kana;
                ExceptFor = exceptFor;
                ExceptForKana = exceptForKana;
            }

            public string Name { get; }
            public string Kana { get; }

            public string ExceptFor { get; }
            public string ExceptForKana { get; }

            public IEnumerable<SublocalityEntry> Populate()
            {
                var ncm = _Number.Match(Name);
                var nkm = _NumberKana.Match(Kana);

                if (ncm.Success && nkm.Success)
                {
                    var c1 = ncm.Groups[1].Value.Aggregate(0, (v, c) => v * 10 + c - '０');
                    var c2 = ncm.Groups[2].Value.Aggregate(0, (v, c) => v * 10 + c - '０');
                    var k1 = ncm.Groups[1].Value.Aggregate(0, (v, c) => v * 10 + c - '0');
                    var k2 = ncm.Groups[2].Value.Aggregate(0, (v, c) => v * 10 + c - '0');
                    if (c1 == k1 && c2 == k2 && c1 < c2)
                    {
                        var pc = Name.Substring(0, ncm.Index);
                        var pk = Kana.Substring(0, nkm.Index);
                        var sc = Name.Substring(ncm.Index + ncm.Length);
                        var sk = Kana.Substring(nkm.Index + nkm.Length);

                        for (var i = c1; i <= c2; i++)
                        {
                            yield return new SublocalityEntry
                                (
                                    $"{pc}{new string(i.ToString("D").Select(c => (char)(c + '０' - '0')).ToArray())}{sc}",
                                    $"{pk}{i}{sk}",
                                    ExceptFor,
                                    ExceptForKana);
                        }

                        yield break;
                    }
                }
                yield return this;
            }
        }

        private static readonly Regex _ParenKana = new Regex(@"\((.*)\)$");
        private static readonly Regex _Paren = new Regex(@"（(.*)）$");
        private static readonly Regex _ExceptForKana = new Regex(@"\ｦﾉｿﾞｸ)$");
        private static readonly Regex _ExceptFor = new Regex(@"を除く）$");

        private static readonly Regex _SplitKana = new Regex(@"(?<=[,]+(\<[^>]+\>)?),");
        private static readonly Regex _Split = new Regex("(?<=[、]+(「[^」]+」)?)、");

        public SublocalityZipcodeReader(TextReader reader, bool leaveOpen = false)
            : base(reader, leaveOpen: leaveOpen)
        { }

        public string ExceptForKana { get; protected set; }

        public string ExceptFor { get; protected set; }

        private int _SublocalityIndex;
        private List<SublocalityEntry> _Sublocalities;

        public string Sublocality { get; protected set; }
        public string SublocalityKana { get; protected set; }

        public override bool MoveNext()
        {
            if (MoveNextSublocality())
            {
                return true;
            }

            if (!base.MoveNext())
            {
                return false;
            }

            ExceptFor = null;
            ExceptForKana = null;

            if (!string.IsNullOrEmpty(Locality)
                && !string.IsNullOrEmpty(LocalityKana))
            {
                var cm = _Paren.Match(Locality);
                var km = _ParenKana.Match(LocalityKana);

                if (cm.Success && km.Success)
                {
                    var cem = _ExceptFor.Match(Locality);
                    var kem = _ExceptForKana.Match(LocalityKana);

                    if (cem.Success && kem.Success)
                    {
                        ExceptFor = Locality.Substring(cm.Index + 1, Locality.Length - 5 - cm.Index);
                        ExceptForKana = LocalityKana.Substring(cem.Index + 1, LocalityKana.Length - 5 - cem.Index);

                        Locality = Locality.Substring(0, cm.Index);
                        LocalityKana = LocalityKana.Substring(0, cem.Index);
                    }
                    else
                    {
                        var cv = cm.Groups[1].Value;
                        var kv = km.Groups[1].Value;

                        var cs = _Split.Split(cv);
                        var ks = _SplitKana.Split(kv);

                        IEnumerable<SublocalityEntry> raws;

                        if (cs.Length == ks.Length)
                        {
                            raws = Enumerable.Range(0, cs.Length).Select(i => new SublocalityEntry(cs[i], ks[i]));
                        }
                        else
                        {
                            raws = new[] { new SublocalityEntry(cv, kv) };
                        }

                        _SublocalityIndex = 0;
                        _Sublocalities = raws.SelectMany(e => e.Populate()).ToList();
                    }
                }
            }

            return true;
        }

        protected bool MoveNextSublocality()
        {
            if (_Sublocalities != null && _SublocalityIndex < _Sublocalities.Count)
            {
                var se = _Sublocalities[_SublocalityIndex];
                Sublocality = se.Name;
                SublocalityKana = se.Kana;
                ExceptFor = se.ExceptFor;
                ExceptForKana = se.ExceptForKana;
                _SublocalityIndex++;
                return true;
            }

            _Sublocalities = null;
            Sublocality = null;
            SublocalityKana = null;

            return false;
        }
    }
}