﻿using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using PenguinUpload.DataModels.Api;
using PenguinUpload.Infrastructure.Upload;
using PenguinUpload.Services.FileStorage;
using PenguinUpload.Utilities;

namespace PenguinUpload.Modules
{
    public class AdHocApiAccessModule : NancyModule
    {
        private readonly IFileUploadHandler _fileUploadHandler;

        public AdHocApiAccessModule(IFileUploadHandler fileUploadHandler) : base("/api")
        {
            _fileUploadHandler = fileUploadHandler;
            Post("/upload", async _ =>
            {
                var request = this.Bind<FileUploadRequest>();

                var uploadResult = await _fileUploadHandler.HandleUpload(request.File.Name, request.File.Value);

                // Register uploaded file
                var storedFilesManager = new StoredFilesManager();
                var storedFile = storedFilesManager.RegisterStoredFileAsync(request.File.Name, uploadResult.FileId);

                return Response.AsJsonNet(storedFile);
            });

            Get("/download/{id}", async args =>
            {
                var storedFilesManager = new StoredFilesManager();
                var storedFile = await storedFilesManager.GetStoredFileByIdentifier((string) args.id);
                if (storedFile == null) return HttpStatusCode.NotFound;

                var fileStream = _fileUploadHandler.RetrieveFileStream(storedFile.Identifier);
                var response = new StreamResponse(() => fileStream, MimeTypes.GetMimeType(storedFile.Name));
                return response.AsAttachment(storedFile.Name);
            });
        }
    }
}