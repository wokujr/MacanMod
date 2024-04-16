using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace MacanMod
{
    public class Companion : BaseScript
    {
        private Ped companion;
        private Ped player;

        public Companion()
        {
            EventHandlers.Add("onClientResourceStart", new Action<string>(OnClientResourceStart));
            EventHandlers.Add("playerSpawned", new Action(OnPlayerSpawned)); // Add event handler for player spawn
        }

        private void OnClientResourceStart(string resourceName)
        {
            if (API.GetCurrentResourceName() != resourceName) return;
            API.RegisterCommand("comp", new Action(SummonCompanion), false);
        }

        private void OnPlayerSpawned()
        {
            // Reset bodyguard state when player spawns
            companion = null;
        }

        private async void SummonCompanion()
        {
            player = Game.Player.Character;
            API.RequestModel((uint)PedHash.AmandaTownley);
            while (!API.HasModelLoaded((uint)PedHash.AmandaTownley))
            {
                Debug.WriteLine("Waiting model to load");
                await Delay(10);
            }

            // Check if bodyguard is dead before creating a new instance
            if (companion != null && !API.IsPedDeadOrDying(companion.Handle, true))
            {
                Debug.WriteLine("Bodyguard is still alive!");
                return;
            }

            companion = await World.CreatePed(PedHash.AmandaTownley, player.Position + (player.ForwardVector * 2));
            companion.Task.LookAt(player);

            // Set the bodyguard as a group member of the player
            API.SetPedAsGroupMember(companion.Handle, API.GetPedGroupIndex(player.Handle));

            // Set bodyguard combat ability and give weapon
            API.SetPedCombatAbility(companion.Handle, 2); // 2: Professional
            API.GiveWeaponToPed(companion.Handle, (uint)WeaponHash.AssaultRifleMk2, 500, false, true);

            // Play greeting speech
            companion.PlayAmbientSpeech("GENERIC_HI");

            // Start checking distance in a loop
            await CheckDistanceLoop();

            //check if companion dead, if dead summon automatically
            await CheckCompanionStatus();
        }

        private async Task CheckDistanceLoop()
        {
            while (true)
            {
                await Delay(1000); // Check every second

                if (companion == null || API.IsPedDeadOrDying(companion.Handle, true))
                {
                    Debug.WriteLine("Bodyguard is dead or not spawned.");
                    return;
                }

                float distance = World.GetDistance(companion.Position, player.Position);

                if (distance > 20.0f) // Adjust the distance threshold as needed
                {
                    // Teleport the bodyguard to the player
                    companion.Position = player.Position + (player.ForwardVector * 2);
                }
            }
        }

        private async Task CheckCompanionStatus()
        {
            while (true)
            {
                await Delay(1000);
                if (companion == null || API.IsPedDeadOrDying(companion.Handle, true))
                {
                    SummonCompanion();
                    return;
                }
            }
        }
    }
}
