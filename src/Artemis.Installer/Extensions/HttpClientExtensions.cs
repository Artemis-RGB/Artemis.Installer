using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Artemis.Installer.Utilities;

namespace Artemis.Installer.Extensions
{
    public static class HttpClientExtensions
    {
        public static async Task DownloadAsync(this HttpClient client, string requestUri, Stream destination,
            IDownloadable downloadable = null, CancellationToken cancellationToken = default)
        {
            // Get the http headers first to examine the content length
            using (HttpResponseMessage response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead))
            {
                long? contentLength = response.Content.Headers.ContentLength;

                using (Stream download = await response.Content.ReadAsStreamAsync())
                {
                    // Ignore progress reporting when no progress reporter was 
                    // passed or when the content length is unknown
                    if (downloadable == null || !contentLength.HasValue)
                    {
                        await download.CopyToAsync(destination);
                        return;
                    }

                    // Convert absolute progress (bytes downloaded) into relative progress (0% - 100%)
                    Progress<long> relativeProgress = new Progress<long>(
                        totalBytes => downloadable.ReportProgress(totalBytes, contentLength.Value, totalBytes / (float) contentLength.Value * 100f)
                    );
                    // Use extension method to report progress while downloading
                    await download.CopyToAsync(destination, 81920, relativeProgress, cancellationToken);
                    downloadable.ReportProgress(contentLength.Value, contentLength.Value, 100f);
                }
            }
        }
    }
}