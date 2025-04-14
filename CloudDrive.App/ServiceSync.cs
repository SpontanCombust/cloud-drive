using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace CloudDrive.App
{
    public class SyncService
    {
        private readonly WebAPIClient _client;
        private readonly string _folderPath;

        public SyncService(string serverUrl, string authToken, string folderPath)
        {
            _folderPath = folderPath;

            _client = new WebAPIClient(serverUrl)
            {
                HttpClient = new HttpClient
                {
                    DefaultRequestHeaders = {
                        Authorization = new AuthenticationHeaderValue("Bearer", authToken)
                    }
                }
            };
        }

        public async Task SynchronizeAsync()
        {
            if (string.IsNullOrEmpty(_folderPath) || !Directory.Exists(_folderPath))
                throw new Exception("Ścieżka do folderu nie została ustawiona lub nie istnieje.");

            // Pobieranie metadanych z serwera
            var serverFiles = await _client.SyncAsync();
            var serverFileNames = serverFiles.Select(f => f.FileName).ToHashSet();

            // Lista plików lokalnych
            var localFiles = Directory.GetFiles(_folderPath);
            var localFileNames = localFiles.Select(Path.GetFileName).ToHashSet();

            // Pliki, które są na serwerze, ale nie ma ich lokalnie → POBIERZ
            var missingLocally = serverFiles.Where(f => !localFileNames.Contains(f.FileName));
            foreach (var file in missingLocally)
            {
                var localPath = Path.Combine(_folderPath, file.FileName);
                using var stream = await _client.GetLatestFileVersionAsync(file.Id);
                using var fs = File.Create(localPath);
                await stream.CopyToAsync(fs);
            }

            // Pliki, które są lokalnie, ale nie ma ich na serwerze → WYŚLIJ
            var toUpload = localFiles.Where(f => !serverFileNames.Contains(Path.GetFileName(f)));
            foreach (var filePath in toUpload)
            {
                using var stream = File.OpenRead(filePath);
                var fileName = Path.GetFileName(filePath);
                await _client.CreateFileAsync(stream, fileName);
            }
        }
    }
}
