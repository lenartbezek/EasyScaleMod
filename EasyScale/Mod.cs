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
        public override string BesiegeVersion { get; } = "v0.3";
        public override Version Version { get; } = Assembly.GetExecutingAssembly().GetName().Version;

        private static bool scalingEnabled;

        internal static CopiedData copiedData = null;
        internal class CopiedData
        {
            internal float? xScale;
            internal float? yScale;
            internal float? zScale;
            internal float? thickness;
            internal bool? cylinderFix;
        }

        private static FieldInfo mapperTypesField = typeof(BlockBehaviour).GetField("mapperTypes", BindingFlags.Instance | BindingFlags.NonPublic);

        public override void OnLoad()
        {
            GameObject.DontDestroyOnLoad(EasyScaleController.Instance);

            scalingEnabled = Configuration.GetBool("enabled", true);
            SettingsMenu.RegisterSettingsButton("SCALE", ToggleEnabled, scalingEnabled, 12);

            Game.OnBlockPlaced += AddSliders;
            Game.OnKeymapperOpen += () =>
            {
                BlockMapper.CurrentInstance.CopyButton.Down += Copy;
                BlockMapper.CurrentInstance.PasteButton.Down += Paste;
                BlockMapper.CurrentInstance.ResetButton.Down += Reset;

                if (!HasSliders(BlockMapper.CurrentInstance.Block))
                    AddSliders(BlockMapper.CurrentInstance.Block);
                AddAllSliders();
            };

            XmlLoader.OnLoad += EasyScaleController.Instance.OnMachineLoad;
            XmlSaver.OnSave += EasyScaleController.Instance.OnMachineSave;
        }

        public override void OnUnload()
        {
            Configuration.SetBool("enabled", scalingEnabled);
        }

        public static bool HasSliders(BlockBehaviour block)
        {
            return block.MapperTypes.Exists(match => match.Key == "scale");
        }

        public static void AddAllSliders()
        {
            foreach (var block in Machine.Active().BuildingBlocks.FindAll(block => !HasSliders(block)))
                AddSliders(block);
        }

        private static void AddSliders(Transform block)
        {
            var blockbehaviour = block.GetComponent<BlockBehaviour>();
            if (!HasSliders(blockbehaviour))
                AddSliders(blockbehaviour);
        }

        public static void AddSliders(BlockBehaviour block)
        {
            // Get current mapper types
            var currentMapperTypes = block.MapperTypes;

            // Create new mapper types
            var scalingToggle = new MToggle("Scaling", "scale", false);
            scalingToggle.DisplayInMapper = scalingEnabled;
            currentMapperTypes.Add(scalingToggle);

            if (block.GetBlockID() == (int)BlockType.Brace || 
                block.GetBlockID() == (int)BlockType.RopeWinch ||
                block.GetBlockID() == (int)BlockType.Spring)
            {
                var thicknessSlider = new MSlider("Thickness", "thickness", block.transform.localScale.x, 0.1f, 3f);
                thicknessSlider.DisplayInMapper = false;
                thicknessSlider.ValueChanged += (float value) =>
                {
                    ScaleBlock(block, new Vector3(value, value, value));
                };
                currentMapperTypes.Add(thicknessSlider);

                var cylinderFixToggle = new MToggle("Linkage Fix", "cylinder-fix", EasyScaleController.Instance.LoadedCylinderFix.Contains(block.Guid));
                cylinderFixToggle.DisplayInMapper = false;
                cylinderFixToggle.Toggled += (bool active) => FixCylinder(block);
                currentMapperTypes.Add(cylinderFixToggle);

                scalingToggle.Toggled += (bool active) =>
                {
                    thicknessSlider.DisplayInMapper = active;
                    cylinderFixToggle.DisplayInMapper = active;
                };
            }
            else
            {
                var xScaleSlider = new MSlider("X Scale", "x-scale", block.transform.localScale.x, 0.1f, 3f);
                xScaleSlider.DisplayInMapper = false;
                xScaleSlider.ValueChanged += (float value) =>
                {
                    var scale = block.transform.localScale;
                    ScaleBlock(block, new Vector3(value, scale.y, scale.z));
                };
                currentMapperTypes.Add(xScaleSlider);
                

                var yScaleSlider = new MSlider("Y Scale", "y-scale", block.transform.localScale.y, 0.1f, 3f);
                yScaleSlider.DisplayInMapper = false;
                yScaleSlider.ValueChanged += (float value) =>
                {
                    var scale = block.transform.localScale;
                    ScaleBlock(block, new Vector3(scale.x, value, scale.z));
                };
                currentMapperTypes.Add(yScaleSlider);

                var zScaleSlider = new MSlider("Z Scale", "z-scale", block.transform.localScale.z, 0.1f, 3f);
                zScaleSlider.DisplayInMapper = false;
                zScaleSlider.ValueChanged += (float value) =>
                {
                    var scale = block.transform.localScale;
                    ScaleBlock(block, new Vector3(scale.x, scale.y, value));
                };
                currentMapperTypes.Add(zScaleSlider);

                scalingToggle.Toggled += (bool active) =>
                {
                    xScaleSlider.DisplayInMapper = active;
                    yScaleSlider.DisplayInMapper = active;
                    zScaleSlider.DisplayInMapper = active;
                };
            }

            // Mod enable toggle
            OnToggle += (bool enabled) =>
            {
                scalingToggle.DisplayInMapper = enabled;
                scalingToggle.IsActive = false;
            };

            // Set new mapper types
            mapperTypesField.SetValue(block, currentMapperTypes);

            // Add initial state
            block.InitialState.Write("bmt-x-scale", 1f);
            block.InitialState.Write("bmt-y-scale", 1f);
            block.InitialState.Write("bmt-y-scale", 1f);
            block.InitialState.Write("bmt-thickness", 1f);
            block.InitialState.Write("bmt-cylinder-fix", false);
        }

        internal static void Reset()
        {
#if DEBUG
            Debug.Log("Resetting for " + BlockMapper.CurrentInstance.Block.name);
#endif
            var b = BlockMapper.CurrentInstance.Block;
            if (b.GetBlockID() == (int)BlockType.Brace ||
                b.GetBlockID() == (int)BlockType.RopeWinch ||
                b.GetBlockID() == (int)BlockType.Spring)
            {
                b.Sliders.Find(s => s.Key == "thickness").Value = 1f;
                b.Toggles.Find(s => s.Key == "cylinder-fix").IsActive = false;
            }
            else
            {
                b.Sliders.Find(s => s.Key == "x-scale").Value = 1;
                b.Sliders.Find(s => s.Key == "y-scale").Value = 1;
                b.Sliders.Find(s => s.Key == "z-scale").Value = 1;
            }
        }

        internal static void Copy()
        {
#if DEBUG
            Debug.Log("Copying from " + BlockMapper.CurrentInstance.Block.name);
#endif
            var b = BlockMapper.CurrentInstance.Block;
            copiedData = new CopiedData
            {
                xScale = b.Sliders.Find(s => s.Key == "x-scale")?.Value,
                yScale = b.Sliders.Find(s => s.Key == "y-scale")?.Value,
                zScale = b.Sliders.Find(s => s.Key == "z-scale")?.Value,
                thickness = b.Sliders.Find(s => s.Key == "thickness")?.Value,
                cylinderFix = b.Toggles.Find(s => s.Key == "cylinder-fix")?.IsActive
            };
        }

        internal static void Paste()
        {
#if DEBUG
            Debug.Log("Pasting to " + BlockMapper.CurrentInstance.Block.name);
#endif
            if (copiedData == null)
                return;
            var b = BlockMapper.CurrentInstance.Block;
            b.Toggles.Find(s => s.Key == "scale").IsActive = true;
            if (copiedData.thickness.HasValue && b.Sliders.Exists(s => s.Key == "thickness"))
                b.Sliders.Find(s => s.Key == "thickness").Value = copiedData.thickness.Value;
            if (copiedData.cylinderFix.HasValue && b.Toggles.Exists(s => s.Key == "cylinder-fix"))
                b.Toggles.Find(s => s.Key == "cylinder-fix").IsActive = copiedData.cylinderFix.Value;
            if (copiedData.xScale.HasValue && b.Sliders.Exists(s => s.Key == "x-scale"))
                b.Sliders.Find(s => s.Key == "x-scale").Value = copiedData.xScale.Value;
            if (copiedData.yScale.HasValue && b.Sliders.Exists(s => s.Key == "y-scale"))
                b.Sliders.Find(s => s.Key == "y-scale").Value = copiedData.yScale.Value;
            if (copiedData.zScale.HasValue && b.Sliders.Exists(s => s.Key == "z-scale"))
                b.Sliders.Find(s => s.Key == "z-scale").Value = copiedData.zScale.Value;
        }

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
                braceCode.CreateCylinderBetweenPoints(braceCode.startPoint.position, braceCode.endPoint.position, braceCode.radius);

                float length_scale = 1;
                if (block.Toggles.Find(toggle => toggle.Key == "cylinder-fix").IsActive)
                    length_scale = (braceCode.endPoint.position - braceCode.startPoint.position).magnitude / block.transform.localScale.y;
                else
                    length_scale = (braceCode.endPoint.position - braceCode.startPoint.position).magnitude;
                braceCode.cylinder.localScale = new Vector3(braceCode.radius, length_scale, braceCode.radius);
                return;
            }

            block.transform.localScale = scale;
        }

        public static void FixAllCylinders()
        {
            foreach (var block in Machine.Active().BuildingBlocks.FindAll(block => block.GetBlockID() == (int)BlockType.Brace))
                FixCylinder(block);
        }

        public static void FixCylinder(BlockBehaviour block)
        {
            if (block.GetBlockID() == (int)BlockType.Brace)
            {
                var braceCode = block.GetComponent<BraceCode>();
                float length_scale = 1;
                if (block.Toggles.Find(toggle => toggle.Key == "cylinder-fix").IsActive)
                    length_scale = (braceCode.endPoint.position - braceCode.startPoint.position).magnitude / block.transform.localScale.y;
                else
                    length_scale = (braceCode.endPoint.position - braceCode.startPoint.position).magnitude;
                braceCode.cylinder.localScale = new Vector3(braceCode.radius, length_scale, braceCode.radius);
            }
        }

        private delegate void EnableToggleHandler(bool visible);
        private static event EnableToggleHandler OnToggle;

        public static void ToggleEnabled(bool enabled)
        {
            scalingEnabled = enabled;
            OnToggle?.Invoke(enabled);
        }
    }
}
