using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PluginManager.Plugin;
using UnityEngine;
using UnityEngine.SceneManagement;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Lench.EasyScale
{
    [OnGameInit]
    public class Mod : MonoBehaviour
    {
        internal const string SliderSnapBinding = "Slider snap";
        internal const string MoveAllSliderBinding = "Move all sliders";

        private static readonly FieldInfo MapperTypesField =
            typeof(SaveableDataHolder).GetField("mapperTypes", BindingFlags.Instance | BindingFlags.NonPublic);

        private CopiedData _copiedData;
        private bool _keyMapperOpen;

        // ReSharper disable once CollectionNeverQueried.Local
        private List<Guid> _loadedBlocks = new List<Guid>();

        private bool _movingAllSliders;

        public Dictionary<int, Vector3> PrescaleDictionary = new Dictionary<int, Vector3>();
        public string Name { get; } = "Easy Scale";
        public string DisplayName { get; } = "Easy Scale";
        public string Author { get; } = "Lench";
        public Version Version { get; } = Assembly.GetExecutingAssembly().GetName().Version;

        // ReSharper disable once UnusedMember.Local
        private void Start()
        {
            //Game.OnBlockPlaced += AddSliders;

            SceneManager.sceneLoaded += ActivateHiddenBlock;

            XmlLoader.OnLoad += OnMachineLoad;

#if DEBUG
            gameObject.AddComponent<Console>();
#endif
        }

        // ReSharper disable once UnusedMember.Local
        private void Update()
        {
            if (BlockMapper.CurrentInstance != null)
            {
                //AddPiece.Instance.

                // Check for open keymapper
                if (!_keyMapperOpen)
                {
                    OnKeymapperOpen();
                    _keyMapperOpen = true;
                }

                // Handle key presses.
                if (_movingAllSliders && !Input.GetKey(KeyCode.Z))
                    BlockMapper.CurrentInstance.Refresh();

                _movingAllSliders = Input.GetKey(KeyCode.Z);

                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand))
                {
                    if (Input.GetKey(KeyCode.C)) Copy();
                    if (Input.GetKey(KeyCode.V)) Paste();
                }
            }
            else
            {
                _keyMapperOpen = false;
            }
        }

        private void ActivateHiddenBlock(Scene s, LoadSceneMode mode)
        {
            var b = GameObject.Find("HUD/BottomBar/AlignBottomLeft/BLOCK BUTTONS/t_BLOCKS/Scaling Block");
            b?.SetActive(true);
        }

        private void OnMachineLoad(MachineInfo info)
        {
            _loadedBlocks = new List<Guid>();
            foreach (var blockinfo in info.Blocks)
                _loadedBlocks.Add(blockinfo.Guid);
        }

        /// <summary>
        ///     Binds copy, paste and reset to key mapper buttons.
        ///     Checks for sliders and refreshes the mapper.
        /// </summary>
        private void OnKeymapperOpen()
        {
            BlockMapper.CurrentInstance.CopyButton.Click += Copy;
            BlockMapper.CurrentInstance.PasteButton.Click += Paste;
            BlockMapper.CurrentInstance.ResetButton.Click += Reset;

            if (!HasSliders(BlockMapper.CurrentInstance.Block))
            {
                AddSliders(BlockMapper.CurrentInstance.Block);
                BlockMapper.CurrentInstance.Refresh();
            }
            AddAllSliders();
        }

        /// <param name="block"></param>
        /// <returns>True if block has added sliders.</returns>
        public bool HasSliders(BlockBehaviour block)
        {
            return block.MapperTypes.Exists(match => match.Key == "scale");
        }

        /// <summary>
        ///     Adds sliders to all blocks.
        ///     Called on keymapper open.
        /// </summary>
        public void AddAllSliders()
        {
            foreach (var block in Machine.Active().BuildingBlocks)
                if (!HasSliders(block))
                    AddSliders(block);
        }

        /// <summary>
        ///     Adds sliders to the BlockBehaviour object.
        /// </summary>
        /// <param name="block">block's script</param>
        public void AddSliders(BlockBehaviour block)
        {
            // Get current mapper types
            var currentMapperTypes = block.MapperTypes;

            // Create new mapper types
            var scalingToggle = new MToggle("Scaling", "scale", false);
            currentMapperTypes.Add(scalingToggle);

            // Slider definitions
            var xScaleSlider = new MSlider("X Scale", "x-scale", block.transform.localScale.x, 0.1f, 3f, true);
            var yScaleSlider = new MSlider("Y Scale", "y-scale", block.transform.localScale.y, 0.1f, 3f, true);
            var zScaleSlider = new MSlider("Z Scale", "z-scale", block.transform.localScale.z, 0.1f, 3f, true);

            // Slider properties
            xScaleSlider.DisplayInMapper = false;
            xScaleSlider.ValueChanged += value =>
            {
                if (Input.GetKey(KeyCode.LeftShift) && value != SnapSliderValue(value))
                {
                    value = SnapSliderValue(value);
                    xScaleSlider.Value = value;
                }
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    yScaleSlider.Value = value;
                    zScaleSlider.Value = value;
                }
                var scale = block.transform.localScale;
                ScaleBlock(block, new Vector3(value, scale.y, scale.z));
            };
            currentMapperTypes.Add(xScaleSlider);

            yScaleSlider.DisplayInMapper = false;
            yScaleSlider.ValueChanged += value =>
            {
                if (Input.GetKey(KeyCode.LeftShift) && value != SnapSliderValue(value))
                {
                    value = SnapSliderValue(value);
                    yScaleSlider.Value = value;
                }
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    xScaleSlider.Value = value;
                    zScaleSlider.Value = value;
                }
                var scale = block.transform.localScale;
                ScaleBlock(block, new Vector3(scale.x, value, scale.z));
            };
            currentMapperTypes.Add(yScaleSlider);

            zScaleSlider.DisplayInMapper = false;
            zScaleSlider.ValueChanged += value =>
            {
                if (Input.GetKey(KeyCode.LeftShift) && value != SnapSliderValue(value))
                {
                    value = SnapSliderValue(value);
                    zScaleSlider.Value = value;
                }
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    xScaleSlider.Value = value;
                    yScaleSlider.Value = value;
                }
                var scale = block.transform.localScale;
                ScaleBlock(block, new Vector3(scale.x, scale.y, value));
            };
            currentMapperTypes.Add(zScaleSlider);

            scalingToggle.Toggled += active =>
            {
                xScaleSlider.DisplayInMapper = active;
                yScaleSlider.DisplayInMapper = active;
                zScaleSlider.DisplayInMapper = active;
            };

            // Set new mapper types
            MapperTypesField.SetValue(block, currentMapperTypes);
        }

        /// <summary>
        ///     Returns the value rounded to tne nearest snapping value.
        /// </summary>
        /// <param name="value">Raw slider value.</param>
        /// <returns>Snapped slider value.</returns>
        private static float SnapSliderValue(float value)
        {
            if (value % 1f > 0.225f && value % 1f < 0.275f)
                return Mathf.Floor(value) + 0.25f;
            if (value % 1f > 0.725f && value % 1f < 0.775f)
                return Mathf.Floor(value) + 0.75f;
            return Mathf.Round(value * 10f) / 10f;
        }

        /// <summary>
        ///     Called on Reset button click.
        /// </summary>
        internal static void Reset()
        {
            var b = BlockMapper.CurrentInstance.Block;
            b.Toggles.First(s => s.Key == "scale").IsActive = false;
            b.Sliders.First(s => s.Key == "x-scale").Value = 1;
            b.Sliders.First(s => s.Key == "y-scale").Value = 1;
            b.Sliders.First(s => s.Key == "z-scale").Value = 1;
        }

        /// <summary>
        ///     Called on Ctrl+C and Copy button click.
        /// </summary>
        internal void Copy()
        {
            var b = BlockMapper.CurrentInstance.Block;
            _copiedData = new CopiedData
            {
                Enabled = b.Toggles.First(s => s.Key == "scale").IsActive,
                Scale = b.transform.localScale,
                CylinderFix = b.Toggles.First(s => s.Key == "length-fix")?.IsActive
            };
        }

        /// <summary>
        ///     Called on Ctrl+V and Paste button click.
        /// </summary>
        internal void Paste()
        {
            if (_copiedData == null)
                return;
            var b = BlockMapper.CurrentInstance.Block;
            b.Toggles.First(s => s.Key == "scale").IsActive = _copiedData.Enabled;
            if (b.Sliders.Any(s => s.Key == "x-scale"))
                b.Sliders.First(s => s.Key == "x-scale").Value = _copiedData.Scale.x;
            if (b.Sliders.Any(s => s.Key == "y-scale"))
                b.Sliders.First(s => s.Key == "y-scale").Value = _copiedData.Scale.y;
            if (b.Sliders.Any(s => s.Key == "z-scale"))
                b.Sliders.First(s => s.Key == "z-scale").Value = _copiedData.Scale.z;
            if (_copiedData.CylinderFix.HasValue && b.Toggles.Any(s => s.Key == "length-fix"))
                b.Toggles.First(s => s.Key == "length-fix").IsActive = _copiedData.CylinderFix.Value;
        }

        /// <summary>
        ///     Scales the block to a given scale.
        /// </summary>
        /// <param name="block">BlockBehaviour object</param>
        /// <param name="scale">Vector3 scale</param>
        public void ScaleBlock(BlockBehaviour block, Vector3 scale)
        {
            if (block.BlockID == (int) BlockType.Brace)
            {
                var braceCode = block.GetComponent<BraceCode>();

                var startPoint = braceCode.startPoint.position;
                var endPoint = braceCode.endPoint.position;

                block.transform.localScale = scale;

                braceCode.startPoint.position = startPoint;
                braceCode.endPoint.position = endPoint;
                return;
            }

            if (block.BlockID == (int) BlockType.Spring || block.BlockID == (int) BlockType.RopeWinch)
            {
                var springCode = block.GetComponent<SpringCode>();

                var startPoint = springCode.startPoint.position;
                var endPoint = springCode.endPoint.position;

                block.transform.localScale = scale;

                springCode.startPoint.position = startPoint;
                springCode.endPoint.position = endPoint;
                return;
            }

            block.transform.localScale = scale;
        }

        private class CopiedData
        {
            public bool? CylinderFix;
            public bool Enabled;
            public Vector3 Scale = Vector3.one;
        }
    }
}