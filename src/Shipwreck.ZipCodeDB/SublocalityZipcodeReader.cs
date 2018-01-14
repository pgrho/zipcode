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

            public string Name { get; }
            public string Kana { get; }

            public string ExceptFor { get; }
            public string ExceptForKana { get; }
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

                        // TODO: expand tilde
                        _SublocalityIndex = 0;
                        _Sublocalities = raws.ToList();
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