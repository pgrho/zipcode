using System;
using System.IO;

namespace Shipwreck.Postal
{
    public class LocalityZipCodeReader : RawLocalityZipCodeReader
    {
        public LocalityZipCodeReader(TextReader reader, bool leaveOpen = false)
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