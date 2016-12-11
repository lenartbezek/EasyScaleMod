using spaar.ModLoader;
using System.Reflection;
using UnityEngine;
// ReSharper disable UnusedMember.Local
// ReSharper disable PossibleNullReferenceException

namespace Lench.EasyScale
{
    public class EasyScaleController : SingleInstance<EasyScaleController>
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
            EasyScale.OnToggle += (active) =>
            {
                if (!active) DestroyImmediate(_prescalePanel);
            };
        }

        private void Update()
        {
            if (!EasyScale.ModEnabled) return;

            // Handle key presses.
            if (BlockMapper.CurrentInstance != null)
            {
                if (_movingAllSliders && !Keybindings.Get(EasyScale.MoveAllSliderBinding).IsDown())
                    BlockMapper.CurrentInstance.Refresh();

                _movingAllSliders = Keybindings.Get(EasyScale.MoveAllSliderBinding).IsDown();

                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand))
                {
                    if (Input.GetKey(KeyCode.C))
                        EasyScale.Copy();
                    if (Input.GetKey(KeyCode.V))
                        EasyScale.Paste();
                }
            }

            // Check for AddPiece and create or destroy PrescalePanel
            if (Game.AddPiece)
            {
                if (_prescalePanel == null)
                {
                    _prescalePanel = gameObject.AddComponent<PrescalePanel>();
                    _prescalePanel.OnScaleChanged += SetPrescale;
                    _prescalePanel.OnToggle += EnablePrescale;
                }
            }
            else
            {
                if (_prescalePanel != null)
                {
                    DestroyImmediate(_prescalePanel);
                }
            }

            // Update PrescalePanel slider values
            if (!Game.AddPiece) return;
            if (_currentBlockType == Game.AddPiece.BlockType && _currentGhost != null) return;
            _currentBlockType = Game.AddPiece.BlockType;
            _currentGhost = GhostFieldInfo.GetValue(Game.AddPiece) as Transform;

            if (_currentGhost == null || !EasyScale.PrescaleEnabled || !EasyScale.ModEnabled) return;
            if (EasyScale.PrescaleDictionary.ContainsKey(_currentBlockType))
            {
                _prescalePanel.Scale = EasyScale.PrescaleDictionary[_currentBlockType];
                _prescalePanel.RefreshSliderStrings();
            }
            else
            {
                _prescalePanel.Scale = PrefabMaster.GetDefaultScale(_currentBlockType);
                _prescalePanel.RefreshSliderStrings();
            }
        }

        private void EnablePrescale(bool enable)
        {
            var scale = enable && EasyScale.PrescaleDictionary.ContainsKey(_currentBlockType)
                ? EasyScale.PrescaleDictionary[_currentBlockType]
                : PrefabMaster.GetDefaultScale(_currentBlockType);
            ScaleGhost(scale);
        }

        private void SetPrescale(Vector3 scale)
        {
            if (EasyScale.PrescaleEnabled) ScaleGhost(scale);
            EasyScale.PrescaleDictionary[_currentBlockType] = scale;
        }

        private void ScaleGhost(Vector3 scale)
        {
            if (_currentBlockType == (int) BlockType.Wheel)
                scale.Scale(new Vector3(0.55f, 0.55f, 0.55f));
            if (_currentGhost)
                _currentGhost.localScale = scale;
        }
    }
}