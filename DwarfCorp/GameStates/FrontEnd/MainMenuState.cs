using System;
using System.Collections.Generic;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using System.IO;

namespace DwarfCorp.GameStates
{
    public class MainMenuState : MenuState
    {
        public MainMenuState(DwarfGame game) :
            base(game)
        {
       
        }

        public void MakeMenu(DirectoryInfo GameToContinue)
        {
            var frame = CreateMenu(Library.GetString("main-menu-title"));

            if (GameToContinue != null && NewOverworldFile.CheckCompatibility(GameToContinue.FullName))
            {
                CreateMenuItem(frame,
                    "Continue",
                    NewOverworldFile.GetOverworldName(GameToContinue.FullName),
                    (sender, args) => {

                        var file = NewOverworldFile.Load(GameToContinue.FullName);
                        GameStateManager.PopState();
                        var overworldSettings = file.CreateSettings();
                        overworldSettings.InstanceSettings.LoadType = LoadType.LoadFromFile;
                        overworldSettings.InstanceSettings.ExistingFile = DwarfGame.GetWorldDirectory() + Path.DirectorySeparatorChar + overworldSettings.Name + Path.DirectorySeparatorChar + String.Format("{0}-{1}", (int)overworldSettings.InstanceSettings.Origin.X, (int)overworldSettings.InstanceSettings.Origin.Y);
                        GameStateManager.PushState(new LoadState(Game, overworldSettings, LoadTypes.UseExistingOverworld));
                    });
            }

            CreateMenuItem(frame,
                Library.GetString("new-game"),
                Library.GetString("new-game-tooltip"),
                (sender, args) => GameStateManager.PushState(new WorldGeneratorState(Game, Overworld.Create(), WorldGeneratorState.PanelStates.Generate)));

            CreateMenuItem(frame, 
                Library.GetString("load-game"),
                Library.GetString("load-game-tooltip"),
                (sender, args) => GameStateManager.PushState(new WorldLoaderState(Game)));

            CreateMenuItem(frame, 
                Library.GetString("options"),
                Library.GetString("options-tooltip"),
                (sender, args) => GameStateManager.PushState(new OptionsState(Game)));

            CreateMenuItem(frame,
                Library.GetString("manage-mods"),
                Library.GetString("manage-mods-tooltip"), 
                (sender, args) => GameStateManager.PushState(new ModManagement.ManageModsState(Game)));

            CreateMenuItem(frame, 
                Library.GetString("credits"),
                Library.GetString("credits-tooltip"),
                (sender, args) => GameStateManager.PushState(new CreditsState(GameState.Game)));

#if DEBUG
            CreateMenuItem(frame, "QUICKPLAY", "",
                (sender, args) =>
                {
                    DwarfGame.LogSentryBreadcrumb("Menu", "User generating a random world.");

                    var overworldSettings = Overworld.Create();
                    overworldSettings.InstanceSettings.InitalEmbarkment = new Embarkment(overworldSettings);
                    overworldSettings.InstanceSettings.InitalEmbarkment.Funds = 1000u;
                    overworldSettings.InstanceSettings.InitalEmbarkment.Employees.Add(Applicant.Random("Crafter", overworldSettings.Company));
                    overworldSettings.InstanceSettings.InitalEmbarkment.Employees.Add(Applicant.Random("Manager", overworldSettings.Company));
                    overworldSettings.InstanceSettings.InitalEmbarkment.Employees.Add(Applicant.Random("Miner", overworldSettings.Company));
                    overworldSettings.InstanceSettings.InitalEmbarkment.Employees.Add(Applicant.Random("Wizard", overworldSettings.Company));
                    overworldSettings.InstanceSettings.InitalEmbarkment.Employees.Add(Applicant.Random("Soldier", overworldSettings.Company));
                    overworldSettings.InstanceSettings.InitalEmbarkment.Employees.Add(Applicant.Random("Musketeer", overworldSettings.Company));

                    GameStateManager.PushState(new LoadState(Game, overworldSettings, LoadTypes.GenerateOverworld));
                });

            CreateMenuItem(frame, "GIANT QUICKPLAY", "",
                (sender, args) =>
                {
                    DwarfGame.LogSentryBreadcrumb("Menu", "User generating a random world.");

                    var overworldSettings = Overworld.Create();
                    overworldSettings.InstanceSettings.Cell = new ColonyCell { Bounds = new Rectangle(0, 0, 64, 64), Faction = overworldSettings.ColonyCells.GetCellAt(0,0).Faction };
                    overworldSettings.InstanceSettings.InitalEmbarkment = new Embarkment(overworldSettings);
                    overworldSettings.InstanceSettings.InitalEmbarkment.Funds = 1000u;
                    overworldSettings.InstanceSettings.InitalEmbarkment.Employees.Add(Applicant.Random("Crafter", overworldSettings.Company));
                    overworldSettings.InstanceSettings.InitalEmbarkment.Employees.Add(Applicant.Random("Manager", overworldSettings.Company));
                    overworldSettings.InstanceSettings.InitalEmbarkment.Employees.Add(Applicant.Random("Miner", overworldSettings.Company));
                    overworldSettings.InstanceSettings.InitalEmbarkment.Employees.Add(Applicant.Random("Wizard", overworldSettings.Company));
                    overworldSettings.InstanceSettings.InitalEmbarkment.Employees.Add(Applicant.Random("Soldier", overworldSettings.Company));
                    overworldSettings.InstanceSettings.InitalEmbarkment.Employees.Add(Applicant.Random("Musketeer", overworldSettings.Company));

                    GameStateManager.PushState(new LoadState(Game, overworldSettings, LoadTypes.GenerateOverworld));
                });

            CreateMenuItem(frame, "Dwarf Designer", "Open the dwarf designer.",
                (sender, args) =>
                {
                    GameStateManager.PushState(new Debug.DwarfDesignerState(GameState.Game));
                });

            CreateMenuItem(frame, "Yarn test", "", (sender, args) =>
            {
                GameStateManager.PushState(new YarnState(null, "test.conv", "Start", new Yarn.MemoryVariableStore()));
            });
#endif

            CreateMenuItem(frame, 
                Library.GetString("quit"),
                Library.GetString("quit-tooltip"),
                (sender, args) => Game.Exit());

            FinishMenu();
        }

        public override void OnEnter()
        {
            // Make sure that this memory gets cleaned up!!
            EntityFactory.Cleanup();
            Drawer3D.Cleanup();
            ParticleEmitter.Cleanup();
            PlayState.Input = null;
            InputManager.Cleanup();
            LayeredSprites.LayerLibrary.Cleanup();

            base.OnEnter();

            var worldDirectory = System.IO.Directory.CreateDirectory(DwarfGame.GetWorldDirectory());
            var dirs = worldDirectory.EnumerateDirectories().ToList();
            dirs.Sort((a, b) =>
        {
            var aMeta = a.GetFiles("meta.txt");
            var bMeta = b.GetFiles("meta.txt");
            if (aMeta.Length > 0 && bMeta.Length > 0)
                return bMeta[0].LastWriteTime.CompareTo(aMeta[0].LastWriteTime);

            return b.LastWriteTime.CompareTo(a.LastWriteTime);
        });

            MakeMenu(dirs.FirstOrDefault());
            IsInitialized = true;

            DwarfTime.LastTime.Speed = 1.0f;
            SoundManager.PlayMusic("menu_music");
            SoundManager.StopAmbience();
        }

        public override void Update(DwarfTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void Render(DwarfTime gameTime)
        {
            base.Render(gameTime);
        }
    }

}
