namespace Shipwreck.Postal
{
    internal static class ZipCodeReaderTestHelper
    {
        public static string LocalityRow(
            string cityCode = null,
            string zipCode = null,
            string zipCode5 = null,
            string prefectureName = null, string prefectureKana = null,
            string cityName = null, string cityKana = null,
            string localityName = null, string localityKana = null)
            => $"{cityCode},{zipCode5 ?? zipCode5?.Substring(3)},{zipCode},{prefectureKana},{cityKana},{localityKana},{prefectureName},{cityName},{localityName},0,0,0,0,0,0";
    }
}