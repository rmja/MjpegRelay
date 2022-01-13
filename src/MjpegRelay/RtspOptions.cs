using RtspClientSharp;

namespace MjpegRelay
{
    public class RtspOptions
    {
        public RtpTransportProtocol Transport { get; set; } = RtpTransportProtocol.TCP;
        public string StreamUrl { get; set; }
    }
}
