using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using SmartParking.Core.Models;

namespace SmartParking.Core.Hubs
{
    public class ParkingHub : Hub
    {
        public async Task SendParkingUpdate(ParkingSlot slot)
        {
            await Clients.All.SendAsync("ReceiveParkingUpdate", slot);
        }
        
        public async Task SendVehicleUpdate(Vehicle vehicle)
        {
            await Clients.All.SendAsync("ReceiveVehicleUpdate", vehicle);
        }
    }
}
