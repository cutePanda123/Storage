﻿using System;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ApiApp.Common
{
    public class Utils
    {
        public static string GenerateWriteSasUrl(CloudBlockBlob blockblob)
        {
            string sas = blockblob.GetSharedAccessSignature(
                new SharedAccessBlobPolicy
                {
                    Permissions = SharedAccessBlobPermissions.Create | SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write,
                    SharedAccessExpiryTime = DateTime.UtcNow.AddHours(1)
                });

            return blockblob.Uri.AbsoluteUri + sas;
        }

        public static string GenerateReadSasUrl(CloudBlockBlob blockblob)
        {
            string sas = blockblob.GetSharedAccessSignature(
                new SharedAccessBlobPolicy
                {
                    Permissions = SharedAccessBlobPermissions.Read,
                    SharedAccessExpiryTime = DateTime.UtcNow.AddDays(1)
                });

            return blockblob.Uri.AbsoluteUri + sas;
        }
    }
}