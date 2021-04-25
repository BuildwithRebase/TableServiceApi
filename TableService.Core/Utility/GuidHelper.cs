using System;
using System.Security.Cryptography;

namespace TableService.Core.Utility
{
    public static class GuidHelper
    {
        public static Guid CreateCryptographicallySecureGuid()
        {
            using (var provider = new RNGCryptoServiceProvider())
            {
                var bytes = new byte[16];
                provider.GetBytes(bytes);

                return new Guid(bytes);
            }
        }
    }
}
