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
                AssertNextRow(zr, locName: locName, locKana: locKana);

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
                AssertNextRow(
                    zr,
                    locName: locName,
                    locKana: locKana,
                    subName: subName,
                    subKana: subKana);

                Assert.False(zr.MoveNext());
            }
        }

        [Fact]
        public void SplitSublocalityTest()
        {
            var locName = "大字";
            var locKana = "ｵｵｱｻﾞ";
            var subName1 = "１丁目";
            var subKana1 = "1ﾁｮｳﾒ";
            var subName2 = "２丁目";
            var subKana2 = "2ﾁｮｳﾒ";

            using (var sr = new StringReader(
                LocalityRow(
                    cityCode: CITY_CODE, zipCode: ZIP_CODE,
                    prefectureName: PREF_NAME,
                    prefectureKana: PREF_KANA,
                    cityName: CITY_NAME,
                    cityKana: CITY_KANA,
                    localityName: $"{locName}（{subName1}、{subName2}）",
                    localityKana: $"{locKana}({subKana1}､{subKana2})")))
            using (var zr = new SublocalityZipcodeReader(sr))
            {
                AssertNextRow(
                    zr,
                    locName: locName,
                    locKana: locKana,
                    subName: subName1,
                    subKana: subKana1);
                AssertNextRow(
                    zr,
                    locName: locName,
                    locKana: locKana,
                    subName: subName2,
                    subKana: subKana2);

                Assert.False(zr.MoveNext());
            }
        }

        [Fact]
        public void SplitExceptForTest()
        {
            var locName = "大字";
            var locKana = "ｵｵｱｻﾞ";
            var exceptFor1 = "１丁目";
            var exceptKana1 = "1ﾁｮｳﾒ";
            var exceptFor2 = "２丁目";
            var exceptKana2 = "2ﾁｮｳﾒ";

            using (var sr = new StringReader(
                LocalityRow(
                    cityCode: CITY_CODE, zipCode: ZIP_CODE,
                    prefectureName: PREF_NAME,
                    prefectureKana: PREF_KANA,
                    cityName: CITY_NAME,
                    cityKana: CITY_KANA,
                    localityName: $"{locName}（{exceptFor1}、{exceptFor2}を除く）",
                    localityKana: $"{locKana}({exceptKana1}､{exceptKana2}ｦﾉｿﾞｸ)")))
            using (var zr = new SublocalityZipcodeReader(sr))
            {
                AssertNextRow(
                    zr,
                    locName: locName,
                    locKana: locKana,
                    exceptFor: exceptFor1 + "、" + exceptFor2,
                    exceptForKana: exceptKana1 + "､" + exceptKana2);

                Assert.False(zr.MoveNext());
            }
        }

        private static void AssertNextRow(
            SublocalityZipcodeReader zr,
            string locName = null, string locKana = null,
            string subName = null, string subKana = null,
            string exceptFor = null, string exceptForKana = null)
        {
            Assert.True(zr.MoveNext());
            Assert.Equal(CITY_CODE, zr.CityCode);
            Assert.Equal(ZIP_CODE, zr.ZipCode7);
            Assert.Equal(PREF_NAME, zr.Prefecture);
            Assert.Equal(PREF_KANA, zr.PrefectureKana);
            Assert.Equal(CITY_NAME, zr.City);
            Assert.Equal(CITY_KANA, zr.CityKana);
            Assert.Equal(locName ?? string.Empty, zr.Locality ?? string.Empty);
            Assert.Equal(locKana ?? string.Empty, zr.LocalityKana ?? string.Empty);
            Assert.Equal(subName ?? string.Empty, zr.Sublocality ?? string.Empty);
            Assert.Equal(subKana ?? string.Empty, zr.SublocalityKana ?? string.Empty);
            Assert.Equal(exceptFor ?? string.Empty, zr.ExceptFor ?? string.Empty);
            Assert.Equal(exceptForKana ?? string.Empty, zr.ExceptForKana ?? string.Empty);
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