﻿using SCD.Core.DataModels;
using SCD.Core.Utilities;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SCD.Core;

public static class AlbumDownloader
{
    public static event Action<AlbumFile>? FileChanged;
    public static event Action<Exception>? ErrorOccurred;

    public async static Task DownloadAlbumAsync(Album album, string downloadLocation, IProgress<decimal> progress, CancellationToken token)
    {
        if(!Directory.Exists(downloadLocation))
            Directory.CreateDirectory(downloadLocation);

        if(album.AlbumFiles.Count == 0)
            return;

        FileDownloader downloadHandler = new FileDownloader(progress, 1024 * 256, 1000);

        do
        {
            AlbumFile file = album.AlbumFiles.Peek();

            FileChanged?.Invoke(file);

            // If the file url is empty or the file name is empty, then skip over the file
            if(string.IsNullOrEmpty(file.Filename) || string.IsNullOrEmpty(file.Url))
                continue;

            string filePath = Path.Combine(downloadLocation, Parser.ParseValidFilename(file.Filename));

            if(File.Exists(filePath))
            {
                album.AlbumFiles.Dequeue();

                continue;
            }

            try
            {
                await downloadHandler.DownloadFileAsync(file, filePath, token);
            }
            catch(Exception ex)
            {
                File.Delete(filePath);

                token.ThrowIfCancellationRequested();

                if(ex is IOException or HttpRequestException)
                    continue;

                ErrorOccurred?.Invoke(ex);

                throw;
            }

            album.AlbumFiles.Dequeue();
        } while(album.AlbumFiles.Count > 0);
    }
}