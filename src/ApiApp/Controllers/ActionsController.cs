﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using ApiApp.Common;
using ApiApp.Filters;
using ApiApp.Models;
using Newtonsoft.Json;
using Swashbuckle.Swagger.Annotations;

namespace ApiApp.Controllers
{
    [TncExceptionFilter]
    [CustomHeaderFilter]
    public class ActionsController : BaseController
    {
        [Route("api/actions/uploadbatch")]
        [HttpPost]
        [SwaggerOperation("Upload")]
        [SwaggerResponse(HttpStatusCode.Accepted)]
        public async Task<IHttpActionResult> UploadImageBatchAsync([FromBody]UploadBatchRequest value)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value.BatchTag))
                {
                    throw new TncException()
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        ErrorCode = ErrorCode.ProperyCannotBeEmpty,
                        ErrorMessage = string.Format(ErrorMessages.PropertyCannotBeEmpty, "BatchTag")
                    };
                }

                if (string.IsNullOrWhiteSpace(value.CSVMetadata))
                {
                    throw new TncException()
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        ErrorCode = ErrorCode.ProperyCannotBeEmpty,
                        ErrorMessage = string.Format(ErrorMessages.PropertyCannotBeEmpty, "CSVMetadata")
                    };
                }

                List<AnimalImage> animalImages;
                try
                {
                    animalImages = AnimalImage.ReadFromCsv(value.CSVMetadata);
                }
                catch (Exception ex)
                {
                    throw new TncException()
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        ErrorCode = ErrorCode.BadCSVMetadata,
                        ErrorMessage = ex.Message
                    };
                }

                MetadataValidate(animalImages, a => string.IsNullOrWhiteSpace(a.OriginalFileId), "OriginalFileId is not present");
                MetadataValidate(animalImages, a => string.IsNullOrWhiteSpace(a.OriginalFolderId), "OriginalFolderId is not present");
                MetadataValidate(animalImages, a => string.IsNullOrWhiteSpace(a.OriginalImageId), "OriginalImageId is not present");
                MetadataValidate(animalImages, a => string.IsNullOrWhiteSpace(a.FileFormat), "FileFormat is not present");

                var operation = new OperationResult
                {
                    CreationTime = DateTime.UtcNow,
                    Id = Guid.NewGuid().ToString(),
                    Status = new OperationStatus { Code = OperationStage.New }
                };

                await this.AppConfiguration.CosmosDBClient.UpsertDocumentAsync(
                    this.AppConfiguration.OperationResultCollectionUri,
                    operation);

                Uri originalUri = this.Request.RequestUri;
                UriBuilder uriBuilder = new UriBuilder(originalUri.Scheme, originalUri.Host, originalUri.Port, $"/api/operationresults/{operation.Id}");

                this.Request.Properties[Constants.CustomResponseHeaders] = new Dictionary<string, string>()
                    {
                        { Constants.LocationHeader, uriBuilder.Uri.AbsoluteUri },
                        { Constants.RetryAfterHeader, Constants.RetryIntervalInSeconds.ToString() }
                    };

                return this.Accepted();
            }
            catch (TncException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new TncException
                {
                    ErrorCode = "UnexpectedException",
                    ErrorMessage = ex.ToString()
                };
            }
        }

        private static void MetadataValidate(List<AnimalImage> animalImages, Func<AnimalImage, bool> validator, string message)
        {
            AnimalImage invalidImage = animalImages.FirstOrDefault(validator);

            if (invalidImage != null)
            {
                throw new TncException()
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    ErrorCode = ErrorCode.BadCSVMetadata,
                    ErrorMessage = "Message: " + message + " Image: " + JsonConvert.SerializeObject(invalidImage)
                };
            }
        }
    }
}
