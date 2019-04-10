using System.IO;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Shipwreck.Postal
{
    using static ZipCodeReaderTestHelper;

    public class SublocalityZipCodeReaderTest
    {
        private const string CITY_CODE = "99999";
        private const string ZIP_CODE = "1234567";
        private const string PREF_NAME = "東京都";
        private const string PREF_KANA = "ﾄｳｷｮｳﾄ";
        private const string CITY_NAME = "千代田区";
        private const string CITY_KANA = "ﾁﾖﾀﾞｸ";

        private readonly ITestOutputHelper _Output;

        public SublocalityZipCodeReaderTest(ITestOutputHelper output)
        {
            _Output = output;
        }

        [Fact]
        public void NoSublocalityTest()
        {
            var locName = "番地";
            var locKana = "ﾊﾞﾝﾁ";

            using (var sr = new StringReader(
                LocalityRow(
                    cityCode: CITY_CODE, zipCode: ZIP_CODE,
                    prefectureName: PREF_NAME,
                    prefectureKana: PREF_KANA,
                    cityName: CITY_NAME,
                    cityKana: CITY_KANA,
                    localityName: locName,
                    localityKana: locKana)))
            using (var zr = new SublocalityZipcodeReader(sr))
            {
                Assert.True(zr.MoveNext());
                Assert.Equal(CITY_CODE, zr.CityCode);
                Assert.Equal(ZIP_CODE, zr.ZipCode7);
                Assert.Equal(PREF_NAME, zr.Prefecture);
                Assert.Equal(PREF_KANA, zr.PrefectureKana);
                Assert.Equal(CITY_NAME, zr.City);
                Assert.Equal(CITY_KANA, zr.CityKana);
                Assert.Equal(locName, zr.Locality);
                Assert.Equal(locKana, zr.LocalityKana);
                Assert.Equal("", zr.Sublocality ?? "");
                Assert.Equal("", zr.SublocalityKana ?? "");

                Assert.False(zr.MoveNext());
            }
        }

        [Fact]
        public void SublocalityTest()
        {
            var locName = "大字";
            var locKana = "ｵｵｱｻﾞ";
            var subName = "丁目";
            var subKana = "ﾁｮｳﾒ";

            using (var sr = new StringReader(
                LocalityRow(
                    cityCode: CITY_CODE, zipCode: ZIP_CODE,
                    prefectureName: PREF_NAME,
                    prefectureKana: PREF_KANA,
                    cityName: CITY_NAME,
                    cityKana: CITY_KANA,
                    localityName: $"{locName}（{subName}）",
                    localityKana: $"{locKana}({subKana})")))
            using (var zr = new SublocalityZipcodeReader(sr))
            {
                Assert.True(zr.MoveNext());
                Assert.Equal(CITY_CODE, zr.CityCode);
                Assert.Equal(ZIP_CODE, zr.ZipCode7);
                Assert.Equal(PREF_NAME, zr.Prefecture);
                Assert.Equal(PREF_KANA, zr.PrefectureKana);
                Assert.Equal(CITY_NAME, zr.City);
                Assert.Equal(CITY_KANA, zr.CityKana);
                Assert.Equal(locName, zr.Locality);
                Assert.Equal(locKana, zr.LocalityKana);
                Assert.Equal(subName, zr.Sublocality);
                Assert.Equal(subKana, zr.SublocalityKana);

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