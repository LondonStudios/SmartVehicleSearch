using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace SmartVehicleSearch
{
    public class Main : BaseScript
    {
        public Dictionary<int, string> carInventories = new Dictionary<int, string>();
        public int local;
        public Main()
        {
            TriggerEvent("chat:addSuggestion", "/setvehinv", "Sets your vehicle inventory", new[]
            {
                new { name="inventory", help="Inventory contents" },
            });
            TriggerEvent("chat:addSuggestion", "/clearvehinv", "Clear your vehicle inventory");
            TriggerEvent("chat:addSuggestion", "/searchveh", "Searches the vehicle in front of you");

            EventHandlers["Client:SyncInventory"] += new Action<int, string, bool>((key, value, delete) =>
            {
                if (delete == true)
                {
                    if (carInventories.ContainsKey(key))
                    {
                        carInventories.Remove(key);
                    }
                }
                else
                {
                    if (!carInventories.ContainsKey(key))
                    {
                        carInventories.Add(key, value);
                    }
                }
            });

            RegisterCommand("setvehinv", new Action<int, List<object>, string>((source, args, raw) =>
            {
                if (IsStringNullOrEmpty(Convert.ToString(args[0])))
                {
                    ChatMessage("Usage /setvehinv [inventory].");
                }
                else
                {
                    var argshandler = args.ConvertAll(x => Convert.ToString(x));
                    var vehicleInventory = string.Join(", ", argshandler);
                    if (VehicleValid())
                    {
                        SetNetworkIdExistsOnAllMachines(VehToNet((GetVehiclePedIsIn(PlayerPedId(), false))), true);
                        TriggerServerEvent("Server:SyncInventory", VehToNet((GetVehiclePedIsIn(PlayerPedId(), false))), vehicleInventory, false);
                        ChatMessage($"Vehicle inventory set to {vehicleInventory}.");
                    }
                    else
                    {
                        ChatMessage("Unable to set vehicle inventory.");
                        ErrorSound();
                    }
                }

            }), false);
        }

        private void ErrorSound()
        {
            PlaySoundFrontend(-1, "Place_Prop_Fail", "DLC_Dmod_Prop_Editor_Sounds", false);
        }

        [Command("clearvehinv")]
        private void ClearVehInventory()
        {
            if (carInventories.ContainsKey(VehToNet((GetVehiclePedIsIn(PlayerPedId(), false)))))
            {
                TriggerServerEvent("Server:SyncInventory", VehToNet((GetVehiclePedIsIn(PlayerPedId(), false))), "", true);
                ChatMessage("Vehicle inventory cleared");
            }
            else
            {
                ChatMessage("Unable to clear vehicle inventory.");
                ErrorSound();
            }
        }

        [Command("searchveh")]
        private void SearchVehicle()
        {
            var target = Raycast();
            if (target != 0)
            {
                if (carInventories.ContainsKey(VehToNet(target)))
                {
                    ProcessSearch(VehToNet(target));
                    ChatMessage("You are starting a vehicle search.");
                }
                else
                {
                    ChatMessage("This vehicle inventory has not been set.");
                    ErrorSound();
                }
            }
            else
            {
                ChatMessage("No vehicle found.");
                ErrorSound();
            }
        }

        private async void ProcessSearch(int target)
        {
            int i = 0;
            await RequestSet("move_ped_crouched");
            SetPedMovementClipset(PlayerPedId(), "move_ped_crouched", 0.5f);
            while (i < 6)
            {
                SetVehicleDoorOpen(local, i, false, false);
                await Delay(200);
                i++;
            }
            await Delay(20000);
            ResetPedMovementClipset(PlayerPedId(), 0.0f);
            string inventory;
            var tryGet = carInventories.TryGetValue(target, out inventory);
            ChatMessage($"You found {inventory} whilst searching this vehicle.");
            TriggerServerEvent("Server:SyncInventory", target, "", true);
            i = 0;
            while (i < 6)
            {
                SetVehicleDoorShut(local, i, false);
                await Delay(200);
                i++;
            }
            await Delay(0);
        }

        private async Task RequestSet(string animSet)
        {
            while (!HasAnimSetLoaded(animSet))
            {
                RequestAnimSet(animSet);
                await Delay(100);
            }
            await Delay(100);
        }

        private int Raycast()
        {
            var location = GetEntityCoords(PlayerPedId(), true);
            var offSet = GetOffsetFromEntityInWorldCoords(PlayerPedId(), 0.0f, 6.0f, 0.0f);
            var shapeTest = StartShapeTestRay(location.X, location.Y, location.Z, offSet.X, offSet.Y, offSet.Z, 2, PlayerPedId(), 0);
            bool hit = false;
            Vector3 endCoords = new Vector3(0f, 0f, 0f);
            Vector3 surfaceNormal = new Vector3(0f, 0f, 0f);
            int entityHit = 0;
            var result = GetShapeTestResult(shapeTest, ref hit, ref endCoords, ref surfaceNormal, ref entityHit);
            local = entityHit;
            return entityHit;
        }

        private bool VehicleValid()
        {
            var vehicle = GetVehiclePedIsIn(PlayerPedId(), false);
            if (vehicle == 0)
            {
                return false;
            }
            else
            {
                if (carInventories.ContainsKey(VehToNet(vehicle)))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        [Command("clear")]
        private void ClearChat()
        {
            TriggerEvent("chat:clear");
        }

        private void ChatMessage(string message)
        {
            TriggerEvent("chat:addMessage", new
            {
                color = new[] { 255, 192, 41 },
                args = new[] { "[VehicleSearch]", $"{message}" }
            });
        }
    }
}
