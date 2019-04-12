using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Shipwreck.Postal
{
    public class SublocalityZipcodeReader : LocalityZipCodeReader
    {
        private sealed class SublocalityEntry
        {
            private static readonly Regex _PointyKana = new Regex(@"\<(.*)ｦﾉｿﾞｸ\>$");
            private static readonly Regex _Pointy = new Regex(@"「(.*)を除く」$");

            private static readonly Regex _Number = new Regex(@"([０-９]+)～([０-９]+)");
            private static readonly Regex _NumberKana = new Regex(@"([0-9]+)\-([0-9]+)");
            private static readonly Regex _NumberSingle = new Regex(@"^[０-９]+$");

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

            private static int ParseFullWidth(string s)
                => s.Aggregate(0, (v, c) => v * 10 + c - '０');

            private static int ParseHalfWidth(string s)
                => s.Aggregate(0, (v, c) => v * 10 + c - '0');

            public IEnumerable<SublocalityEntry> Populate()
            {
                var ncm = _Number.Match(Name);
                var nkm = _NumberKana.Match(Kana);

                if (ncm.Success && nkm.Success)
                {
                    var c1 = ParseFullWidth(ncm.Groups[1].Value);
                    var c2 = ParseFullWidth(ncm.Groups[2].Value);
                    var k1 = ParseHalfWidth(ncm.Groups[1].Value);
                    var k2 = ParseHalfWidth(ncm.Groups[2].Value);

                    if (c1 == k1 && c2 == k2 && c1 < c2)
                    {
                        var pc = Name.Substring(0, ncm.Index);
                        var pk = Kana.Substring(0, nkm.Index);
                        var sc = Name.Substring(ncm.Index + ncm.Length);
                        var sk = Kana.Substring(nkm.Index + nkm.Length);

                        Func<int, bool> excepted = null;

                        if (ExceptFor != null && ExceptFor.StartsWith(pc) && ExceptFor.EndsWith(sc)
                            && ExceptForKana != null && ExceptForKana.StartsWith(pk) && ExceptForKana.EndsWith(sk))
                        {
                            var ecs = ExceptFor.Substring(pc.Length, ExceptFor.Length - pc.Length - sc.Length).Split('、');
                            var eks = ExceptForKana.Substring(pk.Length, ExceptForKana.Length - pk.Length - sk.Length).Split(',');

                            if (ecs.Length == eks.Length)
                            {
                                for (var i = 0; i < ecs.Length; i++)
                                {
                                    var ec = ecs[i];
                                    var ek = eks[i];

                                    var encm = _Number.Match(ec);
                                    var enkm = _NumberKana.Match(ek);

                                    if (encm.Success && encm.Index == 0 && encm.Length == ec.Length
                                        && enkm.Success && enkm.Index == 0 && enkm.Length == ek.Length)
                                    {
                                        var ec1 = ParseFullWidth(encm.Groups[1].Value);
                                        var ec2 = ParseFullWidth(encm.Groups[2].Value);
                                        var ek1 = ParseFullWidth(enkm.Groups[1].Value);
                                        var ek2 = ParseFullWidth(enkm.Groups[2].Value);

                                        if (ec1 == ek1 && ec2 == ek2 && ec1 < ec2)
                                        {
                                            excepted = excepted != null ? v => excepted(v) || ec1 <= v && v <= ec2
                                                        : (Func<int, bool>)(v => ec1 <= v && v <= ec2);
                                        }
                                        else
                                        {
                                            excepted = null;
                                            break;
                                        }
                                    }
                                    else if (_NumberSingle.IsMatch(ec)
                                        && int.TryParse(ek, out var ekn)
                                        && ekn == ParseFullWidth(ec))
                                    {
                                        excepted = excepted != null ? v => excepted(v) || v == ekn
                                                    : (Func<int, bool>)(v => v == ekn);
                                    }
                                    else
                                    {
                                        excepted = null;
                                        break;
                                    }
                                }
                            }
                        }

                        var ef = ExceptFor;
                        var efk = ExceptForKana;
                        if (excepted != null)
                        {
                            ef = null;
                            efk = null;
                        }

                        for (var i = c1; i <= c2; i++)
                        {
                            if (excepted?.Invoke(i) == true)
                            {
                                continue;
                            }

                            yield return new SublocalityEntry
                                (
                                    $"{pc}{new string(i.ToString("D").Select(c => (char)(c + '０' - '0')).ToArray())}{sc}",
                                    $"{pk}{i}{sk}",
                                    ef,
                                    efk);
                        }

                        yield break;
                    }
                }
                yield return this;
            }
        }

        private static readonly Regex _ParenKana = new Regex(@"\((.*)\)$");
        private static readonly Regex _Paren = new Regex(@"（(.*)）$");
        private static readonly Regex _ExceptForKana = new Regex(@"ｦﾉｿﾞｸ\)$");
        private static readonly Regex _ExceptFor = new Regex(@"を除く）$");

        private static readonly Regex _SplitKana = new Regex(@"(?<!\<[^>]*)､");
        private static readonly Regex _Split = new Regex(@"(?<!「[^」]*)、");

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
                        ExceptForKana = LocalityKana.Substring(km.Index + 1, LocalityKana.Length - 7 - km.Index);
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
                        MoveNextSublocality();
                    }
                    Locality = Locality.Substring(0, cm.Index);
                    LocalityKana = LocalityKana.Substring(0, km.Index);
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