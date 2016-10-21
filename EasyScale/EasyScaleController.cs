using System;
using System.Collections.Generic;
using spaar.ModLoader;
using UnityEngine;

namespace Lench.EasyScale
{
    public class EasyScaleController : SingleInstance<EasyScaleController>
    {
        public override string Name { get; } = "Easy Scale Controller";

        internal List<Guid> LoadedCylinderFix = new List<Guid>();

        private bool MovingAllSliders = false;

        internal void OnMachineSave(MachineInfo info)
        {
            try
            {
                foreach (var blockinfo in info.Blocks.FindAll(b => b.ID == (int)BlockType.Brace))
                {
                    var block = ReferenceMaster.BuildingBlocks.Find(b => b.Guid == blockinfo.Guid);
                    if (block != null)
                    {
                        var toggle = block.Toggles.Find(t => t.Key == "length-fix");
                        if (block.Toggles.Find(t => t.Key == "length-fix").IsActive)
                            blockinfo.BlockData.Write("bmt-length-fix", true);
                    }
                }
            }
            catch (Exception e)
            {
                ModConsole.AddMessage(LogType.Error, "[EasyScale]: Error saving length fix braces.", e.Message + "\n" + e.StackTrace);
            }
        }

        internal void OnMachineLoad(MachineInfo info)
        {
            try
            {
                LoadedCylinderFix = new List<Guid>();
                foreach (var blockinfo in info.Blocks.FindAll(b => b.ID == (int)BlockType.Brace))
                {
                    if (blockinfo.BlockData.HasKey("bmt-length-fix") &&
                        blockinfo.BlockData.ReadBool("bmt-length-fix"))
                        LoadedCylinderFix.Add(blockinfo.Guid);
                }
            }
            catch (Exception e)
            {
                ModConsole.AddMessage(LogType.Error, "[EasyScale]: Error loading length fix braces.", e.Message + "\n" + e.StackTrace);
            }
        }

        private void Update()
        {
            if (BlockMapper.CurrentInstance != null)
            {
                if (MovingAllSliders && !Keybindings.Get(EasyScale.MoveAllSliderBinding).IsDown())
                    BlockMapper.CurrentInstance.Refresh();

                MovingAllSliders = Keybindings.Get(EasyScale.MoveAllSliderBinding).IsDown();

                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand))
                {
                    if (Input.GetKey(KeyCode.C))
                        EasyScale.Copy(); 
                    if (Input.GetKey(KeyCode.V))
                        EasyScale.Paste();
                } 
            }
        }
    }
}
