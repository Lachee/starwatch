/*
START LICENSE DISCLAIMER
Starwatch is a Starbound Server manager with player management, crash recovery and a REST and websocket (live) API. 
Copyright(C) 2020 Lachee

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program. If not, see < https://www.gnu.org/licenses/ >.
END LICENSE DISCLAIMER
*/
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
