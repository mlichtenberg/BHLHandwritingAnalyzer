using System.Net;
using System.Text;

namespace BHLHandwritingAnalyzer
{
    public static class BHLApi3
    {
        private static string _getItemMetadataEndpoint = "https://www.biodiversitylibrary.org/api3?op=GetItemMetadata&id={0}&pages={1}&ocr={2}&parts={3}&format={4}&apikey={5}";
        private static string _getPageMetadataEndpoint = "https://www.biodiversitylibrary.org/api3?op=GetPageMetadata&pageid={0}&ocr={1}&names={2}&format={3}&apikey={4}";

        public static string GetItemMetadata(int id, bool includePages, bool includeOcr, bool includeParts,
            ResponseFormat format, string apiKey)
        {
            string pages = includePages ? "t" : "f";
            string ocr = includeOcr ? "t" : "f";
            string parts = includeParts ? "t" : "f";
            string fmt = (format == ResponseFormat.Json ? "json" : "xml");
            string url = string.Format(_getItemMetadataEndpoint, id.ToString(), pages, ocr, parts, fmt, apiKey);
            return InvokeMethod(url);
        }

        public static string GetPageMetadata(int id, bool includeOcr, bool includeNames,
            ResponseFormat format, string apiKey)
        {
            string ocr = includeOcr ? "t" : "f";
            string names = includeNames ? "t" : "f";
            string fmt = (format == ResponseFormat.Json ? "json" : "xml");
            string url = string.Format(_getPageMetadataEndpoint, id.ToString(), ocr, names, fmt, apiKey);
            return InvokeMethod(url);
        }

        private static string InvokeMethod(string url)
        {
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            string apiResponse = wc.DownloadString(url);
            return apiResponse;
        }

        public enum ResponseFormat
        {
            Xml,
            Json
        }
    }
}
