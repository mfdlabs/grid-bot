using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Instrumentation
{
    internal class ExtendedWebClient : WebClient
    {
        internal void UploadStringGzipped(string address, string data, string username, string password)
        {
            Headers.Add("content-encoding", "gzip");
            AddAuthorizationHeader(username, password);
            UploadData(address, GZip(data));
        }

        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest webRequest = GetWebRequest(uri);
            if (webRequest != null)
            {
                webRequest.Timeout = _TimeoutInMilliseconds;
            }
            return webRequest;
        }

        private static byte[] GZip(string str)
        {
            byte[] byteEncodedData = Encoding.UTF8.GetBytes(str);
            byte[] gzippedResult;
            using (var memStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memStream, CompressionMode.Compress, true))
                {
                    gzipStream.Write(byteEncodedData, 0, byteEncodedData.Length);
                }
                gzippedResult = memStream.ToArray();
            }
            return gzippedResult;
        }

        private void AddAuthorizationHeader(string username, string password)
        {
            if (username.IsNullOrEmpty() || password.IsNullOrEmpty())
            {
                return;
            }
            Headers[HttpRequestHeader.Authorization] = $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"))}";
        }

        private const int _TimeoutInMilliseconds = 20000;
    }
}
