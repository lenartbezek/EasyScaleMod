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

        private static bool enabled;
        private static FieldInfo mapperTypesField = typeof(BlockBehaviour).GetField("mapperTypes", BindingFlags.Instance | BindingFlags.NonPublic);
        public override void OnLoad()
        {
            enabled = Configuration.GetBool("enabled", true);
            SettingsMenu.RegisterSettingsButton("SCALE", ToggleEnabled, enabled, 12);

            Game.OnBlockPlaced += (Transform block) => { AddSliders(); };
            Game.OnBlockPlaced += (Transform block) => { FixCylinders(); };
            Game.OnKeymapperOpen += CheckBlockMapper;
        }

        public override void OnUnload()
        {
            Configuration.SetBool("enabled", enabled);
            ToggleEnabled(false);

            Game.OnBlockPlaced -= (Transform block) => { AddSliders(); };
            Game.OnBlockPlaced -= (Transform block) => { FixCylinders(); };
            Game.OnKeymapperOpen -= CheckBlockMapper;
        }

        public static void CheckBlockMapper()
        {
            if (!HasSliders(BlockMapper.CurrentInstance.Block))
            {
                AddSliders(BlockMapper.CurrentInstance.Block);
                BlockMapper.CurrentInstance.Refresh();
            }
            AddSliders();
        }

        public static bool HasSliders(BlockBehaviour block)
        {
            return block.MapperTypes.Exists(match => match.Key == "scale");
        }

        public static void AddSliders()
        {
            foreach (var block in Machine.Active().BuildingBlocks.FindAll(block => !HasSliders(block)))
                AddSliders(block);
        }

        public static void AddSliders(BlockBehaviour block)
        {
            var currentMapperTypes = block.MapperTypes;

            var scalingToggle = new MToggle("Scaling", "scale", false);
            scalingToggle.DisplayInMapper = enabled;
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

                scalingToggle.Toggled += (bool active) =>
                {
                    thicknessSlider.DisplayInMapper = active;
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

            OnToggle += (bool enabled) =>
            {
                scalingToggle.DisplayInMapper = enabled;
                scalingToggle.IsActive = false;
            };

            mapperTypesField.SetValue(block, currentMapperTypes);
        }

        public static void ScaleBlock(BlockBehaviour block, Vector3 scale)
        {
            if (block.GetBlockID() == (int)BlockType.Brace)
            {
                var code = block.GetComponent<BraceCode>();

                var startPoint = code.startPoint.position;
                var endPoint = code.endPoint.position;

                block.transform.localScale = scale;

                code.SetStartPos(startPoint);
                code.SetEndPos(endPoint);
                code.CreateCylinderBetweenPoints(code.startPoint.position, code.endPoint.position, code.radius);
                code.cylinder.localScale = new Vector3(code.radius, (endPoint - startPoint).magnitude / scale.y, code.radius);
                return;
            }

            if (block.GetBlockID() == (int)BlockType.Spring ||
                block.GetBlockID() == (int)BlockType.RopeWinch)
            {
                var code = block.GetComponent<SpringCode>();

                var startPoint = code.startPoint.position;
                var endPoint = code.endPoint.position;

                block.transform.localScale = scale;

                code.SetStartPos(startPoint);
                code.SetEndPos(endPoint);
                code.CreateCylinderBetweenPoints(code.startPoint.position, code.endPoint.position, code.radius);
                code.cylinder.localScale = new Vector3(code.radius, (endPoint - startPoint).magnitude / scale.y, code.radius);
                return;
            }

            block.transform.localScale = scale;
        }

        public static void FixCylinders()
        {
            foreach (var block in Machine.Active().BuildingBlocks.FindAll(block =>
                block.GetBlockID() == (int)BlockType.Brace ||
                block.GetBlockID() == (int)BlockType.RopeWinch ||
                block.GetBlockID() == (int)BlockType.Spring))
                FixCylinders(block);
        }

        public static void FixCylinders(BlockBehaviour block)
        {
            if (block.GetBlockID() == (int)BlockType.Brace)
            {
                var code = block.GetComponent<BraceCode>();

                var startPoint = code.startPoint.position;
                var endPoint = code.endPoint.position;

                block.transform.localScale = new Vector3(block.transform.localScale.x, block.transform.localScale.x, block.transform.localScale.x);

                code.SetStartPos(startPoint);
                code.SetEndPos(endPoint);
                code.CreateCylinderBetweenPoints(code.startPoint.position, code.endPoint.position, code.radius);
                code.cylinder.localScale = new Vector3(code.radius, (endPoint - startPoint).magnitude / block.transform.localScale.y, code.radius);
                return;
            }

            if (block.GetBlockID() == (int)BlockType.Spring ||
                block.GetBlockID() == (int)BlockType.RopeWinch)
            {
                var code = block.GetComponent<SpringCode>();

                var startPoint = code.startPoint.position;
                var endPoint = code.endPoint.position;

                block.transform.localScale = new Vector3(block.transform.localScale.x, block.transform.localScale.x, block.transform.localScale.x);

                code.SetStartPos(startPoint);
                code.SetEndPos(endPoint);
                code.CreateCylinderBetweenPoints(code.startPoint.position, code.endPoint.position, code.radius);
                code.cylinder.localScale = new Vector3(code.radius, (endPoint - startPoint).magnitude / block.transform.localScale.y, code.radius);
                return;
            }
        }

        public delegate void EnableToggleHandler(bool visible);
        public static event EnableToggleHandler OnToggle;

        public static void ToggleEnabled(bool enabled)
        {
            EasyScale.enabled = enabled;
            OnToggle?.Invoke(enabled);
        }
    }
}
