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
        private const string PREF_NAME = "�����s";
        private const string PREF_KANA = "ĳ����";
        private const string CITY_NAME = "���c��";
        private const string CITY_KANA = "���޸";

        private readonly ITestOutputHelper _Output;

        public SublocalityZipCodeReaderTest(ITestOutputHelper output)
        {
            _Output = output;
        }

        [Fact]
        public void NoSublocalityTest()
        {
            var locName = "�Ԓn";
            var locKana = "����";

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
            var locName = "�厚";
            var locKana = "�����";
            var subName = "����";
            var subKana = "����";

            using (var sr = new StringReader(
                LocalityRow(
                    cityCode: CITY_CODE, zipCode: ZIP_CODE,
                    prefectureName: PREF_NAME,
                    prefectureKana: PREF_KANA,
                    cityName: CITY_NAME,
                    cityKana: CITY_KANA,
                    localityName: $"{locName}�i{subName}�j",
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
            var locName = "�厚";
            var locKana = "�����";
            var subName1 = "�P����";
            var subKana1 = "1����";
            var subName2 = "�Q����";
            var subKana2 = "2����";

            using (var sr = new StringReader(
                LocalityRow(
                    cityCode: CITY_CODE, zipCode: ZIP_CODE,
                    prefectureName: PREF_NAME,
                    prefectureKana: PREF_KANA,
                    cityName: CITY_NAME,
                    cityKana: CITY_KANA,
                    localityName: $"{locName}�i{subName1}�A{subName2}�j",
                    localityKana: $"{locKana}({subKana1}�{subKana2})")))
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
            var locName = "�厚";
            var locKana = "�����";
            var exceptFor1 = "�P����";
            var exceptKana1 = "1����";
            var exceptFor2 = "�Q����";
            var exceptKana2 = "2����";

            using (var sr = new StringReader(
                LocalityRow(
                    cityCode: CITY_CODE, zipCode: ZIP_CODE,
                    prefectureName: PREF_NAME,
                    prefectureKana: PREF_KANA,
                    cityName: CITY_NAME,
                    cityKana: CITY_KANA,
                    localityName: $"{locName}�i{exceptFor1}�A{exceptFor2}�������j",
                    localityKana: $"{locKana}({exceptKana1}�{exceptKana2}�ɿ޸)")))
            using (var zr = new SublocalityZipcodeReader(sr))
            {
                AssertNextRow(
                    zr,
                    locName: locName,
                    locKana: locKana,
                    exceptFor: exceptFor1 + "�A" + exceptFor2,
                    exceptForKana: exceptKana1 + "�" + exceptKana2);

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