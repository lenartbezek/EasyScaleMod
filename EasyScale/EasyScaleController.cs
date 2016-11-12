using spaar.ModLoader;
using System.Reflection;
using UnityEngine;

namespace Lench.EasyScale
{
    public class EasyScaleController : SingleInstance<EasyScaleController>
    {
        public override string Name { get; } = "Easy Scale";

        private bool _movingAllSliders = false;
        private int _currentBlockType = 1;
        private PrescalePanel _prescalePanel;
        private Transform _currentGhost;

        private static readonly FieldInfo GhostFieldInfo = typeof(AddPiece).GetField("_currentGhost", BindingFlags.Instance | BindingFlags.NonPublic);

        private void Update()
        {
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
            if (Game.AddPiece)
            {
                if (_currentBlockType != Game.AddPiece.BlockType || _currentGhost == null)
                {
                    _currentBlockType = Game.AddPiece.BlockType;
                    _currentGhost = GhostFieldInfo.GetValue(Game.AddPiece) as Transform;

                    if (_currentGhost != null)
                        if (EasyScale.PrescaleDictionary.ContainsKey(_currentBlockType))
                        {
                            _currentGhost.localScale = EasyScale.PrescaleDictionary[_currentBlockType];
                        }
                        else
                        {
                            EasyScale.PrescaleDictionary[_currentBlockType] = PrefabMaster.GetDefaultScale(_currentBlockType);
                        }
                }
            }
        }

        private void SetPrescale(Vector3 scale)
        {
            if (_currentGhost)
            {
                _currentGhost.localScale = scale;
            }
            EasyScale.PrescaleDictionary[_currentBlockType] = scale;
        }
    }
}
