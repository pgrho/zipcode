using Microsoft.VisualBasic.FileIO;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Shipwreck.Postal
{
    public class RawLocalityZipCodeReader : IDisposable
    {
        private static readonly Regex _BooleanPattern = new Regex(@"^\s*1\s*$");

        // TODO: remove VB dependency
        private TextFieldParser _Parser;

        private readonly bool _LeaveOpen;

        public RawLocalityZipCodeReader(TextReader reader, bool leaveOpen = false)
        {
            _Parser = new TextFieldParser(reader) { Delimiters = new[] { "," }, TrimWhiteSpace = true };
            _LeaveOpen = leaveOpen;
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

        public virtual bool MoveNext()
        {
            var fs = _PrefetchedFields ?? _Parser?.ReadFields();
            if (fs == null)
            {
                return false;
            }
            _PrefetchedFields = null;

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

        private string[] _PrefetchedFields;

        protected string PrefetchedCityCode => _PrefetchedFields?[0];
        protected string PrefetchedZipCode5 => _PrefetchedFields?[1];
        protected string PrefetchedZipCode7 => _PrefetchedFields?[2];

        protected string PrefetchedPrefectureKana => _PrefetchedFields?[3];
        protected string PrefetchedCityKana => _PrefetchedFields?[4];
        protected string PrefetchedLocalityKana => _PrefetchedFields?[5];

        protected string PrefetchedPrefecture => _PrefetchedFields?[6];
        protected string PrefetchedCity => _PrefetchedFields?[7];
        protected string PrefetchedLocality => _PrefetchedFields?[8];

        protected bool Prefetch()
            => (_PrefetchedFields = _Parser?.ReadFields()) != null;

        protected void DiscardPrefetchedFields()
            => _PrefetchedFields = null;

        #endregion Prefetching

        #region IDisposable Support

        protected bool IsDisposed { get; private set; }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    if (!_LeaveOpen)
                    {
                        _Parser?.Dispose();
                    }
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