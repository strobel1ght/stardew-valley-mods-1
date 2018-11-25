using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;

namespace LivelyPets
{
  public class ModEntry : Mod
  {
    private Pet vanillaPet;
    private LivelyPet livelyPet;
    private ModData petData;
    private PetCommands petCommands;

    public override void Entry(IModHelper helper)
    {
      petCommands = Helper.Data.ReadJsonFile<PetCommands>("commands.json") ?? new PetCommands();
      TimeEvents.AfterDayStarted += TimeEvents_AfterDayStarted;
      SaveEvents.AfterLoad += SaveEvents_AfterLoad;
      SaveEvents.BeforeSave += SaveEvents_BeforeSave;
      GameEvents.OneSecondTick += GameEvents_OneSecondTick;
      GameEvents.UpdateTick += GameEvents_UpdateTick;
      PlayerEvents.Warped += PlayerEvents_Warped;
    }

    private void SaveEvents_AfterLoad(object sender, EventArgs e)
    {
      petData = Helper.Data.ReadJsonFile<ModData>($"data/{Constants.SaveFolderName}.json") ?? new ModData();
    }

    private void GameEvents_UpdateTick(object sender, EventArgs e)
    {
    }

    private void CheckChatForCommands()
    {
      var messages = this.Helper.Reflection.GetField<List<ChatMessage>>(Game1.chatBox, "messages", true).GetValue();
      var lastMsg = ChatMessage.makeMessagePlaintext(messages.LastOrDefault()?.message);
      var farmerName = lastMsg.Substring(0, lastMsg.IndexOf(':'));
      lastMsg = lastMsg.Replace($"{farmerName}: ", ""); // Remove sender name from text
      Monitor.Log(farmerName);
      Monitor.Log(lastMsg);
    }

    private void PlayerEvents_Warped(object sender, EventArgsPlayerWarped e)
    {
      livelyPet?.warpToFarmer();
    }

    private void GameEvents_OneSecondTick(object sender, EventArgs e)
    {
      if (!Context.IsWorldReady || livelyPet == null) return;
      if (!livelyPet.isNearFarmer)
        livelyPet.UpdatePathToFarmer();

      CheckChatForCommands();
    }

    private void SaveEvents_BeforeSave(object sender, EventArgs e)
    {
      // Preserve defaults in save so game doesn't break without mod
      var characters = Helper.Reflection.GetField<NetCollection<NPC>>(Game1.getFarm(), "characters").GetValue();
      if (!characters.Contains(vanillaPet)) characters.Add(vanillaPet);
      RemovePet(livelyPet);
    }

    private void TimeEvents_AfterDayStarted(object sender, EventArgs e)
    {
      vanillaPet = GetPet(Game1.player.getPetName());
      if (!Context.IsWorldReady || vanillaPet == null) return;
      RemovePet(vanillaPet);

      if (vanillaPet is Dog dog)
        livelyPet = new LivelyDog(dog);
      else if (vanillaPet is Cat cat)
        livelyPet = new LivelyCat(cat, Monitor);

      Game1.getFarm().characters.Add(livelyPet);
    }

    private void RemovePet(Pet target)
    {
      foreach (var pet in Game1.getFarm().characters.ToList())
      {
        if (pet.GetType().IsInstanceOfType(target) && pet.Name == target.Name)
        {
          Game1.getFarm().characters.Remove(pet);
          break;
        }
      }
    }

    private Pet GetPet(string petName)
    {
      foreach (var npc in Game1.getFarm().characters.ToList())
      {
        if (npc is Pet pet && pet.Name == petName)
          return pet;
      }

      return null;
    }
  }
}