using System.Collections.Generic;
using spaar.ModLoader;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

// ReSharper disable UnusedMember.Local
// ReSharper disable PossibleNullReferenceException

namespace Lench.EasyScale
{
    public class Controller : SingleInstance<Controller>
    {
        public override string Name { get; } = "Easy Scale";

        private bool _movingAllSliders;
        private int _currentBlockType = 1;
        private PrescalePanel _prescalePanel;
        private Transform _currentGhost;

        private static readonly FieldInfo GhostFieldInfo = typeof(AddPiece).GetField("_currentGhost",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private void Start()
        {
            Mod.OnToggle += (active) =>
            {
                if (!active) DestroyImmediate(_prescalePanel);
            };
            Mod.OnToggle += EnablePrescale;
            SceneManager.sceneLoaded += ActivateHiddenBlock;

            CheckForModUpdate();
        }

        private void Update()
        {
            if (!Mod.ModEnabled) return;

            // Handle key presses.
            if (BlockMapper.CurrentInstance != null)
            {
                if (_movingAllSliders && !Keybindings.Get(Mod.MoveAllSliderBinding).IsDown())
                    BlockMapper.CurrentInstance.Refresh();

                _movingAllSliders = Keybindings.Get(Mod.MoveAllSliderBinding).IsDown();

                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand))
                {
                    if (Input.GetKey(KeyCode.C))
                        Mod.Copy();
                    if (Input.GetKey(KeyCode.V))
                        Mod.Paste();
                }
            }

            
            if (Game.AddPiece)
            {
                // Create PrescalePanel
                if (_prescalePanel == null)
                {
                    _prescalePanel = gameObject.AddComponent<PrescalePanel>();
                    _prescalePanel.OnScaleChanged += SetPrescale;
                    _prescalePanel.OnToggle += EnablePrescale;
                }

                // Update PrescalePanel slider values when switching block
                if (_currentBlockType != Game.AddPiece.BlockType || _currentGhost == null)
                {
                    _currentBlockType = Game.AddPiece.BlockType;
                    _currentGhost = GhostFieldInfo.GetValue(Game.AddPiece) as Transform;

                    if (_currentGhost != null && Mod.PrescaleEnabled && Mod.ModEnabled)
                        if (Mod.PrescaleDictionary.ContainsKey(_currentBlockType))
                        {
                            _prescalePanel.Scale = Mod.PrescaleDictionary[_currentBlockType];
                            _prescalePanel.RefreshSliderStrings();
                        }
                        else
                        {
                            _prescalePanel.Scale = PrefabMaster.GetDefaultScale(_currentBlockType);
                            _prescalePanel.RefreshSliderStrings();
                        }
                }
            }
            else
            {
                // Destroy PrescalePanel
                if (_prescalePanel != null)
                {
                    DestroyImmediate(_prescalePanel);
                }
            }
        }

        private static void ActivateHiddenBlock(Scene s, LoadSceneMode mode)
        {
            var b = GameObject.Find("HUD/BottomBar/AlignBottomLeft/BLOCK BUTTONS/t_BLOCKS/Scaling Block");
            b?.SetActive(true);
        }

        public void EnablePrescale(bool enable)
        {
            var scale = enable && Mod.PrescaleDictionary.ContainsKey(_currentBlockType)
                ? Mod.PrescaleDictionary[_currentBlockType]
                : PrefabMaster.GetDefaultScale(_currentBlockType);
            ScaleGhost(scale);
        }

        public void SetPrescale(Vector3 scale)
        {
            if (Mod.PrescaleEnabled) ScaleGhost(scale);
            Mod.PrescaleDictionary[_currentBlockType] = scale;
        }

        public void ScaleGhost(Vector3 scale)
        {
            if (_currentBlockType == (int) BlockType.Wheel)
                scale.Scale(new Vector3(0.55f, 0.55f, 0.55f));
            if (_currentGhost)
                _currentGhost.localScale = scale;
        }

        public void CheckForModUpdate(bool verbose = false)
        {
            var updater = gameObject.AddComponent<Updater>();
            updater.Check(
                "Easy Scale Mod",
                "https://api.github.com/repos/lench4991/EasyScaleMod/releases/latest",
                Assembly.GetExecutingAssembly().GetName().Version,
                new List<Updater.Link>
                    {
                            new Updater.Link { DisplayName = "Spiderling forum page", URL = "http://forum.spiderlinggames.co.uk/index.php?threads/3314/" },
                            new Updater.Link { DisplayName = "GitHub release page", URL = "https://github.com/lench4991/EasyScaleMod/releases/latest" }
                    },
                verbose);
        }
    }
}