using System;
using System.Reflection;
using spaar.ModLoader;
using UnityEngine;

namespace Lench.EasyScale
{
    public class EasyScale : Mod
    {
        public override string Name { get; } = "Easy Scale";
        public override string DisplayName { get; } = "Easy Scale";
        public override string Author { get; } = "Lench";
        public override bool CanBeUnloaded { get; } = false;
        public override Version Version { get; } = Assembly.GetExecutingAssembly().GetName().Version;
        
#if DEBUG
        public override string VersionExtra { get; } = "debug";
#endif

        private static bool scalingEnabled;

        private static CopiedData copiedData = null;

        private class CopiedData
        {
            public bool enabled = false;
            public Vector3 scale = Vector3.one;
            public bool? cylinderFix;
        }

        private static FieldInfo mapperTypesField = typeof(BlockBehaviour).GetField("mapperTypes", BindingFlags.Instance | BindingFlags.NonPublic);

        internal const string SliderSnapBinding = "Slider snap";
        internal const string MoveAllSliderBinding = "Move all sliders";

        public override void OnLoad()
        {
            GameObject.DontDestroyOnLoad(EasyScaleController.Instance);

            scalingEnabled = Configuration.GetBool("enabled", true);
            SettingsMenu.RegisterSettingsButton("SCALE", ToggleEnabled, scalingEnabled, 12);

            Keybindings.AddKeybinding(SliderSnapBinding, new Key(KeyCode.LeftShift, KeyCode.None));
            Keybindings.AddKeybinding(MoveAllSliderBinding, new Key(KeyCode.None, KeyCode.Z));

            Game.OnBlockPlaced += AddSliders;
            Game.OnKeymapperOpen += OnKeymapperOpen;

            XmlLoader.OnLoad += EasyScaleController.Instance.OnMachineLoad;
            XmlSaver.OnSave += EasyScaleController.Instance.OnMachineSave;
        }

        public override void OnUnload()
        {
            Configuration.SetBool("enabled", scalingEnabled);
        }

        /// <summary>
        /// Binds copy, paste and reset to key mapper buttons.
        /// Checks for sliders and refreshes the mapper.
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
        public static bool HasSliders(BlockBehaviour block)
        {
            return block.MapperTypes.Exists(match => match.Key == "scale");
        }

        /// <summary>
        /// Adds sliders to all blocks.
        /// Called on keymapper open.
        /// </summary>
        public static void AddAllSliders()
        {
            foreach (var block in Machine.Active().BuildingBlocks)
                if (!HasSliders(block))
                    AddSliders(block);
        }

        /// <summary>
        /// Wrapper for AddSliders block.
        /// Checks for existing sliders before adding.
        /// </summary>
        /// <param name="block">block's Transform</param>
        private static void AddSliders(Transform block)
        {
            var blockbehaviour = block.GetComponent<BlockBehaviour>();
            if (!HasSliders(blockbehaviour))
                AddSliders(blockbehaviour);
        }

        /// <summary>
        /// Adds sliders to the BlockBehaviour object.
        /// </summary>
        /// <param name="block">block's script</param>
        public static void AddSliders(BlockBehaviour block)
        {
#if DEBUG
            Debug.Log("Adding sliders to " + block.name + " " + block.Guid);
#endif

            // Get current mapper types
            var currentMapperTypes = block.MapperTypes;

            // Create new mapper types
            var scalingToggle = new MToggle("Scaling", "scale", false);
            scalingToggle.DisplayInMapper = scalingEnabled;
            currentMapperTypes.Add(scalingToggle);

            // Slider definitions
            var xScaleSlider = new MSlider("X Scale", "x-scale", block.transform.localScale.x, 0.1f, 3f);
            var yScaleSlider = new MSlider("Y Scale", "y-scale", block.transform.localScale.y, 0.1f, 3f);
            var zScaleSlider = new MSlider("Z Scale", "z-scale", block.transform.localScale.z, 0.1f, 3f);

            // Slider properties
            xScaleSlider.DisplayInMapper = false;
            xScaleSlider.ValueChanged += (float value) =>
            {
                if (Keybindings.Get(SliderSnapBinding).IsDown() && value != SnapSliderValue(value))
                {
                    value = SnapSliderValue(value);
                    xScaleSlider.Value = value;
                }
                if (Keybindings.Get(MoveAllSliderBinding).IsDown())
                {
                    yScaleSlider.Value = value;
                    zScaleSlider.Value = value;
                }
                var scale = block.transform.localScale;
                ScaleBlock(block, new Vector3(value, scale.y, scale.z));
#if DEBUG
                Debug.Log(block.name + " X -> " + value + ".");
#endif
            };
            currentMapperTypes.Add(xScaleSlider);

            yScaleSlider.DisplayInMapper = false;
            yScaleSlider.ValueChanged += (float value) =>
            {
                if (Keybindings.Get(SliderSnapBinding).IsDown() && value != SnapSliderValue(value))
                {
                    value = SnapSliderValue(value);
                    yScaleSlider.Value = value;
                }
                if (Keybindings.Get(MoveAllSliderBinding).IsDown())
                {
                    xScaleSlider.Value = value;
                    zScaleSlider.Value = value;
                }
                var scale = block.transform.localScale;
                ScaleBlock(block, new Vector3(scale.x, value, scale.z));

#if DEBUG
                Debug.Log(block.name + " Y -> " + value + ".");
#endif
            };
            currentMapperTypes.Add(yScaleSlider);

            zScaleSlider.DisplayInMapper = false;
            zScaleSlider.ValueChanged += (float value) =>
            {
                if (Keybindings.Get(SliderSnapBinding).IsDown() && value != SnapSliderValue(value))
                {
                    value = SnapSliderValue(value);
                    zScaleSlider.Value = value;
                }
                if (Keybindings.Get(MoveAllSliderBinding).IsDown())
                {
                    xScaleSlider.Value = value;
                    yScaleSlider.Value = value;
                }
                var scale = block.transform.localScale;
                ScaleBlock(block, new Vector3(scale.x, scale.y, value));

#if DEBUG
                Debug.Log(block.name + " Z -> " + value + ".");
#endif
            };
            currentMapperTypes.Add(zScaleSlider);

            // Length fix toggle
            if (block.GetBlockID() == (int)BlockType.Brace)
            {
                var cylinderFixToggle = new MToggle("Length Fix", "length-fix", EasyScaleController.Instance.LoadedCylinderFix.Contains(block.Guid));
                cylinderFixToggle.DisplayInMapper = false;
                cylinderFixToggle.Toggled += (bool active) => FixCylinder(block.GetComponent<BraceCode>());
                currentMapperTypes.Add(cylinderFixToggle);

                scalingToggle.Toggled += (bool active) =>
                {
                    cylinderFixToggle.DisplayInMapper = active;
                };
            }

            scalingToggle.Toggled += (bool active) =>
            {
                xScaleSlider.DisplayInMapper = active;
                yScaleSlider.DisplayInMapper = active;
                zScaleSlider.DisplayInMapper = active;
            };

            // Mod enable toggle
            OnToggle += (bool enabled) =>
            {
                scalingToggle.DisplayInMapper = enabled;
                scalingToggle.IsActive = false;
            };

            // Set new mapper types
            mapperTypesField.SetValue(block, currentMapperTypes);

            // Length fix call
            if (block.GetBlockID() == (int)BlockType.Brace)
            {
                FixCylinder(block.GetComponent<BraceCode>());
            }
        }

        /// <summary>
        /// Returns the value rounded to tne nearest snapping value.
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
        /// Called on Reset button click.
        /// </summary>
        internal static void Reset()
        {
#if DEBUG
            Debug.Log("Resetting for " + BlockMapper.CurrentInstance.Block.name);
#endif
            var b = BlockMapper.CurrentInstance.Block;
            b.Toggles.Find(s => s.Key == "scale").IsActive = false;
            b.Sliders.Find(s => s.Key == "x-scale").Value = 1;
            b.Sliders.Find(s => s.Key == "y-scale").Value = 1;
            b.Sliders.Find(s => s.Key == "z-scale").Value = 1;
        }

        /// <summary>
        /// Called on Ctrl+C and Copy button click.
        /// </summary>
        internal static void Copy()
        {
#if DEBUG
            Debug.Log("Copying from " + BlockMapper.CurrentInstance.Block.name);
#endif
            var b = BlockMapper.CurrentInstance.Block;
            copiedData = new CopiedData
            {
                enabled = b.Toggles.Find(s => s.Key == "scale").IsActive,
                scale = b.transform.localScale,
                cylinderFix = b.Toggles.Find(s => s.Key == "length-fix")?.IsActive
            };
        }

        /// <summary>
        /// Called on Ctrl+V and Paste button click.
        /// </summary>
        internal static void Paste()
        {
#if DEBUG
            Debug.Log("Pasting to " + BlockMapper.CurrentInstance.Block.name);
#endif
            if (copiedData == null)
                return;
            var b = BlockMapper.CurrentInstance.Block;
            b.Toggles.Find(s => s.Key == "scale").IsActive = copiedData.enabled;
            if (b.Sliders.Exists(s => s.Key == "x-scale"))
                b.Sliders.Find(s => s.Key == "x-scale").Value = copiedData.scale.x;
            if (b.Sliders.Exists(s => s.Key == "y-scale"))
                b.Sliders.Find(s => s.Key == "y-scale").Value = copiedData.scale.y;
            if (b.Sliders.Exists(s => s.Key == "z-scale"))
                b.Sliders.Find(s => s.Key == "z-scale").Value = copiedData.scale.z;
            if (copiedData.cylinderFix.HasValue && b.Toggles.Exists(s => s.Key == "length-fix"))
            b.Toggles.Find(s => s.Key == "length-fix").IsActive = copiedData.cylinderFix.Value;
        }

        /// <summary>
        /// Scales the block to a given scale.
        /// </summary>
        /// <param name="block">BlockBehaviour object</param>
        /// <param name="scale">Vector3 scale</param>
        public static void ScaleBlock(BlockBehaviour block, Vector3 scale)
        {
            if (block.GetBlockID() == (int)BlockType.Brace)
            {
                var braceCode = block.GetComponent<BraceCode>();

                var startPoint = braceCode.startPoint.position;
                var endPoint = braceCode.endPoint.position;

                block.transform.localScale = scale;

                braceCode.SetStartPos(startPoint);
                braceCode.SetEndPos(endPoint);
                FixCylinder(braceCode);
                return;
            }

            if (block.GetBlockID() == (int)BlockType.Spring ||
                block.GetBlockID() == (int)BlockType.RopeWinch)
            {
                var springCode = block.GetComponent<SpringCode>();

                var startPoint = springCode.startPoint.position;
                var endPoint = springCode.endPoint.position;

                block.transform.localScale = scale;

                springCode.SetStartPos(startPoint);
                springCode.SetEndPos(endPoint);
                return;
            }

            block.transform.localScale = scale;
        }

        /// <summary>
        /// Calls FixCylinder on all braces.
        /// </summary>
        public static void FixAllCylinders()
        {
            foreach (var block in Machine.Active().BuildingBlocks.FindAll(block => block.GetBlockID() == (int)BlockType.Brace))
                FixCylinder(block.GetComponent<BraceCode>());
        }

        /// <summary>
        /// Fixes brace's cylinder length.
        /// </summary>
        /// <param name="block">BraceCode script</param>
        public static void FixCylinder(BraceCode brace)
        {
#if DEBUG
            if (brace == null)
                Debug.LogError("Brace is null!");
            else if (brace.Toggles.Find(toggle => toggle.Key == "length-fix") == null)
                Debug.LogError("Brace has no added sliders.");
            else if (brace.Toggles.Find(toggle => toggle.Key == "length-fix").IsActive)
                Debug.Log("Length fix for brace " + brace.Guid);
#endif

            brace.CreateCylinderBetweenPoints(brace.startPoint.position, brace.endPoint.position, brace.radius);

            if (brace.Toggles.Find(toggle => toggle.Key == "length-fix").IsActive)
            {
                // Fix cylinder
                var block_scale = brace.transform.localScale;
                var cylinder_length_scale = (brace.endPoint.position - brace.startPoint.position).magnitude;
                brace.cylinder.localScale = new Vector3(brace.radius, cylinder_length_scale / block_scale.y, brace.radius);
            }
            else
            {
                // Reset cylinder
                var cylinder_length_scale = (brace.endPoint.position - brace.startPoint.position).magnitude;
                brace.cylinder.localScale = new Vector3(brace.radius, cylinder_length_scale, brace.radius);
            }
        }

        private delegate void EnableToggleHandler(bool visible);
        private static event EnableToggleHandler OnToggle;

        /// <summary>
        /// Called on setting toggle.
        /// </summary>
        /// <param name="enabled">Is mod enabled</param>
        public static void ToggleEnabled(bool enabled)
        {
            scalingEnabled = enabled;
            OnToggle?.Invoke(enabled);
        }
    }
}
