using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Shipwreck.Postal
{

    public class RawLocalityZipCodeReader : IDisposable
    {
        private static readonly Regex _BooleanPattern = new Regex(@"^\s*1\s*$");

        private CsvReader _Parser;

        public RawLocalityZipCodeReader(TextReader reader, bool leaveOpen = false)
        {
            _Parser = new CsvReader(reader, leaveOpen: leaveOpen);
        }

        public string CityCode { get; protected set; }
        public string ZipCode5 { get; protected set; }
        public string ZipCode7 { get; protected set; }

        public string PrefectureKana { get; protected set; }
        public string CityKana { get; protected set; }
        public string LocalityKana { get; protected set; }

        public string Prefecture { get; protected set; }
        public string City { get; protected set; }
        public string Locality { get; protected set; }

        public bool LocalityHasMultipleZipCodes { get; protected set; }
        public bool IsPartitionedBySublocality { get; protected set; }
        public bool HasChome { get; protected set; }
        public bool ZipCodeHasMultipleLocalities { get; protected set; }

        public bool IsDefault { get; set; }

        public ChangeType ChangeType { get; protected set; }
        public ChangeReason ChangeReason { get; protected set; }

        private string[] _Buffer;

        public virtual bool MoveNext()
        {
            string[] fs;
            if (_IsPrefeched)
            {
                fs = _PrefetchBuffer;
                _IsPrefeched = false;
            }
            else
            {
                if (!_Parser.ReadAndCopyTo(ref _Buffer))
                {
                    return false;
                }
                fs = _Buffer;
            }

            CityCode = string.Intern(fs[0]);
            ZipCode5 = string.Intern(fs[1]);
            ZipCode7 = fs[2];

            PrefectureKana = string.Intern(fs[3]);
            CityKana = string.Intern(fs[4]);
            LocalityKana = string.Intern(fs[5]);

            Prefecture = string.Intern(fs[6]);
            City = string.Intern(fs[7]);
            Locality = string.Intern(fs[8]);

            LocalityHasMultipleZipCodes = _BooleanPattern.IsMatch(fs[9]);
            IsPartitionedBySublocality = _BooleanPattern.IsMatch(fs[10]);
            HasChome = _BooleanPattern.IsMatch(fs[11]);
            ZipCodeHasMultipleLocalities = _BooleanPattern.IsMatch(fs[12]);
            ChangeType = Enum.TryParse(fs[13], out ChangeType ct) ? ct : ChangeType.NotChanged;
            ChangeReason = Enum.TryParse(fs[14], out ChangeReason cr) ? cr : ChangeReason.NotChanged;

            OnRead();

            return true;
        }

        protected virtual void OnRead()
        {
            if (IsDefault = (Locality == "以下に掲載がない場合"))
            {
                LocalityKana = null;
                Locality = null;
            }
        }

        #region Prefetching

        private string[] _PrefetchBuffer;
        private bool _IsPrefeched;

        protected string PrefetchedCityCode => _IsPrefeched ? _PrefetchBuffer?[0] : null;
        protected string PrefetchedZipCode5 => _IsPrefeched ? _PrefetchBuffer?[1] : null;
        protected string PrefetchedZipCode7 => _IsPrefeched ? _PrefetchBuffer?[2] : null;

        protected string PrefetchedPrefectureKana => _IsPrefeched ? _PrefetchBuffer?[3] : null;
        protected string PrefetchedCityKana => _IsPrefeched ? _PrefetchBuffer?[4] : null;
        protected string PrefetchedLocalityKana => _IsPrefeched ? _PrefetchBuffer?[5] : null;

        protected string PrefetchedPrefecture => _IsPrefeched ? _PrefetchBuffer?[6] : null;
        protected string PrefetchedCity => _IsPrefeched ? _PrefetchBuffer?[7] : null;
        protected string PrefetchedLocality => _IsPrefeched ? _PrefetchBuffer?[8] : null;

        protected bool Prefetch()
            => _IsPrefeched = _Parser.ReadAndCopyTo(ref _PrefetchBuffer);

        protected void DiscardPrefetchedFields() => _IsPrefeched = false;

        #endregion Prefetching

        #region IDisposable Support

        protected bool IsDisposed { get; private set; }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    _Parser?.Dispose();
                    _Parser = null;
                }

                IsDisposed = true;
            }
        }

        public void Dispose()
            => Dispose(true);

        #endregion IDisposable Support
    }
}