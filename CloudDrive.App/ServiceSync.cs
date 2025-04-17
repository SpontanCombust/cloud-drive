using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;


namespace CloudDrive.App
{
    public class SyncService
    {

        private readonly WebAPIClient Api;
        private readonly string _folderPath;
        private readonly string authToken = "";

        public SyncService(string serverUrl, string authToken, string folderPath)
        {
            _folderPath = folderPath;
            this.authToken = authToken;

            HttpClient client = new HttpClient
            {
                DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Bearer", authToken) }
            };

            Api = new WebAPIClient(serverUrl, client);
        }
        public async Task SynchronizeAsync()
        {
            if (string.IsNullOrEmpty(_folderPath) || !Directory.Exists(_folderPath))
                throw new Exception("Ścieżka do folderu nie została ustawiona lub nie istnieje.");

            // Pobieranie metadanych z serwera (pliki)
            var response = await Api.SyncAllAsync();
            var serverFiles = response.CurrentFileVersionsInfos;
            var serverFileMap = serverFiles.ToDictionary(f => f.FileId, f => f.ClientDirPath);

            // Lista naszych plików lokalnych
            var localFiles = Directory.GetFiles(_folderPath);
            var localFileMap = new Dictionary<Guid, string>();

            // Iteracja aby zbudować mapę lokalnych plików
            foreach (var path in localFiles)
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var parts = name.Split('_');
                if (parts.Length > 1 && Guid.TryParse(parts[^1], out var id))
                {
                    localFileMap[id] = path;
                }
            }

            foreach (var serverFile in serverFileMap)
            {
                var fileId = serverFile.Key;
                var serverFilePath = serverFile.Value;

                // Jeśli plik z serwera istnieje lokalnie
                if (localFileMap.ContainsKey(fileId))
                {
                    var localFilePath = localFileMap[fileId];

                    if (localFilePath != serverFilePath)
                    {
                        Console.WriteLine($"Plik z serwera znajduje się w innej ścieżce: {serverFilePath}. Pobieram go...");
                        await DownloadFileAsync(fileId.ToString(), localFilePath); // sprawdzić ścieżki
                    }
                }
                else // Jeśli plik z serwera nie istnieje lokalnie
                {
                    Console.WriteLine($"Plik {fileId} nie istnieje lokalnie, należy go pobrać.");
                    await DownloadFileAsync(fileId.ToString(), _folderPath); // sprawdzić ścieżke do pliku
                }
            }

            // Sprawdzamy, czy jakieś pliki lokalne nie istnieją na serwerze (potrzebują wysłania)
            foreach (var localFile in localFileMap)
            {
                var fileId = localFile.Key;

                // Jeśli plik lokalny nie istnieje na serwerze, wysyłamy go

                if (!serverFileMap.ContainsKey(fileId))
                {
                    Console.WriteLine($"Plik lokalny {fileId} nie istnieje na serwerze, wysyłam go.");

                    await UploadFileAsync(localFile.Value);

                    var result = await UploadFileAsync(localFile.Value);

                    if (result != null)
                    {
                        // Zakładam, że result zawiera ClientDirPath i ClientFileName
                        var newServerFilePath = Path.Combine(_folderPath, result.ClientDirPath, $"{result.ClientFileName}_{result.FileId}");

                        // Dodajemy do mapy serwera, żeby uniknąć ponownego przesyłania w tej samej sesji
                        serverFileMap[result.FileId] = newServerFilePath;
                    }

                }
            }
        }


        private async Task<FileVersionDTO> UploadFileAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;

            try
            {
                using var fileStream = File.OpenRead(filePath);
                var fileName = Path.GetFileName(filePath);

                // Wyciągamy ścieżkę katalogu
                var clientDirPath = Path.GetDirectoryName(filePath)?.Replace(_folderPath, "")?.Trim(Path.DirectorySeparatorChar) ?? "";

                if (string.IsNullOrEmpty(clientDirPath))
                {
                    MessageBox.Show("Nie można znaleźć katalogu docelowego dla pliku.");
                    return null;
                }

                // Tworzymy obiekt FileParameter, który zawiera plik do przesłania
                var fileParam = new FileParameter(fileStream, fileName, "application/octet-stream");

                var result = await Api.CreateFileAsync(fileParam, clientDirPath);

                if (result != null)
                {
                    return result.FirstFileVersionInfo;
                }
                else
                {
                    MessageBox.Show("Wystąpił problem podczas wysyłania pliku.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd wysyłania pliku: " + ex.Message);
                return null;
            }
        }


        private async Task DownloadFileAsync(string fileId, string destinationPath)
        {
            if (string.IsNullOrEmpty(fileId))
            {
                MessageBox.Show("Brak identyfikatora pliku.");
                return;
            }

            if (!Guid.TryParse(fileId, out var fileGuid))
            {
                MessageBox.Show("Nieprawidłowy identyfikator pliku (musi być GUID).");
                return;
            }

            try
            {
                var fileResponse = await Api.GetLatestFileVersionAsync(fileGuid);

                using (var fileStream = File.Create(destinationPath))
                {
                    await fileResponse.Stream.CopyToAsync(fileStream);
                    MessageBox.Show($"Pobrano plik do: {destinationPath}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd pobierania pliku: " + ex.Message);
            }
        }
    }
}

