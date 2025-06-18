using System.Globalization;
using System.Net;
using System.Text;
using App.Controllers;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace App.Models;

public class UploadLargeFile_Model
{
    public bool IsSuccessful { get; set; } = false;
    public Dictionary<string, string> FormFileNameFullName { get; set; } = [];
    public Dictionary<string, string> ErrorDescription { get; set; } = [];
    public string UploadedFileGuid { get; set; } = string.Empty;
}
public class UploadLargeFile
{
    readonly IWebHostEnvironment env;
    public UploadLargeFile(IWebHostEnvironment _env)
    {
        env = _env;
    }
    public async Task<UploadLargeFile_Model> GetFormModelAndLargeFiles<T>(HttpContext HttpContext,
    T formModel, List<string> uploadingFormFileNames, ControllerBase controllerBase) where T : class
    {
        UploadLargeFile_Model uploadLargeFile_Model = new();

        if (!await HttpContext.RequestServices.GetService<IAntiforgery>()!.IsRequestValidAsync(HttpContext))
        {
            uploadLargeFile_Model.ErrorDescription.Add("antiforgerytoken", "anti forgery token is incorrect!");
            return uploadLargeFile_Model;
        }

        if (!string.IsNullOrEmpty(HttpContext.Request.ContentType) &&
            !HttpContext.Request.ContentType.Contains("multipart/", StringComparison.OrdinalIgnoreCase))
        {
            uploadLargeFile_Model.ErrorDescription.Add("File",
                $"The request couldn't be processed (Error 1).");
            uploadLargeFile_Model.ErrorDescription.Add("Request.ContentType",
            $"{HttpContext.Request.ContentType}");

            return uploadLargeFile_Model;
        }

        if (!HttpContext.Request.HasFormContentType ||
            !MediaTypeHeaderValue.TryParse(HttpContext.Request.ContentType, out var mediaTypeHeader) ||
            string.IsNullOrEmpty(mediaTypeHeader.Boundary.Value))
        {
            uploadLargeFile_Model.ErrorDescription.Add("Media Type", "Unsupported Media Type Result");
            return uploadLargeFile_Model;
        }

        // Accumulate the form data key-value pairs in the request (formAccumulator).
        var formAccumulator = new KeyValueAccumulator();

        var boundary = HeaderUtilities.RemoveQuotes(mediaTypeHeader.Boundary).Value;
        if (string.IsNullOrWhiteSpace(boundary))
        {
            //throw new InvalidDataException("Missing content-type boundary.");
            uploadLargeFile_Model.ErrorDescription.Add("InvalidDataException", "Missing content-type boundary.");
            return uploadLargeFile_Model;
        }

        if (boundary.Length > 70)
        {
            /*throw new InvalidDataException(
                $"Multipart boundary length limit {70} exceeded.");*/
            uploadLargeFile_Model.ErrorDescription.Add("InvalidDataException", "Multipart boundary length limit {70} exceeded.");
            return uploadLargeFile_Model;
        }

        var reader = new MultipartReader(boundary, HttpContext.Request.Body);
        var section = await reader.ReadNextSectionAsync();

        string fileGuid = Guid.NewGuid().ToString().Replace("-", "");
        uploadLargeFile_Model.UploadedFileGuid = fileGuid;
        try
        {
            while (section != null)
            {
                var hasContentDispositionHeader =
                    ContentDispositionHeaderValue.TryParse(
                        section.ContentDisposition, out var contentDisposition);

                if (hasContentDispositionHeader)
                {
                    //temp dirInfo
                    //var tempFilesDirInfo = Directory.CreateDirectory(Path.Combine(env.ContentRootPath, "TempFiles", fileGuid));

                    if (contentDisposition != null
                        && contentDisposition.DispositionType.Equals("form-data")
                        && (!string.IsNullOrEmpty(contentDisposition.FileName.Value)
                            || !string.IsNullOrEmpty(contentDisposition.FileNameStar.Value)))
                    {
                        var tempFilesDirInfo = Directory.CreateDirectory(Path.Combine(env.ContentRootPath, "TempFiles", fileGuid));
                        var fileName = WebUtility.HtmlEncode(contentDisposition.FileName.Value) ?? "fileName";
                        var key = HeaderUtilities.RemoveQuotes(contentDisposition.Name).Value;

                        foreach (string formFileName in uploadingFormFileNames)
                        {
                            if (key == formFileName)
                            {
                                var saveToTempPath = Path.Combine(tempFilesDirInfo.FullName, fileName);
                                using (var fs = System.IO.File.Create(saveToTempPath))
                                {
                                    await section.Body.CopyToAsync(fs);
                                }
                                uploadLargeFile_Model.FormFileNameFullName.Add(formFileName, saveToTempPath);
                            }
                        }
                    }
                    else if (contentDisposition != null
                            && contentDisposition.DispositionType.Equals("form-data")
                            && string.IsNullOrEmpty(contentDisposition.FileName.Value)
                            && string.IsNullOrEmpty(contentDisposition.FileNameStar.Value))
                    {
                        // Don't limit the key name length because the 
                        // multipart headers length limit is already in effect.
                        var key = HeaderUtilities
                            .RemoveQuotes(contentDisposition.Name).Value;

                        var hasMediaTypeHeader =
                        MediaTypeHeaderValue.TryParse(section.ContentType, out var mediaType);

                        using (var streamReader = new StreamReader(
                            section.Body,
                            mediaType?.Encoding ?? Encoding.UTF8,
                            detectEncodingFromByteOrderMarks: true,
                            bufferSize: 1024,
                            leaveOpen: true))
                        {
                            // The value length limit is enforced by 
                            // MultipartBodyLengthLimit
                            var value = await streamReader.ReadToEndAsync();

                            if (string.Equals(value, "undefined",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                value = string.Empty;
                            }

                            formAccumulator.Append(key!, value);
                        }
                    }
                }

                // Drain any remaining section body that hasn't been consumed and
                // read the headers for the next section.
                section = await reader.ReadNextSectionAsync();
            }
        }
        catch (Exception e)
        {
            var logDirectory = Directory.CreateDirectory(Path.Combine(env.ContentRootPath, "Logs"));
            string logPath = Path.Combine(logDirectory.FullName, $"UploadLargeFile_{Guid.NewGuid().ToString().Replace("-", "")}");//$"{env.ContentRootPath}{ds}Logs{ds}Download_SubmitNewFile_{Guid.NewGuid().ToString().Replace("-", "")}";
            await System.IO.File.WriteAllTextAsync(logPath, e.ToString());

            uploadLargeFile_Model.ErrorDescription.Add(e.Source ?? string.Empty, e.Message);
            return uploadLargeFile_Model;
        }

        /*var controllerBase = HttpContext.GetEndpoint()?.Metadata
        .OfType<ControllerBase>()
        .FirstOrDefault();

        if (controllerBase is null)
        {
            uploadLargeFile_Model.ErrorDescription.Add("ControllerBase", "ControllerBase is null!");
            return uploadLargeFile_Model;
        }*/

        // Bind form data to the model
        //var formData = new FormData();
        //T formModel2 = new(); // needs to define for method where : new()
        var formValueProvider = new FormValueProvider(
            BindingSource.Form,
            new FormCollection(formAccumulator.GetResults()),
            CultureInfo.CurrentCulture);

        var bindingSuccessful = await controllerBase.TryUpdateModelAsync(formModel, prefix: "",
            valueProvider: formValueProvider);

        if (!bindingSuccessful)
        {

            uploadLargeFile_Model.ErrorDescription
            .Add("ControllerBase.TryUpdateModelAsync", "Binding was Not successful");
            return uploadLargeFile_Model;
        }

        uploadLargeFile_Model.IsSuccessful = true;
        return uploadLargeFile_Model;
    }

}