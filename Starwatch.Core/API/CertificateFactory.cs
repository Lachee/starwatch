using Starwatch.Logging;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Starwatch.API
{
    class CertificateFactory
    {
        public string Filename { get; }
        public string Password { get; }
        public Logger Logger { get; }

        public CertificateFactory(string filename, string password, Logger parentLogger = null)
        {
            Logger = new Logger("[CERT]", parentLogger);
            Filename = filename;
            Password = password;
        }
        
        public X509Certificate2 Load()
        {
            if (!File.Exists(Filename))
            {
                Logger.LogError("Cannot load certificate because the file does not exist: {0}", Filename);
                return null;
            }

            Logger.Log("Loading...");
            X509Certificate2 certificate = null;
            try
            {
                certificate = new X509Certificate2(Filename, Password);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to load certificate: {0}");
                return null;
            }

            Logger.Log("Verifying...");
            X509Chain chain = new X509Chain();
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EndCertificateOnly;
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.UrlRetrievalTimeout = TimeSpan.FromSeconds(5);
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority | X509VerificationFlags.IgnoreCertificateAuthorityRevocationUnknown;

            bool happy = chain.Build(certificate);
            bool valid = true;

            //Get elements
            X509ChainElementCollection elements = chain.ChainElements;
            Logger.Log($"Chain built with {elements.Count} elements, happy={happy}");

            //Make sure each element is okay
            for (int i = 0; i < elements.Count; i++)
            {
                X509ChainElement element = elements[i];
                Logger.Log($" > Element {i}: {element.Certificate.GetNameInfo(X509NameType.SimpleName, false)}");
                foreach (X509ChainStatus status in element.ChainElementStatus)
                {
                    Logger.LogError($" >>> {status.Status}: {status.StatusInformation}");
                    valid = false;
                }
            }

            //Final check
            if (!happy && !valid)
            {
                Logger.LogError("Failed to load certificate! Happy: {0}, Valid: {1}", happy, valid);
                return null;
            }

            //return the certificate
            return certificate;
        }
    }
}
