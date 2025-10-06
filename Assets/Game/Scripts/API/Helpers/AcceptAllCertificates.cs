using UnityEngine.Networking;

namespace Game.Scripts.API.Helpers
{
    public class AcceptAllCertificates : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData) => true;
    }
}