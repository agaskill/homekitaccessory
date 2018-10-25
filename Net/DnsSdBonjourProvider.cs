using System.Diagnostics;

namespace HomeKitAccessory.Net
{
    public class DnsSdBonjourProvider : IBonjourProvider
    {
        Process process;

        public void Advertise(DiscoveryInfo info)
        {
            Dispose();
            // dns-sd -R TestDevice1 _hap._tcp local 3001 c#=1 id=54:FB:D8:A0:5A:C6 md=Device1,1 s#=1 sf=1 ci=1
            var psi = new ProcessStartInfo(
                "dns-sd",
                $"-R \"{info.Name}\" _hap._tcp local {info.Port} c#={info.ConfigNumber} id={info.DeviceId} md=\"{info.Name}\" s#=1 pv=1.0 ff=0 sf={(int)info.StatusFlags} ci={info.CategoryId}");
            process = Process.Start(psi);

            //System.AppDomain.CurrentDomain.DomainUnload += (sender, e) => process.Kill();
        }

        public void Dispose()
        {
            if (process != null)
            {
                process.Kill();
                process.Dispose();
                process = null;
            }
        }
    }
}