using System.IO;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Shipwreck.Postal
{
    using static ZipCodeReaderTestHelper;
    public class SublocalityZipCodeReaderTest
    {
        private readonly ITestOutputHelper _Output;

        public SublocalityZipCodeReaderTest(ITestOutputHelper output)
        {
            _Output = output;
        }

        [Fact]
        public void Simple()
        {
            var CC = "99999";
            var Z7 = "1234567";

            var PN = "ìåãûìs";
            var PK = "ƒ≥∑Æ≥ƒ";
            var CN = "êÁë„ìcãÊ";
            var CK = "¡÷¿ﬁ∏";
            var LN = "î‘ín";
            var LK = " ﬁ›¡";

            using (var sr = new StringReader(
                LocalityRow(
                    cityCode: CC, zipCode: Z7,
                    prefectureName: PN,
                    prefectureKana: PK,
                    cityName: CN,
                    cityKana: CK,
                    localityName: LN,
                    localityKana: LK)))
            using (var zr = new SublocalityZipcodeReader(sr))
            {
                Assert.True(zr.MoveNext());
                Assert.Equal(CC, zr.CityCode);
                Assert.Equal(Z7, zr.ZipCode7);
                Assert.Equal(PN, zr.Prefecture);
                Assert.Equal(PK, zr.PrefectureKana);
                Assert.Equal(CN, zr.City);
                Assert.Equal(CK, zr.CityKana);
                Assert.Equal(LN, zr.Locality);
                Assert.Equal(LK, zr.LocalityKana);
                Assert.Equal("", zr.Sublocality ?? "");
                Assert.Equal("", zr.SublocalityKana ?? "");

                Assert.False(zr.MoveNext());
            }
        }

        [Fact]
        public void Test()
        {
            using (var sr = new StreamReader("ADD_1710.CSV", Encoding.GetEncoding(932)))
            using (var zr = new SublocalityZipcodeReader(sr))
            {
                while (zr.MoveNext())
                {
                    _Output.WriteLine($"{zr.CityCode}: {zr.ZipCode7} ({zr.ZipCode5}) {zr.Prefecture} ({zr.PrefectureKana}) {zr.City} ({zr.CityKana}) {zr.Locality} ({zr.LocalityKana}) { zr.Sublocality} ({ zr.SublocalityKana})");
                }
            }
        }
    }
}