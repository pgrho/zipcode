using System.IO;
using System.Text.RegularExpressions;

namespace Shipwreck.ZipCodeDB
{
    public class ConcatenatingLocalityZipCodeReader : LocalityZipCodeReader
    {
        public ConcatenatingLocalityZipCodeReader(TextReader reader, bool leaveOpen = false)
            : base(reader, leaveOpen: leaveOpen)
        { }

        protected override void OnRead()
        {
            var op = Locality.IndexOf('（');
            if (op >= 0)
            {
                while (Locality.IndexOf('）', op + 1) < 0)
                {
                    if (Prefetch()
                        && ZipCode7 == PrefetchedZipCode7
                        && Prefecture == PrefetchedPrefecture
                        && City == PrefetchedCity)
                    {
                        Locality += PrefetchedLocality;
                        LocalityKana += PrefetchedLocalityKana;

                        DiscardPrefetchedFields();
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                base.OnRead();
            }
        }
    }
}