using System.IO;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Shipwreck.ZipCodeDB
{
    public class RawLocalityZipCodeReaderTest
    {
        private readonly ITestOutputHelper _Output;

        public RawLocalityZipCodeReaderTest(ITestOutputHelper output)
        {
            _Output = output;
        }

        [Fact]
        public void Test()
        {
            using (var sr = new StreamReader("ADD_1710.CSV", Encoding.GetEncoding(932)))
            using (var zr = new RawLocalityZipCodeReader(sr))
            {
                while (zr.MoveNext())
                {
                    _Output.WriteLine($"{zr.CityCode}: {zr.ZipCode7} ({zr.ZipCode5}) {zr.Prefecture} ({zr.PrefectureKana}) {zr.City} ({zr.CityKana}) {zr.Locality} ({zr.LocalityKana})");
                }
            }
        }
    }
}